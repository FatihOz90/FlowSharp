using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Abstractions;
using FlowSharp.Application.Nodes;
using FlowSharp.Infrastructure.Workflows;
using FlowSharp.Infrastructure.Workflows.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace FlowSharp.Tests.Infrastructure;

/// <summary>Motorun node'lara verdigi gercek (internal) NodeExecutionContext'i dogrudan test eder.</summary>
public class NodeExecutionContextTests
{
    private static NodeExecutionContext Build(
        JsonObject parameters,
        IReadOnlyList<NodeItem>? items = null,
        JsonObject? trigger = null,
        IReadOnlyDictionary<string, IReadOnlyList<NodeItem>>? outputs = null,
        IServiceProvider? services = null,
        Action<string>? log = null,
        int runIndex = 0) =>
        new("test.node", "Test", parameters,
            items ?? [NodeItem.Empty()],
            outputs ?? new Dictionary<string, IReadOnlyList<NodeItem>>(StringComparer.OrdinalIgnoreCase),
            trigger, runIndex, new ExpressionEvaluator(),
            services ?? new ServiceCollection().BuildServiceProvider(),
            log ?? (_ => { }), CancellationToken.None, Guid.NewGuid());

    [Fact]
    public void GetString_resolves_expression_against_item()
    {
        var ctx = Build(new JsonObject { ["greet"] = "Hi {{ $json.name }}" },
            [NodeItem.From(new JsonObject { ["name"] = "Ada" })]);
        ctx.GetString("greet").Should().Be("Hi Ada");
    }

    [Fact]
    public void GetString_returns_default_when_missing_or_empty()
    {
        var ctx = Build(new JsonObject());
        ctx.GetString("nope", defaultValue: "fallback").Should().Be("fallback");
    }

    [Fact]
    public void GetString_serializes_non_string_parameter()
    {
        var ctx = Build(new JsonObject { ["obj"] = new JsonObject { ["a"] = 1 } });
        ctx.GetString("obj").Should().Contain("\"a\"");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void GetBoolean_parses_string_and_bool(string raw, bool expected)
    {
        Build(new JsonObject { ["flag"] = raw }).GetBoolean("flag").Should().Be(expected);
        Build(new JsonObject { ["flag"] = expected }).GetBoolean("flag").Should().Be(expected);
    }

    [Fact]
    public void GetBoolean_default_when_unparseable()
    {
        Build(new JsonObject { ["flag"] = "maybe" }).GetBoolean("flag", defaultValue: true).Should().BeTrue();
    }

    [Fact]
    public void GetNumber_and_GetInt_parse_numeric_and_string()
    {
        Build(new JsonObject { ["n"] = 3.5 }).GetNumber("n").Should().Be(3.5);
        Build(new JsonObject { ["n"] = "7" }).GetNumber("n").Should().Be(7);
        Build(new JsonObject { ["n"] = "9.9" }).GetInt("n").Should().Be(9);
        Build(new JsonObject()).GetNumber("missing", defaultValue: 42).Should().Be(42);
    }

    [Fact]
    public void GetJson_returns_clone_for_non_expression_object()
    {
        var ctx = Build(new JsonObject { ["cfg"] = new JsonObject { ["k"] = "v" } });
        var node = ctx.GetJson("cfg");
        node.Should().BeOfType<JsonObject>();
        node!["k"]!.GetValue<string>().Should().Be("v");
    }

    [Fact]
    public void GetJson_evaluates_expression_string()
    {
        var ctx = Build(new JsonObject { ["v"] = "{{ $json.age }}" },
            [NodeItem.From(new JsonObject { ["age"] = 30 })]);
        ctx.GetJson("v")!.GetValue<int>().Should().Be(30);
    }

    [Fact]
    public void ResolveValue_recurses_into_objects_and_arrays()
    {
        var ctx = Build(new JsonObject(), [NodeItem.From(new JsonObject { ["x"] = "Y" })]);
        // Karisik metin ifadeleri (gercek kullanim): her biri yeni bir string dugumu uretir.
        var input = new JsonObject
        {
            ["a"] = "deger={{ $json.x }}",
            ["nested"] = new JsonArray("ilk={{ $json.x }}", "plain")
        };
        var resolved = ctx.ResolveValue(input)!.AsObject();
        resolved["a"]!.GetValue<string>().Should().Be("deger=Y");
        resolved["nested"]!.AsArray()[0]!.GetValue<string>().Should().Be("ilk=Y");
        resolved["nested"]!.AsArray()[1]!.GetValue<string>().Should().Be("plain");
    }

    [Fact]
    public void ResolveValue_pure_expression_returning_item_node_is_cloned_not_thrown()
    {
        // Regresyon: tek-ifade deger, item icindeki bir objeyi/dugumu dondurur.
        // Eskiden "node already has a parent" InvalidOperationException firlatiyordu.
        var ctx = Build(new JsonObject(),
            [NodeItem.From(new JsonObject { ["user"] = new JsonObject { ["name"] = "Ada" } })]);
        var input = new JsonObject { ["copy"] = "{{ $json.user }}" };

        var resolved = ctx.ResolveValue(input)!.AsObject();
        resolved["copy"]!["name"]!.GetValue<string>().Should().Be("Ada");
    }

    [Fact]
    public void Log_forwards_to_sink()
    {
        var logs = new List<string>();
        var ctx = Build(new JsonObject(), log: logs.Add);
        ctx.Log("merhaba");
        logs.Should().ContainSingle().Which.Should().Be("merhaba");
    }

    [Fact]
    public async Task GetCredential_returns_field_from_store()
    {
        var store = Substitute.For<ICredentialStore>();
        store.ResolveAsync("openAiApi", "Default", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["apiKey"] = "sk-xyz" });
        var services = new ServiceCollection();
        services.AddSingleton(store);
        var ctx = Build(new JsonObject(), services: services.BuildServiceProvider());

        (await ctx.GetCredentialAsync("openAiApi", "Default", "apiKey")).Should().Be("sk-xyz");
    }

    [Fact]
    public async Task GetCredential_logs_when_store_missing()
    {
        var logs = new List<string>();
        var ctx = Build(new JsonObject(), log: logs.Add);
        (await ctx.GetCredentialAsync("t", "n", "f")).Should().BeNull();
        logs.Should().Contain(l => l.Contains("store"));
    }

    [Fact]
    public async Task GetCredential_logs_when_credential_not_found()
    {
        var store = Substitute.For<ICredentialStore>();
        store.ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<string, string>?)null);
        var services = new ServiceCollection();
        services.AddSingleton(store);
        var logs = new List<string>();
        var ctx = Build(new JsonObject(), services: services.BuildServiceProvider(), log: logs.Add);

        (await ctx.GetCredentialAsync("t", "yok", "f")).Should().BeNull();
        logs.Should().Contain(l => l.Contains("bulunamadi"));
    }
}
