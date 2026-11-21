using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SgdBaldan.Data;

/// <summary>Permite que dotnet ef crie o DbContext em tempo de design sem iniciar a aplicação web.</summary>
public sealed class SgdDbContextFactory : IDesignTimeDbContextFactory<SgdDbContext>
{
    public SgdDbContext CreateDbContext(string[] args)
    {
        var conexao = Environment.GetEnvironmentVariable("ConnectionStrings__Sgd")
            ?? "Server=localhost;Port=3306;Database=sgd_dev;User=sgd_app;Password=;CharSet=utf8mb4;";
        var versaoTexto = Environment.GetEnvironmentVariable("Database__MySqlVersion") ?? "8.0.36";
        var opcoes = new DbContextOptionsBuilder<SgdDbContext>()
            .UseMySql(conexao, new MySqlServerVersion(Version.Parse(versaoTexto)))
            .Options;
        return new SgdDbContext(opcoes);
    }
}
