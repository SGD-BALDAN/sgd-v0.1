using SgdBaldan.Models.Enums;

namespace SgdBaldan.ViewModels;

public class UsuarioListaViewModel
{
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Re { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public StatusUsuario Status { get; set; }
    public DateTime DataCriacao { get; set; }
}
