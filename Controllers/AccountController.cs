using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models.Enums;
using SgdBaldan.Services;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Controllers;

/// <summary>Login, solicitação de acesso e logout.</summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth) => _auth = auth;

    [HttpGet("/")]
    public IActionResult Raiz() => RedirectToAction(nameof(Login));

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirecionarPorPerfil();
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            modelo.Senha = string.Empty;
            return View(modelo);
        }

        var resultado = await _auth.AutenticarAsync(modelo.Re, modelo.Senha);
        if (!resultado.Sucesso)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro!);
            modelo.Senha = string.Empty;
            return View(modelo);
        }

        // Sessão por cookie (expira em Auth:SessaoHoras). "Lembrar" guarda só o RE, no navegador (login.js).
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            UsuarioClaims.Criar(resultado.Usuario!),
            new AuthenticationProperties { IsPersistent = false });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return resultado.Usuario!.Perfil == PerfilUsuario.Programador
            ? LocalRedirect("~/aprovacoes")
            : LocalRedirect("~/inicio");
    }

    [HttpGet("cadastro")]
    public IActionResult Cadastro()
    {
        ViewData["Msg"] = TempData["MsgOk"];
        ViewData["MsgOk"] = TempData["MsgOk"] != null;
        return View(new CadastroViewModel());
    }

    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastro(CadastroViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            modelo.Senha = string.Empty;
            return View(modelo);
        }

        var resultado = await _auth.CadastrarAsync(modelo);
        if (!resultado.Sucesso)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro!);
            modelo.Senha = string.Empty;
            return View(modelo);
        }

        TempData["MsgOk"] = "Solicitação enviada! Você poderá entrar assim que a Gestão aprovar o seu acesso.";
        return RedirectToAction(nameof(Cadastro));
    }

    [HttpPost("sair")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirecionarPorPerfil() =>
        User.IsInRole(Papeis.Programador) ? LocalRedirect("~/aprovacoes") : LocalRedirect("~/inicio");
}
