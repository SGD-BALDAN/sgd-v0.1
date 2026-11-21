using Microsoft.EntityFrameworkCore;
using SgdBaldan.Data;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.Services;

public interface IAnexoService
{
    Task<Resultado<Anexo>> SalvarAsync(int idLaudo, CategoriaAnexo categoria, IFormFile arquivo, CancellationToken cancellationToken = default);
    Task<Anexo?> ObterAsync(int idAnexo, CancellationToken cancellationToken = default);
    Task<Stream?> AbrirAsync(Anexo anexo, CancellationToken cancellationToken = default);
}

public sealed class AnexoService : IAnexoService
{
    private const long TamanhoMaximo = 10 * 1024 * 1024;
    private readonly SgdDbContext _db;
    private readonly IWebHostEnvironment _ambiente;
    private readonly IConfiguration _config;

    public AnexoService(SgdDbContext db, IWebHostEnvironment ambiente, IConfiguration config)
    { _db = db; _ambiente = ambiente; _config = config; }

    public async Task<Resultado<Anexo>> SalvarAsync(int idLaudo, CategoriaAnexo categoria, IFormFile arquivo, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(categoria)) return Resultado<Anexo>.Falha("Categoria de anexo inválida.");
        if (arquivo == null || arquivo.Length == 0) return Resultado<Anexo>.Falha("Selecione um arquivo.");
        if (arquivo.Length > TamanhoMaximo) return Resultado<Anexo>.Falha("O arquivo deve ter no máximo 10 MB.");

        var laudo = await _db.Laudos.Include(x => x.Anexos).FirstOrDefaultAsync(x => x.IdLaudo == idLaudo, cancellationToken);
        if (laudo == null) return Resultado<Anexo>.Falha("Laudo não encontrado.");
        if (laudo.Status == StatusLaudo.Aprovado) return Resultado<Anexo>.Falha("Não é permitido alterar anexos de um laudo finalizado.");

        if (categoria == CategoriaAnexo.DesenhoTecnico && laudo.Modo != ModoLaudo.Projeto)
            return Resultado<Anexo>.Falha("Desenho técnico é permitido apenas no modo PROJETO.");
        if (categoria == CategoriaAnexo.FotoDispositivo && laudo.Modo != ModoLaudo.Legado)
            return Resultado<Anexo>.Falha("Foto do dispositivo é permitida apenas no modo LEGADO.");

        if (categoria is CategoriaAnexo.DesenhoTecnico or CategoriaAnexo.FotoDispositivo)
        {
            var outraPrincipal = laudo.Anexos.Any(x => x.Categoria is CategoriaAnexo.DesenhoTecnico or CategoriaAnexo.FotoDispositivo);
            if (outraPrincipal) return Resultado<Anexo>.Falha("Este laudo já possui um anexo principal (desenho técnico ou foto do dispositivo).");
        }

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (extensao is not ".jpg" and not ".jpeg" and not ".png" and not ".pdf")
            return Resultado<Anexo>.Falha("Formato não permitido. Envie JPEG, PNG ou PDF.");

        var cabecalho = new byte[8];
        await using (var entrada = arquivo.OpenReadStream())
        {
            var lidos = await entrada.ReadAsync(cabecalho.AsMemory(0, cabecalho.Length), cancellationToken);
            if (!AssinaturaValida(cabecalho, lidos, extensao)) return Resultado<Anexo>.Falha("O conteúdo do arquivo não corresponde à extensão informada.");
        }

        var mime = extensao switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", _ => "application/pdf" };
        var raiz = _config["Arquivos:Diretorio"];
        if (string.IsNullOrWhiteSpace(raiz)) raiz = Path.Combine(_ambiente.ContentRootPath, "App_Data", "Anexos");
        else if (!Path.IsPathRooted(raiz)) raiz = Path.Combine(_ambiente.ContentRootPath, raiz);
        Directory.CreateDirectory(raiz);

        var nomeFisico = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(raiz, nomeFisico);
        try
        {
            await using (var destino = new FileStream(caminhoCompleto, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await arquivo.CopyToAsync(destino, cancellationToken);

            var anexo = new Anexo
            {
                IdLaudo = idLaudo,
                Categoria = categoria,
                NomeOriginal = Path.GetFileName(arquivo.FileName),
                TipoMime = mime,
                TamanhoBytes = checked((int)arquivo.Length),
                CaminhoArquivo = nomeFisico,
                DataEnvio = DateTime.Now
            };
            _db.Anexos.Add(anexo);
            await _db.SaveChangesAsync(cancellationToken);
            return Resultado<Anexo>.Ok(anexo);
        }
        catch
        {
            if (File.Exists(caminhoCompleto)) File.Delete(caminhoCompleto);
            throw;
        }
    }

    public Task<Anexo?> ObterAsync(int idAnexo, CancellationToken cancellationToken = default) =>
        _db.Anexos.AsNoTracking().Include(x => x.Laudo)
            .FirstOrDefaultAsync(x => x.IdAnexo == idAnexo, cancellationToken);

    public Task<Stream?> AbrirAsync(Anexo anexo, CancellationToken cancellationToken = default)
    {
        var raiz = _config["Arquivos:Diretorio"];
        if (string.IsNullOrWhiteSpace(raiz)) raiz = Path.Combine(_ambiente.ContentRootPath, "App_Data", "Anexos");
        else if (!Path.IsPathRooted(raiz)) raiz = Path.Combine(_ambiente.ContentRootPath, raiz);
        var nome = Path.GetFileName(anexo.CaminhoArquivo);
        var caminho = Path.GetFullPath(Path.Combine(raiz, nome));
        var raizCompleta = Path.GetFullPath(raiz) + Path.DirectorySeparatorChar;
        if (!caminho.StartsWith(raizCompleta, StringComparison.OrdinalIgnoreCase) || !File.Exists(caminho))
            return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    private static bool AssinaturaValida(byte[] b, int n, string ext) => ext switch
    {
        ".jpg" or ".jpeg" => n >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
        ".png" => n >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A,
        ".pdf" => n >= 5 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46 && b[4] == 0x2D,
        _ => false
    };
}

