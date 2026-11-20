using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SgdBaldan.Infrastructure;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

/// <summary>Produtos e categorias: somente o Programador visualiza e altera (exige o login do Marco 3).</summary>
[Authorize(Roles = Papeis.Programador)]
[Route("catalogo")]
public class CatalogoController : Controller
{
    private readonly ICatalogoService _catalogo;

    public CatalogoController(ICatalogoService catalogo) => _catalogo = catalogo;

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(new CatalogoViewModel
    {
        Produtos = await _catalogo.ListarProdutosAsync(),
        Categorias = await _catalogo.ListarCategoriasAsync(),
        CorSugerida = await _catalogo.SugerirCorAsync()
    });

    [HttpPost("produtos")]
    public async Task<IActionResult> CriarProduto(string? nome) =>
        Voltar(await _catalogo.CriarProdutoAsync(nome), "Produto adicionado.", "produtosCard");

    [HttpPost("produtos/alterar")]
    public async Task<IActionResult> AlterarProduto(int id, bool ativo) =>
        Voltar(await _catalogo.AlterarProdutoAsync(id, ativo), ativo ? "Produto reativado." : "Produto desativado.", "produtosCard");

    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria(string? nome, string? cor) =>
        Voltar(await _catalogo.CriarCategoriaAsync(nome, cor), "Categoria adicionada.", "tiposCard");

    [HttpPost("categorias/alterar")]
    public async Task<IActionResult> AlterarCategoria(int id, bool ativa) =>
        Voltar(await _catalogo.AlterarCategoriaAsync(id, ativa), ativa ? "Categoria reativada." : "Categoria desativada.", "tiposCard");

    private IActionResult Voltar(Resultado resultado, string sucesso, string ancora)
    {
        if (resultado.Sucesso) TempData["Sucesso"] = sucesso;
        else TempData["Erro"] = resultado.Erro;
        return Redirect(Url.Action(nameof(Index)) + "#" + ancora);
    }
}
