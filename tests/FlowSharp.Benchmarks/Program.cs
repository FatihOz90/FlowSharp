using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// Tum benchmark'lari calistirir. Ornek:
//   dotnet run -c Release --project tests/FlowSharp.Benchmarks
//   dotnet run -c Release --project tests/FlowSharp.Benchmarks -- --filter *Engine*

// Ciktiyi (BenchmarkDotNet.Artifacts) calisma dizininden bagimsiz olarak proje klasoru
// altinda tut. Exe: tests/FlowSharp.Benchmarks/bin/<cfg>/<tfm>/ -> 3 ust = proje klasoru.
var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var config = DefaultConfig.Instance
    .WithArtifactsPath(Path.Combine(projectDir, "BenchmarkDotNet.Artifacts"));

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

internal sealed partial class Program;
