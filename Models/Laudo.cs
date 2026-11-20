using SgdBaldan.Models.Enums;

namespace SgdBaldan.Models;

public class Laudo
{
    public int IdLaudo { get; set; }
    public int IdDispositivo { get; set; }
    public Dispositivo Dispositivo { get; set; } = null!;
    public TipoLaudo Tipo { get; set; }
    public ModoLaudo Modo { get; set; }
    public StatusLaudo Status { get; set; } = StatusLaudo.EmAndamento;
    public DateTime DataInicio { get; set; } = DateTime.Today;
    public string OrdemServico { get; set; } = string.Empty;

    /// <summary>Projetista (modo Projeto). Nulo no modo Legado.</summary>
    public int? IdResponsavelProjeto { get; set; }
    public Usuario? ResponsavelProjeto { get; set; }
    /// <summary>Operador que confere/preenche. Nulo enquanto o projetista ainda não passou o laudo ao operador.</summary>
    public int? IdConferente { get; set; }
    public Usuario? Conferente { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;
    public DateTime? DataEnvio { get; set; }

    public decimal? CustoMaterial { get; set; }
    public decimal? CustoMaoObra { get; set; }

    public LaudoCorretiva? Corretiva { get; set; }
    public ICollection<MedidaEspecificacao> Medidas { get; set; } = new List<MedidaEspecificacao>();
    public ICollection<ItemChecklist> Checklist { get; set; } = new List<ItemChecklist>();
    public ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
    public ICollection<LaudoHistorico> Historico { get; set; } = new List<LaudoHistorico>();
}
