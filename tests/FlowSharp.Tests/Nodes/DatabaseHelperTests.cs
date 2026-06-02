using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Nodes.Core.Logic;
using FlowSharp.Nodes.Database;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class DatabaseHelperTests
{
    [Theory]
    [InlineData(DatabaseProvider.Postgres, "users", "\"users\"")]
    [InlineData(DatabaseProvider.SqlServer, "users", "[users]")]
    [InlineData(DatabaseProvider.MySql, "users", "`users`")]
    public void QuoteIdentifier_uses_provider_specific_quoting(DatabaseProvider provider, string id, string expected) =>
        DatabaseNodeHelpers.QuoteIdentifier(provider, id).Should().Be(expected);

    [Theory]
    [InlineData("users; DROP TABLE")]
    [InlineData("1bad")]
    [InlineData("")]
    public void QuoteIdentifier_rejects_invalid_identifiers_sql_injection_guard(string id)
    {
        var act = () => DatabaseNodeHelpers.QuoteIdentifier(DatabaseProvider.Postgres, id);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void QuotePath_prefixes_schema_when_table_has_no_dot()
    {
        DatabaseNodeHelpers.QuotePath(DatabaseProvider.Postgres, "public", "users")
            .Should().Be("\"public\".\"users\"");
    }

    [Fact]
    public void QuotePath_ignores_schema_when_table_already_qualified()
    {
        DatabaseNodeHelpers.QuotePath(DatabaseProvider.Postgres, "public", "other.users")
            .Should().Be("\"other\".\"users\"");
    }

    [Theory]
    [InlineData(DatabaseProvider.Postgres, 5432)]
    [InlineData(DatabaseProvider.SqlServer, 1433)]
    [InlineData(DatabaseProvider.MySql, 3306)]
    public void DefaultPort_per_provider(DatabaseProvider provider, int expected) =>
        DatabaseNodeHelpers.DefaultPort(provider).Should().Be(expected);

    [Fact]
    public void BuildWhere_wraps_clause_or_returns_empty()
    {
        DatabaseNodeHelpers.BuildWhere(null).Should().BeEmpty();
        DatabaseNodeHelpers.BuildWhere("id = 1").Should().Be(" WHERE id = 1");
    }

    [Fact]
    public void ToDbValue_maps_json_kinds()
    {
        DatabaseNodeHelpers.ToDbValue(null).Should().Be(DBNull.Value);
        DatabaseNodeHelpers.ToDbValue(JsonValue.Create(true)).Should().Be(true);
        DatabaseNodeHelpers.ToDbValue(JsonValue.Create(42)).Should().Be(42L);
        DatabaseNodeHelpers.ToDbValue(JsonValue.Create("metin")).Should().Be("metin");
    }

    [Fact]
    public void ToJson_maps_clr_values()
    {
        DatabaseNodeHelpers.ToJson((object?)null).Should().BeNull();
        DatabaseNodeHelpers.ToJson((object?)7)!.GetValue<int>().Should().Be(7);
        DatabaseNodeHelpers.ToJson((object?)true)!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void CreateConnectionStringBuilder_postgres_includes_host_and_db()
    {
        var cs = DatabaseNodeHelpers.CreateConnectionStringBuilder(
            DatabaseProvider.Postgres, "db.example", 5432, "appdb", "u", "p", ssl: false).ConnectionString;
        cs.Should().Contain("db.example").And.Contain("appdb");
    }

    [Fact]
    public void CreateConnectionStringBuilder_sqlserver_named_instance_omits_port()
    {
        var cs = DatabaseNodeHelpers.CreateConnectionStringBuilder(
            DatabaseProvider.SqlServer, @"(localdb)\MSSQLLocalDB", 1433, "db", "u", "p", ssl: true).ConnectionString;
        cs.Should().Contain("(localdb)\\MSSQLLocalDB");
        cs.Should().NotContain(",1433");
    }

    // ConditionEvaluator (internal) dogrudan dogrulama
    [Theory]
    [InlineData("5", "greaterThan", "3", true)]
    [InlineData("3", "greaterThan", "5", false)]
    [InlineData("abc", "contains", "b", true)]
    [InlineData("abc", "startsWith", "ab", true)]
    [InlineData("", "isEmpty", "", true)]
    [InlineData("hello", "isNotEmpty", "", true)]
    [InlineData("true", "isTrue", "", true)]
    [InlineData("10", "equals", "10", true)]
    [InlineData("10.0", "equals", "10", true)]
    public void ConditionEvaluator_covers_operators(string left, string op, string right, bool expected) =>
        ConditionEvaluator.Evaluate(left, op, right).Should().Be(expected);
}
