using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

public class Dispositivo
{
    public int IdDispositivo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int IdProduto { get; set; }
    public Produto Produto { get; set; } = null!;
    public int IdCategoria { get; set; }
    public Categoria Categoria { get; set; } = null!;
    /// <summary>Nasce Pendente no 1º laudo e vira Ativo quando o Programador aprova.</summary>
    public StatusDispositivo Status { get; set; } = StatusDispositivo.Pendente;
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public ICollection<Laudo> Laudos { get; set; } = new List<Laudo>();

    public void Ativar() => Status = StatusDispositivo.Ativo;
    public void Inativar() => Status = StatusDispositivo.Inativo;
}
