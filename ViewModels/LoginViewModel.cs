using System.ComponentModel.DataAnnotations;

namespace SgdBaldan.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe seu RE.")]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "O RE deve ter exatamente 5 dígitos numéricos.")]
    public string Re { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    public bool Lembrar { get; set; }
}
