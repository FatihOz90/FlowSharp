using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Application.Workflows;
using FlowSharp.Nodes.Core.Flow;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Engine;

public class EngineHotspotTests
{
    private static IEnumerable<INodeType> Nodes() =>
    [
        new TestTriggerNode(), new TagNode(), new EmitNode(),
        new DynamicOptionsNode(), new LoopOverItemsNode()
    ];

    // ---- LoadOptionsAsync (dinamik dropdown) ----
    [Fact]
    public async Task LoadOptions_runs_ancestors_and_returns_options_from_upstream()
    {
        var engine = EngineHarness.Create(Nodes());
        // LoadOptionsAsync kendi sabit trigger'ini kullanir; veri upstream node'lardan gelmeli.
        var def = new WorkflowBuilder()
            .Node("src", "test.emit", "Source", new JsonObject { ["count"] = 2 }) // item-0, item-1
            .Node("mid", "test.tag", "Mid", new JsonObject { ["tag"] = "x" })
            .Node("dyn", "test.dynamic", "Dropdown")
            .Connect("src", "mid").Connect("mid", "dyn")
            .BuildElement();

        var options = await engine.LoadOptionsAsync(def, "dyn", "anyParam");

        options.Select(o => o.Value).Should().Equal("item-0", "item-1");
    }

    [Fact]
    public async Task LoadOptions_returns_empty_when_node_not_dynamic()
    {
        var engine = EngineHarness.Create(Nodes());
        var def = new WorkflowBuilder()
            .Node("tag", "test.tag", "Tag")
            .BuildElement();

        var options = await engine.LoadOptionsAsync(def, "tag", "p");
        options.Should().BeEmpty();
    }

    // ---- Ic ice loop (nested loop region hesabi) ----
    [Fact]
    public async Task Nested_loops_process_all_items()
    {
        var engine = EngineHarness.Create(Nodes());
        // dis loop her item icin -> ic loop tekrar doner -> govde tag ekler.
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("outer", "flow.loopOverItems", "Outer", new JsonObject { ["batchSize"] = 1 })
            .Node("inner", "flow.loopOverItems", "Inner", new JsonObject { ["batchSize"] = 1 })
            .Node("body", "test.tag", "Body", new JsonObject { ["tag"] = "seen" })
            .Connect("t", "outer")
            .Connect("outer", "inner", fromPort: 1)   // outer loop -> inner
            .Connect("inner", "body", fromPort: 1)    // inner loop -> body
            .Connect("body", "inner")                 // body -> inner (back-edge)
            .Connect("inner", "outer", fromPort: 0)   // inner done -> outer (back-edge)
            .BuildElement();

        var payload = WorkflowBuilder.TriggerPayload(new[] { new { i = 1 }, new { i = 2 } });
        var result = await engine.ExecuteAsync(def, payload.RootElement);

        result.Succeeded.Should().BeTrue();
        result.Nodes.Should().Contain(n => n.NodeName == "Outer" && n.Status == Application.Workflows.NodeRunStatus.Succeeded);
    }

    // ---- StartNodeName secimi (options.StartNodeName dali) ----
    [Fact]
    public async Task Explicit_start_node_skips_earlier_nodes()
    {
        var engine = EngineHarness.Create(Nodes());
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("a", "test.tag", "A", new JsonObject { ["tag"] = "a" })
            .Node("b", "test.tag", "B", new JsonObject { ["tag"] = "b" })
            .Connect("t", "a").Connect("a", "b")
            .BuildElement();

        var result = await engine.ExecuteAsync(
            def, WorkflowBuilder.TriggerPayload().RootElement,
            new WorkflowExecutionOptions { StartNodeName = "B" });

        result.Succeeded.Should().BeTrue();
        result.Nodes.Should().Contain(n => n.NodeName == "B");
        result.Nodes.Should().NotContain(n => n.NodeName == "A");
    }
}
