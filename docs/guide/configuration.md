# Configuration

FlowSharp is configured through standard ASP.NET Core configuration: `appsettings.json`, environment-specific overrides (`appsettings.Development.json`), environment variables, and secret managers. Nested keys map to environment variables with `__` (double underscore) — for example `Email:Password` becomes `Email__Password`.

## Defaults

The shipped [`appsettings.json`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Web/appsettings.json) is tuned for a frictionless first run: **SQLite**, automatic schema creation, and the worker running **inside the web process**.

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/flowsharp.db"
  },
  "Database": {
    "Provider": "Sqlite",
    "ApplyMigrationsOnStartup": true
  },
  "Worker": {
    "RunInWebProcess": true,
    "MaxConcurrency": 5
  }
}
```

## Database

| Key | Default | Description |
|---|---|---|
| `Database:Provider` | `Sqlite` | `Sqlite`, `Postgres`, or `SqlServer` (case-insensitive; `postgresql`/`npgsql` and `mssql` aliases accepted). |
| `Database:ApplyMigrationsOnStartup` | `true` | Create/upgrade the schema automatically on startup. |
| `ConnectionStrings:DefaultConnection` | SQLite file | Provider-specific connection string. |

See [Database & Migrations](database-migrations.md) for provider details and connection-string examples.

## Worker

| Key | Default | Description |
|---|---|---|
| `Worker:RunInWebProcess` | `true` | When `true`, the queue worker, scheduler, and trigger services run inside the web process (single-process mode). Set to `false` to run [`FlowSharp.Worker`](https://github.com/FlowSharp/FlowSharp/tree/main/src/FlowSharp.Worker) as a separate scalable process. |
| `Worker:MaxConcurrency` | `5` | Maximum number of workflow jobs processed concurrently per worker. |

## Redis

| Key | Default | Description |
|---|---|---|
| `Redis:ConnectionString` | `localhost:6379` | SignalR backplane for live execution events across processes. If unreachable, FlowSharp falls back to in-memory eventing (single-process only). |

## Security

| Key | Description |
|---|---|
| `Security:CredentialEncryptionKey` | Base64-encoded key used to encrypt stored credentials at rest. **Web and Worker must use the same key**, and it must remain stable — rotating it makes existing credentials undecryptable. Provide it via a secret/environment variable in production. |

::: danger Never ship the default key
The repository ships a sample key for local use. Generate your own (32 random bytes, Base64) and supply it through `Security__CredentialEncryptionKey` before any non-local deployment.
:::

See [Credentials](credentials.md) for how credentials are stored and resolved.

## Rate Limiting

Per-user run throttling protects the engine from runaway triggers (for example a chatty webhook).

```jsonc
{
  "RateLimit": {
    "Enabled": true,
    "RunsPerMinutePerUser": 60
  }
}
```

When the limit is exceeded, synchronous webhook calls return `200` with `{ "ignored": true, "reason": "rate_limited" }` so providers like Meta/WhatsApp do not disable the webhook.

## HTTP Nodes (egress safety)

Controls outbound requests made by HTTP nodes and tools.

```jsonc
{
  "HttpNodes": {
    "Exposure": "Local",
    "BlockPrivateNetworks": false
  }
}
```

| Key | Description |
|---|---|
| `HttpNodes:Exposure` | `Local` or `Public`. Setting `Public` implies blocking private networks. |
| `HttpNodes:BlockPrivateNetworks` | When `true` (or `Exposure: Public`), HTTP nodes are blocked from reaching private/loopback addresses, mitigating SSRF in multi-tenant deployments. |

## Executions & Data Retention

```jsonc
{
  "Executions": {
    "SaveData": "All",
    "MaxCount": 1000,
    "MaxAgeDays": 30
  }
}
```

| Key | Default | Description |
|---|---|---|
| `Executions:SaveData` | `All` | How much per-node output is persisted: `All`, `ErrorsOnly`, or `None`. Execution **metadata is always saved**; live monitoring comes from the Redis stream regardless. |
| `Executions:MaxCount` | `1000` | Max executions retained per workflow (`0` = unlimited). Older runs are pruned. |
| `Executions:MaxAgeDays` | `30` | Max age of execution records in days (`0` = unlimited). |

See [Executions & Data](executions-and-data.md).

## Blob Storage (large output offload)

```jsonc
{
  "BlobStorage": {
    "Enabled": true,
    "Directory": "App_Data/blobs",
    "ThresholdBytes": 65536
  }
}
```

| Key | Default | Description |
|---|---|---|
| `BlobStorage:Enabled` | `false` (code default) | When enabled, execution output larger than the threshold is offloaded out of the database. |
| `BlobStorage:Directory` | `App_Data/blobs` | Filesystem root for the blob store. |
| `BlobStorage:ThresholdBytes` | `65536` (64 KB) | Output larger than this is moved to the blob store. |

## RAG (Vector Store)

```jsonc
{
  "Rag": {
    "DatabaseDirectory": "App_Data/rag"
  }
}
```

Embeddings are generated with a bundled local model — no API key is required. Each workspace (workflow) gets its own SQLite vector database under this directory. See [AI Agents & RAG](ai-agents.md).

## Identity & Email

```jsonc
{
  "Identity": {
    "RequireConfirmedAccount": false
  },
  "Email": {
    "Host": "",
    "Port": 587,
    "EnableSsl": true,
    "User": "",
    "Password": "",
    "From": "",
    "FromName": "FlowSharp"
  }
}
```

| Key | Description |
|---|---|
| `Identity:RequireConfirmedAccount` | `false`: users sign in immediately after registering. `true`: a confirmation email must be acknowledged first (requires SMTP). |
| `Email:Host` | SMTP host. **If empty, no email is sent** — the message body (including confirmation links) is written to the application log, keeping local development frictionless. |
| `Email:Port` | SMTP port. Defaults to `587`. |
| `Email:EnableSsl` | Enable SSL/TLS. Defaults to `true`. |
| `Email:User` / `Email:Password` | SMTP credentials. If `User` is empty, the connection is unauthenticated. |
| `Email:From` | Sender address. Falls back to `Email:User`. |
| `Email:FromName` | Sender display name. Defaults to `FlowSharp`. |

Self-registered users receive the `Member` role. See [Roles & Permissions](roles-and-permissions.md).

::: tip
If you enable `Identity:RequireConfirmedAccount`, configure a real `Email:Host` so confirmation emails are actually delivered.
:::

## First Admin (Seeding)

```jsonc
{
  "Seed": {
    "Enabled": true,
    "Admin": {
      "Email": "admin@flowsharp.local",
      "Password": "Admin!2345"
    }
  }
}
```

When seeding is enabled and no users exist, FlowSharp creates the first user and assigns the `Admin` role. Change these defaults before exposing the instance.

## Plugins

```jsonc
{
  "Plugins": {
    "Path": "plugins",
    "OfficialMarketplaceUrl": "https://github.com/FlowSharp/plugins"
  }
}
```

| Key | Description |
|---|---|
| `Plugins:Path` | Directory scanned for plugin folders (absolute, or relative to the content root). |
| `Plugins:OfficialMarketplaceUrl` | GitHub repository browsed by the admin marketplace. See [Marketplace](marketplace.md). |

## Observability

```jsonc
{
  "OpenTelemetry": {
    "Enabled": false,
    "OtlpEndpoint": ""
  }
}
```

See [Observability](observability.md) for OpenTelemetry, Serilog, and health endpoints.

## Logging (Serilog)

```jsonc
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
      }
    }
  }
}
```

## Secrets

Never commit real secrets. Provide `Security:CredentialEncryptionKey`, `Email:Password`, database passwords, and AI API keys through environment variables or a secret manager — for example `Security__CredentialEncryptionKey`, `Email__Password`.
