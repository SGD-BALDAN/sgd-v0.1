using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;
using SgdBaldan.Models.Enums;
using SgdBaldan.Infrastructure;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

[Authorize(Roles = Papeis.Programador)]
public class AprovacoesController : Controller
{
    private readonly IUsuarioService _usuarios;
    private readonly SgdDbContext _db;

    public AprovacoesController(IUsuarioService usuarios, SgdDbContext db)
    { _usuarios = usuarios; _db = db; }

    [HttpGet("aprovacoes")]
    public async Task<IActionResult> Index()
    {
        var modelo = new AprovacoesViewModel
        {
            Solicitacoes = await _usuarios.ListarPendentesAsync(),
            Usuarios = await _usuarios.ListarAsync(),
            LaudosPendentes = await _db.Laudos.AsNoTracking()
                .Include(l => l.Dispositivo).Include(l => l.ResponsavelProjeto).Include(l => l.Conferente)
                .Where(l => l.Status == StatusLaudo.AguardandoAprovacao)
                .OrderBy(l => l.DataEnvio).ToListAsync()
        };
        return View(modelo);
    }
}
