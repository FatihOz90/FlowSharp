# Database & Migrations

FlowSharp persists its data through **Entity Framework Core** and supports three relational database providers without any code changes. The active provider is selected entirely through configuration, and the corresponding schema is created and upgraded automatically at application startup. The default provider is **SQLite**, so a fresh checkout runs with no database server.

## Supported providers

| Provider | `Database:Provider` | Recommended use |
|---|---|---|
| SQLite | `Sqlite` | Local development and single-node / small-team self-hosting |
| PostgreSQL | `Postgres` | Production and multi-user deployments |
| SQL Server | `SqlServer` | Environments standardised on Microsoft SQL Server |

The value is case-insensitive; `postgres`, `postgresql`, and `npgsql` all resolve to the PostgreSQL provider, and `sqlserver` / `mssql` to SQL Server.

## Selecting a provider

Set the provider key and a matching connection string in `appsettings.json` (or via environment variables / secrets):

```json
{
  "Database": {
    "Provider": "Postgres",
    "ApplyMigrationsOnStartup": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=flowsharp;Username=flowsharp;Password=<password>"
  }
}
```

When the Web and Worker run as separate processes, **both** must use the same provider, connection string, and `Security:CredentialEncryptionKey`.

::: tip Switching providers is configuration only
Changing `Database:Provider` does not require regenerating migrations. Each provider ships with its own migration set; the application selects the correct one at runtime. Note that switching providers points the application at a different, empty database — it does not copy existing data between engines.
:::

## Automatic schema management

With `ApplyMigrationsOnStartup` set to `true`, the application applies the selected provider's migrations on startup:

- A **new** database receives the complete schema.
- An **existing** database receives only the pending migrations, providing safe, non-destructive schema upgrades.
- **Concurrent** instances are coordinated through EF Core's migration lock, so simultaneous startups do not conflict.

On SQLite, the data directory (for example `App_Data/`) is created automatically if it does not exist, and Write-Ahead Logging (WAL) is enabled to improve concurrent access between the Web and Worker processes.

For controlled production rollouts you may set `ApplyMigrationsOnStartup` to `false` and apply migrations as an explicit deployment step instead.

## Provider-specific migration sets

EF Core permits only one model snapshot per `DbContext` within a single assembly. To keep every provider's column types native and correct, FlowSharp maintains a **separate migration assembly per provider**:

```text
src/FlowSharp.Migrations.Sqlite     → TEXT, INTEGER
src/FlowSharp.Migrations.Postgres   → jsonb, timestamp with time zone
src/FlowSharp.Migrations.SqlServer  → nvarchar(max), datetimeoffset
```

Each assembly is registered with the corresponding provider through `MigrationsAssembly(...)`, and the Web and Worker projects reference all three so the assemblies are available at runtime. The active provider determines which set is applied.

## For operators

No migration commands are ever required. Select a provider, supply a connection string, and start the application — the schema is created and kept current automatically.

## For contributors

Whenever the data model changes (a new or modified entity), generate a migration for **all three** providers so the sets remain in sync. The application picks the matching set at runtime; the database is never reverse-engineered from the model.

Run each command with the target provider active. The startup project supplies the `DbContext` configuration; environment variables override the provider and connection string at design time.

```powershell
# SQLite
$env:Database__Provider="Sqlite"
$env:ConnectionStrings__DefaultConnection="Data Source=App_Data/flowsharp.db"
dotnet ef migrations add <Name> `
  --project src/FlowSharp.Migrations.Sqlite `
  --startup-project src/FlowSharp.Web

# PostgreSQL
$env:Database__Provider="Postgres"
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=flowsharp;Username=postgres;Password=postgres"
dotnet ef migrations add <Name> `
  --project src/FlowSharp.Migrations.Postgres `
  --startup-project src/FlowSharp.Web

# SQL Server
$env:Database__Provider="SqlServer"
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=flowsharp;User Id=sa;Password=<password>;TrustServerCertificate=True"
dotnet ef migrations add <Name> `
  --project src/FlowSharp.Migrations.SqlServer `
  --startup-project src/FlowSharp.Web
```

::: warning Keep the three sets aligned
A model change that is migrated for only one provider will leave the others out of date. Always add the migration to all three projects in the same change set, and use the same migration `<Name>` for clarity.
:::

Generating a migration does not require a running database; EF Core builds the model in memory. Use `dotnet ef migrations remove --project <migration-project> --startup-project src/FlowSharp.Web` to revert the most recent, unapplied migration.
