using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

// Marco 6: regras centralizadas do ciclo de vida dos laudos.
[Authorize]
[Route("laudos")]
public class LaudosController : Controller
{
    private readonly SgdDbContext _db;
    private readonly IUsuarioAtual _usuario;
    private readonly IAnexoService _anexos;

    public LaudosController(SgdDbContext db, IUsuarioAtual usuario, IAnexoService anexos)
    { _db = db; _usuario = usuario; _anexos = anexos; }

    // M6-08 — lista do usuário, agrupada por tipo e com contadores por status.
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var consulta = _db.Laudos.AsNoTracking().Include(x => x.Dispositivo).ThenInclude(x => x.Produto)
            .Where(x => x.IdResponsavelProjeto == _usuario.IdUsuario || x.IdConferente == _usuario.IdUsuario || _usuario.Perfil == PerfilUsuario.Programador || _usuario.Perfil == PerfilUsuario.Gerente);
        var itens = await consulta.OrderByDescending(x => x.DataAtualizacao).ToListAsync();
        return View(new LaudosListaViewModel { Laudos = itens,
            EmAndamento = itens.Count(x => x.Status == StatusLaudo.EmAndamento),
            AguardandoOperador = itens.Count(x => x.Status == StatusLaudo.AguardandoOperador),
            AguardandoAprovacao = itens.Count(x => x.Status == StatusLaudo.AguardandoAprovacao),
            Reprovados = itens.Count(x => x.Status == StatusLaudo.Reprovado), Aprovados = itens.Count(x => x.Status == StatusLaudo.Aprovado) });
    }

    // M6-02/M6-03 — formulário único; modo decide responsável, conferente e anexos exigidos.
    [HttpGet("novo")]
    public async Task<IActionResult> Novo(TipoLaudo tipo = TipoLaudo.Preventiva, ModoLaudo modo = ModoLaudo.Projeto)
    {
        var vm = new LaudoNovoViewModel { Tipo = tipo, Modo = modo,
            Dispositivos = await _db.Dispositivos.Include(x => x.Produto).OrderBy(x => x.Codigo).ToListAsync() };
        return View(vm);
    }

    [HttpPost("novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Novo(LaudoNovoViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Dispositivos = await _db.Dispositivos.Include(x => x.Produto).ToListAsync(); return View(vm); }
        if (_usuario.Perfil == PerfilUsuario.Gerente) return Forbid();
        // M6-07 — uma liberação por dispositivo e limite de 20 laudos por usuário/tipo.
        // Permite apenas um laudo de Liberação por dispositivo.
        if (vm.Tipo == TipoLaudo.Liberacao &&
            await _db.Laudos.AnyAsync(x =>
                x.IdDispositivo == vm.IdDispositivo &&
                x.Tipo == TipoLaudo.Liberacao))
        {
            ModelState.AddModelError(
                nameof(vm.IdDispositivo),
                "Este dispositivo já possui um laudo de liberação.");
        }
        var dono = vm.Modo == ModoLaudo.Projeto ? _usuario.IdUsuario : vm.IdConferente;
        if (dono.HasValue && await _db.Laudos.CountAsync(x => x.Tipo == vm.Tipo && (x.IdResponsavelProjeto == dono || x.IdConferente == dono)) >= 20)
            ModelState.AddModelError("", "O limite de 20 laudos deste tipo por usuário foi atingido.");
        var dispositivo = await _db.Dispositivos.FindAsync(vm.IdDispositivo);
        if (dispositivo == null) ModelState.AddModelError(nameof(vm.IdDispositivo), "Dispositivo não encontrado.");
        if (!ModelState.IsValid) { vm.Dispositivos = await _db.Dispositivos.Include(x => x.Produto).ToListAsync(); return View(vm); }

        // M6-02/M6-03 — responsável e conferente são definidos automaticamente; não há campo manual na tela.
        int? conferenteAutomatico = vm.Modo == ModoLaudo.Legado
            ? _usuario.IdUsuario
            : await _db.Usuarios.Where(u => u.Perfil == PerfilUsuario.Operacional && u.Status == StatusUsuario.Ativo)
                .OrderBy(u => u.IdUsuario).Select(u => (int?)u.IdUsuario).FirstOrDefaultAsync();
        if (vm.Modo == ModoLaudo.Projeto && !conferenteAutomatico.HasValue)
        {
            ModelState.AddModelError("", "Não existe usuário Operacional ativo para receber este laudo.");
            vm.Dispositivos = await _db.Dispositivos.Include(x => x.Produto).ToListAsync(); return View(vm);
        }
        var laudo = new Laudo { IdDispositivo = vm.IdDispositivo, Tipo = vm.Tipo, Modo = vm.Modo,
            OrdemServico = vm.OrdemServico.Trim(), DataInicio = vm.DataInicio,
            IdResponsavelProjeto = vm.Modo == ModoLaudo.Projeto ? _usuario.IdUsuario : null,
            IdConferente = conferenteAutomatico,
            Status = StatusLaudo.EmAndamento };
        _db.Laudos.Add(laudo);
        if (vm.Tipo == TipoLaudo.Corretiva) { laudo.Corretiva = new LaudoCorretiva(); }
        await _db.SaveChangesAsync();
        await RegistrarHistorico(laudo, "Laudo criado.");
        // M6-02 — o projetista cria a estrutura técnica; M6-03 — no legado o conferente é automático.
        return RedirectToAction(nameof(Editar), new { id = laudo.IdLaudo });
    }

    // M6-01/M6-04/M6-05/M6-06/M6-09 — edição e preenchimento com autorização por perfil/estado.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (!PodeEditarEstrutura(laudo) && !PodePreencher(laudo)) return Forbid();
        return View(new LaudoEditarViewModel { Laudo = laudo, PodeEditarEstrutura = PodeEditarEstrutura(laudo), PodePreencher = PodePreencher(laudo) });
    }

    [HttpPost("{id:int}/estrutura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarEstrutura(int id, LaudoEditarViewModel vm)
    {
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (!PodeEditarEstrutura(laudo)) return Forbid();
        // M6-05 — especificação no formato nominal ± tolerância é interpretada no servidor.
        laudo.Medidas.Clear();
        foreach (var item in vm.Medidas ?? new())
        {
            if (string.IsNullOrWhiteSpace(item.Especificacao)) continue;
            if (!TentarInterpretarEspecificacao(item.Especificacao, out var min, out var max))
            { ModelState.AddModelError("", $"Especificação inválida: {item.Especificacao}. Use, por exemplo, 50 ± 0,5."); continue; }
            laudo.Medidas.Add(new MedidaEspecificacao { Numero = item.Numero, Descricao = item.Descricao,
                Especificacao = item.Especificacao.Trim(), LimiteMin = min, LimiteMax = max, MetodoInstrumento = item.MetodoInstrumento });
        }
        // M6-06 — itens são definidos pelo criador e iniciam sem resultado validado.
        laudo.Checklist.Clear();
        foreach (var item in vm.ItensChecklist ?? new())
            if (!string.IsNullOrWhiteSpace(item.Descricao)) laudo.Checklist.Add(new ItemChecklist { Numero = item.Numero, Descricao = item.Descricao.Trim(), Resultado = ResultadoChecklist.Aguardando });
        // M6-09 — campos estruturados do laudo corretivo e do Ishikawa 6M.
        if (laudo.Tipo == TipoLaudo.Corretiva)
        {
            laudo.Corretiva ??= new LaudoCorretiva();
            laudo.Corretiva.DescricaoFalha = vm.DescricaoFalha;
            laudo.Corretiva.ComponenteAfetado = vm.ComponenteAfetado;
            laudo.Corretiva.CausasSugeridas = vm.CausasSugeridas; // armazenado, não executa IA.
            laudo.Corretiva.Ishikawa ??= new Ishikawa();
            laudo.Corretiva.Ishikawa.Metodo = vm.Metodo; laudo.Corretiva.Ishikawa.Maquina = vm.Maquina;
            laudo.Corretiva.Ishikawa.Material = vm.Material; laudo.Corretiva.Ishikawa.Medicao = vm.Medicao;
            laudo.Corretiva.Ishikawa.MeioAmbiente = vm.MeioAmbiente; laudo.Corretiva.Ishikawa.MaoDeObra = vm.MaoDeObra;
            laudo.Corretiva.Ishikawa.CausaRaiz = vm.CausaRaiz;
            laudo.Corretiva.Acoes.Clear();
            foreach (var acao in vm.Acoes ?? new()) if (!string.IsNullOrWhiteSpace(acao.Oque)) laudo.Corretiva.Acoes.Add(new AcaoCorretiva { Numero = acao.Numero, Oque = acao.Oque, Como = acao.Como });
        }
        if (!ModelState.IsValid) return View("Editar", new LaudoEditarViewModel { Laudo = laudo, PodeEditarEstrutura = true, PodePreencher = false });
        laudo.DataAtualizacao = DateTime.Now; await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Estrutura do laudo salva."; return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost("{id:int}/preenchimento")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarPreenchimento(int id, LaudoEditarViewModel vm)
    {
        var laudo = await CarregarLaudo(id); if (laudo == null) return NotFound();
        if (!PodePreencher(laudo)) return Forbid();
        // M6-04 — o operador não altera responsável nem estrutura; apenas resultados.
        foreach (var medida in laudo.Medidas)
        {
            var entrada = vm.Medicoes?.FirstOrDefault(x => x.IdMedida == medida.IdMedida);
            if (entrada == null) continue;
            medida.Medicao = entrada.Medicao;
            // M6-05 — aprovação/reprovação calculada no servidor, nunca confiada ao navegador.
            medida.Resultado = !entrada.Medicao.HasValue ? ResultadoMedida.Aguardando :
                entrada.Medicao.Value >= medida.LimiteMin && entrada.Medicao.Value <= medida.LimiteMax ? ResultadoMedida.Aprovado : ResultadoMedida.Reprovado;
        }
        foreach (var item in laudo.Checklist)
        {
            var entrada = vm.ResultadosChecklist?.FirstOrDefault(x => x.IdChecklist == item.IdChecklist);
            if (entrada != null) item.Resultado = entrada.Resultado;
        }
        laudo.DataAtualizacao = DateTime.Now; await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Preenchimento salvo."; return RedirectToAction(nameof(Editar), new { id });
    }

    // M6-10 — envio respeita o fluxo PROJETO/LEGADO e exige dados completos.
    [HttpPost("{id:int}/enviar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(int id)
    {
        var laudo = await CarregarLaudo(id); if (laudo == null) return NotFound();
        if (!PodeEditarEstrutura(laudo) && !PodePreencher(laudo)) return Forbid();
        // Só se envia laudo em edição (EmAndamento) ou na etapa do operador; reprovado precisa ser reaberto antes.
        if (laudo.Status != StatusLaudo.EmAndamento && laudo.Status != StatusLaudo.AguardandoOperador)
        { TempData["Erro"] = laudo.Status == StatusLaudo.Reprovado ? "Reabra o laudo para correção antes de reenviar." : "Este laudo não pode ser enviado no status atual."; return RedirectToAction(nameof(Editar), new { id }); }

        // PROJETO, primeiro envio: o projetista entrega só a estrutura; as medições são feitas depois pelo operador.
        var envioDoProjetista = laudo.Modo == ModoLaudo.Projeto && laudo.Status == StatusLaudo.EmAndamento;
        if (envioDoProjetista)
        {
            if (!laudo.Medidas.Any())
            { TempData["Erro"] = "Defina ao menos uma medida antes de enviar ao operador."; return RedirectToAction(nameof(Editar), new { id }); }
            if (!laudo.IdConferente.HasValue)
            { TempData["Erro"] = "Defina o conferente/operador antes de enviar."; return RedirectToAction(nameof(Editar), new { id }); }
            laudo.Status = StatusLaudo.AguardandoOperador;
        }
        else
        {
            // LEGADO (criador preenche tudo) e envio do operador no PROJETO: tudo medido, vai à aprovação.
            if (laudo.Medidas.Any(x => x.Resultado == ResultadoMedida.Aguardando))
            { TempData["Erro"] = "Preencha todas as medições antes de enviar."; return RedirectToAction(nameof(Editar), new { id }); }
            if (laudo.Checklist.Any(x => x.Resultado == ResultadoChecklist.Aguardando))
            { TempData["Erro"] = "Preencha todos os itens do checklist antes de enviar."; return RedirectToAction(nameof(Editar), new { id }); }
            laudo.Status = StatusLaudo.AguardandoAprovacao;
        }
        laudo.DataEnvio = DateTime.Now; laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, "Laudo enviado para a próxima etapa."); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // M6-10 — conclusão/reprovação restritas ao Programador; motivo é obrigatório.
    [HttpPost("{id:int}/aprovar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprovar(int id)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var laudo = await CarregarLaudo(id); if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.AguardandoAprovacao) return BadRequest("Laudo não está aguardando aprovação.");
        laudo.Status = StatusLaudo.Aprovado; laudo.DataAtualizacao = DateTime.Now;
        if (laudo.Dispositivo.Status == StatusDispositivo.Pendente) laudo.Dispositivo.Ativar();
        await RegistrarHistorico(laudo, "Laudo aprovado."); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/reprovar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprovar(int id, string motivo)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var laudo = await CarregarLaudo(id); if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.AguardandoAprovacao) return BadRequest("Laudo não está aguardando aprovação.");
        if (string.IsNullOrWhiteSpace(motivo)) { TempData["Erro"] = "Informe o motivo da reprovação."; return RedirectToAction(nameof(Editar), new { id }); }
        laudo.Status = StatusLaudo.Reprovado; laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, motivo.Trim()); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }

    // M6-10 — reenvio após reprovação volta ao estado inicial e preserva o motivo no histórico.
    [HttpPost("{id:int}/reenviar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reenviar(int id)
    {
        var laudo = await CarregarLaudo(id); if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.Reprovado || !PodeEditarEstrutura(laudo)) return Forbid();
        laudo.Status = StatusLaudo.EmAndamento; laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, "Laudo reaberto para correção e reenvio."); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Editar), new { id });
    }

    // M6-07 — exclusão permitida somente enquanto EmAndamento.
    [HttpPost("{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var laudo = await _db.Laudos.FindAsync(id); if (laudo == null) return NotFound();
        if (laudo.Status != StatusLaudo.EmAndamento || (!PodeEditarEstrutura(laudo) && _usuario.Perfil != PerfilUsuario.Programador)) return Forbid();
        _db.Laudos.Remove(laudo); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }

    // M6-07 — reutiliza somente dados técnicos não pessoais; cria um novo laudo independente.
    [HttpPost("{id:int}/reaproveitar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reaproveitar(int id)
    {
        var origem = await CarregarLaudo(id); if (origem == null) return NotFound();
        if (_usuario.Perfil == PerfilUsuario.Gerente) return Forbid();
        var novo = new Laudo { IdDispositivo = origem.IdDispositivo, Tipo = origem.Tipo, Modo = origem.Modo,
            OrdemServico = "REUSO-" + DateTime.Now.ToString("yyyyMMddHHmmss"), IdResponsavelProjeto = _usuario.IdUsuario,
            IdConferente = _usuario.Perfil == PerfilUsuario.Projetista
                ? await _db.Usuarios.Where(u => u.Perfil == PerfilUsuario.Operacional && u.Status == StatusUsuario.Ativo).OrderBy(u => u.IdUsuario).Select(u => (int?)u.IdUsuario).FirstOrDefaultAsync()
                : _usuario.IdUsuario,
            Status = StatusLaudo.EmAndamento };
        foreach (var m in origem.Medidas) novo.Medidas.Add(new MedidaEspecificacao { Numero=m.Numero, Descricao=m.Descricao, Especificacao=m.Especificacao, LimiteMin=m.LimiteMin, LimiteMax=m.LimiteMax, MetodoInstrumento=m.MetodoInstrumento });
        foreach (var c in origem.Checklist) novo.Checklist.Add(new ItemChecklist { Numero=c.Numero, Descricao=c.Descricao, Resultado=ResultadoChecklist.Aguardando });
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
        // M7-03 — o desenho é reaproveitado por metadados, apontando para o mesmo arquivo físico.
        foreach (var anexo in origem.Anexos.Where(a => a.Categoria == CategoriaAnexo.DesenhoTecnico))
            novo.Anexos.Add(new Anexo { Categoria = anexo.Categoria, NomeOriginal = anexo.NomeOriginal,
                TipoMime = anexo.TipoMime, TamanhoBytes = anexo.TamanhoBytes,
                CaminhoArquivo = anexo.CaminhoArquivo, DataEnvio = DateTime.Now });
        _db.Laudos.Add(novo); await _db.SaveChangesAsync(); await RegistrarHistorico(novo, $"Estrutura reaproveitada do laudo {id}.");
        return RedirectToAction(nameof(Editar), new { id = novo.IdLaudo });
    }

    // M6-07 — endpoint para remoção de dispositivo pendente sem laudos associados.
    [HttpPost("dispositivo/{idDispositivo:int}/remover-pendente")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverDispositivoPendente(int idDispositivo)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var d = await _db.Dispositivos.Include(x => x.Laudos).FirstOrDefaultAsync(x => x.IdDispositivo == idDispositivo);
        if (d == null) return NotFound();
        if (d.Status != StatusDispositivo.Pendente || d.Laudos.Any()) return BadRequest("Somente dispositivo pendente e sem laudos pode ser removido.");
        _db.Dispositivos.Remove(d); await _db.SaveChangesAsync(); return Ok(new { sucesso = true });
    }

    // M7-01/M7-02 — upload validado e armazenado fora de wwwroot.
    [HttpPost("{id:int}/anexos")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> EnviarAnexo(int id, IFormFile arquivo, CategoriaAnexo categoria, CancellationToken cancellationToken)
    {
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (!PodeEditarEstrutura(laudo) && _usuario.Perfil != PerfilUsuario.Programador) return Forbid();
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
        return RedirectToAction(nameof(Editar), new { id });
    }

    // M7-03 — download autenticado; somente participantes do laudo, Programador ou Gerente.
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
        return File(stream, anexo.TipoMime, anexo.NomeOriginal);
    }

    // M8-03 — custos podem ser registrados na aprovação e atualizados após a aprovação.
    [HttpPost("{id:int}/custos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarCustos(int id, decimal? custoMaterial, decimal? custoMaoObra)
    {
        if (_usuario.Perfil != PerfilUsuario.Programador) return Forbid();
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (laudo.Status is not (StatusLaudo.AguardandoAprovacao or StatusLaudo.Aprovado))
        {
            TempData["Erro"] = "Os custos só podem ser registrados durante a aprovação ou após a aprovação.";
            return RedirectToAction(nameof(Editar), new { id });
        }
        const decimal maximo = 99999999.99m;
        if (custoMaterial < 0 || custoMaoObra < 0 || custoMaterial > maximo || custoMaoObra > maximo)
        {
            TempData["Erro"] = "Os custos devem estar entre R$ 0,00 e R$ 99.999.999,99.";
            return RedirectToAction(nameof(Editar), new { id });
        }
        laudo.CustoMaterial = custoMaterial;
        laudo.CustoMaoObra = custoMaoObra;
        laudo.DataAtualizacao = DateTime.Now;
        await RegistrarHistorico(laudo, $"Custos atualizados — material: {laudo.CustoMaterial?.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) ?? "não informado"}; mão de obra: {laudo.CustoMaoObra?.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) ?? "não informado"}.");
        TempData["Sucesso"] = "Custos registrados.";
        return RedirectToAction(nameof(Editar), new { id });
    }

    private async Task<Laudo?> CarregarLaudo(int id) => await _db.Laudos.Include(x => x.Dispositivo).ThenInclude(x => x.Produto)
        .Include(x => x.Medidas).Include(x => x.Checklist).Include(x => x.Anexos).Include(x => x.Historico).ThenInclude(x => x.Usuario)
        .Include(x => x.Corretiva).ThenInclude(x => x!.Ishikawa).Include(x => x.Corretiva).ThenInclude(x => x!.Acoes)
        .FirstOrDefaultAsync(x => x.IdLaudo == id);

    // M6-01 — Projetista altera estrutura em EmAndamento/Reprovado; Operacional preenche apenas em AguardandoOperador.
    // M6-03 — no LEGADO não há projetista: o criador (conferente) edita a estrutura e preenche enquanto o laudo está aberto.
    private bool PodeEditarEstrutura(Laudo l) => _usuario.Perfil == PerfilUsuario.Programador
        || (_usuario.Perfil == PerfilUsuario.Projetista && l.IdResponsavelProjeto == _usuario.IdUsuario && LaudoAberto(l))
        || EhCriadorLegado(l);
    private bool PodePreencher(Laudo l) =>
        (_usuario.Perfil == PerfilUsuario.Operacional && l.IdConferente == _usuario.IdUsuario && l.Status == StatusLaudo.AguardandoOperador)
        || EhCriadorLegado(l);

    private static bool LaudoAberto(Laudo l) => l.Status is StatusLaudo.EmAndamento or StatusLaudo.Reprovado;
    private bool EhCriadorLegado(Laudo l) => l.Modo == ModoLaudo.Legado && l.IdConferente == _usuario.IdUsuario
        && _usuario.Perfil != PerfilUsuario.Gerente && LaudoAberto(l);

    private async Task RegistrarHistorico(Laudo laudo, string? motivo)
    {
        _db.LaudoHistorico.Add(new LaudoHistorico { IdLaudo = laudo.IdLaudo, IdUsuario = _usuario.IdUsuario, Status = laudo.Status, DataHora = DateTime.Now, Motivo = motivo });
        await _db.SaveChangesAsync();
    }

    // Interpretação decimal pt-BR e cálculo dos limites exclusivamente no servidor.
    private static bool TentarInterpretarEspecificacao(string texto, out decimal minimo, out decimal maximo)
    {
        minimo = maximo = 0;
        var partes = texto.Replace("+/-", "±").Split('±', StringSplitOptions.TrimEntries);
        if (partes.Length != 2 || !decimal.TryParse(partes[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), out var nominal) ||
            !decimal.TryParse(partes[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), out var tolerancia) || tolerancia < 0) return false;
        minimo = nominal - tolerancia; maximo = nominal + tolerancia; return true;
    }

    // Rotas antigas dos Marcos anteriores permanecem disponíveis.
    [HttpGet("liberacao")] public IActionResult Liberacao() => RedirectToAction(nameof(Index));
    [HttpGet("liberacao/novo")] public IActionResult LiberacaoNovo() => RedirectToAction(nameof(Novo), new { tipo = TipoLaudo.Liberacao });
    [HttpGet("preventiva")] public IActionResult Preventiva() => RedirectToAction(nameof(Index));
    [HttpGet("preventiva/novo")] public IActionResult PreventivaNovo() => RedirectToAction(nameof(Novo), new { tipo = TipoLaudo.Preventiva });
    [HttpGet("corretiva")] public IActionResult Corretiva() => RedirectToAction(nameof(Index));
    [HttpGet("corretiva/novo")] public IActionResult CorretivaNovo() => RedirectToAction(nameof(Novo), new { tipo = TipoLaudo.Corretiva });
}
