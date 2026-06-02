using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Core.Flow;
using FlowSharp.Nodes.Core.Logic;
using FlowSharp.Nodes.Data;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class FlowAndHtmlNodeTests
{
    private static FakeNodeExecutionContext Ctx(JsonObject parameters, params JsonObject[] items) =>
        new(parameters, items.Length == 0 ? [NodeItem.Empty()] : items.Select(NodeItem.From).ToList());

    // ---- Merge ----
    [Fact]
    public async Task Merge_passes_all_input_items_through()
    {
        var node = new MergeNode();
        var ctx = Ctx(new JsonObject(),
            new JsonObject { ["a"] = 1 }, new JsonObject { ["b"] = 2 });

        var items = (await node.ExecuteAsync(ctx)).PrimaryItems;
        items.Should().HaveCount(2);
    }

    // ---- StopAndError ----
    [Fact]
    public async Task StopAndError_throws_with_message()
    {
        var node = new StopAndErrorNode();
        var ctx = Ctx(new JsonObject { ["message"] = "ozel hata" }, new JsonObject());

        var act = async () => await node.ExecuteAsync(ctx);
        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("ozel hata");
    }

    // ---- Wait (0 saniye -> aninda gecer) ----
    [Fact]
    public async Task Wait_zero_seconds_passes_items_through()
    {
        var node = new WaitNode();
        var ctx = Ctx(new JsonObject { ["seconds"] = "0" }, new JsonObject { ["x"] = 1 });

        var items = (await node.ExecuteAsync(ctx)).PrimaryItems;
        items.Single().Json["x"]!.GetValue<int>().Should().Be(1);
    }

    // ---- HtmlExtract ----
    [Fact]
    public async Task HtmlExtract_text_via_css_selector()
    {
        var node = new HtmlExtractNode();
        var ctx = Ctx(new JsonObject
        {
            ["html"] = "<div><h1>Baslik</h1><span class='price'>99 TL</span></div>",
            ["selector"] = ".price", ["property"] = "text", ["outputField"] = "p"
        }, new JsonObject());

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;
        item["p"]!.GetValue<string>().Should().Be("99 TL");
    }

    [Fact]
    public async Task HtmlExtract_attribute_and_all_matches()
    {
        var node = new HtmlExtractNode();
        var ctx = Ctx(new JsonObject
        {
            ["html"] = "<ul><a href='/a'>A</a><a href='/b'>B</a></ul>",
            ["selector"] = "a", ["property"] = "attribute", ["attribute"] = "href",
            ["all"] = "true", ["outputField"] = "links"
        }, new JsonObject());

        var links = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json["links"]!.AsArray();
        links.Select(x => x!.GetValue<string>()).Should().Equal("/a", "/b");
    }

    // ---- HtmlToText ----
    [Fact]
    public async Task HtmlToText_strips_tags_to_plain_text()
    {
        var node = new HtmlToTextNode();
        var ctx = Ctx(new JsonObject
        {
            ["html"] = "<p>Merhaba <b>dunya</b></p><script>ignored()</script>",
            ["format"] = "text", ["outputField"] = "t"
        }, new JsonObject());

        var text = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json["t"]!.GetValue<string>();
        text.Should().Contain("Merhaba dunya");
        text.Should().NotContain("ignored");
    }

    [Fact]
    public async Task HtmlToText_markdown_renders_links_and_headings()
    {
        var node = new HtmlToTextNode();
        var ctx = Ctx(new JsonObject
        {
            ["html"] = "<h1>Baslik</h1><p>bkz <a href='https://x.com'>link</a></p>",
            ["format"] = "markdown", ["outputField"] = "md"
        }, new JsonObject());

        var md = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json["md"]!.GetValue<string>();
        md.Should().Contain("# Baslik");
        md.Should().Contain("[link](https://x.com)");
    }
}
