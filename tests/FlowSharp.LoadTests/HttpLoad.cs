using System.Diagnostics;
using System.Net.Http.Json;

namespace FlowSharp.LoadTests;

/// <summary>
/// Calisan uygulamaya es zamanli HTTP yuku uygular (webhook endpoint). Kayitli webhook yoksa
/// 404 doner; bu yol bile ASP.NET pipeline + EF sorgusu + JSON serilestirmeyi olcer.
/// throughput (istek/sn), latency yuzdelikleri ve hata oranini raporlar.
/// </summary>
internal static class HttpLoad
{
    public static async Task RunAsync(string baseUrl, int total, int parallelism)
    {
        var handler = new SocketsHttpHandler { MaxConnectionsPerServer = parallelism };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };

        Console.WriteLine($"\n=== HTTP Yuk Testi: {baseUrl} ===");
        Console.WriteLine($"Toplam istek: {total} | Es zamanlilik: {parallelism}\n");

        // Kucuk bir isinma
        try { await client.PostAsJsonAsync("/webhook/warmup", new { ping = 1 }); }
        catch (Exception ex) { Console.WriteLine($"Uygulamaya ulasilamiyor: {ex.Message}"); return; }

        var latencies = new System.Collections.Concurrent.ConcurrentBag<double>();
        var errors = 0;
        using var throttle = new SemaphoreSlim(parallelism);
        var swTotal = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, total).Select(async i =>
        {
            await throttle.WaitAsync();
            try
            {
                var sw = Stopwatch.StartNew();
                var resp = await client.PostAsJsonAsync($"/webhook/load-{i}", new { i, value = "x" });
                sw.Stop();
                latencies.Add(sw.Elapsed.TotalMilliseconds);
                // 404 beklenen (kayitli webhook yok); 5xx gercek hatadir.
                if ((int)resp.StatusCode >= 500) Interlocked.Increment(ref errors);
            }
            catch { Interlocked.Increment(ref errors); }
            finally { throttle.Release(); }
        });

        await Task.WhenAll(tasks);
        swTotal.Stop();

        var sorted = latencies.OrderBy(x => x).ToArray();
        double Pct(double p) => sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * p))];

        Console.WriteLine($"Toplam sure   : {swTotal.Elapsed.TotalSeconds:F2} sn");
        Console.WriteLine($"Throughput    : {total / swTotal.Elapsed.TotalSeconds:F0} istek/sn");
        Console.WriteLine($"Latency p50   : {Pct(0.50):F1} ms");
        Console.WriteLine($"Latency p95   : {Pct(0.95):F1} ms");
        Console.WriteLine($"Latency p99   : {Pct(0.99):F1} ms");
        Console.WriteLine($"Maks latency  : {(sorted.Length > 0 ? sorted[^1] : 0):F1} ms");
        Console.WriteLine($"Hata (5xx/exc): {errors} ({100.0 * errors / total:F1}%)");
    }
}
