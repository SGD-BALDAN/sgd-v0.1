using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

public class AcaoCorretiva
{
    public int IdAcao { get; set; }
    public int IdLaudo { get; set; }
    public LaudoCorretiva LaudoCorretiva { get; set; } = null!;
    public int Numero { get; set; }
    public string? Oque { get; set; }
    public string? Como { get; set; }
}
