using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;

namespace SgdBaldan.Infrastructure;

/// <summary>Opções tipadas lidas de appsettings.json (seção "Laudos").</summary>
public class LaudosOptions
{
    public const string Secao = "Laudos";

    /// <summary>Máximo de laudos por usuário e por tipo (em andamento + reprovados).</summary>
    [Range(1, 200)]
    public int LimiteFila { get; set; } = 20;
}

/// <summary>Opções de autenticação (seção "Auth").</summary>
public class AuthOptions
{
    public const string Secao = "Auth";

    [Range(1, 20)] public int MaxTentativas { get; set; } = 5;
    [Range(10, 3600)] public int BloqueioSegundos { get; set; } = 60;
    [Range(1, 72)] public int SessaoHoras { get; set; } = 8;
}

public static class ServiceCollectionExtensions
{
    /// <summary>Registra o SgdDbContext usando MySQL (Pomelo). A connection string "Sgd" é obrigatória.</summary>
    public static IServiceCollection AddSgdDatabase(this IServiceCollection services, IConfiguration config)
    {
        var conexao = config.GetConnectionString("Sgd");
        if (string.IsNullOrWhiteSpace(conexao))
        {
            throw new InvalidOperationException(
                "Connection string 'Sgd' não configurada. Em desenvolvimento use: " +
                "dotnet user-secrets set \"ConnectionStrings:Sgd\" \"Server=localhost;Port=3306;Database=sgd_dev;User=sgd_app;Password=SUA_SENHA;CharSet=utf8mb4;\" " +
                "— em produção, use a variável de ambiente ConnectionStrings__Sgd.");
        }

        // Versão fixa do servidor: evita abrir conexão com o banco só para iniciar a aplicação.
        var versao = Version.Parse(config["Database:MySqlVersion"] ?? "8.0.36");

        services.AddDbContext<SgdDbContext>(opcoes =>
            opcoes.UseMySql(conexao, new MySqlServerVersion(versao)));

        return services;
    }

    public static IServiceCollection AddSgdOptions(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<LaudosOptions>()
                .Bind(config.GetSection(LaudosOptions.Secao))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        services.AddOptions<AuthOptions>()
                .Bind(config.GetSection(AuthOptions.Secao))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        services.AddOptions<ArmazenamentoOptions>()
                .Bind(config.GetSection(ArmazenamentoOptions.Secao));
        return services;
    }

    /// <summary>
    /// Registra por convenção (escopo Scoped) toda classe de SgdBaldan.Services ou SgdBaldan.Infrastructure
    /// que implemente a interface de mesmo nome com prefixo "I" (ex.: LaudoService : ILaudoService).
    /// Assim, cada marco só precisa criar os arquivos, sem mexer neste método.
    /// </summary>
    public static IServiceCollection AddSgdServices(this IServiceCollection services)
    {
        var tipos = typeof(ServiceCollectionExtensions).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                        && (t.Namespace == "SgdBaldan.Services" || t.Namespace == "SgdBaldan.Infrastructure"));

        foreach (var tipo in tipos)
        {
            var contrato = tipo.GetInterface("I" + tipo.Name);
            if (contrato != null) services.AddScoped(contrato, tipo);
        }
        return services;
    }
}
