using FluentAssertions;
using FlowSharp.Application.Abstractions;
using FlowSharp.Infrastructure.Credentials;
using FlowSharp.Infrastructure.Security;
using FlowSharp.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlowSharp.Tests.Infrastructure;

public class CredentialStoreTests : IDisposable
{
    private readonly SqliteDbFixture db = new();
    private readonly ICredentialProtector protector;

    public CredentialStoreTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:CredentialEncryptionKey"] = Convert.ToBase64String(new byte[32])
            }).Build();
        protector = new CredentialProtector(config);
    }

    public void Dispose() => db.Dispose();

    private CredentialStore NewStore() => new(db.NewContext(), protector);

    [Fact]
    public async Task Save_then_resolve_decrypts_fields()
    {
        var id = await NewStore().SaveAsync(new CredentialInput(
            null, "My OpenAI", "openAiApi",
            new Dictionary<string, string> { ["apiKey"] = "sk-secret" }));

        id.Should().NotBe(Guid.Empty);

        var resolved = await NewStore().ResolveAsync("openAiApi", "My OpenAI", expectedOwnerId: null);
        resolved.Should().NotBeNull();
        resolved!["apiKey"].Should().Be("sk-secret");
    }

    [Fact]
    public async Task Stored_data_is_encrypted_at_rest()
    {
        await NewStore().SaveAsync(new CredentialInput(
            null, "Secret", "generic",
            new Dictionary<string, string> { ["password"] = "p@ss123" }));

        await using var ctx = db.NewContext();
        var row = ctx.Credentials.Single();
        row.EncryptedData.Should().NotContain("p@ss123");
    }

    [Fact]
    public async Task Resolve_unknown_returns_null()
    {
        var resolved = await NewStore().ResolveAsync("none", "none", expectedOwnerId: null);
        resolved.Should().BeNull();
    }

    [Fact]
    public async Task Save_with_existing_id_updates_in_place()
    {
        var id = await NewStore().SaveAsync(new CredentialInput(
            null, "Cred", "t", new Dictionary<string, string> { ["k"] = "v1" }));

        await NewStore().SaveAsync(new CredentialInput(
            id, "Cred", "t", new Dictionary<string, string> { ["k"] = "v2" }));

        await using var ctx = db.NewContext();
        ctx.Credentials.Should().HaveCount(1);
        var resolved = await NewStore().ResolveAsync("t", "Cred", expectedOwnerId: null);
        resolved!["k"].Should().Be("v2");
    }

    [Fact]
    public async Task Resolve_by_id_requires_matching_owner()
    {
        var id = await NewStore().SaveAsync(new CredentialInput(
            null, "Owned", "openAiApi",
            new Dictionary<string, string> { ["apiKey"] = "sk-owned" }, OwnerId: "user-A"));

        // Dogru sahip cozer.
        (await NewStore().ResolveAsync(id, expectedOwnerId: "user-A")).Should().NotBeNull();
        // Baska kullanici (cross-tenant) cozemez -> null.
        (await NewStore().ResolveAsync(id, expectedOwnerId: "user-B")).Should().BeNull();
        // Sahipsiz (sistem) baglam da bu sahipli kaydi cozemez.
        (await NewStore().ResolveAsync(id, expectedOwnerId: null)).Should().BeNull();
    }

    [Fact]
    public async Task List_filters_by_owner_but_null_returns_all()
    {
        await NewStore().SaveAsync(new CredentialInput(null, "A1", "t", new Dictionary<string, string>(), OwnerId: "A"));
        await NewStore().SaveAsync(new CredentialInput(null, "B1", "t", new Dictionary<string, string>(), OwnerId: "B"));

        (await NewStore().ListAsync(ownerId: "A")).Should().ContainSingle(c => c.Name == "A1");
        (await NewStore().ListAsync(ownerId: null)).Should().HaveCount(2);
    }
}
