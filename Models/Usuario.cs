using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;
    /// <summary>RE: único e usado como login.</summary>
    public string Re { get; set; } = string.Empty;
    /// <summary>Somente o hash da senha é guardado.</summary>
    public string SenhaHash { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public StatusUsuario Status { get; set; } = StatusUsuario.Pendente;
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public bool PodeAprovar() => Perfil == PerfilUsuario.Programador;
    public bool SomenteConsulta() => Perfil == PerfilUsuario.Gerente;
}
