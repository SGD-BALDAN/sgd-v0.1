using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;
using SgdBaldan.Models.Enums;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Services;

public interface IUsuarioService
{
    Task<List<UsuarioListaViewModel>> ListarPendentesAsync();
    /// <summary>Usuários já decididos (ativos e inativos).</summary>
    Task<List<UsuarioListaViewModel>> ListarAsync();
    Task<Resultado> AprovarAsync(int id, PerfilUsuario perfil);
    Task<Resultado> RecusarAsync(int id);
    Task<Resultado> InativarAsync(int id, int idUsuarioAtual);
    Task<Resultado> ReativarAsync(int id);
}

public class UsuarioService : IUsuarioService
{
    private readonly SgdDbContext _db;

    public UsuarioService(SgdDbContext db) => _db = db;

    private static UsuarioListaViewModel Mapear(Models.Usuario u) => new()
    {
        IdUsuario = u.IdUsuario,
        Nome = u.Nome,
        Re = u.Re,
        Perfil = u.Perfil,
        Status = u.Status,
        DataCriacao = u.DataCriacao
    };

    public async Task<List<UsuarioListaViewModel>> ListarPendentesAsync()
    {
        var lista = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Status == StatusUsuario.Pendente)
            .OrderBy(u => u.DataCriacao)
            .ToListAsync();
        return lista.Select(Mapear).ToList();
    }

    public async Task<List<UsuarioListaViewModel>> ListarAsync()
    {
        var lista = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Status == StatusUsuario.Ativo || u.Status == StatusUsuario.Inativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();
        return lista.Select(Mapear).ToList();
    }

    public async Task<Resultado> AprovarAsync(int id, PerfilUsuario perfil)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u == null) return Resultado.Falha("Usuário não encontrado.");
        if (u.Status != StatusUsuario.Pendente) return Resultado.Falha("Esta solicitação já foi decidida.");
        u.Perfil = perfil;
        u.Status = StatusUsuario.Ativo;
        await _db.SaveChangesAsync();
        return Resultado.Ok();
    }

    public async Task<Resultado> RecusarAsync(int id)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u == null) return Resultado.Falha("Usuário não encontrado.");
        if (u.Status != StatusUsuario.Pendente) return Resultado.Falha("Esta solicitação já foi decidida.");
        u.Status = StatusUsuario.Recusado;
        await _db.SaveChangesAsync();
        return Resultado.Ok();
    }

    public async Task<Resultado> InativarAsync(int id, int idUsuarioAtual)
    {
        if (id == idUsuarioAtual) return Resultado.Falha("Você não pode inativar o seu próprio usuário.");
        var u = await _db.Usuarios.FindAsync(id);
        if (u == null) return Resultado.Falha("Usuário não encontrado.");
        if (u.Status != StatusUsuario.Ativo) return Resultado.Falha("Só é possível inativar usuários ativos.");

        if (u.Perfil == PerfilUsuario.Programador)
        {
            var outros = await _db.Usuarios.CountAsync(x => x.Perfil == PerfilUsuario.Programador && x.Status == StatusUsuario.Ativo && x.IdUsuario != id);
            if (outros == 0) return Resultado.Falha("Deve existir ao menos um Programador ativo.");
        }

        u.Status = StatusUsuario.Inativo;
        await _db.SaveChangesAsync();
        return Resultado.Ok();
    }

    public async Task<Resultado> ReativarAsync(int id)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u == null) return Resultado.Falha("Usuário não encontrado.");
        if (u.Status != StatusUsuario.Inativo) return Resultado.Falha("Só é possível reativar usuários inativos.");
        u.Status = StatusUsuario.Ativo;
        await _db.SaveChangesAsync();
        return Resultado.Ok();
    }
}
