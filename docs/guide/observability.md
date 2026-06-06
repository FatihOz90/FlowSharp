# Observability

FlowSharp is built to run unattended, so it exposes structured logs, health probes, optional OpenTelemetry traces/metrics, and live in-app execution events.

## Logging (Serilog)

Logging uses **Serilog** with console and rolling file sinks (files under `logs/`). Levels are controlled from the `Serilog` section:

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

In containers, mount `logs/` to a volume (the default `docker-compose.yml` does) so logs persist across restarts. See [Configuration](configuration.md#logging-serilog).

## Health checks

The Web app maps three health endpoints designed for orchestrators and load balancers ([`HealthEndpoints`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Web/Endpoints/HealthEndpoints.cs)):

| Endpoint | Auth | Purpose |
|---|---|---|
| `/health/live` | Anonymous | **Liveness** — runs no checks; if it answers, the process is up. |
| `/health/ready` | Anonymous | **Readiness** — runs `ready`-tagged checks (e.g. database connectivity). |
| `/health` | **Admin only** | Detailed per-component JSON report (status, duration, errors). |

Probes are anonymous because orchestrators and load balancers can't authenticate; the detailed report is restricted to admins so component internals aren't leaked. See [Deployment](deployment.md#health-probes).

```jsonc
// Kubernetes probe example
livenessProbe:  { httpGet: { path: /health/live,  port: 8080 } }
readinessProbe: { httpGet: { path: /health/ready, port: 8080 } }
```

## OpenTelemetry (traces & metrics)

When enabled, FlowSharp emits OTLP traces and metrics for export to a collector (Tempo, Jaeger, Prometheus via collector, Grafana, etc.):

```jsonc
{
  "OpenTelemetry": {
    "Enabled": false,
    "OtlpEndpoint": "http://otel-collector:4317"
  }
}
```

The pipeline ([`ObservabilityExtensions`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Observability/ObservabilityExtensions.cs)) wires up:

- **Tracing** — FlowSharp's own `ActivitySource` (workflow execution spans such as `workflow.execute`), plus ASP.NET Core, HttpClient, and Npgsql instrumentation.
- **Metrics** — FlowSharp's `Meter`, plus ASP.NET Core and HttpClient metrics.

Both export over OTLP to `OpenTelemetry:OtlpEndpoint` when `Enabled` is `true`. If you have no collector, leave `Enabled` at `false`. The instrumentation source is defined in [`FlowSharpTelemetry`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Diagnostics/FlowSharpTelemetry.cs).

## Live execution events (SignalR)

The designer and dashboard show **per-node execution status in real time** over SignalR. With a Redis backplane (`Redis:ConnectionString`), events propagate across processes — so a workflow executed by a separate Worker still streams its progress to every connected browser. Without Redis, eventing falls back to in-memory (single-process only). See [Architecture](architecture.md).

The same channel surfaces AI agent token streaming for the chat interface. In-app user notifications are delivered through `UiNotifier`.

## What to monitor

| Signal | Where |
|---|---|
| Process up / dependencies ready | `/health/live`, `/health/ready` |
| Component breakdown | `/health` (admin) |
| Request & workflow traces | OTLP → your tracing backend |
| Throughput / latency / queue behavior | OTLP metrics |
| Application logs | Serilog console + `logs/` files |
| Execution history & failures | In-app executions view; see [Executions & Data](executions-and-data.md) |
