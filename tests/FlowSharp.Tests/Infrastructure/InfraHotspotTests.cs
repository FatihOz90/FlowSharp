using System.Text.Json;
using FluentAssertions;
using FlowSharp.Infrastructure.Data;
using FlowSharp.Infrastructure.Triggers;
using Xunit;

namespace FlowSharp.Tests.Infrastructure;

public class DatabaseProvidersTests
{
    [Theory]
    [InlineData("Postgres", "Postgres")]
    [InlineData("postgresql", "Postgres")]
    [InlineData("NPGSQL", "Postgres")]
    [InlineData("SqlServer", "SqlServer")]
    [InlineData("mssql", "SqlServer")]
    [InlineData("sql-server", "SqlServer")]
    [InlineData("Sqlite", "Sqlite")]
    [InlineData("sqlite3", "Sqlite")]
    [InlineData("  SQLITE  ", "Sqlite")]   // trim + case
    [InlineData("bilinmeyen", "Postgres")] // varsayilan
    [InlineData(null, "Postgres")]
    [InlineData("", "Postgres")]
    public void Normalize_maps_aliases_and_defaults(string? input, string expected) =>
        DatabaseProviders.Normalize(input).Should().Be(expected);
}

public class SchedulerExtractSchedulesTests
{
    private static JsonElement Def(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Extracts_schedule_trigger_cron()
    {
        var def = Def("""
        {"nodes":[
          {"type":"schedule.trigger","name":"Her Sabah","parameters":{"cron":"0 9 * * *"}}
        ]}
        """);

        SchedulerService.ExtractSchedules(def).Should().ContainSingle()
            .Which.Should().Be(("Her Sabah", "0 9 * * *"));
    }

    [Fact]
    public void Extracts_imap_trigger_pollCron_field()
    {
        var def = Def("""
        {"nodes":[
          {"type":"email.imap.trigger","parameters":{"pollCron":"*/5 * * * *"}}
        ]}
        """);

        var result = SchedulerService.ExtractSchedules(def).Single();
        result.NodeName.Should().Be("Email Trigger"); // varsayilan ad
        result.Cron.Should().Be("*/5 * * * *");
    }

    [Fact]
    public void Ignores_non_trigger_nodes_and_missing_cron()
    {
        var def = Def("""
        {"nodes":[
          {"type":"http.request","parameters":{"url":"x"}},
          {"type":"schedule.trigger","parameters":{}},
          {"type":"schedule.trigger","name":"NoCron"}
        ]}
        """);

        SchedulerService.ExtractSchedules(def).Should().BeEmpty();
    }

    [Fact]
    public void Empty_or_missing_nodes_yields_nothing()
    {
        SchedulerService.ExtractSchedules(Def("""{}""")).Should().BeEmpty();
        SchedulerService.ExtractSchedules(Def("""{"nodes":"notarray"}""")).Should().BeEmpty();
    }
}
