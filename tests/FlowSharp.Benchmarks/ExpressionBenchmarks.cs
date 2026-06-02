using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using FlowSharp.Application.Nodes;
using FlowSharp.Application.Nodes.Expressions;
using FlowSharp.Infrastructure.Workflows.Expressions;

namespace FlowSharp.Benchmarks;

/// <summary>Expression evaluator hot path'i: parametre okurken her item icin cagrilir.</summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class ExpressionBenchmarks
{
    private readonly ExpressionEvaluator evaluator = new();
    private ExpressionContext context = null!;

    [GlobalSetup]
    public void Setup()
    {
        context = new ExpressionContext
        {
            CurrentItem = NodeItem.From(new JsonObject
            {
                ["name"] = "Ada",
                ["user"] = new JsonObject { ["age"] = 30, ["roles"] = new JsonArray("admin", "editor") }
            }),
            ItemIndex = 0
        };
    }

    [Benchmark]
    public string PlainText() => evaluator.EvaluateToString("hicbir ifade yok", context);

    [Benchmark]
    public string SimplePath() => evaluator.EvaluateToString("Selam {{ $json.name }}", context);

    [Benchmark]
    public string NestedPath() => evaluator.EvaluateToString("{{ $json.user.roles[0] }}", context);

    [Benchmark]
    public JsonNode? ToNodeObject() => evaluator.EvaluateToNode("{{ $json.user }}", context);
}
