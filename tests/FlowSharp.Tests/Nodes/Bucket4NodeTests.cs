using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Core.Logic;
using FlowSharp.Nodes.Data;
using FlowSharp.Nodes.Helpers;
using FlowSharp.Nodes.Http;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class Bucket4NodeTests
{
    private static FakeNodeExecutionContext Ctx(JsonObject parameters, params JsonObject[] items) =>
        new(parameters, items.Length == 0 ? [NodeItem.Empty()] : items.Select(NodeItem.From).ToList());

    // ---- ConditionEvaluator: kalan operatorler ----
    [Theory]
    [InlineData("abc", "notEquals", "xyz", true)]
    [InlineData("abc", "notContains", "z", true)]
    [InlineData("abc", "endsWith", "bc", true)]
    [InlineData("3", "lessThan", "5", true)]
    [InlineData("5", "greaterOrEqual", "5", true)]
    [InlineData("4", "lessOrEqual", "5", true)]
    [InlineData("false", "isFalse", "", true)]
    [InlineData("x", "bilinmeyenOperator", "y", false)]
    [InlineData("notnum", "greaterThan", "5", false)]
    public void ConditionEvaluator_remaining_operators(string left, string op, string right, bool expected) =>
        ConditionEvaluator.Evaluate(left, op, right).Should().Be(expected);

    // ---- HttpHelper.TryParseJson ----
    [Fact]
    public void HttpHelper_parses_valid_json()
    {
        var node = HttpHelper.TryParseJson("""{"ok":true}""");
        node!["ok"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void HttpHelper_wraps_non_json_as_value()
    {
        HttpHelper.TryParseJson("duz metin")!.GetValue<string>().Should().Be("duz metin");
    }

    [Fact]
    public void HttpHelper_null_or_empty_returns_null()
    {
        HttpHelper.TryParseJson(null).Should().BeNull();
        HttpHelper.TryParseJson("   ").Should().BeNull();
    }

    // ---- XmlJsonNode: jsonToXml yonu ----
    [Fact]
    public async Task XmlJson_jsonToXml_from_whole_item()
    {
        var node = new XmlJsonNode();
        var ctx = Ctx(new JsonObject
        {
            ["mode"] = "jsonToXml", ["rootElement"] = "kullanici", ["outputField"] = "xml"
        }, new JsonObject { ["ad"] = "Ada", ["yas"] = 30 });

        var xml = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json["xml"]!.GetValue<string>();
        xml.Should().Contain("<kullanici>").And.Contain("<ad>Ada</ad>").And.Contain("<yas>30</yas>");
    }

    [Fact]
    public async Task XmlJson_jsonToXml_from_named_source_field_with_array()
    {
        var node = new XmlJsonNode();
        var ctx = Ctx(new JsonObject
        {
            ["mode"] = "jsonToXml", ["sourceField"] = "liste", ["rootElement"] = "kok", ["outputField"] = "xml"
        }, new JsonObject { ["liste"] = new JsonArray("a", "b") });

        var xml = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json["xml"]!.GetValue<string>();
        xml.Should().Contain("<item>a</item>").And.Contain("<item>b</item>");
    }

    [Fact]
    public async Task XmlJson_xmlToJson_with_attributes_and_repeated_children()
    {
        var node = new XmlJsonNode();
        var ctx = Ctx(new JsonObject
        {
            ["mode"] = "xmlToJson",
            ["xml"] = "<root id='5'><tag>a</tag><tag>b</tag></root>",
            ["outputField"] = "j"
        }, new JsonObject());

        var j = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json["j"]!.AsObject();
        j["@id"]!.GetValue<string>().Should().Be("5");
        j["tag"]!.AsArray().Should().HaveCount(2);
    }

    // ---- WebhookResponseNode ----
    [Fact]
    public async Task WebhookResponse_shapes_status_body_headers()
    {
        var node = new WebhookResponseNode();
        var ctx = Ctx(new JsonObject
        {
            ["statusCode"] = "201",
            ["body"] = "tamam",
            ["headers"] = new JsonObject { ["X-Test"] = "1" }
        }, new JsonObject());

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;
        item["statusCode"]!.GetValue<int>().Should().Be(201);
        item["body"]!.GetValue<string>().Should().Be("tamam");
        item["headers"]!["X-Test"]!.GetValue<string>().Should().Be("1");
    }

    // ---- Aggregate: avg/min/max ----
    [Theory]
    [InlineData("avg", 20)]
    [InlineData("min", 10)]
    [InlineData("max", 30)]
    public async Task Aggregate_numeric_operations(string op, double expected)
    {
        var node = new AggregateNode();
        var ctx = Ctx(new JsonObject { ["operation"] = op, ["field"] = "v" },
            new JsonObject { ["v"] = "10" }, new JsonObject { ["v"] = "20" }, new JsonObject { ["v"] = "30" });

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;
        item[op]!.GetValue<double>().Should().Be(expected);
    }

    [Fact]
    public async Task Aggregate_collect_gathers_into_destination_field()
    {
        var node = new AggregateNode();
        var ctx = Ctx(new JsonObject { ["operation"] = "collect", ["destinationField"] = "hepsi" },
            new JsonObject { ["a"] = 1 }, new JsonObject { ["a"] = 2 });

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;
        item["hepsi"]!.AsArray().Should().HaveCount(2);
    }
}
