using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using FlowSharp.Infrastructure.Workflows;
using FlowSharp.Nodes.Core;
using FlowSharp.Nodes.Core.Flow;
using FlowSharp.Nodes.Core.Logic;
using FlowSharp.Nodes.Triggers;

namespace FlowSharp.Benchmarks;

/// <summary>
/// Workflow motorunu artan item sayilariyla calistirir. MemoryDiagnoser tahsisi gosterir
/// (motor her node'da DeepClone yaptigi icin bellek davranisi kritik).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class EngineBenchmarks
{
    // 100 -> 10.000 item: olcek arttikca sure/bellek nasil buyuyor?
    [Params(100, 1000, 10000)]
    public int ItemCount;

    private WorkflowExecutionEngine engine = null!;
    private JsonElement linearDef;
    private JsonElement expressionDef;
    private JsonElement loopDef;
    private JsonElement payload;

    [GlobalSetup]
    public void Setup()
    {
        engine = Support.BuildEngine(
        [
            new ManualTriggerNode(), new SetNode(), new IfNode(), new FilterNode(), new LoopOverItemsNode()
        ]);

        // 1) Duz zincir: trigger -> Set -> Set -> Set
        linearDef = Support.Definition(
            new JsonArray(
                Support.Node("t", "manual.trigger"),
                Support.Node("s1", "set.fields", new JsonObject { ["fields"] = new JsonObject { ["a"] = "1" } }),
                Support.Node("s2", "set.fields", new JsonObject { ["fields"] = new JsonObject { ["b"] = "2" } }),
                Support.Node("s3", "set.fields", new JsonObject { ["fields"] = new JsonObject { ["c"] = "3" } })),
            new JsonArray(Support.Conn("t", "s1"), Support.Conn("s1", "s2"), Support.Conn("s2", "s3"))
        ).RootElement;

        // 2) Expression cozumlemeli: her item icin {{ $json.value }} cozulur
        expressionDef = Support.Definition(
            new JsonArray(
                Support.Node("t", "manual.trigger"),
                Support.Node("s", "set.fields", new JsonObject
                {
                    ["fields"] = new JsonObject { ["copy"] = "{{ $json.value }}", ["double"] = "{{ $json.i }}-{{ $json.i }}" }
                })),
            new JsonArray(Support.Conn("t", "s"))
        ).RootElement;

        // 3) Loop: tum item'lar 10'arli partiler halinde govdeden gecer
        loopDef = Support.Definition(
            new JsonArray(
                Support.Node("t", "manual.trigger"),
                Support.Node("loop", "flow.loopOverItems", new JsonObject { ["batchSize"] = 10 }),
                Support.Node("body", "set.fields", new JsonObject { ["fields"] = new JsonObject { ["seen"] = "1" } })),
            new JsonArray(Support.Conn("t", "loop"), Support.Conn("loop", "body", 1), Support.Conn("body", "loop"))
        ).RootElement;

        payload = Support.Payload(ItemCount).RootElement;
    }

    private static readonly FlowSharp.Application.Workflows.WorkflowExecutionOptions NoCapture =
        new() { CaptureData = false };

    [Benchmark(Baseline = true)]
    public async Task<bool> LinearChain()
    {
        var result = await engine.ExecuteAsync(linearDef, payload);
        return result.Succeeded;
    }

    // CaptureData=false: agir veri klonlanmaz (SaveData=None / yuksek throughput yolu).
    [Benchmark]
    public async Task<bool> LinearChainNoCapture()
    {
        var result = await engine.ExecuteAsync(linearDef, payload, NoCapture);
        return result.Succeeded;
    }

    [Benchmark]
    public async Task<bool> WithExpressions()
    {
        var result = await engine.ExecuteAsync(expressionDef, payload);
        return result.Succeeded;
    }

    [Benchmark]
    public async Task<bool> LoopOverItems()
    {
        var result = await engine.ExecuteAsync(loopDef, payload);
        return result.Succeeded;
    }
}
