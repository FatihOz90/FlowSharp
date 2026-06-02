using System.Text.Json.Nodes;
using FlowSharp.Application.Nodes;
using FlowSharp.Domain.Nodes;

namespace FlowSharp.Tests.Fixtures;

/// <summary>Tetik node'u: trigger item'larini oldugu gibi gecirir.</summary>
public sealed class TestTriggerNode : NodeType
{
    public override NodeDefinition Definition { get; } = new(
        Key: "test.trigger", DisplayName: "Test Trigger", Category: NodeCategory.Core,
        Kind: NodeKind.Trigger, Description: "test", Parameters: [], Tags: [], Icon: "play");

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context) =>
        Task.FromResult(NodeExecutionResult.Single(context.Items.ToList()));
}

/// <summary>Her item'a "tag" parametresinin degerini "seen" dizisine ekleyen izleyici node.</summary>
public sealed class TagNode : NodeType
{
    public override NodeDefinition Definition { get; } = new(
        Key: "test.tag", DisplayName: "Tag", Category: NodeCategory.Core,
        Kind: NodeKind.Transform, Description: "test", Parameters: [], Tags: [], Icon: "tag");

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context)
    {
        var tag = context.GetString("tag") ?? context.NodeName;
        var output = new List<NodeItem>();
        foreach (var item in context.Items)
        {
            var clone = (JsonObject)item.Json.DeepClone();
            var seen = clone["seen"] as JsonArray ?? [];
            seen.Add(tag);
            clone["seen"] = seen;
            output.Add(NodeItem.From(clone));
        }
        return Task.FromResult(NodeExecutionResult.Single(output));
    }
}

/// <summary>Her zaman hata donen node (error-path testleri icin).</summary>
public sealed class FailingNode : NodeType
{
    public override NodeDefinition Definition { get; } = new(
        Key: "test.fail", DisplayName: "Fail", Category: NodeCategory.Core,
        Kind: NodeKind.Transform, Description: "test", Parameters: [], Tags: [], Icon: "x");

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context) =>
        Task.FromResult(NodeExecutionResult.Failure("kasitli hata"));
}

/// <summary>
/// Dinamik secenekler ureten node: upstream item'larin "name" alanlarini secenek olarak doner.
/// LoadOptionsAsync yolunu test etmek icin.
/// </summary>
public sealed class DynamicOptionsNode : NodeType, IHasDynamicOptions
{
    public override NodeDefinition Definition { get; } = new(
        Key: "test.dynamic", DisplayName: "Dynamic", Category: NodeCategory.Core,
        Kind: NodeKind.Transform, Description: "test", Parameters: [], Tags: [], Icon: "list");

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context) =>
        Task.FromResult(NodeExecutionResult.Single(context.Items.ToList()));

    public Task<IReadOnlyList<NodeParameterOption>> LoadOptionsAsync(INodeExecutionContext context, string parameterKey)
    {
        IReadOnlyList<NodeParameterOption> options = context.Items
            .Select(i => i.Json["name"]?.ToString())
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => new NodeParameterOption(n!, n!))
            .ToList();
        return Task.FromResult(options);
    }
}

/// <summary>Sabit item listesi ureten kaynak node (loop girisi beslemek icin).</summary>
public sealed class EmitNode : NodeType
{
    public override NodeDefinition Definition { get; } = new(
        Key: "test.emit", DisplayName: "Emit", Category: NodeCategory.Core,
        Kind: NodeKind.Transform, Description: "test", Parameters: [], Tags: [], Icon: "plus");

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context)
    {
        // "count" parametresi kadar {n:i, name:"item-i"} item uretir.
        var count = context.GetInt("count", defaultValue: 3);
        var items = Enumerable.Range(0, count)
            .Select(i => NodeItem.From(new JsonObject { ["n"] = i, ["name"] = $"item-{i}" }))
            .ToList();
        return Task.FromResult(NodeExecutionResult.Single(items));
    }
}

/// <summary>Item'lari ikiye ayiran node: cift indeksler port 0, tek indeksler port 1.</summary>
public sealed class SplitNode : NodeType
{
    public override NodeDefinition Definition { get; } = new(
        Key: "test.split", DisplayName: "Split", Category: NodeCategory.Core,
        Kind: NodeKind.Condition, Description: "test", Parameters: [], Tags: [], Icon: "split",
        Outputs: [NodePort.Named("even", "Even"), NodePort.Named("odd", "Odd")]);

    public override Task<NodeExecutionResult> ExecuteAsync(INodeExecutionContext context)
    {
        var even = new List<NodeItem>();
        var odd = new List<NodeItem>();
        for (var i = 0; i < context.Items.Count; i++)
        {
            (i % 2 == 0 ? even : odd).Add(context.Items[i]);
        }
        return Task.FromResult(NodeExecutionResult.Multi([even, odd]));
    }
}
