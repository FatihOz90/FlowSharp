using System.Text.Json.Nodes;
using FlowSharp.Application.Nodes;
using FlowSharp.Domain.Nodes;

namespace FlowSharp.Nodes.Communication.WhatsApp;

/// <summary>
/// WhatsApp Cloud API webhook tetikleyicisi. "events" multiselect'inde secilen her event turu
/// (messages / statuses) icin ayri bir cikis portu uretir (<see cref="IHasDynamicOutputs"/>) ve
/// gelen payload'i ilgili porta, eleman basina item olarak yonlendirir. Boylece kullanici her
/// event'i dogrudan kendi daline baglar; Switch'e gerek kalmaz.
/// </summary>
public sealed class WhatsAppTriggerNode : NodeType, IHasDynamicOutputs
{
    public const string NodeKey = "whatsapp.trigger";

    // Bu node'a OZGU olan tek sey: hangi event'ler var ve etiketleri. Multiselect->port mantigi
    // generic DynamicOutputs yardimcisindadir. Sira kanoniktir (secim degisse de indeks korunur).
    private const string EventsParam = "events";
    private static readonly (string Value, string Label)[] EventOptions =
    [
        ("messages", "Messages"),
        ("statuses", "Statuses")
    ];

    public override NodeDefinition Definition { get; } = new(
        Key: NodeKey,
        DisplayName: "WhatsApp Trigger",
        Category: NodeCategory.Trigger,
        Kind: NodeKind.Trigger,
        Description: "WhatsApp Cloud API'den gelen mesaj/durum geldiginde workflow'u baslatir.",
        Parameters:
        [
            new NodeParameterDefinition("path", "Path", NodeParameterType.String, IsRequired: true,
                DefaultValue: "whatsapp", HelpText: "Webhook URL: /webhook/{workflowKey}/{path}"),
            new NodeParameterDefinition("verifyToken", "Verify Token", NodeParameterType.String,
                HelpText: "Meta panelinde webhook kurulumunda girilen dogrulama anahtari."),
            new NodeParameterDefinition(EventsParam, "Events", NodeParameterType.MultiSelect, DefaultValue: "messages",
                Options: ["messages", "statuses"],
                HelpText: "Her secili event icin ayri bir cikis portu olusur. messages: gelen mesajlar. statuses: teslim/okundu.")
        ],
        Tags: ["trigger", "communication"],
        Icon: "message-circle",
        Color: "#25d366");

    // Generic mekanizma: multiselect -> port basina cikis. WhatsApp'a ozgu bilgi yalnizca EventOptions.
    public IReadOnlyList<NodePort> GetOutputs(IReadOnlyDictionary<string, string> parameters) =>
        DynamicOutputs.FromMultiSelect(parameters, EventsParam, EventOptions);

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context)
    {
        var ports = GetOutputs(new Dictionary<string, string> { [EventsParam] = context.GetString(EventsParam) ?? "" });

        // WhatsApp'a OZGU eslemenin tek yeri: normalize edilmis payload (whatsapp.<event>[]) dizilerini
        // port adina gore (port.Name == event adi) eleman basina item olarak ilgili porta dagit.
        var whatsapp = (context.Items.FirstOrDefault()?.Json as JsonObject)?["whatsapp"] as JsonObject;

        var outputs = ports.Select(port =>
        {
            var array = whatsapp?[port.Name] as JsonArray;
            return array is null
                ? (IReadOnlyList<NodeItem>)[]
                : array.OfType<JsonObject>().Select(item => NodeItem.From(item.DeepClone().AsObject())).ToArray();
        }).ToArray();

        return Task.FromResult(NodeExecutionResult.Multi(outputs));
    }
}
