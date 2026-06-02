using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlowSharp.Tests.Fixtures;

/// <summary>
/// Motor testleri icin akici bir workflow tanim (nodes + connections JSON) olusturucusu.
/// </summary>
public sealed class WorkflowBuilder
{
    private readonly JsonArray nodes = [];
    private readonly JsonArray connections = [];

    public WorkflowBuilder Node(string id, string type, string? name = null, JsonObject? parameters = null)
    {
        nodes.Add(new JsonObject
        {
            ["id"] = id,
            ["type"] = type,
            ["name"] = name ?? id,
            ["parameters"] = parameters ?? new JsonObject()
        });
        return this;
    }

    public WorkflowBuilder Connect(string fromId, string toId, int fromPort = 0, int toPort = 0)
    {
        connections.Add(new JsonObject
        {
            ["from"] = fromId,
            ["fromPort"] = fromPort,
            ["to"] = toId,
            ["toPort"] = toPort
        });
        return this;
    }

    public JsonDocument Build()
    {
        var root = new JsonObject
        {
            ["nodes"] = nodes.DeepClone(),
            ["connections"] = connections.DeepClone()
        };
        return JsonDocument.Parse(root.ToJsonString());
    }

    public JsonElement BuildElement() => Build().RootElement;

    public static JsonDocument TriggerPayload(object? value = null) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value ?? new { source = "manual" }));
}
