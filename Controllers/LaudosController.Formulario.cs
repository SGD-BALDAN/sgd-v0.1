using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

// Criar, abrir e salvar: as três telas (liberação, preventiva, corretiva) usam o MESMO formulário e o servidor
// decide, pelo perfil e pelo status, o que cada pessoa pode alterar.
public partial class LaudosController
{
    // Desenho + fotos podem somar bastante; cada arquivo continua limitado a 10 MB no AnexoService.
    private const long LimiteUpload = 60L * 1024 * 1024;

    // ------------------------------------------------------------------ novo laudo

    [HttpGet("liberacao/novo")] public Task<IActionResult> LiberacaoNovo(string? codigo = null) => NovoAsync(TipoLaudo.Liberacao, codigo);
    [HttpGet("preventiva/novo")] public Task<IActionResult> PreventivaNovo(string? codigo = null) => NovoAsync(TipoLaudo.Preventiva, codigo);
    [HttpGet("corretiva/novo")] public Task<IActionResult> CorretivaNovo(string? codigo = null) => NovoAsync(TipoLaudo.Corretiva, codigo);

    [HttpPost("liberacao/novo"), ValidateAntiForgeryToken, RequestSizeLimit(LimiteUpload)]
    public Task<IActionResult> LiberacaoCriar(LaudoFormViewModel vm) => CriarAsync(TipoLaudo.Liberacao, vm);
    [HttpPost("preventiva/novo"), ValidateAntiForgeryToken, RequestSizeLimit(LimiteUpload)]
    public Task<IActionResult> PreventivaCriar(LaudoFormViewModel vm) => CriarAsync(TipoLaudo.Preventiva, vm);
    [HttpPost("corretiva/novo"), ValidateAntiForgeryToken, RequestSizeLimit(LimiteUpload)]
    public Task<IActionResult> CorretivaCriar(LaudoFormViewModel vm) => CriarAsync(TipoLaudo.Corretiva, vm);

    private async Task<IActionResult> NovoAsync(TipoLaudo tipo, string? codigo)
    {
        // Projetista cria PROJETO, Operacional cria LEGADO; Programador e Gerente não criam.
        var modo = LaudoFluxo.ModoDoPerfil(_usuario.Perfil);
        if (modo == null) return Forbid();

        var vm = new LaudoFormViewModel { Codigo = string.IsNullOrWhiteSpace(codigo) ? null : _dispositivos.NormalizarCodigo(codigo) };
        await PrepararNovoAsync(vm, tipo, modo.Value);

        // Vindo da tela de Dispositivos com o número já escolhido: se ele existe, pré-seleciona produto e categoria.
        if (vm.Codigo != null)
        {
            var existente = await _dispositivos.ObterPorCodigoAsync(vm.Codigo);
            if (existente != null) { vm.IdProduto = existente.IdProduto; vm.IdCategoria = existente.IdCategoria; }
        }
        return View(NomeDaView(tipo, lista: false), vm);
    }

