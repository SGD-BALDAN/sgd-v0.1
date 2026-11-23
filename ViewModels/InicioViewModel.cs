using SgdBaldan.Models.Enums;

namespace SgdBaldan.ViewModels;

public class InicioViewModel
{
    public int LimiteFila { get; set; }
    /// <summary>Laudos do usuário que ocupam a fila de cada tipo.</summary>
    public Dictionary<TipoLaudo, int> Fila { get; set; } = new();
    public int Dispositivos { get; set; }
    /// <summary>Laudos reprovados do usuário (para refazer).</summary>
    public int Reprovados { get; set; }
}
