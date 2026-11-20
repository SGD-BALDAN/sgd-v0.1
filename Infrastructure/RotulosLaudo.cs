using SgdBaldan.Models.Enums;

namespace SgdBaldan.Infrastructure;

/// <summary>Textos e classes CSS usados para exibir os enums nas telas.</summary>
public static class RotulosLaudo
{
    public static string Rotulo(this StatusLaudo s) => s switch
    {
        StatusLaudo.EmAndamento => "Em andamento",
        StatusLaudo.AguardandoOperador => "Aguardando operador",
        StatusLaudo.AguardandoAprovacao => "Aguardando aprovação",
        StatusLaudo.Reprovado => "Refazer laudo",
        StatusLaudo.Aprovado => "Aprovado",
        _ => s.ToString()
    };

    /// <summary>Classe do selo (.pill--...) definida em laudos-lista.css.</summary>
    public static string ClassePill(this StatusLaudo s) => s switch
    {
        StatusLaudo.EmAndamento => "pill--info",
        StatusLaudo.AguardandoOperador => "pill--pend",
        StatusLaudo.AguardandoAprovacao => "pill--pend",
        StatusLaudo.Reprovado => "pill--redo",
        StatusLaudo.Aprovado => "pill--ok",
        _ => "pill--info"
    };

    /// <summary>Classe da bolinha (.dot--...) definida em dispositivos.css.</summary>
    public static string ClasseDot(this StatusLaudo s) => s switch
    {
        StatusLaudo.EmAndamento => "dot--andamento",
        StatusLaudo.AguardandoOperador => "dot--pendente",
        StatusLaudo.AguardandoAprovacao => "dot--pendente",
        StatusLaudo.Reprovado => "dot--reprovado",
        StatusLaudo.Aprovado => "dot--aprovado",
        _ => "dot--sem"
    };

    public static string Rotulo(this StatusDispositivo s) => s switch
    {
        StatusDispositivo.Pendente => "Aguardando aprovação do 1º laudo",
        StatusDispositivo.Ativo => "Ativo",
        StatusDispositivo.Inativo => "Inativo",
        _ => s.ToString()
    };

    public static string Rotulo(this TipoLaudo t) => t switch
    {
        TipoLaudo.Liberacao => "liberação",
        TipoLaudo.Preventiva => "preventiva",
        TipoLaudo.Corretiva => "corretiva",
        _ => t.ToString()
    };

    public static string Sigla(this TipoLaudo t) => t switch
    {
        TipoLaudo.Liberacao => "LIB",
        TipoLaudo.Preventiva => "PRE",
        TipoLaudo.Corretiva => "COR",
        _ => t.ToString()
    };

    /// <summary>Trecho usado na URL: liberacao, preventiva, corretiva.</summary>
    public static string Slug(this TipoLaudo t) => t.ToString().ToLowerInvariant();

    public static bool TentarObterTipo(string? slug, out TipoLaudo tipo) =>
        Enum.TryParse(slug, ignoreCase: true, out tipo) && Enum.IsDefined(typeof(TipoLaudo), tipo);

    public static string Rotulo(this DescricaoMedida d) => d switch
    {
        DescricaoMedida.Diametro => "Diâmetro",
        DescricaoMedida.Posicao => "Posição",
        _ => d.ToString()
    };

    public static string Rotulo(this MetodoInstrumento m) => m switch
    {
        MetodoInstrumento.Paquimetro => "Paquímetro",
        _ => m.ToString()
    };

    public static string Rotulo(this ResultadoMedida r) => r switch
    {
        ResultadoMedida.Aguardando => "Aguardando",
        ResultadoMedida.Aprovado => "Aprovado",
        ResultadoMedida.Reprovado => "Reprovado",
        _ => r.ToString()
    };

    public static string Rotulo(this ResultadoChecklist r) => r switch
    {
        ResultadoChecklist.Ok => "OK",
        ResultadoChecklist.NaoOk => "NÃO OK",
        ResultadoChecklist.Na => "N/A",
        _ => r.ToString()
    };

    public static string Rotulo(this TipoFalha t) => t switch
    {
        TipoFalha.DanoOperacional => "Dano operacional",
        TipoFalha.NaoConformidadeDimensional => "Não conformidade dimensional",
        _ => t.ToString()
    };

    public static string Rotulo(this CategoriaAnexo c) => c switch
    {
        CategoriaAnexo.DesenhoTecnico => "Desenho técnico",
        CategoriaAnexo.FotoDispositivo => "Foto do dispositivo",
        CategoriaAnexo.FotoOcorrencia => "Foto da situação atual",
        CategoriaAnexo.FotoTrabalho => "Foto do trabalho realizado",
        _ => c.ToString()
    };
}