    private async Task<IActionResult> CriarAsync(TipoLaudo tipo, LaudoFormViewModel vm)
    {
        // A regra vale no servidor, não depende do que a tela mostra.
        var modo = LaudoFluxo.ModoDoPerfil(_usuario.Perfil);
        if (modo == null) return Forbid();
        var legado = modo == ModoLaudo.Legado;     // legado: o operador faz tudo, inclusive os resultados

        ValidarCabecalho(vm);

        // O dispositivo é informado pelo número: se existe, é usado; se não, nasce Pendente (exige produto e categoria).
        Dispositivo? dispositivo = null;
        if (string.IsNullOrWhiteSpace(vm.Codigo))
            ModelState.AddModelError(nameof(vm.Codigo), "Informe o número do dispositivo.");
        else
        {
            var r = await _dispositivos.ObterOuCriarPendenteAsync(vm.Codigo, vm.IdProduto, vm.IdCategoria);
            if (!r.Sucesso || r.Valor == null) ModelState.AddModelError(nameof(vm.Codigo), r.Erro ?? "Dispositivo inválido.");
            else dispositivo = r.Valor;
        }

        // Uma liberação por dispositivo e limite de laudos na fila por usuário/tipo.
        if (dispositivo != null && tipo == TipoLaudo.Liberacao && dispositivo.IdDispositivo != 0
            && await _db.Laudos.AnyAsync(x => x.IdDispositivo == dispositivo.IdDispositivo && x.Tipo == TipoLaudo.Liberacao))
            ModelState.AddModelError(nameof(vm.Codigo), "Este dispositivo já possui um laudo de liberação.");
        if (await ContarFilaAsync(tipo, _usuario.IdUsuario) >= _opcoes.LimiteFila)
            ModelState.AddModelError("", $"O limite de {_opcoes.LimiteFila} laudos deste tipo em andamento foi atingido.");

        var laudo = new Laudo
        {
            Tipo = tipo,
            Modo = modo.Value,
            OrdemServico = (vm.OrdemServico ?? "").Trim(),
            DataInicio = vm.DataInicio.Date,
            // Projeto: nasce sem conferente (o primeiro operador que assumir passa a ser). Legado: o próprio operador.
            IdResponsavelProjeto = legado ? null : _usuario.IdUsuario,
            IdConferente = legado ? _usuario.IdUsuario : null,
            Status = StatusLaudo.EmAndamento
        };
        SincronizarMedidas(laudo, vm, preencher: legado);
        SincronizarChecklist(laudo, vm, preencher: legado);
        if (tipo == TipoLaudo.Corretiva) AplicarCorretiva(laudo, vm);

        if (!ModelState.IsValid || dispositivo == null)
        {
            await PrepararNovoAsync(vm, tipo, modo.Value);
            return View(NomeDaView(tipo, lista: false), vm);
        }

        // Dispositivo existente: só a chave. Dispositivo novo (sem Id): vai junto e é gravado na mesma operação.
        if (dispositivo.IdDispositivo != 0) laudo.IdDispositivo = dispositivo.IdDispositivo; else laudo.Dispositivo = dispositivo;
        _db.Laudos.Add(laudo);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            ModelState.AddModelError(nameof(vm.Codigo), "Não foi possível salvar o laudo. Se for uma liberação, o dispositivo pode já ter um laudo de liberação.");
            await PrepararNovoAsync(vm, tipo, modo.Value);
            return View(NomeDaView(tipo, lista: false), vm);
        }

