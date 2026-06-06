# Triggers & Scheduling

A trigger is the node that starts a workflow. Every workflow begins with exactly one active trigger path. This page explains each trigger type and how scheduling works.

## How triggers fire

The engine distinguishes **manual** starts from **event-based** triggers:

- **Manual Trigger** runs only when you click *Execute* in the designer (or run partial/up-to-node executions).
- **Event-based triggers** (webhook, schedule, IMAP, WhatsApp, chat, workflow, error) fire from real events — but **only when the workflow is toggled Active**. An inactive workflow never reacts to external events.

When you execute a workflow manually that has multiple trigger types on the canvas, the engine starts from the Manual Trigger if one is present, so you can test without firing live events. Real events arrive with an explicit start node and are unaffected by this.

## Trigger types

### Manual Trigger — `manual.trigger`

Starts a run on demand from the designer. Ideal for building and testing. No activation required.

### Schedule Trigger — `schedule.trigger`

Runs a workflow on a **cron** schedule. See [Scheduling](#scheduling) below.

### Webhook — `webhook.trigger`

Exposes an HTTP endpoint that starts the workflow on an incoming request. Supports synchronous responses via the `Respond to Webhook` node and includes built-in handling for WhatsApp/Meta verification. See the dedicated [Webhooks](webhooks.md) guide.

### WhatsApp Trigger — `whatsapp.trigger`

A specialization built on the webhook pipeline: incoming WhatsApp Cloud API messages are normalized into a friendly `whatsapp` payload, with status callbacks (sent/delivered/read) filtered out so they don't re-trigger the flow. See [Webhooks](webhooks.md).

### Email Trigger (IMAP) — `email.imap.trigger`

Polls an IMAP mailbox using an `imap` credential and starts the workflow when new mail arrives. Configure host, port, and folder via the credential and node parameters.

### AI Chat UI — `chat.trigger`

Starts the workflow from FlowSharp's built-in chat interface. Commonly paired with an [AI Agent](ai-agents.md) to build a conversational assistant.

### Execute Workflow Trigger — `flow.executeWorkflowTrigger`

Marks a workflow as callable from another workflow's `Execute Workflow` node, enabling reusable sub-workflows. The trigger payload carries the caller's input (and sub-workflow depth).

### Error Trigger — `error.trigger`

Runs **when another workflow fails**. After a failed run, FlowSharp invokes workflows that start with an Error Trigger, passing details of the failing workflow and execution, so you can centralize alerting and recovery.

## Scheduling

The [`SchedulerService`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Triggers/SchedulerService.cs) is a background service that:

1. Polls about **every 20 seconds**.
2. Loads **active** workflows and inspects their `schedule.trigger` nodes.
3. Parses each cron expression with [Cronos](https://github.com/HangfireIO/Cronos) and computes the next run time.
4. **Enqueues** a job when the scheduled time is due (it does not execute inline — the queue worker picks it up).

Cron times are evaluated in **UTC**.

### Cron expressions

Cronos supports standard 5-field cron, plus seconds (6 fields) and macros:

```text
*/5 * * * *      every 5 minutes
0 * * * *        hourly, on the hour
0 9 * * 1-5      09:00 on weekdays
0 0 1 * *        midnight on the 1st of each month
@daily           once a day at midnight
```

::: warning Single-scheduler assumption
The scheduler is designed for a single active instance. If you run **multiple worker replicas**, add a distributed lock (or run exactly one scheduler) so a scheduled workflow isn't enqueued more than once per tick. See [Deployment](deployment.md).
:::

## Activating a workflow

Toggle a workflow **Active** to enable its event-based triggers (schedule, webhook, IMAP, WhatsApp, chat, workflow, error). Manual execution from the designer always works regardless of the active state and is the recommended way to test before activating.
