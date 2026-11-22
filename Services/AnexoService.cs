using System.Text;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;
using SgdBaldan.Infrastructure;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.Services;

public interface IAnexoService
{
    /// <summary>Valida (tamanho e tipo real do conteúdo), grava o arquivo em disco e registra os metadados.</summary>
    Task<Resultado<Anexo>> SalvarAsync(int idLaudo, CategoriaAnexo categoria, IFormFile? arquivo, CancellationToken cancellationToken = default);

    /// <summary>Metadados do anexo (sem rastreamento do EF). Null se não existir.</summary>
    Task<Anexo?> ObterAsync(int idAnexo, CancellationToken cancellationToken = default);

    /// <summary>Abre o arquivo físico do anexo. Null se ele não estiver no armazenamento.</summary>
    Task<Stream?> AbrirAsync(Anexo anexo, CancellationToken cancellationToken = default);
}

/// <summary>
/// Anexos de laudo: JPEG, PNG ou PDF de até 10 MB. O tipo é identificado pelo conteúdo do arquivo
/// (não pela extensão nem pelo Content-Type enviados pelo navegador). Quem pode anexar é decidido no controller.
/// </summary>
public class AnexoService : IAnexoService
{
    public const int TamanhoMaximoBytes = 10 * 1024 * 1024;

    private readonly SgdDbContext _db;
    private readonly IArmazenamentoArquivos _arquivos;
    private readonly ILogger<AnexoService> _logger;

    public AnexoService(SgdDbContext db, IArmazenamentoArquivos arquivos, ILogger<AnexoService> logger)
    {
        _db = db;
        _arquivos = arquivos;
        _logger = logger;
    }

    public async Task<Resultado<Anexo>> SalvarAsync(int idLaudo, CategoriaAnexo categoria, IFormFile? arquivo, CancellationToken cancellationToken = default)
    {
        if (arquivo is null || arquivo.Length == 0)
            return Resultado<Anexo>.Falha("Selecione um arquivo para enviar.");
        if (!Enum.IsDefined(categoria))
            return Resultado<Anexo>.Falha("Categoria de anexo inválida.");
        if (arquivo.Length > TamanhoMaximoBytes)
            return Resultado<Anexo>.Falha("O arquivo excede o limite de 10 MB.");
        if (!await _db.Laudos.AnyAsync(l => l.IdLaudo == idLaudo, cancellationToken))
            return Resultado<Anexo>.Falha("Laudo não encontrado.");

        // Identifica o tipo pelos primeiros bytes do arquivo.
        var cabecalho = new byte[8];
        int lidos;
        await using (var leitura = arquivo.OpenReadStream())
            lidos = await leitura.ReadAtLeastAsync(cabecalho, cabecalho.Length, throwOnEndOfStream: false, cancellationToken);

        var tipo = DetectarTipo(cabecalho, lidos);
        if (tipo is null)
            return Resultado<Anexo>.Falha("Tipo de arquivo não permitido. Envie JPEG, PNG ou PDF.");

        string caminho;
        await using (var fluxo = arquivo.OpenReadStream())
            caminho = await _arquivos.SalvarAsync(fluxo, tipo.Value.Extensao, cancellationToken);

        var anexo = new Anexo
        {
            IdLaudo = idLaudo,
            Categoria = categoria,
            NomeOriginal = NormalizarNome(arquivo.FileName, tipo.Value.Extensao),
            TipoMime = tipo.Value.Mime,
            TamanhoBytes = (int)arquivo.Length,
            CaminhoArquivo = caminho,
            DataEnvio = DateTime.Now
        };

        try
        {
            _db.Anexos.Add(anexo);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Não deixa arquivo órfão no disco se o registro no banco falhar.
            _logger.LogError(ex, "Falha ao registrar o anexo do laudo {IdLaudo}; removendo o arquivo gravado.", idLaudo);
            try { await _arquivos.ExcluirAsync(caminho, CancellationToken.None); }
            catch (Exception exLimpeza) { _logger.LogWarning(exLimpeza, "Não foi possível remover o arquivo órfão {Caminho}.", caminho); }
            throw;
        }

        return Resultado<Anexo>.Ok(anexo);
    }

    public Task<Anexo?> ObterAsync(int idAnexo, CancellationToken cancellationToken = default) =>
        _db.Anexos.AsNoTracking().FirstOrDefaultAsync(a => a.IdAnexo == idAnexo, cancellationToken);

    public Task<Stream?> AbrirAsync(Anexo anexo, CancellationToken cancellationToken = default) =>
        _arquivos.AbrirAsync(anexo.CaminhoArquivo, cancellationToken);

    private static (string Extensao, string Mime)? DetectarTipo(byte[] b, int lidos)
    {
        if (lidos >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return ("jpg", "image/jpeg");
        if (lidos >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
                       && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return ("png", "image/png");
        if (lidos >= 5 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46 && b[4] == 0x2D)
            return ("pdf", "application/pdf");
        return null;
    }

    /// <summary>Nome exibido no download: sem caminho, sem caracteres problemáticos e com a extensão real do conteúdo.</summary>
    private static string NormalizarNome(string? nomeEnviado, string extensao)
    {
        var nome = nomeEnviado ?? string.Empty;
        var corte = nome.LastIndexOfAny(new[] { '/', '\\' });
        if (corte >= 0) nome = nome[(corte + 1)..];

        var proibidos = new HashSet<char>("\"<>|:*?");
        var limpo = new StringBuilder();
        foreach (var c in Path.GetFileNameWithoutExtension(nome))
            if (!char.IsControl(c) && !proibidos.Contains(c)) limpo.Append(c);

        var baseNome = limpo.ToString().Trim();
        if (baseNome.Length == 0) baseNome = "arquivo";

        // A coluna aceita 255 caracteres, contando a extensão.
        var sufixo = "." + extensao;
        if (baseNome.Length > 255 - sufixo.Length) baseNome = baseNome[..(255 - sufixo.Length)];
        return baseNome + sufixo;
    }
}
