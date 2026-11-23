using System.Globalization;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;

namespace SgdBaldan.Services;

/// <summary>
/// Regras do ciclo de vida do laudo, separadas por perfil e sem acesso a banco/HTTP.
/// Tudo que a tela mostra (campos editáveis, botões) e tudo que o servidor aceita sai daqui.
///
///  • Projetista  → cria laudo modo PROJETO: define a estrutura (medidas/checklist) e envia ao operador. Não mede nem confere.
///  • Operacional → (a) assume laudo PROJETO da fila e preenche medições/checklist; 
///                  (b) cria laudo modo LEGADO: faz o papel do projetista E preenche os resultados.
///  • Programador → só aprova/reprova (e registra custos); não cria nem edita o conteúdo.
///  • Gerente     → somente leitura de todos os laudos.
/// </summary>
public static class LaudoFluxo
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // ------------------------------------------------------------------ perfis e modos

    /// <summary>Modo do laudo que o perfil pode criar. Null = o perfil não cria laudo.</summary>
    public static ModoLaudo? ModoDoPerfil(PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Projetista => ModoLaudo.Projeto,
        PerfilUsuario.Operacional => ModoLaudo.Legado,
        _ => null
    };

    public static bool PodeCriar(PerfilUsuario perfil) => ModoDoPerfil(perfil).HasValue;

    // ------------------------------------------------------------------ estados

    /// <summary>Laudo ainda na mesa de quem o criou (pode alterar a estrutura).</summary>
    public static bool LaudoAberto(Laudo l) => l.Status is StatusLaudo.EmAndamento or StatusLaudo.Reprovado;

    /// <summary>PROJETO enviado pelo projetista, aguardando um operador assumir.</summary>
    public static bool NaFilaDoOperador(Laudo l) =>
        l.Modo == ModoLaudo.Projeto && l.Status == StatusLaudo.AguardandoOperador && l.IdConferente == null;

    /// <summary>Status que ocupam a "fila" do usuário (limite por tipo).</summary>
    public static bool OcupaFila(StatusLaudo s) =>
        s is StatusLaudo.EmAndamento or StatusLaudo.AguardandoOperador or StatusLaudo.Reprovado;

    // ------------------------------------------------------------------ permissões

    /// <summary>No LEGADO o operador que criou o laudo é projetista + conferente enquanto o laudo está aberto.</summary>
    public static bool EhCriadorLegado(Laudo l, PerfilUsuario perfil, int idUsuario) =>
        perfil == PerfilUsuario.Operacional && l.Modo == ModoLaudo.Legado && l.IdConferente == idUsuario && LaudoAberto(l);

    /// <summary>Altera medidas, checklist, cabeçalho e dados da corretiva.</summary>
    public static bool PodeEditarEstrutura(Laudo l, PerfilUsuario perfil, int idUsuario) =>
        (perfil == PerfilUsuario.Projetista && l.IdResponsavelProjeto == idUsuario && LaudoAberto(l))
        || EhCriadorLegado(l, perfil, idUsuario);

    /// <summary>Preenche medições e resultados do checklist.</summary>
    public static bool PodePreencher(Laudo l, PerfilUsuario perfil, int idUsuario) =>
        (perfil == PerfilUsuario.Operacional && l.Modo == ModoLaudo.Projeto && l.IdConferente == idUsuario && l.Status == StatusLaudo.AguardandoOperador)
        || EhCriadorLegado(l, perfil, idUsuario);

    /// <summary>Abre o laudo (mesmo que só para leitura).</summary>
    public static bool PodeVer(Laudo l, PerfilUsuario perfil, int idUsuario) => perfil switch
    {
        PerfilUsuario.Programador or PerfilUsuario.Gerente => true,
        PerfilUsuario.Projetista => l.IdResponsavelProjeto == idUsuario,
        PerfilUsuario.Operacional => l.IdConferente == idUsuario || l.IdResponsavelProjeto == idUsuario || NaFilaDoOperador(l),
        _ => false
    };

    // ------------------------------------------------------------------ medidas

    /// <summary>Lê "nominal ± tolerância" (vírgula decimal pt-BR). Aceita também "+/-".</summary>
    public static bool TentarInterpretarEspecificacao(string? texto, out decimal minimo, out decimal maximo)
    {
        minimo = maximo = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        var partes = texto.Replace("+/-", "±").Split('±', StringSplitOptions.TrimEntries);
        const NumberStyles estilo = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (partes.Length != 2
            || !decimal.TryParse(partes[0], estilo, PtBr, out var nominal)
            || !decimal.TryParse(partes[1], estilo, PtBr, out var tolerancia)
            || tolerancia < 0) return false;
        minimo = nominal - tolerancia;
        maximo = nominal + tolerancia;
        return true;
    }

    /// <summary>Lê a medição digitada (aceita "50,5" e "50.5"). Vazio = sem medição (true com valor nulo).</summary>
    public static bool TentarLerMedicao(string? texto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(texto)) return true;
        var t = texto.Trim();
        // "1.234,5" → 1234.5 | "50,5" → 50.5 | "50.5" → 50.5
        t = t.Contains(',') ? t.Replace(".", "").Replace(',', '.') : t;
        if (!decimal.TryParse(t, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v)) return false;
        valor = v;
        return true;
    }

    /// <summary>Aprovação/reprovação sempre calculada no servidor.</summary>
    public static ResultadoMedida CalcularResultado(decimal? medicao, decimal? min, decimal? max)
    {
        if (!medicao.HasValue || !min.HasValue || !max.HasValue) return ResultadoMedida.Aguardando;
        return medicao.Value >= min.Value && medicao.Value <= max.Value ? ResultadoMedida.Aprovado : ResultadoMedida.Reprovado;
    }

    public static string Formatar(decimal? valor) => valor.HasValue ? valor.Value.ToString("0.####", PtBr) : string.Empty;

    // ------------------------------------------------------------------ envio

    /// <summary>
    /// Confere se o laudo pode ser enviado agora. Retorna null quando está tudo certo, ou a mensagem para o usuário.
    /// Projeto (1º envio): o projetista entrega só a estrutura. Legado / envio do operador: tudo medido e conferido.
    /// </summary>
    public static string? ValidarEnvio(Laudo l)
    {
        if (l.Status != StatusLaudo.EmAndamento && l.Status != StatusLaudo.AguardandoOperador)
            return l.Status == StatusLaudo.Reprovado
                ? "Reabra o laudo para correção antes de reenviar."
                : "Este laudo não pode ser enviado no status atual.";

        if (l.Medidas.Count == 0 && l.Checklist.Count == 0)
            return "Defina ao menos uma medida ou um item de checklist antes de enviar.";

        var envioDoProjetista = EnvioDoProjetista(l);
        var estruturaFeitaAqui = envioDoProjetista || l.Modo == ModoLaudo.Legado;
        if (estruturaFeitaAqui && l.Tipo == TipoLaudo.Corretiva && string.IsNullOrWhiteSpace(l.Corretiva?.DescricaoFalha))
            return "Descreva a falha encontrada antes de enviar.";

        if (envioDoProjetista) return null;

        if (l.Medidas.Any(x => x.Resultado == ResultadoMedida.Aguardando))
            return "Preencha todas as medições antes de enviar.";
        if (l.Checklist.Any(x => x.Resultado == ResultadoChecklist.Aguardando))
            return "Preencha todos os itens do checklist antes de enviar (OK, NÃO OK ou N/A).";
        return null;
    }

    /// <summary>Primeiro envio do PROJETO: vai para a fila dos operadores.</summary>
    public static bool EnvioDoProjetista(Laudo l) => l.Modo == ModoLaudo.Projeto && l.Status == StatusLaudo.EmAndamento;
}
