using System.Text.RegularExpressions;

namespace SgdBaldan.Infrastructure;

public static class CoresHelper
{
    private static readonly Regex Hex = new(@"^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public static bool EhHexValido(string? cor) => cor != null && Hex.IsMatch(cor);

    /// <summary>Texto escuro sobre cores claras e branco sobre cores escuras.</summary>
    public static string TextoSobre(string? hex)
    {
        if (!EhHexValido(hex)) return "#ffffff";
        var r = Convert.ToInt32(hex!.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);
        return (0.299 * r + 0.587 * g + 0.114 * b) > 160 ? "#10151c" : "#ffffff";
    }
}
