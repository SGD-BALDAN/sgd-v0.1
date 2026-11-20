namespace SgdBaldan.ViewModels;

public class ErrorViewModel
{
    public int Codigo { get; set; } = 500;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? RequestId { get; set; }
}
