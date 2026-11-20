using SgdBaldan.Infrastructure;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.ViewModels;

public class DispositivoListaItemViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Produto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string CategoriaCor { get; set; } = "#5b6b82";
    public string TextoSobre => CoresHelper.TextoSobre(CategoriaCor);
    public StatusDispositivo Status { get; set; }
    public DateTime? UltimaAtualizacao { get; set; }
    /// <summary>Status do laudo mais recente (nulo = dispositivo sem laudo).</summary>
    public StatusLaudo? StatusMaisRecente { get; set; }

    public int AprovadosLiberacao { get; set; }
    public int AprovadosPreventiva { get; set; }
    public int AprovadosCorretiva { get; set; }
    /// <summary>Laudo mais recente de cada tipo (qualquer status), para abrir direto.</summary>
    public int? UltimoLiberacao { get; set; }
    public int? UltimoPreventiva { get; set; }
    public int? UltimoCorretiva { get; set; }

    public int Aprovados(TipoLaudo t) => t switch
    {
        TipoLaudo.Liberacao => AprovadosLiberacao,
        TipoLaudo.Preventiva => AprovadosPreventiva,
        _ => AprovadosCorretiva
    };

    public int? Ultimo(TipoLaudo t) => t switch
    {
        TipoLaudo.Liberacao => UltimoLiberacao,
        TipoLaudo.Preventiva => UltimoPreventiva,
        _ => UltimoCorretiva
    };
}

public class CategoriaOpcaoViewModel
{
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = "#5b6b82";
}

public class ResumoCategoriaViewModel
{
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = "#5b6b82";
    public int Quantidade { get; set; }
    public int Percentual { get; set; }
}

public class DispositivosIndexViewModel
{
    public List<DispositivoListaItemViewModel> Itens { get; set; } = new();
    public List<CategoriaOpcaoViewModel> Categorias { get; set; } = new();

    public List<ResumoCategoriaViewModel> ResumoPorCategoria() =>
        Itens.GroupBy(i => new { i.Categoria, i.CategoriaCor })
             .Select(g => new ResumoCategoriaViewModel
             {
                 Nome = g.Key.Categoria,
                 Cor = g.Key.CategoriaCor,
                 Quantidade = g.Count(),
                 Percentual = Itens.Count == 0 ? 0 : (int)Math.Round(g.Count() * 100.0 / Itens.Count)
             })
             .OrderByDescending(r => r.Quantidade).ThenBy(r => r.Nome)
             .ToList();
}

public class LaudoAprovadoItemViewModel
{
    public int IdLaudo { get; set; }
    public TipoLaudo Tipo { get; set; }
    public string OrdemServico { get; set; } = string.Empty;
    public string? Responsavel { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTime? AprovadoEm { get; set; }
}

public class MedidaLeituraViewModel
{
    public int Numero { get; set; }
    public string? Descricao { get; set; }
    public string? Especificacao { get; set; }
    public decimal? Minimo { get; set; }
    public decimal? Maximo { get; set; }
    public string? Metodo { get; set; }
    public decimal? Medicao { get; set; }
    public string Resultado { get; set; } = string.Empty;
}

public class ChecklistLeituraViewModel
{
    public int Numero { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
}

public class LiberacaoVigenteViewModel
{
    public int IdLaudo { get; set; }
    public DateTime DataInicio { get; set; }
    public string OrdemServico { get; set; } = string.Empty;
    public string? ResponsavelProjeto { get; set; }
    public string? Conferente { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTime? AprovadoEm { get; set; }
    public List<MedidaLeituraViewModel> Medidas { get; set; } = new();
    public List<ChecklistLeituraViewModel> Checklist { get; set; } = new();
}

public class DispositivoDetalheViewModel
{
    public int IdDispositivo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Produto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string CategoriaCor { get; set; } = "#5b6b82";
    public string TextoSobre => CoresHelper.TextoSobre(CategoriaCor);
    public StatusDispositivo Status { get; set; }
    public DateTime? UltimaAtualizacao { get; set; }
    public StatusLaudo? StatusMaisRecente { get; set; }
    /// <summary>Já existe laudo de liberação (em qualquer status)? Só pode haver um por dispositivo.</summary>
    public bool TemLiberacao { get; set; }
    public LiberacaoVigenteViewModel? LiberacaoVigente { get; set; }
    public List<LaudoAprovadoItemViewModel> Aprovados { get; set; } = new();
}
