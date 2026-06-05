using FlowSharp.Domain.Nodes;

namespace FlowSharp.Application.Nodes;

/// <summary>
/// <see cref="IHasDynamicOutputs"/> implemente eden node'lar icin tekrar kullanilabilir yardimcilar.
/// Provider'dan bagimsizdir; WhatsApp'a ozgu hicbir sey icermez.
/// </summary>
public static class DynamicOutputs
{
    /// <summary>
    /// Bir MultiSelect parametresinde secilen her secenek icin bir cikis portu uretir. Portlar
    /// <paramref name="options"/> sirasinda (kanonik) dondurulur; boylece secim degisse de mevcut
    /// port indeksleri korunur. Hicbir secim yoksa ilk secenek tek port olarak dondurulur
    /// (node'un sifir cikisi olmasin).
    /// </summary>
    public static IReadOnlyList<NodePort> FromMultiSelect(
        IReadOnlyDictionary<string, string> parameters,
        string parameterKey,
        IReadOnlyList<(string Value, string Label)> options)
    {
        var selected = ParseSelected(parameters.GetValueOrDefault(parameterKey));
        var ports = options
            .Where(option => selected.Contains(option.Value))
            .Select(option => NodePort.Named(option.Value, option.Label))
            .ToArray();

        return ports.Length > 0
            ? ports
            : [NodePort.Named(options[0].Value, options[0].Label)];
    }

    /// <summary>MultiSelect CSV degerini secili oge kumesine cevirir.</summary>
    public static HashSet<string> ParseSelected(string? csv) =>
        (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
