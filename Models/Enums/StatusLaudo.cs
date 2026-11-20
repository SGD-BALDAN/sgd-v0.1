namespace SgdBaldan.Models.Enums;

/// <summary>Projeto: EmAndamento → AguardandoOperador → AguardandoAprovacao. Legado: EmAndamento → AguardandoAprovacao. Depois: Aprovado ou Reprovado (volta a EmAndamento/Reprovado para refazer).</summary>
public enum StatusLaudo
{
    EmAndamento,
    AguardandoOperador,
    AguardandoAprovacao,
    Reprovado,
    Aprovado
}
