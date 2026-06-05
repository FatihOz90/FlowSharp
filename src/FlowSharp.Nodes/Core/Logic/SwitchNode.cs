using System.Text.Json.Nodes;
using FlowSharp.Application.Nodes;
using FlowSharp.Domain.Nodes;

namespace FlowSharp.Nodes.Core.Logic;

/// <summary>
/// Item'lari kurallara gore dallandirir. "rules" parametresi [{"value":"A","label":"opsiyonel"}, ...]
/// formatinda bir JSON dizisidir. HER KURAL BIR CIKIS PORTU uretir (sirayla); kuraldaki "label"
/// verilirse port o adla, yoksa "value" ile gosterilir (<see cref="IHasDynamicOutputs"/>). value1 ile
/// esit olan ilk kuralin portuna gonderilir; hicbir kurala uymayan item'lar sondaki "Fallback" portuna gider.
/// </summary>
public sealed class SwitchNode : NodeType, IHasDynamicOutputs
{
    private const string RulesParam = "rules";
    private const string DefaultRules = "[{\"value\":\"a\",\"label\":\"A\"},{\"value\":\"b\",\"label\":\"B\"}]";

    public override NodeDefinition Definition { get; } = new(
        Key: "switch.condition",
        DisplayName: "Switch",
        Category: NodeCategory.Core,
        Kind: NodeKind.Condition,
        Description: "Esleyen kurala gore item'lari farkli cikislara yonlendirir.",
        Parameters:
        [
            new NodeParameterDefinition("value1", "Value", NodeParameterType.String, IsRequired: true,
                HelpText: "Kurallarla karsilastirilacak deger. Ornek: {{$json.status}}"),
            new NodeParameterDefinition(RulesParam, "Rules (JSON)", NodeParameterType.Json,
                DefaultValue: DefaultRules,
                HelpText: "Her kural bir cikis portu olur (sirayla). {\"value\":\"...\",\"label\":\"opsiyonel ad\"}. " +
                          "label yoksa value gosterilir. Hicbirine uymayanlar Fallback'e gider.")
        ],
        Tags: ["core", "logic"],
        Icon: "bezier2",
        Color: "#408000",
        // Statik varsayilan (katalog icin); gercek portlar GetOutputs ile instance'a gore uretilir.
        Outputs: BuildPorts(ParseRules(DefaultRules)));

    public IReadOnlyList<NodePort> GetOutputs(IReadOnlyDictionary<string, string> parameters) =>
        BuildPorts(ParseRules(parameters.GetValueOrDefault(RulesParam)));

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context)
    {
        var rules = ParseRules(context.GetString(RulesParam));
        var fallbackPort = rules.Count; // Fallback her zaman son port.

        var outputs = new List<NodeItem>[rules.Count + 1];
        for (var i = 0; i < outputs.Length; i++)
        {
            outputs[i] = [];
        }

        var items = context.Items.Count > 0 ? context.Items : [NodeItem.Empty()];
        for (var index = 0; index < items.Count; index++)
        {
            var value1 = context.GetString("value1", index) ?? string.Empty;
            var matched = rules.FindIndex(rule =>
                string.Equals(rule.Value, value1, StringComparison.OrdinalIgnoreCase));

            outputs[matched >= 0 ? matched : fallbackPort].Add(items[index]);
        }

        return Task.FromResult(NodeExecutionResult.Multi(outputs));
    }

    /// <summary>Kurallardan cikis portlarini uretir: her kural icin bir port + sonda Fallback.</summary>
    private static IReadOnlyList<NodePort> BuildPorts(List<Rule> rules)
    {
        var ports = rules
            .Select((rule, i) => NodePort.Named(i.ToString(),
                !string.IsNullOrWhiteSpace(rule.Label) ? rule.Label!
                : !string.IsNullOrWhiteSpace(rule.Value) ? rule.Value!
                : $"Output {i + 1}"))
            .ToList();

        ports.Add(NodePort.Named("fallback", "Fallback (eslesmeyen)"));
        return ports;
    }

    private static List<Rule> ParseRules(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            if (JsonNode.Parse(raw) is JsonArray array)
            {
                return array.OfType<JsonObject>()
                    .Select(o => new Rule(
                        o.TryGetPropertyValue("value", out var v) ? v?.ToString() : null,
                        o.TryGetPropertyValue("label", out var l) ? l?.ToString() : null))
                    .ToList();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Gecersiz JSON: kural yok kabul edilir (her sey Fallback'e gider).
        }

        return [];
    }

    private readonly record struct Rule(string? Value, string? Label);
}
