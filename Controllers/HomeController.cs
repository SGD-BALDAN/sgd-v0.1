using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SgdBaldan.Infrastructure;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

public class HomeController : Controller
{
    private readonly LaudosOptions _laudos;

    public HomeController(IOptions<LaudosOptions> laudos) => _laudos = laudos.Value;

    [HttpGet("inicio")]
    public IActionResult Index() => View(new InicioViewModel { LimiteFila = _laudos.LimiteFila });
}
