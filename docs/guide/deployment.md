# Deployment

FlowSharp ships two deployable apps — **Web** (Blazor UI + endpoints) and **Worker** (queue consumer) — from a single multi-stage `Dockerfile`. You can run everything in one process for small setups, or scale Web and Worker independently for production.

## Docker images

The [`Dockerfile`](https://github.com/FlowSharp/FlowSharp/blob/main/Dockerfile) defines two final targets on a .NET 10 Alpine runtime:

```bash
docker build --target web    -t flowsharp-web:latest .
docker build --target worker -t flowsharp-worker:latest .
```

Both expose port `8080`. The images create `plugins/`, `logs/`, and `rag_db/` directories for runtime data.

## Docker Compose

The bundled [`docker-compose.yml`](https://github.com/FlowSharp/FlowSharp/blob/main/docker-compose.yml) brings up Web, Worker, Redis, Postgres, and PgBouncer:

```bash
docker compose up -d --build
# UI at http://localhost:8080  (admin@flowsharp.local / Admin!2345)
```

Out of the box, Web and Worker use **SQLite** on a shared volume and **Redis** for cross-process events. Persistent volumes back the SQLite database, plugins, logs, and RAG data so they survive restarts.

### Switching to PostgreSQL

Postgres and PgBouncer are already defined in the compose file. To route the app through PgBouncer (transaction pooling), change the Web and Worker environment:

```yaml
environment:
  - Database__Provider=Postgres
  - ConnectionStrings__DefaultConnection=Host=pgbouncer;Port=5432;Database=flowsharp_db;Username=postgres;Password=postgres;No Reset On Close=true;Max Auto Prepare=0
```

::: tip PgBouncer transaction mode
In transaction pooling mode server-side prepared statements aren't supported, so the connection string disables them with `No Reset On Close=true;Max Auto Prepare=0`. PgBouncer lets many Web/Worker instances share a small pool of real database connections.
:::

### Production checklist for Compose

- Set a unique `Security__CredentialEncryptionKey` (Base64, 32 bytes) — **identical** on Web and Worker.
- Change the seeded admin credentials.
- Use Postgres (or SQL Server) instead of SQLite for multi-instance setups.
- Keep `Redis__ConnectionString` shared so live events reach every Web instance.
- If you enable `BlobStorage__Enabled`, point `BlobStorage__Directory` at a **shared** volume.

## Kubernetes

The [`k8s/`](https://github.com/FlowSharp/FlowSharp/tree/main/k8s) folder contains manifests for a horizontally scalable deployment: Web (multiple instances) + Worker (autoscaled) + PgBouncer + Redis + Postgres.

| File | Contents |
|---|---|
| `00-namespace.yaml` | `flowsharp` namespace |
| `10-secret.example.yaml` | Example secret (create the real one with `kubectl create secret`) |
| `11-configmap.yaml` | Non-secret settings (provider, Redis, rate limit, OTel, worker concurrency) |
| `20-redis.yaml` | Redis (cross-process Pub/Sub) |
| `21-postgres.yaml` | Postgres StatefulSet (dev/test; use managed Postgres in production) |
| `22-pgbouncer.yaml` | PgBouncer connection pooling (transaction mode) |
| `40-web.yaml` | Web Deployment + Service + Ingress |
| `41-worker.yaml` | Worker Deployment |
| `50-web-hpa.yaml` | Web HPA (CPU) |
| `51-worker-hpa.yaml` | Worker HPA (CPU) — simple option |
| `52-worker-keda-scaledobject.yaml` | Worker autoscaling on **queue depth** (KEDA) — recommended |

### Deploy

```bash
kubectl apply -f k8s/00-namespace.yaml

kubectl -n flowsharp create secret generic flowsharp-secrets \
  --from-literal=CredentialEncryptionKey="$(openssl rand -base64 32)" \
  --from-literal=PostgresPassword="A-STRONG-PASSWORD"

kubectl apply -f k8s/11-configmap.yaml -f k8s/20-redis.yaml -f k8s/21-postgres.yaml -f k8s/22-pgbouncer.yaml
kubectl apply -f k8s/40-web.yaml -f k8s/41-worker.yaml

kubectl apply -f k8s/50-web-hpa.yaml
# Choose ONE for the worker:
kubectl apply -f k8s/52-worker-keda-scaledobject.yaml   # KEDA (recommended)
# kubectl apply -f k8s/51-worker-hpa.yaml               # CPU-based (no KEDA)
```

::: warning Don't apply both worker scalers
`51-worker-hpa.yaml` and `52-worker-keda-scaledobject.yaml` both manage the same Deployment. Apply only one — KEDA creates its own HPA.
:::

## Scaling model

- **Web → CPU HPA.** Blazor Server is memory/CPU sensitive; target ~70% CPU, 2–10 replicas.
- **Worker → queue depth (KEDA).** Scales on the number of runnable jobs in `workflow_jobs` (`desiredReplicas = ceil(pending / 20)`, 1–20 replicas). I/O-bound work like LLM calls keeps CPU low while the queue grows — exactly where CPU-based autoscaling falls short. Without KEDA, the CPU-based worker HPA is a reasonable fallback.

## Production notes

- **Migration race.** Only the **Web** app applies migrations (`Database__ApplyMigrationsOnStartup=true`); it's disabled on the Worker. On first deploy, multiple Web replicas could try to migrate at once — start Web at `replicas: 1`, let migrations finish, then scale up; or run a dedicated migration Job. (EF Core's migration lock also coordinates concurrent startups — see [Database & Migrations](database-migrations.md).)
- **Managed Postgres.** The StatefulSet is for dev/test. In production use a managed database and point PgBouncer's `DB_HOST` at it (skip `21-postgres.yaml`).
- **Shared state.** SQLite and blob offload require a shared (RWX) volume across replicas — prefer Postgres + object storage at scale.
- **Observability.** Set `OpenTelemetry__OtlpEndpoint` to an in-cluster collector, or set `OpenTelemetry__Enabled=false` if none. See [Observability](observability.md).

## Health probes

The Web app exposes probe endpoints for orchestrators (see [Observability](observability.md#health-checks)):

| Endpoint | Use |
|---|---|
| `/health/live` | Liveness — process is up (anonymous, no checks). |
| `/health/ready` | Readiness — critical dependencies (database) ready (anonymous). |
| `/health` | Detailed component report (**Admin only**). |
