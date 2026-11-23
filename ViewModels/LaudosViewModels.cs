using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.ViewModels;

/// <summary>Tela "Meus laudos" de um tipo (Liberação, Preventiva ou Corretiva).</summary>
public class LaudosListaViewModel
{
    public TipoLaudo Tipo { get; set; }
    public List<Laudo> Laudos { get; set; } = new();
    /// <summary>Em andamento (inclui os que aguardam o operador).</summary>
    public int EmAndamento { get; set; }
    public int AguardandoAprovacao { get; set; }
    public int Reprovados { get; set; }
    public int Aprovados { get; set; }
    /// <summary>Laudos do usuário que ocupam a fila deste tipo e o limite configurado.</summary>
    public int Fila { get; set; }
    public int LimiteFila { get; set; }
    public int PercentualFila => LimiteFila <= 0 ? 0 : Math.Min(100, (int)Math.Round(Fila * 100.0 / LimiteFila));
    public bool PodeCriar { get; set; }
    public bool EhOperador { get; set; }
}

/// <summary>
/// Formulário único dos laudos (criação e edição), usado pelas três telas (liberação, preventiva, corretiva).
/// As propriedades sem atributo vêm do formulário; as marcadas [BindNever] são calculadas no servidor.
/// </summary>
public class LaudoFormViewModel
{
    // ---------------------------------------------------------------- vindo do formulário
    [StringLength(50, ErrorMessage = "O número do dispositivo pode ter até 50 caracteres.")]
    public string? Codigo { get; set; }
    /// <summary>Só é exigido quando o número do dispositivo ainda não existe.</summary>
    public int? IdProduto { get; set; }
    public int? IdCategoria { get; set; }
    [Required(ErrorMessage = "Informe a ordem de serviço."), StringLength(30, ErrorMessage = "A ordem de serviço pode ter até 30 caracteres.")]
    public string? OrdemServico { get; set; }
    [DataType(DataType.Date)] public DateTime DataInicio { get; set; } = DateTime.Today;
    /// <summary>"salvar" ou "enviar" (botão clicado).</summary>
    public string? Acao { get; set; }

    public List<MedidaForm> Medidas { get; set; } = new();
    public List<ChecklistForm> Checklist { get; set; } = new();

    // Corretiva
    public string? DescricaoFalha { get; set; }
    public string? ComponenteAfetado { get; set; }
    public TipoFalha? TipoFalha { get; set; }
    public string? CausasSugeridas { get; set; }
    public string? OutrasInformacoes { get; set; }
    public string? CausaRaiz { get; set; }
    public List<string> CausasMetodo { get; set; } = new();
    public List<string> CausasMaquina { get; set; } = new();
    public List<string> CausasMaterial { get; set; } = new();
    public List<string> CausasMedicao { get; set; } = new();
    public List<string> CausasMeioAmbiente { get; set; } = new();
    public List<string> CausasMaoDeObra { get; set; } = new();
    public List<AcaoForm> Acoes { get; set; } = new();

    // Arquivos
    public IFormFile? Desenho { get; set; }
    public List<IFormFile> FotosOcorrencia { get; set; } = new();
    public List<IFormFile> FotosTrabalho { get; set; } = new();

