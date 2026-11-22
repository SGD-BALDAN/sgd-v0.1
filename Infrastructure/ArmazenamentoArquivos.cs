using Microsoft.Extensions.Options;

namespace SgdBaldan.Infrastructure;

/// <summary>Opções de armazenamento (seção "Armazenamento" do appsettings).</summary>
public class ArmazenamentoOptions
{
    public const string Secao = "Armazenamento";

    /// <summary>
    /// Pasta onde os anexos são gravados. Caminho relativo é resolvido a partir da raiz da aplicação.
    /// Em produção, prefira um caminho absoluto (variável de ambiente Armazenamento__Pasta).
    /// </summary>
    public string? Pasta { get; set; }
}

/// <summary>Grava, lê e apaga arquivos em disco. O banco guarda apenas o caminho relativo devolvido por <see cref="SalvarAsync"/>.</summary>
public interface IArmazenamentoArquivos
{
    /// <summary>Grava o conteúdo com nome gerado (GUID) e devolve o caminho relativo (usa "/" como separador).</summary>
    Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken cancellationToken = default);

    /// <summary>Abre o arquivo para leitura. Devolve null se ele não existir.</summary>
    Task<Stream?> AbrirAsync(string caminho, CancellationToken cancellationToken = default);

    /// <summary>Apaga o arquivo (não faz nada se ele não existir).</summary>
    Task ExcluirAsync(string caminho, CancellationToken cancellationToken = default);
}

public class ArmazenamentoArquivos : IArmazenamentoArquivos
{
    private static readonly StringComparison Comparacao =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _pastaBase;

    public ArmazenamentoArquivos(IOptions<ArmazenamentoOptions> opcoes, IWebHostEnvironment ambiente)
    {
        var pasta = string.IsNullOrWhiteSpace(opcoes.Value.Pasta)
            ? Path.Combine("App_Data", "anexos")
            : opcoes.Value.Pasta.Trim();

        _pastaBase = Path.GetFullPath(
            Path.IsPathRooted(pasta) ? pasta : Path.Combine(ambiente.ContentRootPath, pasta));

        // Arquivos de laudo nunca podem ficar acessíveis como arquivo estático.
        if (!string.IsNullOrEmpty(ambiente.WebRootPath))
        {
            var webRoot = Path.GetFullPath(ambiente.WebRootPath).TrimEnd(Path.DirectorySeparatorChar);
            if (_pastaBase.Equals(webRoot, Comparacao) ||
                _pastaBase.StartsWith(webRoot + Path.DirectorySeparatorChar, Comparacao))
            {
                throw new InvalidOperationException(
                    "A pasta de armazenamento de anexos não pode ficar dentro do wwwroot. Ajuste 'Armazenamento:Pasta'.");
            }
        }
    }

    public async Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken cancellationToken = default)
    {
        extensao = (extensao ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (extensao.Length is < 1 or > 5 || !extensao.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9'))
            throw new ArgumentException("Extensão inválida.", nameof(extensao));

        var agora = DateTime.Now;
        var relativo = $"{agora:yyyy}/{agora:MM}/{Guid.NewGuid():N}.{extensao}";
        var completo = Resolver(relativo);

        Directory.CreateDirectory(Path.GetDirectoryName(completo)!);
        try
        {
            await using var destino = new FileStream(completo, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, FileOptions.Asynchronous);
            await conteudo.CopyToAsync(destino, cancellationToken);
        }
        catch
        {
            // Não deixa arquivo parcial no disco.
            if (File.Exists(completo)) File.Delete(completo);
            throw;
        }
        return relativo;
    }

    public Task<Stream?> AbrirAsync(string caminho, CancellationToken cancellationToken = default)
    {
        var completo = Resolver(caminho);
        if (!File.Exists(completo)) return Task.FromResult<Stream?>(null);

        Stream fluxo = new FileStream(completo, FileMode.Open, FileAccess.Read,
            FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(fluxo);
    }

    public Task ExcluirAsync(string caminho, CancellationToken cancellationToken = default)
    {
        var completo = Resolver(caminho);
        if (File.Exists(completo)) File.Delete(completo);
        return Task.CompletedTask;
    }

    /// <summary>Converte o caminho relativo em absoluto e garante que ele continua dentro da pasta base.</summary>
    private string Resolver(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            throw new ArgumentException("Caminho do arquivo não informado.", nameof(caminho));

        var completo = Path.GetFullPath(
            Path.Combine(_pastaBase, caminho.Replace('/', Path.DirectorySeparatorChar)));

        if (!completo.StartsWith(_pastaBase + Path.DirectorySeparatorChar, Comparacao))
            throw new InvalidOperationException("Caminho de arquivo inválido.");

        return completo;
    }
}
