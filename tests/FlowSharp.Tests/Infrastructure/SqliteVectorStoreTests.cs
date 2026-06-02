using FluentAssertions;
using FlowSharp.Application.Ai;
using FlowSharp.Infrastructure.Ai;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FlowSharp.Tests.Infrastructure;

/// <summary>Gercek SQLite dosyalariyla RAG vektor deposunu (upsert/search/clear) test eder.</summary>
public class SqliteVectorStoreTests : IDisposable
{
    private readonly string tempDir;
    private readonly SqliteVectorStore store;

    public SqliteVectorStoreTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "flowsharp-rag-" + Guid.NewGuid().ToString("N"));
        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(tempDir);
        store = new SqliteVectorStore(env, Options.Create(new RagOptions { DatabaseDirectory = "rag" }));
    }

    public void Dispose()
    {
        try { Directory.Delete(tempDir, recursive: true); } catch { /* temizlik best-effort */ }
    }

    private static VectorRecord Rec(string id, params float[] v) => new(id, $"text-{id}", v, null);

    [Fact]
    public async Task Upsert_then_search_returns_most_similar_first()
    {
        await store.UpsertAsync("ws1", "docs",
        [
            Rec("a", 1f, 0f, 0f),
            Rec("b", 0f, 1f, 0f),
            Rec("c", 0.9f, 0.1f, 0f)
        ]);

        var results = await store.SearchAsync("ws1", "docs", [1f, 0f, 0f], topK: 2);

        results.Should().HaveCount(2);
        results[0].Id.Should().Be("a");        // birebir ayni yon -> en yuksek skor
        results[0].Score.Should().BeApproximately(1f, 0.0001f);
        results[1].Id.Should().Be("c");         // ikinci en yakin
    }

    [Fact]
    public async Task Upsert_updates_existing_id()
    {
        await store.UpsertAsync("ws1", "docs", [Rec("x", 1f, 0f)]);
        await store.UpsertAsync("ws1", "docs", [new VectorRecord("x", "guncel", [0f, 1f], "meta")]);

        var results = await store.SearchAsync("ws1", "docs", [0f, 1f], topK: 5);
        results.Should().ContainSingle();
        results[0].Text.Should().Be("guncel");
        results[0].Metadata.Should().Be("meta");
    }

    [Fact]
    public async Task Scopes_are_isolated_in_separate_databases()
    {
        await store.UpsertAsync("workspaceA", "docs", [Rec("a", 1f, 0f)]);

        // Farkli scope -> ayri .db dosyasi -> bos.
        var other = await store.SearchAsync("workspaceB", "docs", [1f, 0f], topK: 5);
        other.Should().BeEmpty();
    }

    [Fact]
    public async Task Clear_removes_collection_records()
    {
        await store.UpsertAsync("ws1", "docs", [Rec("a", 1f, 0f), Rec("b", 0f, 1f)]);
        await store.ClearAsync("ws1", "docs");

        (await store.SearchAsync("ws1", "docs", [1f, 0f], topK: 5)).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_empty_collection_returns_empty()
    {
        (await store.SearchAsync("ws1", "yok", [1f, 0f], topK: 5)).Should().BeEmpty();
    }
}
