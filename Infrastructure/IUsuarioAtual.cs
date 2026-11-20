using System.Security.Claims;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.Infrastructure;

/// <summary>Usuário logado (lido das claims do cookie).</summary>
public interface IUsuarioAtual
{
    bool Autenticado { get; }
    int IdUsuario { get; }
    string Nome { get; }
    PerfilUsuario Perfil { get; }
}

public class UsuarioAtual : IUsuarioAtual
{
    private readonly IHttpContextAccessor _http;

    public UsuarioAtual(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public bool Autenticado => Principal?.Identity?.IsAuthenticated == true;
    public int IdUsuario => int.Parse(Valor(ClaimTypes.NameIdentifier));
    public string Nome => Valor(ClaimTypes.Name);
    public PerfilUsuario Perfil => Enum.Parse<PerfilUsuario>(Valor(ClaimTypes.Role));

    private string Valor(string tipo) =>
        Principal?.FindFirst(tipo)?.Value ?? throw new InvalidOperationException("Usuário não autenticado.");
}
