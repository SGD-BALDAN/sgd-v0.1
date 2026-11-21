using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SgdBaldan.Data;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Services;

public class ResultadoLogin : Resultado
{
    public Usuario? Usuario { get; init; }

    public static ResultadoLogin Autenticado(Usuario usuario) => new() { Sucesso = true, Usuario = usuario };
    public static new ResultadoLogin Falha(string erro) => new() { Sucesso = false, Erro = erro };
}

public interface IAuthService
{
    Task<ResultadoLogin> AutenticarAsync(string re, string senha);
    Task<Resultado> CadastrarAsync(CadastroViewModel modelo);
}

public class AuthService : IAuthService
{
    private sealed class Tentativa
    {
        public int Falhas;
        public DateTime? BloqueadoAte;
    }

    // Hash de uma senha qualquer: usado quando o RE não existe, para o tempo de resposta não revelar isso.
    private static readonly Usuario UsuarioFalso = new() { Re = "falso" };
    private static readonly string HashFalso = new PasswordHasher<Usuario>().HashPassword(UsuarioFalso, "senha-qualquer-1234");

    private readonly SgdDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly AuthOptions _opcoes;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthService(SgdDbContext db, IMemoryCache cache, IOptions<AuthOptions> opcoes)
    {
        _db = db;
        _cache = cache;
        _opcoes = opcoes.Value;
    }

    public async Task<ResultadoLogin> AutenticarAsync(string re, string senha)
    {
        re = (re ?? string.Empty).Trim();
        var chave = "login:" + re.ToLowerInvariant();
        var tentativa = _cache.GetOrCreate(chave, e =>
        {
            e.SlidingExpiration = TimeSpan.FromMinutes(15);
            return new Tentativa();
        })!;

        lock (tentativa)
        {
            if (tentativa.BloqueadoAte is DateTime ate && ate > DateTime.Now)
            {
                var segundos = (int)Math.Ceiling((ate - DateTime.Now).TotalSeconds);
                return ResultadoLogin.Falha($"Muitas tentativas. Tente novamente em {segundos} segundo(s).");
            }
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Re == re);
        if (usuario == null)
        {
            _hasher.VerifyHashedPassword(UsuarioFalso, HashFalso, senha ?? string.Empty);
            RegistrarFalha(tentativa);
            return ResultadoLogin.Falha("RE ou senha inválidos.");
        }

        var verificacao = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha ?? string.Empty);
        if (verificacao == PasswordVerificationResult.Failed)
        {
            RegistrarFalha(tentativa);
            return ResultadoLogin.Falha("RE ou senha inválidos.");
        }

        if (verificacao == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = _hasher.HashPassword(usuario, senha!);
            await _db.SaveChangesAsync();
        }

        // Senha correta: agora sim informa a situação do acesso.
        switch (usuario.Status)
        {
            case StatusUsuario.Pendente:
                return ResultadoLogin.Falha("Seu acesso ainda não foi aprovado pela Gestão. Aguarde a liberação.");
            case StatusUsuario.Recusado:
                return ResultadoLogin.Falha("Sua solicitação de acesso foi recusada. Procure a Gestão.");
            case StatusUsuario.Inativo:
                return ResultadoLogin.Falha("Usuário inativo. Procure a Gestão.");
        }

        _cache.Remove(chave);
        return ResultadoLogin.Autenticado(usuario);
    }

    private void RegistrarFalha(Tentativa tentativa)
    {
        lock (tentativa)
        {
            tentativa.Falhas++;
            if (tentativa.Falhas >= _opcoes.MaxTentativas)
            {
                tentativa.BloqueadoAte = DateTime.Now.AddSeconds(_opcoes.BloqueioSegundos);
                tentativa.Falhas = 0;
            }
        }
    }

    public async Task<Resultado> CadastrarAsync(CadastroViewModel modelo)
    {
        var re = modelo.Re.Trim();
        var nome = Regex.Replace(modelo.Nome.Trim(), @"\s+", " ");
        if (modelo.PerfilSolicitado is null) return Resultado.Falha("Selecione o perfil de acesso.");

        if (await _db.Usuarios.AnyAsync(u => u.Re == re))
            return Resultado.Falha("Este RE já possui cadastro.");

        var usuario = new Usuario
        {
            Nome = nome,
            Re = re,
            Perfil = modelo.PerfilSolicitado.Value,
            Status = StatusUsuario.Pendente
        };
        usuario.SenhaHash = _hasher.HashPassword(usuario, modelo.Senha);
        _db.Usuarios.Add(usuario);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Resultado.Falha("Este RE já possui cadastro.");
        }
        return Resultado.Ok();
    }
}
