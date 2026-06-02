using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FlowSharp.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FlowSharp.Tests.Web;

/// <summary>
/// Tum Web uygulamasini (in-memory SQLite ile) ayaga kaldirip webhook pipeline'ini uctan uca test eder.
/// </summary>
public class WebhookEndpointTests : IClassFixture<WebhookEndpointTests.Factory>
{
    private readonly Factory factory;

    public WebhookEndpointTests(Factory factory) => this.factory = factory;

    [Fact]
    public async Task Unregistered_webhook_returns_404_json()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/webhook/does-not-exist", new { hello = "world" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("kayitli webhook yok");
    }

    [Fact]
    public async Task App_boots_and_serves_requests()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/webhook/anything");
        // Kayit yoksa 404; onemli olan pipeline'in 5xx olmadan calismasi.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();

            builder.UseEnvironment("Development");
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
            builder.UseSetting("Seed:Enabled", "false");
            builder.UseSetting("Worker:RunInWebProcess", "false");
            builder.UseSetting("Security:CredentialEncryptionKey", Convert.ToBase64String(new byte[32]));

            builder.ConfigureServices(services =>
            {
                // Uygulamanin DbContext kaydini, acik tutulan tek bir in-memory baglantiyla degistir.
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                connection.Dispose();
            }
        }
    }
}
