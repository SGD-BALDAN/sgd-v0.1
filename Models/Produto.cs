using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

public class Produto
{
    public int IdProduto { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
