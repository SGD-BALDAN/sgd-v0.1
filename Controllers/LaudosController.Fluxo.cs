using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;
using SgdBaldan.Services;

namespace SgdBaldan.Controllers;

// Passagem do laudo entre as pessoas: projetista → fila dos operadores → operador → gestão (Programador).
// As telas chamam estas ações pelos botões da barra inferior do formulário.
public partial class LaudosController
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // ------------------------------------------------------------------ envio

    /// <summary>
    /// PROJETO, 1º envio: o projetista entrega a estrutura e o laudo entra na fila dos operadores.
    /// LEGADO e envio do operador: tudo medido/conferido, vai para a aprovação do Programador.
    /// </summary>
    private async Task<Resultado<string>> EnviarAsync(Laudo laudo)
    {
        if (!LaudoFluxo.PodeEditarEstrutura(laudo, _usuario.Perfil, _usuario.IdUsuario)
            && !LaudoFluxo.PodePreencher(laudo, _usuario.Perfil, _usuario.IdUsuario))
            return Resultado<string>.Falha("Você não pode enviar este laudo.");

        var erro = LaudoFluxo.ValidarEnvio(laudo);
        if (erro != null) return Resultado<string>.Falha(erro);

        var envioDoProjetista = LaudoFluxo.EnvioDoProjetista(laudo);
        laudo.Status = envioDoProjetista ? StatusLaudo.AguardandoOperador : StatusLaudo.AguardandoAprovacao;
        laudo.DataEnvio = DateTime.Now;
        laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, envioDoProjetista ? "Laudo enviado aos operadores." : "Laudo enviado para aprovação.");

        return Resultado<string>.Ok(envioDoProjetista
            ? "Laudo enviado aos operadores: ele aparece na fila para um operador assumir e preencher as medições."
            : "Laudo enviado para aprovação.");
    }

    [HttpPost("{id:int}/enviar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(int id)
    {
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (!LaudoFluxo.PodeVer(laudo, _usuario.Perfil, _usuario.IdUsuario)) return Forbid();
        var envio = await EnviarAsync(laudo);
        if (!envio.Sucesso) { TempData["Erro"] = envio.Erro; return Redirect("~/laudos/" + id); }
        TempData["Sucesso"] = envio.Valor;
        return Redirect("~/laudos/" + laudo.Tipo.Slug());
    }

    // ------------------------------------------------------------------ fila do operador

    // PROJETO enviado pelo projetista e ainda sem conferente. O primeiro operador que assume passa a ser o
    // conferente e o único que preenche as medições.
    [HttpPost("{id:int}/assumir"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Assumir(int id)
    {
        if (_usuario.Perfil != PerfilUsuario.Operacional) return Forbid();
        var laudo = await _db.Laudos.FirstOrDefaultAsync(x => x.IdLaudo == id);
        if (laudo == null) return NotFound();
        if (laudo.IdConferente == _usuario.IdUsuario && laudo.Status == StatusLaudo.AguardandoOperador)
            return Redirect("~/laudos/" + id);   // já é seu

        var voltar = "~/laudos/" + laudo.Tipo.Slug();
        if (!LaudoFluxo.NaFilaDoOperador(laudo))
        {
            TempData["Erro"] = "Este laudo não está mais disponível para ser assumido.";
            return Redirect(voltar);
        }

        // Atualização condicional: se dois operadores clicarem ao mesmo tempo, só um consegue alterar a linha.
        var idUsuario = _usuario.IdUsuario;
        var agora = DateTime.Now;
        var alteradas = await _db.Laudos
            .Where(x => x.IdLaudo == id && x.IdConferente == null && x.Status == StatusLaudo.AguardandoOperador)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdConferente, (int?)idUsuario).SetProperty(x => x.DataAtualizacao, agora));
        if (alteradas == 0)
        {
            TempData["Erro"] = "Outro operador assumiu este laudo primeiro.";
            return Redirect(voltar);
        }
        await RegistrarHistorico(laudo, "Laudo assumido pelo operador.");
        return Redirect("~/laudos/" + id);
    }

    // ------------------------------------------------------------------ decisão da gestão (Programador)

    [HttpPost("{id:int}/aprovar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprovar(int id)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.AguardandoAprovacao) return BadRequest("Laudo não está aguardando aprovação.");
        laudo.Status = StatusLaudo.Aprovado;
        laudo.DataAtualizacao = DateTime.Now;
        if (laudo.Dispositivo.Status == StatusDispositivo.Pendente) laudo.Dispositivo.Ativar();
        await RegistrarHistorico(laudo, "Laudo aprovado.");
        TempData["Sucesso"] = "Laudo aprovado.";
        return Redirect("~/aprovacoes");
    }

    [HttpPost("{id:int}/reprovar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprovar(int id, string? motivo)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.AguardandoAprovacao) return BadRequest("Laudo não está aguardando aprovação.");
        if (string.IsNullOrWhiteSpace(motivo)) { TempData["Erro"] = "Informe o motivo da reprovação."; return Redirect("~/laudos/" + id); }
        laudo.Status = StatusLaudo.Reprovado;
        laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, motivo.Trim());
        TempData["Sucesso"] = "Laudo reprovado e devolvido para refazer.";
        return Redirect("~/aprovacoes");
    }

    // Custos: registrados na aprovação e atualizáveis depois dela.
    [HttpPost("{id:int}/custos"), ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarCustos(int id, decimal? custoMaterial, decimal? custoMaoObra)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (laudo.Status is not (StatusLaudo.AguardandoAprovacao or StatusLaudo.Aprovado))
        {
            TempData["Erro"] = "Os custos só podem ser registrados durante a aprovação ou após a aprovação.";
            return Redirect("~/laudos/" + id);
        }
        const decimal maximo = 99999999.99m;
        if (!ModelState.IsValid || custoMaterial < 0 || custoMaoObra < 0 || custoMaterial > maximo || custoMaoObra > maximo)
        {
            TempData["Erro"] = "Os custos devem ser valores entre R$ 0,00 e R$ 99.999.999,99 (use vírgula nos centavos).";
            return Redirect("~/laudos/" + id);
        }
        laudo.CustoMaterial = custoMaterial;
        laudo.CustoMaoObra = custoMaoObra;
        laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, $"Custos atualizados — material: {custoMaterial?.ToString("F2", PtBr) ?? "não informado"}; mão de obra: {custoMaoObra?.ToString("F2", PtBr) ?? "não informado"}.");
        TempData["Sucesso"] = "Custos registrados.";
        return Redirect("~/laudos/" + id);
    }

    // ------------------------------------------------------------------ reprovado, exclusão e reaproveitamento

    // Reprovado volta para EmAndamento (o motivo continua no histórico) e quem criou o laudo corrige e reenvia.
    [HttpPost("{id:int}/reabrir")]
    [HttpPost("{id:int}/reenviar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reabrir(int id)
    {
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.Reprovado || !LaudoFluxo.PodeEditarEstrutura(laudo, _usuario.Perfil, _usuario.IdUsuario)) return Forbid();
        laudo.Status = StatusLaudo.EmAndamento;
        laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, "Laudo reaberto para correção e reenvio.");
        TempData["Sucesso"] = "Laudo reaberto para correção.";
        return Redirect("~/laudos/" + id);
    }

    // Exclusão só enquanto EmAndamento e só por quem edita a estrutura (criador).
    [HttpPost("{id:int}/excluir"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var laudo = await _db.Laudos.FindAsync(id);
        if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.EmAndamento || !LaudoFluxo.PodeEditarEstrutura(laudo, _usuario.Perfil, _usuario.IdUsuario)) return Forbid();
        var tipo = laudo.Tipo;
        var idDispositivo = laudo.IdDispositivo;
        _db.Laudos.Remove(laudo);
        await _db.SaveChangesAsync();
        await _dispositivos.RemoverSePendenteSemLaudosAsync(idDispositivo);   // não deixa dispositivo pendente sem laudo
        TempData["Sucesso"] = "Laudo excluído.";
        return Redirect("~/laudos/" + tipo.Slug());
    }

    // Reutiliza só dados técnicos (sem medições/resultados) e cria um novo laudo independente.
    [HttpPost("{id:int}/reaproveitar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reaproveitar(int id)
    {
        var origem = await CarregarLaudo(id);
        if (origem == null) return NotFound();
        var modo = LaudoFluxo.ModoDoPerfil(_usuario.Perfil);
        if (modo == null || modo != origem.Modo) return Forbid();   // cada perfil só reaproveita o que pode criar
        if (!LaudoFluxo.PodeVer(origem, _usuario.Perfil, _usuario.IdUsuario)) return Forbid();

        if (origem.Tipo == TipoLaudo.Liberacao)
        {
            TempData["Erro"] = "O dispositivo já possui laudo de liberação; não é possível criar outro.";
            return Redirect("~/laudos/" + id);
        }
        if (await ContarFilaAsync(origem.Tipo, _usuario.IdUsuario) >= _opcoes.LimiteFila)
        {
            TempData["Erro"] = $"O limite de {_opcoes.LimiteFila} laudos deste tipo em andamento foi atingido.";
            return Redirect("~/laudos/" + id);
        }

        var projeto = origem.Modo == ModoLaudo.Projeto;
        var novo = new Laudo
        {
            IdDispositivo = origem.IdDispositivo, Tipo = origem.Tipo, Modo = origem.Modo,
            OrdemServico = "REUSO-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            IdResponsavelProjeto = projeto ? _usuario.IdUsuario : null,
            IdConferente = projeto ? null : _usuario.IdUsuario,
            Status = StatusLaudo.EmAndamento
        };
        foreach (var m in origem.Medidas)
            novo.Medidas.Add(new MedidaEspecificacao { Numero = m.Numero, Descricao = m.Descricao, Especificacao = m.Especificacao, LimiteMin = m.LimiteMin, LimiteMax = m.LimiteMax, MetodoInstrumento = m.MetodoInstrumento, Resultado = ResultadoMedida.Aguardando });
        foreach (var c in origem.Checklist)
            novo.Checklist.Add(new ItemChecklist { Numero = c.Numero, Descricao = c.Descricao, Resultado = ResultadoChecklist.Aguardando });
        if (origem.Tipo == TipoLaudo.Corretiva && origem.Corretiva != null)
        {
            novo.Corretiva = new LaudoCorretiva
            {
                DescricaoFalha = origem.Corretiva.DescricaoFalha,
                ComponenteAfetado = origem.Corretiva.ComponenteAfetado,
                TipoFalha = origem.Corretiva.TipoFalha,
                CausasSugeridas = origem.Corretiva.CausasSugeridas,
                OutrasInformacoes = origem.Corretiva.OutrasInformacoes,
                Ishikawa = origem.Corretiva.Ishikawa == null ? null : new Ishikawa
                {
                    Metodo = origem.Corretiva.Ishikawa.Metodo, Maquina = origem.Corretiva.Ishikawa.Maquina,
                    Material = origem.Corretiva.Ishikawa.Material, Medicao = origem.Corretiva.Ishikawa.Medicao,
                    MeioAmbiente = origem.Corretiva.Ishikawa.MeioAmbiente, MaoDeObra = origem.Corretiva.Ishikawa.MaoDeObra,
                    CausaRaiz = origem.Corretiva.Ishikawa.CausaRaiz
                }
            };
            foreach (var acao in origem.Corretiva.Acoes)
                novo.Corretiva.Acoes.Add(new AcaoCorretiva { Numero = acao.Numero, Oque = acao.Oque, Como = acao.Como });
        }
        // O desenho é reaproveitado por metadados, apontando para o mesmo arquivo físico.
        foreach (var anexo in origem.Anexos.Where(a => a.Categoria == CategoriaAnexo.DesenhoTecnico))
            novo.Anexos.Add(new Anexo { Categoria = anexo.Categoria, NomeOriginal = anexo.NomeOriginal, TipoMime = anexo.TipoMime, TamanhoBytes = anexo.TamanhoBytes, CaminhoArquivo = anexo.CaminhoArquivo, DataEnvio = DateTime.Now });

        _db.Laudos.Add(novo);
        await _db.SaveChangesAsync();
        await RegistrarHistorico(novo, $"Estrutura reaproveitada do laudo {id}.");
        TempData["Sucesso"] = "Novo laudo criado com a estrutura do anterior. Informe a ordem de serviço.";
        return Redirect("~/laudos/" + novo.IdLaudo);
    }

    // Remoção de dispositivo pendente sem laudos (usada pela gestão).
    [HttpPost("dispositivo/{idDispositivo:int}/remover-pendente"), ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverDispositivoPendente(int idDispositivo)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var d = await _db.Dispositivos.Include(x => x.Laudos).FirstOrDefaultAsync(x => x.IdDispositivo == idDispositivo);
        if (d == null) return NotFound();
        if (d.Status != StatusDispositivo.Pendente || d.Laudos.Any()) return BadRequest("Somente dispositivo pendente e sem laudos pode ser removido.");
        _db.Dispositivos.Remove(d);
        await _db.SaveChangesAsync();
        return Ok(new { sucesso = true });
    }

    // ------------------------------------------------------------------ anexos

    // Upload avulso (o formulário principal já envia os arquivos ao salvar). Mantido para compatibilidade.
    [HttpPost("{id:int}/anexos"), ValidateAntiForgeryToken, RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> EnviarAnexo(int id, IFormFile arquivo, CategoriaAnexo categoria, CancellationToken cancellationToken)
    {
        var laudo = await _db.Laudos.AsNoTracking().FirstOrDefaultAsync(x => x.IdLaudo == id, cancellationToken);
        if (laudo == null) return NotFound();
        var estrutura = LaudoFluxo.PodeEditarEstrutura(laudo, _usuario.Perfil, _usuario.IdUsuario);
        var fotoDoOperador = LaudoFluxo.PodePreencher(laudo, _usuario.Perfil, _usuario.IdUsuario)
            && categoria is CategoriaAnexo.FotoOcorrencia or CategoriaAnexo.FotoTrabalho;
        if (!estrutura && !fotoDoOperador && _usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        try
        {
            var resultado = await _anexos.SalvarAsync(id, categoria, arquivo, cancellationToken);
            if (!resultado.Sucesso) TempData["Erro"] = resultado.Erro;
            else TempData["Sucesso"] = "Anexo enviado com sucesso.";
        }
        catch (Exception)
        {
            TempData["Erro"] = "Não foi possível armazenar o anexo. Verifique o log do servidor.";
        }
        return Redirect("~/laudos/" + id);
    }

    // Download/visualização autenticada: participantes do laudo, Programador ou Gerente.
    // Sem nome de arquivo na resposta, imagens e PDFs abrem direto no navegador (e servem de <img> na tela).
    [HttpGet("anexos/{idAnexo:int}/download")]
    public async Task<IActionResult> DownloadAnexo(int idAnexo, CancellationToken cancellationToken)
    {
        var anexo = await _anexos.ObterAsync(idAnexo, cancellationToken);
        if (anexo == null) return NotFound();
        var laudo = await _db.Laudos.AsNoTracking().FirstOrDefaultAsync(x => x.IdLaudo == anexo.IdLaudo, cancellationToken);
        if (laudo == null) return NotFound();
        var autorizado = _usuario.Perfil is PerfilUsuario.Programador or PerfilUsuario.Gerente
            || laudo.IdResponsavelProjeto == _usuario.IdUsuario || laudo.IdConferente == _usuario.IdUsuario;
        if (!autorizado) return Forbid();
        var stream = await _anexos.AbrirAsync(anexo, cancellationToken);
        if (stream == null) return NotFound("Arquivo não encontrado no armazenamento.");
        return File(stream, anexo.TipoMime);
    }

    // ------------------------------------------------------------------ histórico

    private async Task RegistrarHistorico(Laudo laudo, string? motivo)
    {
        _db.LaudoHistorico.Add(new LaudoHistorico { IdLaudo = laudo.IdLaudo, IdUsuario = _usuario.IdUsuario, Status = laudo.Status, DataHora = DateTime.Now, Motivo = motivo });
        await _db.SaveChangesAsync();
    }
}
