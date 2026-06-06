# Executions & Data

Every workflow run produces an **execution record** — status, timing, and per-node output — that powers the run log, dashboard counts, and debugging. This page explains how runs are queued, what data is stored, and how to keep storage bounded.

## The job queue

`WorkflowRunner` decides how a run happens:

- **Synchronous** (`ExecuteNowAsync`) — used for webhooks that must return a response to the caller. The run executes immediately in the calling process.
- **Queued** — most triggers enqueue a job into the database `workflow_jobs` table. The [`QueueWorkerService`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Queue/QueueWorkerService.cs) leases and runs jobs, up to `Worker:MaxConcurrency` at a time.

Because the queue lives in the database (not in memory), it survives restarts and lets you scale Web and Worker independently. Provider-specific queue implementations exist for SQLite, PostgreSQL, and SQL Server. See [Architecture](architecture.md#queue-and-worker).

## What gets stored

Each execution always stores **metadata**: workflow id, status (`Succeeded` / `Failed`), start/finish times, and node count. Heavy **node output** is stored according to `Executions:SaveData`:

| `SaveData` | Node output stored |
|---|---|
| `All` (default) | Output for every node. |
| `ErrorsOnly` | Output only for failed runs. |
| `None` | No node output — metadata only. |

Live monitoring in the browser comes from the Redis event stream regardless of this setting, so `None` still gives you real-time node status while keeping the database lean.

```jsonc
{
  "Executions": {
    "SaveData": "All",
    "MaxCount": 1000,
    "MaxAgeDays": 30
  }
}
```

## Retention & pruning

After each run, old executions are pruned per workflow:

- **`MaxCount`** — keep at most N executions per workflow (`0` = unlimited).
- **`MaxAgeDays`** — delete executions older than N days (`0` = unlimited).

Tune these for your storage budget and audit needs.

## Large output offload (Blob Storage)

When an execution's output exceeds a threshold, it can be **offloaded** out of the database to a blob store, keeping the relational database small and fast.

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
| `Enabled` | `false` | When off, all output stays in the database (no behavior change). |
| `Directory` | `App_Data/blobs` | Filesystem root for the blob store. |
| `ThresholdBytes` | `65536` (64 KB) | Output larger than this is offloaded. |

::: tip Shared storage for multi-process
If Web and Worker run in separate containers and you enable blob storage, point `BlobStorage:Directory` at a **shared volume** so both processes can read offloaded output. The same applies to SQLite database/RAG files. See [Deployment](deployment.md).
:::

## Rate limiting

To protect the engine from runaway triggers, runs are throttled per user:

```jsonc
{
  "RateLimit": {
    "Enabled": true,
    "RunsPerMinutePerUser": 60
  }
}
```

When the limit is hit, queued runs are rejected and synchronous webhook calls return `200` with `{ "ignored": true, "reason": "rate_limited" }`. See [Webhooks](webhooks.md#rate-limiting).

## Error workflows

When a run **fails**, FlowSharp triggers any workflows that start with an [Error Trigger](triggers-and-scheduling.md#error-trigger-errortrigger), passing the failing workflow and execution details — a single place to centralize alerting and recovery.

## Capturing data for debugging

- **Run log** — inspect each node's input/output for a run (subject to `SaveData`).
- **Pinned data** — pin sample data on a node so downstream nodes run against it without re-executing upstream or firing live triggers. See [Architecture](architecture.md#the-execution-engine).
- **Partial / up-to-node execution** — run only a target node and its ancestors to iterate quickly in the designer.
