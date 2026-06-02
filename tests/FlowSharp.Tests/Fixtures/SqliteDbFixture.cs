using FlowSharp.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowSharp.Tests.Fixtures;

/// <summary>
/// Her test icin yalitilmis, bellek-ici (in-memory) bir SQLite veritabani saglar.
/// Baglanti acik tutuldugu surece sema yasar; Dispose ile temizlenir.
/// </summary>
public sealed class SqliteDbFixture : IDisposable
{
    private readonly SqliteConnection connection;

    public SqliteDbFixture()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        using var ctx = new ApplicationDbContext(options);
        ctx.Database.EnsureCreated();
        OptionsBuilder = options;
    }

    public DbContextOptions<ApplicationDbContext> OptionsBuilder { get; }

    public ApplicationDbContext NewContext() => new(OptionsBuilder);

    public void Dispose() => connection.Dispose();
}