        await RegistrarHistorico(laudo, "Laudo criado.");
        var erros = await SalvarArquivosAsync(laudo.IdLaudo, vm, estrutura: true, preencher: legado);
        return await FinalizarSalvamentoAsync(laudo, vm.Acao, erros);
    }

    /// <summary>Listas de apoio e permissões da tela de laudo novo.</summary>
    private async Task PrepararNovoAsync(LaudoFormViewModel vm, TipoLaudo tipo, ModoLaudo modo)
    {
        vm.Tipo = tipo;
        vm.Modo = modo;
        vm.Status = StatusLaudo.EmAndamento;
        vm.IdLaudo = null;
        vm.PodeEditarEstrutura = true;
        vm.PodePreencher = modo == ModoLaudo.Legado;
        vm.NomeResponsavel = _usuario.Nome;
        vm.NomeConferente = modo == ModoLaudo.Legado ? _usuario.Nome : "";
        vm.Dispositivos = await _db.Dispositivos.AsNoTracking().Include(x => x.Produto).Where(x => x.Status != StatusDispositivo.Inativo).OrderBy(x => x.Codigo).ToListAsync();
        vm.Produtos = await _db.Produtos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync();
        vm.Categorias = await _db.Categorias.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync();
    }

    // ------------------------------------------------------------------ abrir / salvar

    // /laudos/{id} e /laudos/editar/{id} (usada pela tela de Dispositivos) abrem o mesmo formulário.
    [HttpGet("{id:int}")]
    [HttpGet("editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        if (!LaudoFluxo.PodeVer(laudo, _usuario.Perfil, _usuario.IdUsuario)) return Forbid();
        return View(NomeDaView(laudo.Tipo, lista: false), MontarViewModel(laudo));
    }

    [HttpPost("{id:int}/salvar"), ValidateAntiForgeryToken, RequestSizeLimit(LimiteUpload)]
    public async Task<IActionResult> Salvar(int id, LaudoFormViewModel vm)
    {
        var laudo = await CarregarLaudo(id);
        if (laudo == null) return NotFound();
        var estrutura = LaudoFluxo.PodeEditarEstrutura(laudo, _usuario.Perfil, _usuario.IdUsuario);
        var preencher = LaudoFluxo.PodePreencher(laudo, _usuario.Perfil, _usuario.IdUsuario);
        if (!estrutura && !preencher) return Forbid();   // consulta (Gerente, Programador...) não grava

        // O número do dispositivo nunca muda depois do laudo criado; o cabeçalho só vale para quem edita a estrutura.
        ModelState.Remove(nameof(vm.Codigo));
        if (!estrutura) ModelState.Remove(nameof(vm.OrdemServico));

        if (estrutura)
        {
            ValidarCabecalho(vm);
            if (ModelState.IsValid) { laudo.OrdemServico = vm.OrdemServico!.Trim(); laudo.DataInicio = vm.DataInicio.Date; }
            SincronizarMedidas(laudo, vm, preencher);
            SincronizarChecklist(laudo, vm, preencher);
            if (laudo.Tipo == TipoLaudo.Corretiva) AplicarCorretiva(laudo, vm);
        }
        else AplicarPreenchimento(laudo, vm);

        if (!ModelState.IsValid)
        {
            LaudoFormViewModel exibir;
            if (estrutura)
            {
                // Mantém o que a pessoa digitou (todas as linhas são editáveis para ela).
                exibir = vm;
                AplicarContexto(exibir, laudo);
            }
            else
            {
                // Operador: a estrutura não é postada; parte do banco e devolve só o que ele digitou.
                _db.ChangeTracker.Clear();
                var original = await CarregarLaudo(id);
                exibir = MontarViewModel(original!);
                foreach (var m in exibir.Medidas)
                    m.Medicao = vm.Medidas.FirstOrDefault(x => x.IdMedida == m.IdMedida)?.Medicao ?? m.Medicao;
                foreach (var c in exibir.Checklist)
                    c.Resultado = vm.Checklist.FirstOrDefault(x => x.IdChecklist == c.IdChecklist)?.Resultado ?? c.Resultado;
            }
            return View(NomeDaView(laudo.Tipo, lista: false), exibir);
        }

        laudo.DataAtualizacao = DateTime.Now;
        await _db.SaveChangesAsync();
        var erros = await SalvarArquivosAsync(id, vm, estrutura, preencher);
        return await FinalizarSalvamentoAsync(laudo, vm.Acao, erros);
    }

    /// <summary>Depois de gravar: se o botão foi "Concluir e enviar", tenta enviar; senão volta ao laudo.</summary>
    private async Task<IActionResult> FinalizarSalvamentoAsync(Laudo laudo, string? acao, List<string> errosArquivos)
    {
        var avisos = new List<string>(errosArquivos);
        if (string.Equals(acao, "enviar", StringComparison.OrdinalIgnoreCase))
        {
            var envio = await EnviarAsync(laudo);
            if (envio.Sucesso)
            {
                TempData["Sucesso"] = envio.Valor;
                if (avisos.Count > 0) TempData["Erro"] = string.Join("\n", avisos);
                return Redirect("~/laudos/" + laudo.Tipo.Slug());
            }
            avisos.Add("Laudo salvo, mas não foi enviado: " + envio.Erro);
        }
        else TempData["Sucesso"] = "Laudo salvo.";

        if (avisos.Count > 0) TempData["Erro"] = string.Join("\n", avisos);
        return Redirect("~/laudos/" + laudo.IdLaudo);
    }

    // ------------------------------------------------------------------ montagem do ViewModel (do banco)

    private LaudoFormViewModel MontarViewModel(Laudo l)
    {
        var vm = new LaudoFormViewModel
        {
            Codigo = l.Dispositivo.Codigo,
            IdProduto = l.Dispositivo.IdProduto,
            IdCategoria = l.Dispositivo.IdCategoria,
            OrdemServico = l.OrdemServico,
            DataInicio = l.DataInicio,
            Medidas = l.Medidas.OrderBy(x => x.Numero).Select(m => new MedidaForm
            {
                IdMedida = m.IdMedida, Numero = m.Numero, Descricao = m.Descricao, Especificacao = m.Especificacao,
                Metodo = m.MetodoInstrumento, Medicao = LaudoFluxo.Formatar(m.Medicao),
                LimiteMin = m.LimiteMin, LimiteMax = m.LimiteMax, Resultado = m.Resultado
            }).ToList(),
            Checklist = l.Checklist.OrderBy(x => x.Numero).Select(c => new ChecklistForm
            {
                IdChecklist = c.IdChecklist, Numero = c.Numero, Descricao = c.Descricao, Resultado = c.Resultado
            }).ToList()
        };

        var cor = l.Corretiva;
        if (cor != null)
        {
            vm.DescricaoFalha = cor.DescricaoFalha;
            vm.ComponenteAfetado = cor.ComponenteAfetado;
            vm.TipoFalha = cor.TipoFalha;
            vm.CausasSugeridas = cor.CausasSugeridas;
            vm.OutrasInformacoes = cor.OutrasInformacoes;
            vm.CausaRaiz = cor.Ishikawa?.CausaRaiz;
            vm.CausasMetodo = Dividir(cor.Ishikawa?.Metodo);
            vm.CausasMaquina = Dividir(cor.Ishikawa?.Maquina);
            vm.CausasMaterial = Dividir(cor.Ishikawa?.Material);
            vm.CausasMedicao = Dividir(cor.Ishikawa?.Medicao);
            vm.CausasMeioAmbiente = Dividir(cor.Ishikawa?.MeioAmbiente);
            vm.CausasMaoDeObra = Dividir(cor.Ishikawa?.MaoDeObra);
            vm.Acoes = cor.Acoes.OrderBy(a => a.Numero).Select(a => new AcaoForm { Numero = a.Numero, Oque = a.Oque, Como = a.Como }).ToList();
        }

        AplicarContexto(vm, l);
        return vm;
    }

    /// <summary>Permissões, nomes e anexos do laudo (nada disso vem do navegador).</summary>
    private void AplicarContexto(LaudoFormViewModel vm, Laudo l)
    {
        var perfil = _usuario.Perfil;
        var idUsuario = _usuario.IdUsuario;

        vm.IdLaudo = l.IdLaudo;
        vm.Tipo = l.Tipo;
        vm.Modo = l.Modo;
        vm.Status = l.Status;
        vm.Codigo = l.Dispositivo.Codigo;
        vm.IdProduto = l.Dispositivo.IdProduto;
        vm.IdCategoria = l.Dispositivo.IdCategoria;
        vm.NomeProduto = l.Dispositivo.Produto?.Nome ?? "";
        vm.NomeCategoria = l.Dispositivo.Categoria?.Nome ?? "";
        vm.NomeResponsavel = l.ResponsavelProjeto?.Nome ?? l.Conferente?.Nome ?? "";
        vm.NomeConferente = l.Conferente?.Nome ?? "";

        vm.PodeEditarEstrutura = LaudoFluxo.PodeEditarEstrutura(l, perfil, idUsuario);
        vm.PodePreencher = LaudoFluxo.PodePreencher(l, perfil, idUsuario);
        vm.PodeAssumir = perfil == PerfilUsuario.Operacional && LaudoFluxo.NaFilaDoOperador(l);
        vm.PodeDecidir = perfil == PerfilUsuario.Programador && l.Status == StatusLaudo.AguardandoAprovacao;
        vm.PodeRegistrarCustos = perfil == PerfilUsuario.Programador && (l.Status == StatusLaudo.AguardandoAprovacao || l.Status == StatusLaudo.Aprovado);
        vm.PodeReabrir = l.Status == StatusLaudo.Reprovado && vm.PodeEditarEstrutura;
        vm.PodeExcluir = l.Status == StatusLaudo.EmAndamento && vm.PodeEditarEstrutura;
        vm.PodeReaproveitar = l.Tipo != TipoLaudo.Liberacao && LaudoFluxo.ModoDoPerfil(perfil) == l.Modo
            && (l.Status == StatusLaudo.Aprovado || l.Status == StatusLaudo.Reprovado);

        vm.MotivoReprovacao = l.Status == StatusLaudo.Reprovado
            ? l.Historico.OrderByDescending(h => h.DataHora).FirstOrDefault(h => h.Status == StatusLaudo.Reprovado && !string.IsNullOrWhiteSpace(h.Motivo))?.Motivo
            : null;
        vm.CustoMaterial = l.CustoMaterial;
        vm.CustoMaoObra = l.CustoMaoObra;

        vm.DesenhoAtual = l.Anexos.Where(a => a.Categoria == CategoriaAnexo.DesenhoTecnico).OrderByDescending(a => a.DataEnvio).FirstOrDefault();
        vm.FotosOcorrenciaAtuais = l.Anexos.Where(a => a.Categoria == CategoriaAnexo.FotoOcorrencia).OrderBy(a => a.DataEnvio).ToList();
        vm.FotosTrabalhoAtuais = l.Anexos.Where(a => a.Categoria == CategoriaAnexo.FotoTrabalho).OrderBy(a => a.DataEnvio).ToList();
    }

    private async Task<Laudo?> CarregarLaudo(int id) => await _db.Laudos
        .Include(x => x.Dispositivo).ThenInclude(x => x.Produto)
        .Include(x => x.Dispositivo).ThenInclude(x => x.Categoria)
        .Include(x => x.ResponsavelProjeto).Include(x => x.Conferente)
        .Include(x => x.Medidas).Include(x => x.Checklist).Include(x => x.Anexos)
        .Include(x => x.Historico).ThenInclude(x => x.Usuario)
        .Include(x => x.Corretiva).ThenInclude(x => x!.Ishikawa)
        .Include(x => x.Corretiva).ThenInclude(x => x!.Acoes)
        .AsSplitQuery()
        .FirstOrDefaultAsync(x => x.IdLaudo == id);

    // ------------------------------------------------------------------ aplicar o formulário no laudo

    private void ValidarCabecalho(LaudoFormViewModel vm)
    {
        if (vm.DataInicio.Year < 2000) ModelState.AddModelError(nameof(vm.DataInicio), "Informe a data de início.");
    }

    /// <summary>
    /// Estrutura das medidas (Nº, descrição, especificação, método). Linhas existentes são atualizadas no lugar
    /// (preserva medições já feitas); linhas novas são criadas; as que saíram do formulário são removidas.
    /// A medição só é aceita de quem pode preencher, e o resultado é sempre calculado aqui.
    /// </summary>
    private void SincronizarMedidas(Laudo laudo, LaudoFormViewModel vm, bool preencher)
    {
        var existentes = laudo.Medidas.Where(m => m.IdMedida != 0).ToDictionary(m => m.IdMedida);
        var mantidas = new HashSet<int>();
        var numero = 0;
        foreach (var item in vm.Medidas)
        {
            if (string.IsNullOrWhiteSpace(item.Especificacao)) continue;   // linha em branco
            numero++;
            if (item.Especificacao.Trim().Length > 30)
            { ModelState.AddModelError("", $"Medida {numero}: a especificação pode ter até 30 caracteres."); continue; }
            if (!LaudoFluxo.TentarInterpretarEspecificacao(item.Especificacao, out var min, out var max))
            { ModelState.AddModelError("", $"Medida {numero}: especificação inválida ({item.Especificacao.Trim()}). Use, por exemplo, 50 ± 0,5."); continue; }

            decimal? medicao = null;
            if (preencher && !LaudoFluxo.TentarLerMedicao(item.Medicao, out medicao))
            { ModelState.AddModelError("", $"Medida {numero}: medição inválida ({item.Medicao})."); continue; }
            if (medicao.HasValue && Math.Abs(medicao.Value) >= 1_000_000m)
            { ModelState.AddModelError("", $"Medida {numero}: medição fora do limite permitido."); continue; }

            MedidaEspecificacao alvo;
            if (item.IdMedida != 0 && existentes.TryGetValue(item.IdMedida, out var atual)) alvo = atual;
            else { alvo = new MedidaEspecificacao(); laudo.Medidas.Add(alvo); }

            alvo.Numero = numero;
            alvo.Descricao = item.Descricao;
            alvo.Especificacao = item.Especificacao.Trim();
            alvo.LimiteMin = min;
            alvo.LimiteMax = max;
            alvo.MetodoInstrumento = item.Metodo;
            if (preencher) alvo.Medicao = medicao;
            alvo.Resultado = LaudoFluxo.CalcularResultado(alvo.Medicao, min, max);
            if (alvo.IdMedida != 0) mantidas.Add(alvo.IdMedida);
        }
        foreach (var removida in laudo.Medidas.Where(m => m.IdMedida != 0 && !mantidas.Contains(m.IdMedida)).ToList())
            laudo.Medidas.Remove(removida);
    }

    private void SincronizarChecklist(Laudo laudo, LaudoFormViewModel vm, bool preencher)
    {
        var existentes = laudo.Checklist.Where(c => c.IdChecklist != 0).ToDictionary(c => c.IdChecklist);
        var mantidos = new HashSet<int>();
        var numero = 0;
        foreach (var item in vm.Checklist)
        {
            if (string.IsNullOrWhiteSpace(item.Descricao)) continue;
            numero++;
            if (item.Descricao.Trim().Length > 255)
            { ModelState.AddModelError("", $"Checklist {numero}: a descrição pode ter até 255 caracteres."); continue; }

            ItemChecklist? alvo = null;
            if (item.IdChecklist != 0) existentes.TryGetValue(item.IdChecklist, out alvo);
            if (alvo == null) { alvo = new ItemChecklist { Resultado = ResultadoChecklist.Aguardando }; laudo.Checklist.Add(alvo); }

            alvo.Numero = numero;
            alvo.Descricao = item.Descricao.Trim();
            if (preencher) alvo.Resultado = item.Resultado;   // OK / NÃO OK / N/A (ou Aguardando)
            if (alvo.IdChecklist != 0) mantidos.Add(alvo.IdChecklist);
        }
        foreach (var removido in laudo.Checklist.Where(c => c.IdChecklist != 0 && !mantidos.Contains(c.IdChecklist)).ToList())
            laudo.Checklist.Remove(removido);
    }

    /// <summary>Operador num laudo de PROJETO: só medições e resultados do checklist (a estrutura não muda).</summary>
    private void AplicarPreenchimento(Laudo laudo, LaudoFormViewModel vm)
    {
        foreach (var medida in laudo.Medidas)
        {
            var entrada = vm.Medidas.FirstOrDefault(x => x.IdMedida == medida.IdMedida);
            if (entrada == null) continue;
            if (!LaudoFluxo.TentarLerMedicao(entrada.Medicao, out var valor) || (valor.HasValue && Math.Abs(valor.Value) >= 1_000_000m))
            { ModelState.AddModelError("", $"Medida {medida.Numero}: medição inválida ({entrada.Medicao})."); continue; }
            medida.Medicao = valor;
            medida.Resultado = LaudoFluxo.CalcularResultado(valor, medida.LimiteMin, medida.LimiteMax);
        }
        foreach (var item in laudo.Checklist)
        {
            var entrada = vm.Checklist.FirstOrDefault(x => x.IdChecklist == item.IdChecklist);
            if (entrada != null) item.Resultado = entrada.Resultado;
        }
    }

    /// <summary>Campos exclusivos da corretiva: falha, Ishikawa 6M (uma causa por linha) e plano de ação.</summary>
    private void AplicarCorretiva(Laudo laudo, LaudoFormViewModel vm)
    {
        Limite(vm.DescricaoFalha, 1500, "Descrição da falha");
        Limite(vm.ComponenteAfetado, 150, "Componente afetado");
        Limite(vm.CausasSugeridas, 1500, "Possíveis causas raízes");
        Limite(vm.OutrasInformacoes, 1500, "Outras informações");
        Limite(vm.CausaRaiz, 500, "Causa raiz");

        var corretiva = laudo.Corretiva ??= new LaudoCorretiva();
        corretiva.DescricaoFalha = Limpar(vm.DescricaoFalha);
        corretiva.ComponenteAfetado = Limpar(vm.ComponenteAfetado);
        corretiva.TipoFalha = vm.TipoFalha;
        corretiva.CausasSugeridas = Limpar(vm.CausasSugeridas);
        corretiva.OutrasInformacoes = Limpar(vm.OutrasInformacoes);   // campos de IA (relato/resumo) continuam sem uso

        var ishikawa = corretiva.Ishikawa ??= new Ishikawa();
        ishikawa.Metodo = Juntar(vm.CausasMetodo, "Método");
        ishikawa.Maquina = Juntar(vm.CausasMaquina, "Máquina");
        ishikawa.Material = Juntar(vm.CausasMaterial, "Material");
        ishikawa.Medicao = Juntar(vm.CausasMedicao, "Medição");
        ishikawa.MeioAmbiente = Juntar(vm.CausasMeioAmbiente, "Meio ambiente");
        ishikawa.MaoDeObra = Juntar(vm.CausasMaoDeObra, "Mão de obra");
        ishikawa.CausaRaiz = Limpar(vm.CausaRaiz);

        corretiva.Acoes.Clear();
        var n = 0;
        foreach (var acao in vm.Acoes)
        {
            if (string.IsNullOrWhiteSpace(acao.Oque)) continue;
            n++;
            Limite(acao.Oque, 1000, $"Ação {n} (o que)");
            Limite(acao.Como, 1000, $"Ação {n} (como)");
            corretiva.Acoes.Add(new AcaoCorretiva { Numero = n, Oque = acao.Oque.Trim(), Como = Limpar(acao.Como) });
        }
    }

    private void Limite(string? valor, int maximo, string campo)
    {
        if (valor != null && valor.Trim().Length > maximo)
            ModelState.AddModelError("", $"{campo}: máximo de {maximo} caracteres.");
    }

    private string? Juntar(List<string> causas, string campo)
    {
        var limpas = causas.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim().Replace("\r", " ").Replace("\n", " ")).ToList();
        if (limpas.Count == 0) return null;
        var texto = string.Join("\n", limpas);
        if (texto.Length > 1500) ModelState.AddModelError("", $"Ishikawa — {campo}: texto muito longo (máximo de 1500 caracteres).");
        return texto;
    }

    private static string? Limpar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static List<string> Dividir(string? texto) =>
        (texto ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>
    /// Grava os arquivos do formulário. Desenho técnico: quem edita a estrutura. Fotos da corretiva: quem edita
    /// a estrutura ou o operador que está preenchendo. Falhas viram avisos; o laudo já está salvo.
    /// </summary>
    private async Task<List<string>> SalvarArquivosAsync(int idLaudo, LaudoFormViewModel vm, bool estrutura, bool preencher)
    {
        var erros = new List<string>();

        async Task Guardar(IFormFile? arquivo, CategoriaAnexo categoria)
        {
            if (arquivo == null || arquivo.Length == 0) return;
            try
            {
                var r = await _anexos.SalvarAsync(idLaudo, categoria, arquivo, HttpContext.RequestAborted);
                if (!r.Sucesso) erros.Add($"{arquivo.FileName}: {r.Erro}");
            }
            catch (Exception)
            {
                erros.Add($"{arquivo.FileName}: não foi possível armazenar o arquivo. Verifique o log do servidor.");
            }
        }

        if (estrutura) await Guardar(vm.Desenho, CategoriaAnexo.DesenhoTecnico);
        if (estrutura || preencher)
        {
            foreach (var foto in vm.FotosOcorrencia) await Guardar(foto, CategoriaAnexo.FotoOcorrencia);
            foreach (var foto in vm.FotosTrabalho) await Guardar(foto, CategoriaAnexo.FotoTrabalho);
        }
        return erros;
    }
}
