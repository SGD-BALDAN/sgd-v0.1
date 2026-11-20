using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;

namespace SgdBaldan.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSgdDatabase(
        this IServiceCollection services,
        IConfiguration config)
    {
        var conexao = config.GetConnectionString("Sgd");

        if (string.IsNullOrWhiteSpace(conexao))
        {
            throw new InvalidOperationException(
                "Connection string 'Sgd' não configurada.");
        }

        var versao = Version.Parse(
            config["Database:MySqlVersion"] ?? "8.0.36");

        services.AddDbContext<SgdDbContext>(opcoes =>
            opcoes.UseMySql(
                conexao,
                new MySqlServerVersion(versao)));

        return services;
    }
}