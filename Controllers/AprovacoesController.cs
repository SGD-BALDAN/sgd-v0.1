using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SgdBaldan.Infrastructure;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

[Authorize(Roles = Papeis.Programador)]
public class AprovacoesController : Controller
{
    private readonly IUsuarioService _usuarios;

    public AprovacoesController(IUsuarioService usuarios) => _usuarios = usuarios;

    [HttpGet("aprovacoes")]
    public async Task<IActionResult> Index()
    {
        var modelo = new AprovacoesViewModel
        {
            Solicitacoes = await _usuarios.ListarPendentesAsync(),
            Usuarios = await _usuarios.ListarAsync()
        };
        return View(modelo);
    }
}
