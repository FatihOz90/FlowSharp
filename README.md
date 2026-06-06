# FlowSharp

*Read this in [Türkçe](README.tr.md).*

![FlowSharp workflow automation hero](docs/public/assets/flowsharp-hero.png)

FlowSharp is a node-based workflow automation platform built with **C#**, **.NET 10**, and **Blazor**. It includes a visual workflow designer, executable automation nodes, AI agents with RAG, webhook and schedule triggers, background workers, and runtime-loadable C# plugins.

![FlowSharp workflow designer](docs/public/assets/flowsharp-designer-mockup.png)

## Highlights

- Visual workflow designer with node palette, connections, parameters, and live run status.
- Executable nodes for HTTP, databases (PostgreSQL, SQL Server, MySQL, SQLite), email and messaging (Slack, Discord, Telegram, WhatsApp), logic, data transforms, and sandboxed JavaScript.
- AI agents powered by Semantic Kernel with model, tool (incl. MCP), and memory sub-nodes, plus a local-embedding vector store for RAG. Providers include OpenAI, Azure OpenAI, Anthropic, Gemini, Groq, Mistral, Cohere, Ollama, and more.
- Manual, schedule (cron), webhook, IMAP, chat, WhatsApp, workflow, and error triggers — with synchronous webhook responses.
- Runtime plugin system: drop C# source files into `plugins/` and load new nodes (compiled with Roslyn) without rebuilding the app.
- ASP.NET Core Identity, role/permission policies, AES-GCM encrypted credentials, owner-scoped data isolation, SignalR live events, Serilog logs, and optional OpenTelemetry.


## Quick Start

Run the stack with Docker Compose:

```bash
docker compose up -d --build
```

Open:

```text
http://localhost:8080
```

Default admin account:

```text
admin@flowsharp.local
Admin!2345
```

The default Docker Compose setup uses SQLite for the application database and Redis for cross-process workflow events.

## Local Development

Requirements:

- .NET 10 SDK
- Docker, optional but useful for Redis and database services

Build:

```powershell
dotnet restore
dotnet build
```

Run the Web app:

```powershell
dotnet run --project src/FlowSharp.Web
```

That's all you need for development: by default `Worker:RunInWebProcess` is `true`, so the queue worker, scheduler, and trigger services run inside the web process. The SQLite schema is created automatically on startup — there is no separate migration step.

To run the worker as its own scalable process (production-like), set `Worker:RunInWebProcess` to `false` and start it in another terminal:

```powershell
dotnet run --project src/FlowSharp.Worker
```

When Web and Worker run separately they must share the same database, `Security:CredentialEncryptionKey`, and (ideally) a Redis instance. See [Deployment](docs/guide/deployment.md).

## Database & Migrations

FlowSharp is built on Entity Framework Core and supports three database providers out of the box:

| Provider | `Database:Provider` | Recommended use |
|---|---|---|
| SQLite | `Sqlite` | Local development and single-node / small-team self-hosting |
| PostgreSQL | `Postgres` | Production and multi-user deployments |
| SQL Server | `SqlServer` | Environments standardised on Microsoft SQL Server |

### Selecting a provider

The active provider is determined entirely by configuration — no code changes are required. Set the provider key and a matching connection string:

```json
{
  "Database": { "Provider": "Postgres", "ApplyMigrationsOnStartup": true },
  "ConnectionStrings": { "DefaultConnection": "Host=...;Database=...;Username=...;Password=..." }
}
```

When `ApplyMigrationsOnStartup` is `true`, the application applies the selected provider's migrations automatically at startup. A new database receives the full schema; an existing database receives only the pending migrations, enabling safe, non-destructive schema upgrades. Concurrent instances are coordinated through EF Core's migration lock.

### Provider-specific migration sets

Each provider maintains its own native migration assembly, so column types are always correct for the target engine (for example `jsonb` on PostgreSQL, `TEXT` on SQLite, `nvarchar(max)` on SQL Server):

```text
src/FlowSharp.Migrations.Sqlite
src/FlowSharp.Migrations.Postgres
src/FlowSharp.Migrations.SqlServer
```

At runtime the matching set is selected automatically based on the configured provider.

### Operators

No migration commands are ever required. Select a provider, supply a connection string, and start the application — the schema is created and kept up to date automatically.

### Contributors

When the data model changes (a new or modified entity), generate a migration for **all three** providers so the sets stay in sync. The exact commands are documented in [Database & Migrations](docs/guide/database-migrations.md).


## Testing

Run the test suite:

```powershell
dotnet test
```

Generate a browsable HTML code coverage report (cleans old results, restores the
local `reportgenerator` tool, runs tests with coverage, writes the report to
`tests/FlowSharp.Tests/CoverageReport/index.html`):

```powershell
./scripts/coverage.ps1
```

The script is cross-platform (PowerShell 7+). Use `-NoOpen` in CI to skip opening
the browser. Coverage output (`TestResults/`, `CoverageReport/`) is git-ignored.


## Documentation

**Introduction**
- [Getting Started](docs/guide/getting-started.md)
- [Architecture](docs/guide/architecture.md)
- [Configuration](docs/guide/configuration.md)
- [Database & Migrations](docs/guide/database-migrations.md)
- [Deployment](docs/guide/deployment.md)

**Building Workflows**
- [Built-in Nodes](docs/guide/built-in-nodes.md)
- [Triggers & Scheduling](docs/guide/triggers-and-scheduling.md)
- [Expressions](docs/guide/expressions.md)
- [Credentials](docs/guide/credentials.md)
- [Webhooks](docs/guide/webhooks.md)
- [AI Agents & RAG](docs/guide/ai-agents.md)
- [Executions & Data](docs/guide/executions-and-data.md)

**Plugin System**
- [Plugin Development](docs/guide/plugin-development.md)
- [Marketplace](docs/guide/marketplace.md)

**Operations**
- [Roles & Permissions](docs/guide/roles-and-permissions.md)
- [Observability](docs/guide/observability.md)

## Project Structure

```text
src/
|-- FlowSharp.Web            Blazor UI, Identity, designer, webhooks, marketplace
|-- FlowSharp.Worker         Background worker for queued and scheduled jobs
|-- FlowSharp.Domain         Workflow, execution, queue, credential, and node models
|-- FlowSharp.Application    Interfaces and application contracts
|-- FlowSharp.Infrastructure EF Core, workflow engine, queue, plugins, scheduler
|-- FlowSharp.Nodes          Built-in workflow nodes
```

## License

FlowSharp is licensed under the **Elastic License 2.0 (ELv2)**. See [LICENSE.md](LICENSE.md).

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.
