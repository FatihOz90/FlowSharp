using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlowSharp.Web.Services;

/// <summary>
/// JSON metin alanlari icin tek merkezden cagrilan UI yardimcisi. Gecerli JSON'u okunabilir
/// (girintili) hale getirir; gecersiz JSON'a DOKUNMAZ (kullanici verisini bozmaz). Ifade iceren
/// ama yine de gecerli JSON olan degerler de (orn. {"to":"{{$json.from}}"}) sorunsuz bicimlenir.
/// </summary>
public static class JsonText
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static string Beautify(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw ?? string.Empty;
        }

        try
        {
            var node = JsonNode.Parse(raw);
            return node?.ToJsonString(Indented) ?? raw;
        }
        catch (JsonException)
        {
            return raw; // Gecersiz JSON: oldugu gibi birak.
        }
    }
}