    // ---------------------------------------------------------------- contexto (nunca vem do navegador)
    [BindNever, ValidateNever] public TipoLaudo Tipo { get; set; }
    [BindNever, ValidateNever] public ModoLaudo Modo { get; set; }
    [BindNever, ValidateNever] public StatusLaudo Status { get; set; } = StatusLaudo.EmAndamento;
    [BindNever, ValidateNever] public int? IdLaudo { get; set; }
    [BindNever, ValidateNever] public bool PodeEditarEstrutura { get; set; }
    [BindNever, ValidateNever] public bool PodePreencher { get; set; }
    [BindNever, ValidateNever] public bool PodeDecidir { get; set; }
    [BindNever, ValidateNever] public bool PodeReabrir { get; set; }
    [BindNever, ValidateNever] public bool PodeExcluir { get; set; }
    [BindNever, ValidateNever] public bool PodeReaproveitar { get; set; }
    [BindNever, ValidateNever] public bool PodeAssumir { get; set; }
    [BindNever, ValidateNever] public bool PodeRegistrarCustos { get; set; }
    [BindNever, ValidateNever] public string NomeResponsavel { get; set; } = "";
    [BindNever, ValidateNever] public string NomeConferente { get; set; } = "";
    [BindNever, ValidateNever] public string NomeProduto { get; set; } = "";
    [BindNever, ValidateNever] public string NomeCategoria { get; set; } = "";
    [BindNever, ValidateNever] public string? MotivoReprovacao { get; set; }
    [BindNever, ValidateNever] public decimal? CustoMaterial { get; set; }
    [BindNever, ValidateNever] public decimal? CustoMaoObra { get; set; }
    [BindNever, ValidateNever] public List<Produto> Produtos { get; set; } = new();
    [BindNever, ValidateNever] public List<Categoria> Categorias { get; set; } = new();
    [BindNever, ValidateNever] public List<Dispositivo> Dispositivos { get; set; } = new();
    [BindNever, ValidateNever] public Anexo? DesenhoAtual { get; set; }
    [BindNever, ValidateNever] public List<Anexo> FotosOcorrenciaAtuais { get; set; } = new();
    [BindNever, ValidateNever] public List<Anexo> FotosTrabalhoAtuais { get; set; } = new();

    public bool Novo => IdLaudo == null;
    /// <summary>Ninguém aqui altera nada: Gerente, Programador e quem só está consultando.</summary>
    public bool SomenteLeitura => !PodeEditarEstrutura && !PodePreencher;
    /// <summary>Em edição, o dispositivo (número, produto, categoria) não muda.</summary>
    public bool DispositivoTravado => !Novo;
    public bool PodeSalvar => PodeEditarEstrutura || PodePreencher;
    public bool PodeEnviar => PodeSalvar && Status != StatusLaudo.Reprovado;
}

public class MedidaForm
{
    public int IdMedida { get; set; }
    public int Numero { get; set; }
    public DescricaoMedida? Descricao { get; set; }
    public string? Especificacao { get; set; }
    public MetodoInstrumento? Metodo { get; set; }
    /// <summary>Texto digitado (vírgula ou ponto); convertido no servidor.</summary>
    public string? Medicao { get; set; }
    // Somente exibição
    [BindNever, ValidateNever] public decimal? LimiteMin { get; set; }
    [BindNever, ValidateNever] public decimal? LimiteMax { get; set; }
    [BindNever, ValidateNever] public ResultadoMedida Resultado { get; set; } = ResultadoMedida.Aguardando;
}

public class ChecklistForm
{
    public int IdChecklist { get; set; }
    public int Numero { get; set; }
    public string? Descricao { get; set; }
    public ResultadoChecklist Resultado { get; set; } = ResultadoChecklist.Aguardando;
}

public class AcaoForm
{
    public int Numero { get; set; }
    public string? Oque { get; set; }
    public string? Como { get; set; }
}

/// <summary>Dados de uma linha das tabelas/lista do formulário (o índice é texto para servir ao modelo "__I__").</summary>
public class LinhaMedidaModel
{
    public string Indice { get; set; } = "0";
    public MedidaForm Item { get; set; } = new();
    public bool EditaEstrutura { get; set; }
    public bool EditaMedicao { get; set; }
}

public class LinhaChecklistModel
{
    public string Indice { get; set; } = "0";
    public ChecklistForm Item { get; set; } = new();
    public bool EditaEstrutura { get; set; }
    public bool EditaResultado { get; set; }
}

public class LinhaAcaoModel
{
    public string Indice { get; set; } = "0";
    public AcaoForm Item { get; set; } = new();
    public bool Edita { get; set; }
}
