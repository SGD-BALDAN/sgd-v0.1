using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

/// <summary>Registro de cada mudança de status (quem, quando e, na reprovação, o motivo).</summary>
public class LaudoHistorico
{
    public int IdHistorico { get; set; }
    public int IdLaudo { get; set; }
    public Laudo Laudo { get; set; } = null!;
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public StatusLaudo Status { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;
    public string? Motivo { get; set; }
}
