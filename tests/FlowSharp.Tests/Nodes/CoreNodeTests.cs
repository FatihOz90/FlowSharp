using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Core;
using FlowSharp.Nodes.Core.Logic;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class CoreNodeTests
{
    [Fact]
    public async Task SetNode_sets_static_and_expression_fields()
    {
        var node = new SetNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject
            {
                ["fields"] = new JsonObject { ["status"] = "active", ["greeting"] = "Hi {{ $json.name }}" }
            },
            items: [NodeItem.From(new JsonObject { ["name"] = "Ada" })]);

        var result = await node.ExecuteAsync(ctx);

        result.Succeeded.Should().BeTrue();
        var item = result.PrimaryItems.Single().Json;
        item["status"]!.GetValue<string>().Should().Be("active");
        item["greeting"]!.GetValue<string>().Should().Be("Hi Ada");
        item["name"]!.GetValue<string>().Should().Be("Ada"); // mevcut alan korunur
    }

    [Fact]
    public async Task SetNode_copies_nested_object_via_pure_expression()
    {
        // Regresyon: tek-ifade alan, item icindeki bir objeyi kopyalar.
        // Eskiden "node already has a parent" hatasi firlatiyordu.
        var node = new SetNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject
            {
                ["fields"] = new JsonObject { ["copy"] = "{{ $json.user }}" }
            },
            items: [NodeItem.From(new JsonObject
            {
                ["user"] = new JsonObject { ["name"] = "Ada" }
            })]);

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;
        item["copy"]!["name"]!.GetValue<string>().Should().Be("Ada");
    }

    [Fact]
    public async Task SetNode_keepOnlySet_drops_existing_fields()
    {
        var node = new SetNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject
            {
                ["fields"] = new JsonObject { ["only"] = "me" },
                ["keepOnlySet"] = true
            },
            items: [NodeItem.From(new JsonObject { ["old"] = "gone" })]);

        var result = await node.ExecuteAsync(ctx);

        var item = result.PrimaryItems.Single().Json;
        item.ContainsKey("old").Should().BeFalse();
        item["only"]!.GetValue<string>().Should().Be("me");
    }

    [Theory]
    [InlineData("active", "equals", "active", 1, 0)]
    [InlineData("active", "equals", "passive", 0, 1)]
    public async Task IfNode_routes_to_true_or_false_port(
        string value1, string op, string value2, int expectedTrue, int expectedFalse)
    {
        var node = new IfNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["value1"] = value1, ["operation"] = op, ["value2"] = value2 },
            items: [NodeItem.From(new JsonObject { ["x"] = 1 })]);

        var result = await node.ExecuteAsync(ctx);

        result.Succeeded.Should().BeTrue();
        result.Outputs[0].Should().HaveCount(expectedTrue);  // true port
        result.Outputs[1].Should().HaveCount(expectedFalse); // false port
    }

    [Fact]
    public async Task IfNode_splits_multiple_items_by_condition()
    {
        var node = new IfNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject
            {
                ["value1"] = "{{ $json.status }}", ["operation"] = "equals", ["value2"] = "ok"
            },
            items:
            [
                NodeItem.From(new JsonObject { ["status"] = "ok" }),
                NodeItem.From(new JsonObject { ["status"] = "no" }),
                NodeItem.From(new JsonObject { ["status"] = "ok" })
            ]);

        var result = await node.ExecuteAsync(ctx);

        result.Outputs[0].Should().HaveCount(2);
        result.Outputs[1].Should().HaveCount(1);
    }
}
