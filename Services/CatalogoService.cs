using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Services;

public interface ICatalogoService
{
    Task<List<ProdutoViewModel>> ListarProdutosAsync(bool apenasAtivos = false);
    Task<List<CategoriaViewModel>> ListarCategoriasAsync(bool apenasAtivas = false);
    Task<Resultado> CriarProdutoAsync(string? nome);
    Task<Resultado> AlterarProdutoAsync(int id, bool ativo);
    Task<Resultado> CriarCategoriaAsync(string? nome, string? cor);
    Task<Resultado> AlterarCategoriaAsync(int id, bool ativa);
    /// <summary>Primeira cor sugerida que ainda não é usada por nenhuma categoria.</summary>
    Task<string> SugerirCorAsync();
}

/// <summary>Produtos e categorias. Quem pode alterar (somente o Programador) é garantido no controller.</summary>
public class CatalogoService : ICatalogoService
{
    private static readonly string[] Sugestoes =
        { "#e11d48", "#0ea5e9", "#a16207", "#9333ea", "#14b8a6", "#f97316", "#6366f1", "#84cc16" };

    private readonly SgdDbContext _db;

    public CatalogoService(SgdDbContext db) => _db = db;

    private static string Normalizar(string? texto) => Regex.Replace((texto ?? string.Empty).Trim(), @"\s+", " ");

    public async Task<List<ProdutoViewModel>> ListarProdutosAsync(bool apenasAtivos = false)
    {
        var consulta = _db.Produtos.AsNoTracking().AsQueryable();
        if (apenasAtivos) consulta = consulta.Where(p => p.Ativo);
        var lista = await consulta.OrderBy(p => p.Nome).ToListAsync();
        return lista.Select(p => new ProdutoViewModel { IdProduto = p.IdProduto, Nome = p.Nome, Ativo = p.Ativo }).ToList();
    }

    public async Task<List<CategoriaViewModel>> ListarCategoriasAsync(bool apenasAtivas = false)
    {
        var consulta = _db.Categorias.AsNoTracking().AsQueryable();
        if (apenasAtivas) consulta = consulta.Where(c => c.Ativo);
        var lista = await consulta.OrderBy(c => c.Nome).ToListAsync();
        return lista.Select(c => new CategoriaViewModel { IdCategoria = c.IdCategoria, Nome = c.Nome, Cor = c.Cor, Ativo = c.Ativo }).ToList();
    }

    public async Task<Resultado> CriarProdutoAsync(string? nome)
    {
        nome = Normalizar(nome);
        if (nome.Length == 0) return Resultado.Falha("Informe o nome do produto.");
        if (nome.Length > 40) return Resultado.Falha("O nome do produto deve ter no máximo 40 caracteres.");
        if (await _db.Produtos.AnyAsync(p => p.Nome == nome)) return Resultado.Falha("Já existe um produto com esse nome.");

        _db.Produtos.Add(new Produto { Nome = nome });
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Resultado.Falha("Já existe um produto com esse nome."); }
        return Resultado.Ok();
    }

    public async Task<Resultado> AlterarProdutoAsync(int id, bool ativo)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto == null) return Resultado.Falha("Produto não encontrado.");
        produto.Ativo = ativo;
        await _db.SaveChangesAsync();
        return Resultado.Ok();
    }

    public async Task<Resultado> CriarCategoriaAsync(string? nome, string? cor)
    {
        nome = Normalizar(nome);
        cor = (cor ?? string.Empty).Trim().ToLowerInvariant();
        if (nome.Length == 0) return Resultado.Falha("Informe o nome da categoria.");
        if (nome.Length > 30) return Resultado.Falha("O nome da categoria deve ter no máximo 30 caracteres.");
        if (!CoresHelper.EhHexValido(cor)) return Resultado.Falha("Escolha uma cor válida.");
        if (await _db.Categorias.AnyAsync(c => c.Nome == nome)) return Resultado.Falha("Já existe uma categoria com esse nome.");
        if (await _db.Categorias.AnyAsync(c => c.Cor == cor)) return Resultado.Falha("Essa cor já é usada por outra categoria. Escolha uma cor diferente.");

        _db.Categorias.Add(new Categoria { Nome = nome, Cor = cor });
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Resultado.Falha("Já existe uma categoria com esse nome ou cor."); }
        return Resultado.Ok();
    }

    public async Task<Resultado> AlterarCategoriaAsync(int id, bool ativa)
    {
        var categoria = await _db.Categorias.FindAsync(id);
        if (categoria == null) return Resultado.Falha("Categoria não encontrada.");
        categoria.Ativo = ativa;
        await _db.SaveChangesAsync();
        return Resultado.Ok();
    }

    public async Task<string> SugerirCorAsync()
    {
        var usadas = await _db.Categorias.AsNoTracking().Select(c => c.Cor).ToListAsync();
        var usadasMin = usadas.Select(c => c.ToLowerInvariant()).ToHashSet();
        return Sugestoes.FirstOrDefault(c => !usadasMin.Contains(c)) ?? "#ffffff";
    }
}
