using System.Text.Json;
using System.Text.Json.Nodes;
using FlowSharp.Application.Nodes;
using FlowSharp.Application.Nodes.Agents;
using FlowSharp.Infrastructure.Workflows;
using FlowSharp.Infrastructure.Workflows.Expressions;
using FlowSharp.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowSharp.Benchmarks;

/// <summary>Benchmark'larda gercek node'larla bir motor kurmak icin yardimcilar.</summary>
internal static class Support
{
    public static WorkflowExecutionEngine BuildEngine(IEnumerable<INodeType> nodes)
    {
        var registry = new NodeRegistry(nodes);
        var services = new ServiceCollection();
        services.AddHttpClient("workflow-nodes");
        return new WorkflowExecutionEngine(
            registry, new ExpressionEvaluator(), new NoopAgentExecutor(),
            services.BuildServiceProvider(), NullLogger<WorkflowExecutionEngine>.Instance);
    }

    public static JsonDocument Definition(JsonArray nodes, JsonArray connections) =>
        JsonDocument.Parse(new JsonObject { ["nodes"] = nodes, ["connections"] = connections }.ToJsonString());

    public static JsonObject Node(string id, string type, JsonObject? parameters = null) => new()
    {
        ["id"] = id, ["type"] = type, ["name"] = id, ["parameters"] = parameters ?? new JsonObject()
    };

    public static JsonObject Conn(string from, string to, int fromPort = 0) => new()
    {
        ["from"] = from, ["fromPort"] = fromPort, ["to"] = to, ["toPort"] = 0
    };

    /// <summary>N adet { i, value } item iceren bir trigger payload uretir.</summary>
    public static JsonDocument Payload(int count)
    {
        var array = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            array.Add(new JsonObject { ["i"] = i, ["value"] = $"item-{i}" });
        }
        return JsonDocument.Parse(array.ToJsonString());
    }

    private sealed class NoopAgentExecutor : IAgentExecutor
    {
        public Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(AgentResult.Ok(NodeItem.Empty()));
    }
}
