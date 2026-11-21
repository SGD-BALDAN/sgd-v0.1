using SgdBaldan.Models;

namespace SgdBaldan.ViewModels;

/// <summary>Tela de gestão do Programador. Cada marco acrescenta uma seção.</summary>
public class AprovacoesViewModel
{
    public List<UsuarioListaViewModel> Solicitacoes { get; set; } = new();
    public List<UsuarioListaViewModel> Usuarios { get; set; } = new();
    public List<Laudo> LaudosPendentes { get; set; } = new();
}
