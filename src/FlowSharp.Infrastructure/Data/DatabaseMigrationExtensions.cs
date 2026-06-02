using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowSharp.Infrastructure.Data;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // SQLite, veri dosyasini olusturur ama UST KLASORU olusturmaz. App_Data silinmisse
        // migration "unable to open database file" verir; klasoru once garantiye al.
        if (dbContext.Database.IsSqlite())
        {
            EnsureSqliteDirectoryExists(dbContext, logger);
        }

        // Tum saglayicilar (Sqlite/Postgres/SqlServer) artik kendi native migration setine sahip;
        // hangisi aktifse MigrationsAssembly ile o set secilir ve uygulanir. Bu, var olan
        // veritabanlarinda guvenli sema yukseltmesi saglar (EnsureCreated'in aksine).
        logger.LogInformation("Applying pending EF Core migrations ({Provider}).", dbContext.Database.ProviderName);
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("EF Core migrations are up to date.");

        // SQLite: WAL modu okuyuculari yazicidan ayirir; Web + Worker es zamanli erisiminde
        // kilitlenmeyi buyuk olcude onler. WAL dosya basliginda kalici olur, bir kez yeter.
        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        }
    }

    private static void EnsureSqliteDirectoryExists(ApplicationDbContext dbContext, ILogger logger)
    {
        var connectionString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            logger.LogInformation("SQLite veri klasoru olusturuldu: {Directory}", directory);
        }
    }
}
