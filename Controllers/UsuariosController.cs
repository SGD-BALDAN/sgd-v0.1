using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models.Enums;
using SgdBaldan.Services;

namespace SgdBaldan.Controllers;

/// <summary>Gestão de usuários (somente Programador). A tela fica em Aprovações.</summary>
[Authorize(Roles = Papeis.Programador)]
[Route("usuarios")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarios;
    private readonly IUsuarioAtual _atual;

    public UsuariosController(IUsuarioService usuarios, IUsuarioAtual atual)
    {
        _usuarios = usuarios;
        _atual = atual;
    }

    [HttpPost("aprovar")]
    public async Task<IActionResult> Aprovar(int id, PerfilUsuario perfil) =>
        Voltar(await _usuarios.AprovarAsync(id, perfil), "Usuário aprovado e acesso liberado.");

    [HttpPost("recusar")]
    public async Task<IActionResult> Recusar(int id) =>
        Voltar(await _usuarios.RecusarAsync(id), "Solicitação recusada.");

    [HttpPost("inativar")]
    public async Task<IActionResult> Inativar(int id) =>
        Voltar(await _usuarios.InativarAsync(id, _atual.IdUsuario), "Usuário inativado.");

    [HttpPost("reativar")]
    public async Task<IActionResult> Reativar(int id) =>
        Voltar(await _usuarios.ReativarAsync(id), "Usuário reativado.");

    private IActionResult Voltar(Resultado resultado, string sucesso)
    {
        if (resultado.Sucesso) TempData["Sucesso"] = sucesso;
        else TempData["Erro"] = resultado.Erro;
        return Redirect(Url.Action("Index", "Aprovacoes") + "#usuariosCard");
    }
}
