using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

/// <summary>Diagrama 6M. Cada coluna aceita várias causas (uma por linha).</summary>
public class Ishikawa
{
    public int IdLaudo { get; set; }
    public LaudoCorretiva LaudoCorretiva { get; set; } = null!;
    public string? Metodo { get; set; }
    public string? Maquina { get; set; }
    public string? Material { get; set; }
    public string? Medicao { get; set; }
    public string? MeioAmbiente { get; set; }
    public string? MaoDeObra { get; set; }
    public string? CausaRaiz { get; set; }
}
