using System.ComponentModel.DataAnnotations;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.ViewModels;

public class CadastroViewModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu RE.")]
    [StringLength(30)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "O RE deve ter apenas letras, números, ponto, hífen ou sublinhado.")]
    public string Re { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe uma senha.")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o perfil de acesso.")]
    public PerfilUsuario? PerfilSolicitado { get; set; }
}
