using System.Text.RegularExpressions;

namespace FlowSharp.Web.Logging;

/// <summary>
/// Log degerlerine uygulanan merkezi temizleme kurallari. <see cref="ScrubbingSink"/> tarafindan kullanilir.
/// </summary>
internal static partial class LogScrubber
{
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    /// <summary>Once e-postalari maskeler, ardindan kontrol karakterlerini temizler.</summary>
    public static string Scrub(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var masked = EmailRegex().Replace(value, static match => MaskEmail(match.Value));
        return StripControlChars(masked);
    }

    /// <summary>CR/LF dahil tum kontrol karakterlerini bosluga cevirir; log forging'i onler.</summary>
    private static string StripControlChars(string value)
    {
        var hasControl = false;
        foreach (var ch in value)
        {
            if (char.IsControl(ch) && ch != '\t')
            {
                hasControl = true;
                break;
            }
        }

        if (!hasControl)
        {
            return value;
        }

        return string.Create(value.Length, value, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var ch = source[i];
                span[i] = char.IsControl(ch) && ch != '\t' ? ' ' : ch;
            }
        });
    }

    /// <summary>E-postayi maskeler: <c>jo***@example.com</c>. Tanilamaya yeter, tam adresi sizdirmaz.</summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0)
        {
            return $"{email[0]}***";
        }

        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
