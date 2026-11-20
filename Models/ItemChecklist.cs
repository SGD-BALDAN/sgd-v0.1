using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

public class ItemChecklist
{
    public int IdChecklist { get; set; }
    public int IdLaudo { get; set; }
    public Laudo Laudo { get; set; } = null!;
    public int Numero { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public ResultadoChecklist Resultado { get; set; } = ResultadoChecklist.Ok;
}
