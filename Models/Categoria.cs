using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

/// <summary>Categoria = tipo do dispositivo (Solda, Montagem...), com cor própria.</summary>
public class Categoria
{
    public int IdCategoria { get; set; }
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cor em hexadecimal (#rrggbb), única entre as categorias.</summary>
    public string Cor { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
