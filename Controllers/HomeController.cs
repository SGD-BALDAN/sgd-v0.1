using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SgdBaldan.Data;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models.Enums;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

public class HomeController : Controller
{
    private readonly LaudosOptions _laudos;
    private readonly SgdDbContext _db;
    private readonly IUsuarioAtual _usuario;

    public HomeController(IOptions<LaudosOptions> laudos, SgdDbContext db, IUsuarioAtual usuario)
    { _laudos = laudos.Value; _db = db; _usuario = usuario; }

    [HttpGet("inicio")]
    public async Task<IActionResult> Index()
    {
        var id = _usuario.IdUsuario;
        var meus = await _db.Laudos.AsNoTracking()
            .Where(x => x.IdResponsavelProjeto == id || x.IdConferente == id)
            .Select(x => new { x.Tipo, x.Status }).ToListAsync();

        var modelo = new InicioViewModel
        {
            LimiteFila = _laudos.LimiteFila,
            Dispositivos = await _db.Dispositivos.CountAsync(x => x.Status != StatusDispositivo.Inativo),
            Reprovados = meus.Count(x => x.Status == StatusLaudo.Reprovado)
        };
        foreach (var tipo in Enum.GetValues<TipoLaudo>())
            modelo.Fila[tipo] = meus.Count(x => x.Tipo == tipo && LaudoFluxo.OcupaFila(x.Status));
        return View(modelo);
    }
}
