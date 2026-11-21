using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.Infrastructure;

public static class UsuarioClaims
{
    public static ClaimsPrincipal Criar(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new("re", usuario.Re),
            new(ClaimTypes.Role, usuario.Perfil.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}

public static class AutenticacaoExtensions
{
    /// <summary>Cookie de sessão + política padrão: todo endpoint exige login, salvo [AllowAnonymous].</summary>
    public static IServiceCollection AddSgdAutenticacao(this IServiceCollection services, IConfiguration config, IHostEnvironment ambiente)
    {
        var opcoes = config.GetSection(AuthOptions.Secao).Get<AuthOptions>() ?? new AuthOptions();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = "/login";
                o.LogoutPath = "/sair";
                o.AccessDeniedPath = "/erro/403";
                o.Cookie.Name = ".SGD.Auth";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = ambiente.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                o.ExpireTimeSpan = TimeSpan.FromHours(opcoes.SessaoHoras);
                o.SlidingExpiration = false;   // a sessão dura no máximo SessaoHoras
                o.Events.OnValidatePrincipal = ValidarUsuarioAsync;
            });

        services.AddAuthorization(o =>
            o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    /// <summary>A cada requisição confere se o usuário continua ativo e com o mesmo perfil.</summary>
    private static async Task ValidarUsuarioAsync(CookieValidatePrincipalContext contexto)
    {
        var id = contexto.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var perfil = contexto.Principal?.FindFirst(ClaimTypes.Role)?.Value;
        var valido = false;

        if (int.TryParse(id, out var idUsuario))
        {
            var db = contexto.HttpContext.RequestServices.GetRequiredService<SgdDbContext>();
            var u = await db.Usuarios.AsNoTracking()
                .Where(x => x.IdUsuario == idUsuario)
                .Select(x => new { x.Status, x.Perfil })
                .FirstOrDefaultAsync();
            valido = u != null && u.Status == StatusUsuario.Ativo && u.Perfil.ToString() == perfil;
        }

        if (!valido)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
