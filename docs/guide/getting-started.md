# Getting Started with FlowSharp

FlowSharp is a node-based workflow automation platform built with **C# / .NET 10** and **Blazor**. You design workflows visually, run them on real executable nodes, and extend the platform with runtime-loaded C# plugins.

## Prerequisites

| For | You need |
|---|---|
| Running with Docker | [Docker](https://docs.docker.com/get-docker/) + Docker Compose |
| Local development | [.NET 10 SDK](https://dotnet.microsoft.com/download) |
| Multi-process / cross-instance events | Redis (optional — falls back to in-memory when absent) |
| Production database | PostgreSQL or SQL Server (optional — SQLite works out of the box) |

FlowSharp ships with **SQLite** as the default database and applies its schema automatically on first start, so no database server or migration step is required to get going.

## Quick Start with Docker Compose

Build and run the full stack:

```bash
docker compose up -d --build
```

Then open:

```text
http://localhost:8080
```

Sign in with the seeded administrator account:

```text
admin@flowsharp.local
Admin!2345
```

The default `docker-compose.yml` runs the Web container with **SQLite** for the application database and **Redis** for cross-process workflow events. The worker runs as a separate container. PostgreSQL and PgBouncer are also defined in the compose file and can be enabled by switching `Database__Provider` and the connection string — see [Deployment](deployment.md).

::: warning Change the defaults before exposing FlowSharp
The seeded admin password and the `Security__CredentialEncryptionKey` in `docker-compose.yml` are for local evaluation only. Replace both before running anywhere reachable. See [Configuration](configuration.md) and [Deployment](deployment.md).
:::

## Local Development

Clone and restore:

```bash
git clone https://github.com/FlowSharp/FlowSharp.git
cd FlowSharp
dotnet restore
dotnet build
```

Run the Web app:

```powershell
dotnet run --project src/FlowSharp.Web
```

By default `appsettings.json` sets `Worker:RunInWebProcess` to `true`, so the background worker, scheduler, and trigger services all run **inside the web process** — a single command is enough for development. The SQLite schema is created automatically on startup (`Database:ApplyMigrationsOnStartup` defaults to `true`); there is **no separate `dotnet ef database update` step**.

### Running the worker as a separate process

For a production-like setup, run the worker on its own and disable in-process execution:

```jsonc
// appsettings.json (or environment variables)
{
  "Worker": { "RunInWebProcess": false }
}
```

```powershell
# Terminal 1
dotnet run --project src/FlowSharp.Web
# Terminal 2
dotnet run --project src/FlowSharp.Worker
```

When Web and Worker run as separate processes they **must** share the same database provider, connection string, and `Security:CredentialEncryptionKey`, and should share a Redis instance so live execution events propagate between them.

## First Workflow

1. Sign in and open **Workflows → New**.
2. Drag a **Manual Trigger** onto the canvas.
3. Add a node (for example **HTTP Request** or **Set**) and connect the trigger's output to it.
4. Click **Execute** to run the workflow and inspect per-node output in the run log.
5. Toggle the workflow **Active** to enable event-based triggers (webhook, schedule, IMAP, etc.).

## Next Steps

- [Architecture](architecture.md) — how the engine, queue, and worker fit together.
- [Built-in Nodes](built-in-nodes.md) — the full node catalog.
- [Expressions](expressions.md) — reference data from other nodes with the double-brace expression syntax.
- [Configuration](configuration.md) — every `appsettings.json` section.
- [Plugin Development](plugin-development.md) — add your own nodes in C#.
