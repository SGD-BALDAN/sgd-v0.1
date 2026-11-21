using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

[AllowAnonymous]
public class ErroController : Controller
{
    private readonly ILogger<ErroController> _logger;

    public ErroController(ILogger<ErroController> logger) => _logger = logger;

    [Route("erro/{codigo:int?}")]
    public IActionResult Index(int? codigo)
    {
        var excecao = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (excecao?.Error is not null)
            _logger.LogError(excecao.Error, "Erro não tratado em {Caminho}", excecao.Path);

        var status = codigo is >= 400 and <= 599 ? codigo.Value : 500;
        var (titulo, mensagem) = status switch
        {
            403 => ("Acesso negado", "Você não tem permissão para acessar esta página."),
            404 => ("Página não encontrada", "O endereço acessado não existe ou foi removido."),
            _ => ("Algo deu errado", "Ocorreu um erro inesperado. Tente novamente em instantes.")
        };

        Response.StatusCode = status;
        return View(new ErrorViewModel
        {
            Codigo = status,
            Titulo = titulo,
            Mensagem = mensagem,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
