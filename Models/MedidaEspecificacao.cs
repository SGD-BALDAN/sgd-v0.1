using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

public class MedidaEspecificacao
{
    public int IdMedida { get; set; }
    public int IdLaudo { get; set; }
    public Laudo Laudo { get; set; } = null!;
    public int Numero { get; set; }
    public DescricaoMedida? Descricao { get; set; }
    /// <summary>Ex.: "50 ± 0,5".</summary>
    public string? Especificacao { get; set; }
    public decimal? LimiteMin { get; set; }
    public decimal? LimiteMax { get; set; }
    public MetodoInstrumento? MetodoInstrumento { get; set; }
    public decimal? Medicao { get; set; }
    public ResultadoMedida Resultado { get; set; } = ResultadoMedida.Aguardando;
}
