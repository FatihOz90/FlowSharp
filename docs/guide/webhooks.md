# Webhooks

Webhooks let external systems trigger FlowSharp workflows over HTTP — and, optionally, receive a synchronous response. They turn a workflow into a custom HTTP endpoint.

## Endpoint scheme

When a workflow contains a `webhook.trigger` node and is **Active**, FlowSharp serves it under a workflow-scoped path:

```text
/webhook/{workflowKey}/{path}
```

- **`workflowKey`** is a stable per-workflow key, so two workflows can use the same `path` without colliding.
- **`path`** is the catch-all path you configure on the trigger node; it may be empty (`/webhook/{workflowKey}`).
- The HTTP **method** is matched too, so the same path can route different verbs to different workflows.

Requests are matched by `(workflowKey, method, path)`. No match returns **404** with a JSON error.

## Synchronous execution and responses

Webhook requests execute the workflow **synchronously** and return a result to the caller:

1. If the run contains a successful **`Respond to Webhook`** (`webhook.response`) node, its output defines the HTTP response — `statusCode`, `headers`, and `body`. This lets you build full custom HTTP APIs in the designer.
2. Otherwise, on success the workflow output is returned as JSON (`200`); on failure a `500` with the error is returned.

### Custom response shape

The `Respond to Webhook` node produces an item like:

```json
{
  "statusCode": 200,
  "headers": { "Content-Type": "application/json" },
  "body": "{\"ok\":true}"
}
```

If `Content-Type` is omitted, FlowSharp sends `application/json` when the body is valid JSON, otherwise `text/plain`.

## Request payload

The trigger payload passed into the workflow is built from the incoming request:

```json
{
  "source": "webhook",
  "node": "Webhook",
  "method": "POST",
  "path": "orders/new",
  "query": { "ref": "abc" },
  "headers": { "Content-Type": "application/json" },
  "body": { "...": "parsed JSON, or the raw string if not JSON" }
}
```

Reference these fields in downstream nodes with expressions — for example the `body.orderId` field via `$trigger`, or a header such as `X-Signature`:

```text
{{ $trigger.body.orderId }}
{{ $trigger.headers["X-Signature"] }}
```

See [Expressions](expressions.md).

## WhatsApp / Meta support

The webhook pipeline has first-class handling for the WhatsApp Cloud API (and Meta webhooks in general):

- **Verification handshake** — a `GET` with `hub.mode=subscribe&hub.challenge=...` returns the challenge as `text/plain` **without running the workflow**, so subscription verification succeeds.
- **Payload normalization** — when the body is a WhatsApp event, FlowSharp sets `source` to `whatsapp` and adds a normalized `whatsapp` object alongside the raw `body`.
- **Event filtering** — status callbacks the outbound message node generates (sent / delivered / read) are filtered so they don't re-trigger the workflow (and your AI). Ignored events get a `200` with `{ "ignored": true }`, because Meta disables webhooks that don't return `2xx`.

See [Triggers & Scheduling](triggers-and-scheduling.md) for the WhatsApp trigger and [Built-in Nodes](built-in-nodes.md#communication) for the WhatsApp message node.

## Rate limiting

Webhook runs are subject to the per-user run limit (`RateLimit:RunsPerMinutePerUser`). When exceeded, the request returns `200` with `{ "ignored": true, "reason": "rate_limited" }` (again, to keep `2xx`-expecting providers happy) and the run is skipped. See [Configuration](configuration.md#rate-limiting).

## Error handling

- Unmatched route → `404` with a JSON error.
- Rate limited → `200` `{ "ignored": true, "reason": "rate_limited" }`.
- Unexpected error during execution → `500` with a JSON error.
- Failed workflow with no `Respond to Webhook` node → `500` with the workflow error.

Implementation: [`WebhookEndpoints`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Web/Endpoints/WebhookEndpoints.cs).
