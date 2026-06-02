using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Data;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

/// <summary>
/// HtmlToTextNode'un markdown render dallarini (RenderMarkdown) ayri ayri kapsar.
/// Bu metot cok dalli (h1-h6, bold, italic, code, link, liste, br) oldugundan
/// her dal icin kucuk bir ornek veririz.
/// </summary>
public class HtmlToTextMarkdownTests
{
    private static async Task<string> Md(string html)
    {
        var node = new HtmlToTextNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["html"] = html, ["format"] = "markdown", ["outputField"] = "md" },
            items: [NodeItem.Empty()]);
        return (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json["md"]!.GetValue<string>();
    }

    [Theory]
    [InlineData("<h1>Bir</h1>", "# Bir")]
    [InlineData("<h2>Iki</h2>", "## Iki")]
    [InlineData("<h3>Uc</h3>", "### Uc")]
    [InlineData("<h4>Dort</h4>", "#### Dort")]
    [InlineData("<h5>Bes</h5>", "##### Bes")]
    [InlineData("<h6>Alti</h6>", "###### Alti")]
    public async Task Headings_h1_to_h6(string html, string expected) =>
        (await Md(html)).Should().Contain(expected);

    [Fact]
    public async Task Bold_strong_and_b() =>
        (await Md("<p><strong>kalin</strong> ve <b>kalin2</b></p>"))
            .Should().Contain("**kalin**").And.Contain("**kalin2**");

    [Fact]
    public async Task Italic_em_and_i() =>
        (await Md("<p><em>egik</em> ve <i>egik2</i></p>"))
            .Should().Contain("*egik*").And.Contain("*egik2*");

    [Fact]
    public async Task Inline_code() =>
        (await Md("<p><code>kod</code></p>")).Should().Contain("`kod`");

    [Fact]
    public async Task Link_with_href() =>
        (await Md("<a href='https://x.com'>tikla</a>")).Should().Contain("[tikla](https://x.com)");

    [Fact]
    public async Task Unordered_list_renders_dashes()
    {
        var md = await Md("<ul><li>bir</li><li>iki</li></ul>");
        md.Should().Contain("- bir").And.Contain("- iki");
    }

    [Fact]
    public async Task Ordered_list_renders_numbers()
    {
        var md = await Md("<ol><li>ilk</li><li>ikinci</li></ol>");
        md.Should().Contain("1. ilk").And.Contain("2. ikinci");
    }

    [Fact]
    public async Task Line_break_br()
    {
        var md = await Md("<p>satir1<br>satir2</p>");
        md.Should().Contain("satir1").And.Contain("satir2");
    }

    [Fact]
    public async Task Script_and_style_are_ignored()
    {
        var md = await Md("<p>gorunur</p><script>gizli()</script><style>.x{}</style>");
        md.Should().Contain("gorunur");
        md.Should().NotContain("gizli").And.NotContain(".x{}");
    }

    [Fact]
    public async Task Nested_formatting_inside_list()
    {
        var md = await Md("<ul><li><strong>onemli</strong> madde</li></ul>");
        md.Should().Contain("- **onemli** madde");
    }
}
