using System.Text.Json;
using FluentAssertions;
using FlowSharp.Infrastructure.Triggers;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Infrastructure;

public class WebhookRegistrarTests : IDisposable
{
    private readonly SqliteDbFixture db = new();

    public void Dispose() => db.Dispose();

    private WebhookRegistrar NewRegistrar() => new(db.NewContext());

    private static JsonElement Def(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task Sync_registers_webhook_trigger_nodes()
    {
        var workflowId = Guid.NewGuid();
        var def = Def("""
        {"nodes":[
          {"type":"webhook.trigger","name":"In","parameters":{"path":"/orders/","method":"post"}}
        ]}
        """);

        await NewRegistrar().SyncAsync(workflowId, def, isActive: true);

        var match = await NewRegistrar().ResolveAsync("POST", "orders");
        match.Should().NotBeNull();
        match!.WorkflowId.Should().Be(workflowId);
        match.NodeName.Should().Be("In");
    }

    [Fact]
    public async Task Sync_inactive_workflow_registers_nothing()
    {
        var def = Def("""{"nodes":[{"type":"webhook.trigger","parameters":{"path":"x"}}]}""");
        await NewRegistrar().SyncAsync(Guid.NewGuid(), def, isActive: false);

        (await NewRegistrar().ResolveAsync("POST", "x")).Should().BeNull();
    }

    [Fact]
    public async Task Sync_replaces_previous_registrations_for_same_workflow()
    {
        var id = Guid.NewGuid();
        await NewRegistrar().SyncAsync(id, Def("""{"nodes":[{"type":"webhook.trigger","parameters":{"path":"old"}}]}"""), true);
        await NewRegistrar().SyncAsync(id, Def("""{"nodes":[{"type":"webhook.trigger","parameters":{"path":"new"}}]}"""), true);

        (await NewRegistrar().ResolveAsync("POST", "old")).Should().BeNull();
        (await NewRegistrar().ResolveAsync("POST", "new")).Should().NotBeNull();
    }

    [Fact]
    public async Task Sync_defaults_path_and_method_when_missing()
    {
        await NewRegistrar().SyncAsync(Guid.NewGuid(),
            Def("""{"nodes":[{"type":"webhook.trigger","parameters":{}}]}"""), true);

        // Varsayilanlar: path "my-webhook", method "POST".
        (await NewRegistrar().ResolveAsync("POST", "my-webhook")).Should().NotBeNull();
    }

    [Fact]
    public async Task Resolve_is_case_insensitive_on_method()
    {
        await NewRegistrar().SyncAsync(Guid.NewGuid(),
            Def("""{"nodes":[{"type":"webhook.trigger","parameters":{"path":"hook","method":"GET"}}]}"""), true);

        (await NewRegistrar().ResolveAsync("get", "/hook/")).Should().NotBeNull();
    }

    [Fact]
    public async Task Resolve_unknown_returns_null()
    {
        (await NewRegistrar().ResolveAsync("POST", "nope")).Should().BeNull();
    }

    [Fact]
    public async Task Sync_ignores_non_webhook_nodes()
    {
        await NewRegistrar().SyncAsync(Guid.NewGuid(),
            Def("""{"nodes":[{"type":"http.request","parameters":{"url":"x"}}]}"""), true);

        (await NewRegistrar().ResolveAsync("POST", "x")).Should().BeNull();
    }
}
