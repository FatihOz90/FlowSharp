# Architecture

FlowSharp follows clean-architecture layering to keep workflow logic decoupled from presentation and infrastructure. The same engine and node library run inside the Web app and the standalone Worker.

## Project Structure

```text
src/
├─ FlowSharp.Web              # Blazor UI, Identity, webhook & health endpoints, marketplace
├─ FlowSharp.Worker           # BackgroundService host that processes queued jobs
├─ FlowSharp.Domain           # Core entities: Workflow, execution, queue, credential, node models
├─ FlowSharp.Application      # Contracts: INodeType, INodeRegistry, IPluginManager, expressions, AI
├─ FlowSharp.Infrastructure   # EF Core, execution engine, DB queue, plugins (Roslyn), scheduler
└─ FlowSharp.Nodes            # Built-in node library (HTTP, data, DB, AI, communication, triggers)

src/ (migrations)
├─ FlowSharp.Migrations.Sqlite
├─ FlowSharp.Migrations.Postgres
└─ FlowSharp.Migrations.SqlServer
```

Each database provider has its own native migration assembly, selected at runtime from configuration. See [Database & Migrations](database-migrations.md).

## Technical Stack

| Component | Technology | Notes |
|---|---|---|
| **UI** | Blazor Web App (Interactive Server) | Dashboard and visual designer canvas. |
| **Backend** | ASP.NET Core (.NET 10) | Hosts the UI, webhook endpoints, and Identity. |
| **Realtime** | SignalR + Redis backplane | Live per-node execution status in the browser; Redis lets events cross the Web/Worker process boundary (falls back to in-memory single-process). |
| **Database** | SQLite, PostgreSQL, or SQL Server + EF Core | Stores workflows, executions, credentials, and queue jobs. SQLite is the default. |
| **Queue** | DB-backed `workflow_jobs` table | Provider-specific queue implementations for reliable, at-least-once job processing. |
| **Worker** | `BackgroundService` | Consumes jobs from the database queue; runs in-process or as a separate daemon. |
| **Scheduler** | Cron (`Cronos`) polling service | Enqueues due `schedule.trigger` workflows. |
| **Plugins** | Roslyn C# compiler | Compiles `.cs` source from `plugins/` into a collectible `AssemblyLoadContext` at runtime. |
| **AI** | Microsoft Semantic Kernel | Orchestrates OpenAI, Azure OpenAI, Anthropic, Gemini, Groq, Mistral, Cohere, Ollama, and more. |
| **Logging** | Serilog | Console + rolling file sinks; optional OpenTelemetry/OTLP export. |

## Execution Flow

```text
Trigger (manual / webhook / schedule / IMAP / chat / workflow / error)
        │
        ▼
WorkflowRunner ──► (enqueue job)  or  (ExecuteNowAsync for synchronous webhooks)
        │
        ▼
WorkflowExecutionEngine
        │  parse definition (nodes + connections)
        │  split main flow vs. AI sub-node connections
        │  topological order (Kahn) + reachability from start nodes
        │  per node: resolve input items → INodeType.ExecuteAsync → port outputs
        ▼
NodeRunData log  ──►  live events (SignalR)  +  persisted execution record
```

### The execution engine

[`WorkflowExecutionEngine`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Workflows/WorkflowExecutionEngine.cs) interprets a workflow's JSON definition as a directed graph:

- **Topological scheduling** — nodes run in dependency order (Kahn's algorithm); cyclic remnants fall back to definition order.
- **Multi-output routing** — branching nodes (IF, Switch) emit items on numbered output ports, routed to the correct downstream inputs.
- **Loop regions** — a `flow.loopOverItems` node and the sub-graph reachable from its *loop* port form a region the engine drives in batches, including nested loops.
- **AI sub-nodes** — connections into non-`main` input ports (Model, Tool, Memory) are separated out; the agent node consumes them via the [`IAgentExecutor`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Nodes/Agents/IAgentExecutor.cs).
- **Pinned data** — a node can carry sample data (`__pinned`) that flows downstream without executing it, useful for testing without live trigger events.
- **Partial execution** — running "up to" a node executes only that node and its ancestors, so you can test a sub-graph in the designer.

### Nodes

Every node implements [`INodeType`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Nodes/INodeType.cs): a `Definition` (palette metadata, parameters, ports, credentials) and an `ExecuteAsync` method. Nodes are discovered automatically by assembly scanning into the [`INodeRegistry`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Nodes/INodeRegistry.cs) and surfaced — localized — through the `INodeCatalog`. See [Built-in Nodes](built-in-nodes.md) and [Plugin Development](plugin-development.md).

### Queue and worker

`WorkflowRunner` either runs a workflow synchronously (webhooks that need a response) or enqueues a job in the database `workflow_jobs` table. The [`QueueWorkerService`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Queue/QueueWorkerService.cs) leases and processes jobs up to `Worker:MaxConcurrency` at a time. Because the queue lives in the database, Web and Worker can be scaled independently. See [Executions & Data](executions-and-data.md).

### Multi-process and scaling

Run the Worker separately (`Worker:RunInWebProcess = false`) and scale Web and Worker independently. A shared Redis backplane keeps live execution events flowing to every connected browser regardless of which process executed the workflow. See [Deployment](deployment.md).
