using SgdBaldan.Infrastructure;

namespace SgdBaldan.ViewModels;

public class ProdutoViewModel
{
    public int IdProduto { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}

public class CategoriaViewModel
{
    public int IdCategoria { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = "#5b6b82";
    public bool Ativo { get; set; }
    public string TextoSobre => CoresHelper.TextoSobre(Cor);
}

/// <summary>Tela /catalogo (somente Programador).</summary>
public class CatalogoViewModel
{
    public List<ProdutoViewModel> Produtos { get; set; } = new();
    public List<CategoriaViewModel> Categorias { get; set; } = new();
    public string CorSugerida { get; set; } = "#e11d48";
}
