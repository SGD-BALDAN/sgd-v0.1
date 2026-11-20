using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

/// <summary>Metadados de um arquivo (desenho técnico ou foto). O arquivo fica em disco; aqui só o caminho.</summary>
public class Anexo
{
    public int IdAnexo { get; set; }
    public int IdLaudo { get; set; }
    public Laudo Laudo { get; set; } = null!;
    public CategoriaAnexo Categoria { get; set; }
    public string NomeOriginal { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public int TamanhoBytes { get; set; }
    public string CaminhoArquivo { get; set; } = string.Empty;
    public DateTime DataEnvio { get; set; } = DateTime.Now;
}
