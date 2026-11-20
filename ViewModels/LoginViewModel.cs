using System.ComponentModel.DataAnnotations;

namespace SgdBaldan.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe seu RE.")]
    [StringLength(30, ErrorMessage = "O RE deve ter no máximo 30 caracteres.")]
    public string Re { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    public bool Lembrar { get; set; }
}
