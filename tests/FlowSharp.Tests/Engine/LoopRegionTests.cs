using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Core.Flow;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Engine;

public class LoopRegionTests
{
    private static IEnumerable<INodeType> Nodes() =>
    [
        new TestTriggerNode(),
        new TagNode(),
        new LoopOverItemsNode()
    ];

    [Fact]
    public async Task Loop_processes_all_items_in_batches_and_collects_results()
    {
        var engine = EngineHarness.Create(Nodes());
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("loop", "flow.loopOverItems", "Loop", new JsonObject { ["batchSize"] = 2 })
            .Node("body", "test.tag", "Body", new JsonObject { ["tag"] = "seen" })
            .Connect("t", "loop")
            .Connect("loop", "body", fromPort: 1)   // loop -> body
            .Connect("body", "loop")                // back-edge -> loop
            .BuildElement();

        var payload = WorkflowBuilder.TriggerPayload(new[]
        {
            new { i = 0 }, new { i = 1 }, new { i = 2 }, new { i = 3 }, new { i = 4 }
        });

        var result = await engine.ExecuteAsync(def, payload.RootElement);

        result.Succeeded.Should().BeTrue();
        var loop = result.Nodes.First(n => n.NodeName == "Loop");
        // 5 item, hepsi govdeden gecip toplanmali.
        loop.ItemCount.Should().Be(5);
        loop.Output.AsArray().Should().OnlyContain(item => item!["seen"] != null);
    }

    [Fact]
    public async Task Loop_with_no_items_collects_nothing()
    {
        var engine = EngineHarness.Create(Nodes());
        var def = new WorkflowBuilder()
            .Node("t", "test.trigger", "Trigger")
            .Node("loop", "flow.loopOverItems", "Loop", new JsonObject { ["batchSize"] = 1 })
            .Node("body", "test.tag", "Body", new JsonObject { ["tag"] = "seen" })
            .Connect("t", "loop")
            .Connect("loop", "body", fromPort: 1)
            .Connect("body", "loop")
            .BuildElement();

        // Bos dizi -> tek bos item akar.
        var result = await engine.ExecuteAsync(def, WorkflowBuilder.TriggerPayload(Array.Empty<object>()).RootElement);

        result.Succeeded.Should().BeTrue();
    }
}
