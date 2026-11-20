using System.ComponentModel.DataAnnotations;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.ViewModels;


public class LaudosListaViewModel
{
    public List<Laudo> Laudos { get; set; } = new();
    public int EmAndamento { get; set; }
    public int AguardandoOperador { get; set; }
    public int AguardandoAprovacao { get; set; }
    public int Reprovados { get; set; }
    public int Aprovados { get; set; }
}


public class LaudoNovoViewModel
{
    [Required] public TipoLaudo Tipo { get; set; }
    [Required] public ModoLaudo Modo { get; set; }
    [Range(1, int.MaxValue)] public int IdDispositivo { get; set; }
    [Required, StringLength(30)] public string OrdemServico { get; set; } = "";
    [DataType(DataType.Date)] public DateTime DataInicio { get; set; } = DateTime.Today;
    public int? IdConferente { get; set; }
    public List<Dispositivo> Dispositivos { get; set; } = new();
}


public class LaudoEditarViewModel
{
    public Laudo Laudo { get; set; } = null!;
    public bool PodeEditarEstrutura { get; set; }
    public bool PodePreencher { get; set; }
    public List<MedidaEstruturaInput> Medidas { get; set; } = new();
    public List<ChecklistEstruturaInput> ItensChecklist { get; set; } = new();
    public List<MedicaoInput> Medicoes { get; set; } = new();
    public List<ChecklistResultadoInput> ResultadosChecklist { get; set; } = new();
    public string? DescricaoFalha { get; set; }
    public string? ComponenteAfetado { get; set; }
    public string? CausasSugeridas { get; set; }
    public string? Metodo { get; set; } public string? Maquina { get; set; } public string? Material { get; set; }
    public string? Medicao { get; set; } public string? MeioAmbiente { get; set; } public string? MaoDeObra { get; set; }
    public string? CausaRaiz { get; set; }
    public List<AcaoInput> Acoes { get; set; } = new();
}
public class MedidaEstruturaInput { public int Numero { get; set; } public DescricaoMedida? Descricao { get; set; } public string? Especificacao { get; set; } public MetodoInstrumento? MetodoInstrumento { get; set; } }
public class ChecklistEstruturaInput { public int Numero { get; set; } public string Descricao { get; set; } = ""; }
public class MedicaoInput { public int IdMedida { get; set; } public decimal? Medicao { get; set; } }
public class ChecklistResultadoInput { public int IdChecklist { get; set; } public ResultadoChecklist Resultado { get; set; } }
public class AcaoInput { public int Numero { get; set; } public string? Oque { get; set; } public string? Como { get; set; } }
