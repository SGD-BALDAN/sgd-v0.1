using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

/// <summary>Dados exclusivos do laudo corretiva (1:1 com Laudo).</summary>
public class LaudoCorretiva
{
    public int IdLaudo { get; set; }
    public Laudo Laudo { get; set; } = null!;
    public string? DescricaoFalha { get; set; }
    // Campos de IA: existem, mas ainda não são usados.
    public string? ComponenteAfetado { get; set; }
    public TipoFalha? TipoFalha { get; set; }
    public string? CausasSugeridas { get; set; }
    public string? RelatoIa { get; set; }
    public string? ResumoIa { get; set; }
    public string? OutrasInformacoes { get; set; }

    public Ishikawa? Ishikawa { get; set; }
    public ICollection<AcaoCorretiva> Acoes { get; set; } = new List<AcaoCorretiva>();
}
