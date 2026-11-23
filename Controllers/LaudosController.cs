using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SgdBaldan.Data;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

/// <summary>
/// Laudos (liberação, preventiva e corretiva). Dividido em três arquivos:
///  • LaudosController.cs            → dependências, rotas por tipo e listagem;
///  • LaudosController.Formulario.cs → criar, abrir e salvar (as telas com o layout do SGD);
///  • LaudosController.Fluxo.cs      → enviar, assumir, aprovar, reprovar, reabrir, excluir, anexos e custos.
/// As regras de "quem pode o quê" ficam em Services/LaudoFluxo.cs.
/// </summary>
[Authorize]
[Route("laudos")]
public partial class LaudosController : Controller
{
    private readonly SgdDbContext _db;
    private readonly IUsuarioAtual _usuario;
    private readonly IAnexoService _anexos;
    private readonly IDispositivoService _dispositivos;
    private readonly LaudosOptions _opcoes;

    public LaudosController(SgdDbContext db, IUsuarioAtual usuario, IAnexoService anexos, IDispositivoService dispositivos, IOptions<LaudosOptions> opcoes)
    { _db = db; _usuario = usuario; _anexos = anexos; _dispositivos = dispositivos; _opcoes = opcoes.Value; }

    // ------------------------------------------------------------------ rotas antigas (compatibilidade)

    // /laudos e /laudos/novo?tipo=... redirecionam para a tela do tipo; sem tipo, volta ao Início.
    [HttpGet("")]
    public IActionResult Index(TipoLaudo? tipo = null) =>
        tipo.HasValue ? Redirect("~/laudos/" + tipo.Value.Slug()) : Redirect("~/inicio");

    [HttpGet("novo")]
    public IActionResult Novo(TipoLaudo? tipo = null, string? codigo = null)
    {
        if (!tipo.HasValue) return Redirect("~/inicio");
        var url = "~/laudos/" + tipo.Value.Slug() + "/novo";
        if (!string.IsNullOrWhiteSpace(codigo)) url += "?codigo=" + Uri.EscapeDataString(codigo);
        return Redirect(url);
    }

    // ------------------------------------------------------------------ listas por tipo

    [HttpGet("liberacao")] public Task<IActionResult> Liberacao() => ListarAsync(TipoLaudo.Liberacao);
    [HttpGet("preventiva")] public Task<IActionResult> Preventiva() => ListarAsync(TipoLaudo.Preventiva);
    [HttpGet("corretiva")] public Task<IActionResult> Corretiva() => ListarAsync(TipoLaudo.Corretiva);

    /// <summary>Cada perfil enxerga só o que faz parte do seu fluxo.</summary>
    private async Task<IActionResult> ListarAsync(TipoLaudo tipo)
    {
        var id = _usuario.IdUsuario;
        var perfil = _usuario.Perfil;

        IQueryable<Laudo> consulta = _db.Laudos.AsNoTracking()
            .Include(x => x.Dispositivo).ThenInclude(x => x.Produto)
            .Where(x => x.Tipo == tipo);

        consulta = perfil switch
        {
            // Projetista: os laudos que ele criou.
            PerfilUsuario.Projetista => consulta.Where(x => x.IdResponsavelProjeto == id),
            // Operacional: os que assumiu/criou (legado) e a fila de projetos esperando um operador.
            PerfilUsuario.Operacional => consulta.Where(x => x.IdConferente == id || x.IdResponsavelProjeto == id
                || (x.Modo == ModoLaudo.Projeto && x.Status == StatusLaudo.AguardandoOperador && x.IdConferente == null)),
            // Programador: só recebe o laudo depois que o operador envia (fila de aprovação e decididos).
            PerfilUsuario.Programador => consulta.Where(x => x.Status == StatusLaudo.AguardandoAprovacao
                || x.Status == StatusLaudo.Aprovado || x.Status == StatusLaudo.Reprovado),
            // Gerente: lê todos.
            _ => consulta
        };

        var itens = await consulta.OrderByDescending(x => x.DataAtualizacao).ToListAsync();
        var modelo = new LaudosListaViewModel
        {
            Tipo = tipo,
            Laudos = itens,
            EmAndamento = itens.Count(x => x.Status is StatusLaudo.EmAndamento or StatusLaudo.AguardandoOperador),
            AguardandoAprovacao = itens.Count(x => x.Status == StatusLaudo.AguardandoAprovacao),
            Reprovados = itens.Count(x => x.Status == StatusLaudo.Reprovado),
            Aprovados = itens.Count(x => x.Status == StatusLaudo.Aprovado),
            LimiteFila = _opcoes.LimiteFila,
            Fila = LaudoFluxo.PodeCriar(perfil) ? await ContarFilaAsync(tipo, id) : 0,
            PodeCriar = LaudoFluxo.PodeCriar(perfil),
            EhOperador = perfil == PerfilUsuario.Operacional
        };
        return View(NomeDaView(tipo, lista: true), modelo);
    }

    /// <summary>Laudos do usuário deste tipo que ainda ocupam a fila (não conta aprovados nem os que estão com a gestão).</summary>
    private async Task<int> ContarFilaAsync(TipoLaudo tipo, int idUsuario)
    {
        var laudos = await _db.Laudos.AsNoTracking()
            .Where(x => x.Tipo == tipo && (x.IdResponsavelProjeto == idUsuario || x.IdConferente == idUsuario))
            .Select(x => x.Status).ToListAsync();
        return laudos.Count(LaudoFluxo.OcupaFila);
    }

    /// <summary>Views mantidas com o layout original: Liberacao/Preventiva/Corretiva (lista) e *Novo (formulário).</summary>
    private static string NomeDaView(TipoLaudo tipo, bool lista) => (tipo, lista) switch
    {
        (TipoLaudo.Liberacao, true) => "Liberacao",
        (TipoLaudo.Preventiva, true) => "Preventiva",
        (TipoLaudo.Corretiva, true) => "Corretiva",
        (TipoLaudo.Liberacao, false) => "LiberacaoNovo",
        (TipoLaudo.Preventiva, false) => "PreventivaNovo",
        _ => "CorretivaNovo"
    };
}
