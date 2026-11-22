using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.Data;

/// <summary>Aplica migrations (se configurado) e semeia dados iniciais.</summary>
public static class DbInicializador
{
    public static async Task InicializarAsync(IServiceProvider servicos, IConfiguration config, ILogger logger)
    {
        using var escopo = servicos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SgdDbContext>();
        try
        {
            if (config.GetValue<bool>("Database:AplicarMigrations"))
                await db.Database.MigrateAsync();

            await SemearCatalogoAsync(db);
            await SemearProgramadorAsync(db, config, logger);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Falha ao inicializar o banco de dados do SGD. A aplicação será interrompida para evitar operar sem persistência.");
            throw new InvalidOperationException("Não foi possível inicializar o banco de dados do SGD. Confira a connection string e aplique as migrations.", ex);
        }
    }

    private static async Task SemearCatalogoAsync(SgdDbContext db)
    {
        if (!await db.Categorias.AnyAsync())
        {
            db.Categorias.AddRange(
                new Categoria { Nome = "Solda", Cor = "#7b4fd0" },
                new Categoria { Nome = "Montagem", Cor = "#1f6fe0" },
                new Categoria { Nome = "Matriz", Cor = "#c2410c" },
                new Categoria { Nome = "Aferição", Cor = "#0f9d58" },
                new Categoria { Nome = "Ferramenta de dobra", Cor = "#be185d" },
                new Categoria { Nome = "Furadeira", Cor = "#12a3a8" },
                new Categoria { Nome = "Usinagem", Cor = "#d98a1c" },
                new Categoria { Nome = "Placa de moldagem", Cor = "#4d7c0f" });
        }
        if (!await db.Produtos.AnyAsync())
        {
            db.Produtos.AddRange(
                new Produto { Nome = "Plataforma Dianteira" },
                new Produto { Nome = "Base de Corte" });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Cria o primeiro Programador a partir de Seed:Programador:{Nome,Re,Senha} (user-secrets).</summary>
    private static async Task SemearProgramadorAsync(SgdDbContext db, IConfiguration config, ILogger logger)
    {
        if (await db.Usuarios.AnyAsync(u => u.Perfil == PerfilUsuario.Programador)) return;

        var nome = config["Seed:Programador:Nome"];
        var re = config["Seed:Programador:Re"];
        var senha = config["Seed:Programador:Senha"];
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(re) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning("Nenhum Programador cadastrado. Configure Seed:Programador:Nome, Re e Senha (user-secrets) e reinicie.");
            return;
        }

        re = re.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(re, @"^[0-9]{5}$"))
        {
            logger.LogError("Seed:Programador:Re inválido: o RE deve ter exatamente 5 dígitos numéricos. Programador inicial não criado.");
            return;
        }

        var usuario = new Usuario
        {
            Nome = nome.Trim(),
            Re = re,
            Perfil = PerfilUsuario.Programador,
            Status = StatusUsuario.Ativo
        };
        usuario.SenhaHash = new PasswordHasher<Usuario>().HashPassword(usuario, senha);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        logger.LogInformation("Programador inicial criado (RE {Re}). Remova Seed:Programador:Senha dos segredos.", usuario.Re);
    }
}
