using Microsoft.AspNetCore.Mvc;
using SgdBaldan.Services;

namespace SgdBaldan.Controllers;

public class DispositivosController : Controller
{
    private readonly IDispositivoService _dispositivos;

    public DispositivosController(IDispositivoService dispositivos) => _dispositivos = dispositivos;

    [HttpGet("dispositivos")]
    public async Task<IActionResult> Index() => View(await _dispositivos.ListarAsync());

    [HttpGet("dispositivos/{codigo}")]
    public async Task<IActionResult> Detalhes(string codigo)
    {
        var modelo = await _dispositivos.ObterDetalheAsync(codigo);
        if (modelo == null) return NotFound();
        return View(modelo);
    }
}
