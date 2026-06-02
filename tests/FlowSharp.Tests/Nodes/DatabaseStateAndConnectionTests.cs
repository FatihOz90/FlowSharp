using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Database;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class DatabaseStateAndConnectionTests
{
    // ---- DatabaseConnectionNode: testConnection=false -> sadece state uretir (baglanmaz) ----
    [Fact]
    public async Task ConnectionNode_emits_state_without_connecting_when_test_disabled()
    {
        var node = new PostgresConnectionNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject
            {
                ["_credential"] = "Local PG",
                ["database"] = "appdb",
                ["schema"] = "public",
                ["testConnection"] = false
            });

        var result = await node.ExecuteAsync(ctx);

        result.Succeeded.Should().BeTrue();
        var json = result.PrimaryItems.Single().Json;
        json["provider"]!.GetValue<string>().Should().Be("Postgres");
        json["credentialName"]!.GetValue<string>().Should().Be("Local PG");
        json["database"]!.GetValue<string>().Should().Be("appdb");
    }

    [Fact]
    public async Task ConnectionNode_fails_without_credential()
    {
        var node = new PostgresConnectionNode();
        var ctx = new FakeNodeExecutionContext(parameters: new JsonObject { ["testConnection"] = false });

        var result = await node.ExecuteAsync(ctx);
        result.Succeeded.Should().BeFalse();
    }

    // ---- DatabaseNodeHelpers.ReadState: ConnectionNode ciktisini geri okur (round-trip) ----
    [Fact]
    public async Task ReadState_round_trips_connection_node_output()
    {
        var node = new SqlServerConnectionNode();
        var emit = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["_credential"] = "MSSQL", ["schema"] = "dbo", ["testConnection"] = false });
        var stateJson = (await node.ExecuteAsync(emit)).PrimaryItems.Single().Json;

        // Bu cikti bir sonraki node'un girisidir:
        var downstream = new FakeNodeExecutionContext(items: [NodeItem.From(stateJson)]);
        var state = DatabaseNodeHelpers.ReadState(downstream);

        state.Should().NotBeNull();
        state!.Provider.Should().Be(DatabaseProvider.SqlServer);
        state.CredentialName.Should().Be("MSSQL");
        state.Schema.Should().Be("dbo");
    }

    [Fact]
    public void ReadState_returns_null_for_non_state_item()
    {
        var ctx = new FakeNodeExecutionContext(items: [NodeItem.From(new JsonObject { ["foo"] = "bar" })]);
        DatabaseNodeHelpers.ReadState(ctx).Should().BeNull();
    }

    // ---- ReadTable / ReadSchema / ReadColumns (saf yardimcilar) ----
    [Fact]
    public void ReadTable_prefers_parameter_then_item()
    {
        var fromParam = new FakeNodeExecutionContext(parameters: new JsonObject { ["table"] = "users" });
        DatabaseNodeHelpers.ReadTable(fromParam).Should().Be("users");

        var fromItem = new FakeNodeExecutionContext(items: [NodeItem.From(new JsonObject { ["table"] = "orders" })]);
        DatabaseNodeHelpers.ReadTable(fromItem).Should().Be("orders");
    }

    [Fact]
    public void ReadColumns_parses_json_object_parameter()
    {
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["columnsJson"] = new JsonObject { ["name"] = "Ada", ["age"] = 30 } });

        var columns = DatabaseNodeHelpers.ReadColumns(ctx);
        columns.Should().ContainKey("name").And.ContainKey("age");
        columns["name"]!.GetValue<string>().Should().Be("Ada");
    }

    [Fact]
    public void ReadColumns_empty_when_not_object()
    {
        DatabaseNodeHelpers.ReadColumns(new FakeNodeExecutionContext()).Should().BeEmpty();
    }
}
