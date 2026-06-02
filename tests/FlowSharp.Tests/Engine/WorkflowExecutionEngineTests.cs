using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Application.Workflows;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Engine;

public class WorkflowExecutionEngineTests
{
    private static JsonObject Tag(string value) => new() { ["tag"] = value };

    [Fact]
    public async Task Runs_linear_chain_in_topological_order()
    {
        var engine = EngineHarness.Create();
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("a", "test.tag", "A", Tag("a"))
            .Node("b", "test.tag", "B", Tag("b"))
            .Connect("t", "a").Connect("a", "b")
            .BuildElement();

        var result = await engine.ExecuteAsync(def, WorkflowBuilder.TriggerPayload(new[] { new { id = 1 } }).RootElement);

        result.Succeeded.Should().BeTrue();
        result.Nodes.Should().HaveCount(3);
        var last = result.Nodes[^1];
        last.NodeName.Should().Be("B");
        last.Output[0]!["seen"]!.AsArray().Select(x => x!.GetValue<string>())
            .Should().Equal("a", "b");
    }

    [Fact]
    public async Task CaptureData_false_keeps_dataflow_but_omits_heavy_output()
    {
        var engine = EngineHarness.Create();
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("a", "test.tag", "A", Tag("a"))
            .Node("b", "test.tag", "B", Tag("b"))
            .Connect("t", "a").Connect("a", "b")
            .BuildElement();

        var result = await engine.ExecuteAsync(
            def, WorkflowBuilder.TriggerPayload(new[] { new { id = 1 } }).RootElement,
            new WorkflowExecutionOptions { CaptureData = false });

        // Akis dogru calismis olmali (B, A'dan sonra; her ikisi de gormus).
        result.Succeeded.Should().BeTrue();
        result.Nodes.Should().HaveCount(3);
        var last = result.Nodes[^1];
        last.NodeName.Should().Be("B");
        last.ItemCount.Should().Be(1);              // metadata korunur
        last.Output.AsArray().Should().BeEmpty();    // agir veri klonlanmadi
        result.Output["main"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Failing_node_stops_execution_and_reports_error()
    {
        var engine = EngineHarness.Create();
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("f", "test.fail", "Boom")
            .Node("a", "test.tag", "After", Tag("after"))
            .Connect("t", "f").Connect("f", "a")
            .BuildElement();

        var result = await engine.ExecuteAsync(def, WorkflowBuilder.TriggerPayload().RootElement);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("kasitli hata");
        result.Nodes.Should().NotContain(n => n.NodeName == "After");
    }

    [Fact]
    public async Task Multi_output_node_routes_items_to_correct_ports()
    {
        var engine = EngineHarness.Create();
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("s", "test.split", "Split")
            .Node("e", "test.tag", "EvenSink", Tag("even"))
            .Node("o", "test.tag", "OddSink", Tag("odd"))
            .Connect("t", "s")
            .Connect("s", "e", fromPort: 0)
            .Connect("s", "o", fromPort: 1)
            .BuildElement();

        var payload = WorkflowBuilder.TriggerPayload(new[]
        {
            new { i = 0 }, new { i = 1 }, new { i = 2 }, new { i = 3 }
        });

        var result = await engine.ExecuteAsync(def, payload.RootElement);

        result.Succeeded.Should().BeTrue();
        var even = result.Nodes.First(n => n.NodeName == "EvenSink");
        var odd = result.Nodes.First(n => n.NodeName == "OddSink");
        even.ItemCount.Should().Be(2);
        odd.ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task Unreachable_node_is_skipped()
    {
        var engine = EngineHarness.Create();
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("a", "test.tag", "Reached", Tag("a"))
            .Node("orphan", "test.tag", "Orphan", Tag("x"))
            .Connect("t", "a")
            .BuildElement();

        var result = await engine.ExecuteAsync(def, WorkflowBuilder.TriggerPayload().RootElement);

        result.Succeeded.Should().BeTrue();
        result.Nodes.Should().NotContain(n => n.NodeName == "Orphan");
    }

    [Fact]
    public async Task Unknown_node_type_passes_through_as_skipped()
    {
        var engine = EngineHarness.Create();
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("u", "does.not.exist", "Ghost")
            .Connect("t", "u")
            .BuildElement();

        var result = await engine.ExecuteAsync(def, WorkflowBuilder.TriggerPayload(new[] { new { id = 1 } }).RootElement);

        result.Succeeded.Should().BeTrue();
        var ghost = result.Nodes.First(n => n.NodeName == "Ghost");
        ghost.Status.Should().Be(NodeRunStatus.Skipped);
    }

    [Fact]
    public async Task Merge_combines_inputs_from_multiple_sources()
    {
        var engine = EngineHarness.Create();
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("s", "test.split", "Split")
            .Node("m", "test.tag", "Merge", Tag("merged"))
            .Connect("t", "s")
            .Connect("s", "m", fromPort: 0)
            .Connect("s", "m", fromPort: 1)
            .BuildElement();

        var payload = WorkflowBuilder.TriggerPayload(new[] { new { i = 0 }, new { i = 1 }, new { i = 2 } });
        var result = await engine.ExecuteAsync(def, payload.RootElement);

        result.Succeeded.Should().BeTrue();
        result.Nodes.First(n => n.NodeName == "Merge").ItemCount.Should().Be(3);
    }
}
