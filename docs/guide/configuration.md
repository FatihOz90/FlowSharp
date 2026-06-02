# Configuration

FlowSharp is configured using standard `appsettings.json` parameters.

## Core Settings

Below is the standard configuration template:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=flowsharp_db;Username=postgres;Password=Postgres"
  },
  "Database": {
    "Provider": "Postgres",
    "ApplyMigrationsOnStartup": false
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Worker": {
    "RunInWebProcess": false
  },
  "Plugins": {
    "Path": "plugins",
    "OfficialMarketplaceUrl": "https://github.com/FlowSharp/plugins"
  }
}
```

### Options

*   **`Worker:RunInWebProcess`**: When set to `true`, the background job runner processes jobs inside the web application context (single-process mode). Set to `false` for high-performance multi-process setups where a separate worker process handles the load.
*   **`Plugins:OfficialMarketplaceUrl`**: Points to the GitHub repository hosting community plugins. FlowSharp uses this URL to fetch, download, and dynamically hot-load node plugins.

## Database Providers

Set `Database:Provider` to one of:

*   **`Postgres`**: Recommended for production and multi-user deployments.
*   **`SqlServer`**: For environments standardised on Microsoft SQL Server.
*   **`Sqlite`**: Local development and single-node / small-team self-hosting.

Each provider ships with its own native migration set, applied automatically at startup when `ApplyMigrationsOnStartup` is `true`. See [Database & Migrations](database-migrations.md) for details.

### SQL Server

```jsonc
{
  "Database": {
    "Provider": "SqlServer",
    "ApplyMigrationsOnStartup": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FlowSharpDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"
  }
}
```

### SQLite

```jsonc
{
  "Database": {
    "Provider": "Sqlite",
    "ApplyMigrationsOnStartup": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/flowsharp.db"
  }
}
```

## Authentication & Email

### Account confirmation

`Identity:RequireConfirmedAccount` controls whether new accounts must verify their email address before signing in:

```jsonc
{
  "Identity": {
    "RequireConfirmedAccount": false
  }
}
```

*   **`false`**: Users can sign in immediately after registering. Convenient for local development and trusted environments.
*   **`true`**: A confirmation email is sent on registration and must be acknowledged before sign-in. Requires a working SMTP configuration (see below).

Self-registered users are automatically assigned the `Member` role. See [Roles And Permissions](roles-and-permissions.md) for the resulting capabilities and data isolation.

### SMTP (email delivery)

Email — account confirmation and password reset — is delivered over SMTP using the `Email` section:

```jsonc
{
  "Email": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "User": "no-reply@example.com",
    "Password": "<smtp-password>",
    "From": "no-reply@example.com",
    "FromName": "FlowSharp"
  }
}
```

| Setting | Description |
|---|---|
| `Email:Host` | SMTP server host. **If left empty, no email is sent** — the message body (including confirmation links) is written to the application log instead. This keeps local development frictionless. |
| `Email:Port` | SMTP port. Defaults to `587`. |
| `Email:EnableSsl` | Enables SSL/TLS. Defaults to `true`. |
| `Email:User` / `Email:Password` | SMTP credentials. If `User` is empty, the connection is made without authentication. |
| `Email:From` | Sender address. Falls back to `Email:User` when omitted. |
| `Email:FromName` | Display name for the sender. Defaults to `FlowSharp`. |

::: tip
If you enable `Identity:RequireConfirmedAccount`, configure a real `Email:Host` so confirmation emails are actually delivered. With no SMTP host configured, the confirmation link is only available in the application log.
:::

Provide secrets such as `Email:Password` through environment variables or a secret manager rather than committing them — for example `Email__Password`.
