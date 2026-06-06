# Built-in Nodes

FlowSharp discovers nodes automatically from `INodeType` implementations and groups them by category in the designer palette. Display names and descriptions are localized to the UI language. This page lists the nodes that ship in the box; you can add your own via [plugins](plugin-development.md).

Each node is identified by a stable **key** (e.g. `http.request`). Keys are what workflow definitions store, so they are stable across UI translations.

## Triggers

Triggers start a workflow. See [Triggers & Scheduling](triggers-and-scheduling.md) for behavior details.

| Key | Name | Description |
|---|---|---|
| `manual.trigger` | Manual Trigger | Starts a workflow manually from the designer. |
| `schedule.trigger` | Schedule Trigger | Runs periodically from a cron expression. |
| `webhook.trigger` | Webhook | Starts from an incoming HTTP request. |
| `email.imap.trigger` | Email Trigger (IMAP) | Starts when new email arrives. |
| `chat.trigger` | AI Chat UI | Starts from the built-in chat interface. |
| `whatsapp.trigger` | WhatsApp Trigger | Starts from a WhatsApp Cloud API message webhook. |
| `flow.executeWorkflowTrigger` | Execute Workflow Trigger | Starts when called by another workflow. |
| `error.trigger` | Error Trigger | Runs when another workflow fails. |

## Core (Flow & Logic)

| Key | Name | Description |
|---|---|---|
| `if.condition` | IF | Branches true/false from a condition. |
| `switch.condition` | Switch | Routes items to different outputs by rule. |
| `filter.items` | Filter | Drops items that don't match a condition. |
| `merge.items` | Merge | Combines two or more branches into one stream. |
| `set.fields` | Set | Adds or overwrites item fields. |
| `no.op` | No Operation | Passes data through unchanged. |
| `flow.wait` | Wait | Pauses the workflow for a duration. |
| `flow.stopAndError` | Stop And Error | Stops the workflow with a custom error. |
| `flow.loopOverItems` | Loop Over Items | Processes items in batches (loop region). |
| `flow.executeWorkflow` | Execute Workflow | Runs another workflow and returns its output. |
| `core.stickyNote` | Note / Group | Canvas note for grouping and documenting nodes. |

## Data

| Key | Name | Description |
|---|---|---|
| `datetime.action` | Date & Time | Produces or formats date/time values. |
| `transform.crypto` | Crypto | Hash, HMAC, and Base64 operations. |
| `transform.csv` | CSV | Converts items to CSV, or parses CSV into items. |
| `transform.spreadsheet` | Spreadsheet | Reads Excel/CSV; each row becomes an item. |
| `transform.htmlExtract` | HTML Extract | Extracts values from HTML with CSS selectors. |
| `transform.htmlToText` | HTML to Text | Converts HTML to plain text or Markdown. |
| `transform.xmlJson` | XML ↔ JSON | Converts XML to JSON or JSON to XML. |

> The **Set** and **Filter** nodes are categorized under Data; flow/logic primitives like IF, Switch, and Merge live under Core.

## HTTP

| Key | Name | Description |
|---|---|---|
| `http.request` | HTTP Request | Calls any REST API with a selectable method. |
| `http.get` | HTTP GET | Fixed GET request. |
| `http.post` | HTTP POST | Fixed POST request. |
| `http.put` | HTTP PUT | Fixed PUT request. |
| `http.patch` | HTTP PATCH | Fixed PATCH request. |
| `http.delete` | HTTP DELETE | Fixed DELETE request. |
| `webhook.response` | Respond to Webhook | Returns a custom HTTP response to the webhook caller. |

Outbound requests respect the `HttpNodes` egress settings (private-network blocking / SSRF protection). See [Configuration](configuration.md#http-nodes-egress-safety).

## Database

FlowSharp uses a **connection → operation** model. A *connection* node selects a credential (or a local file, for SQLite) and emits a reusable connection context; downstream *operation* nodes consume it. This means one credential drives many operations and full multi-table CRUD.

### Connections

| Key | Name | Description |
|---|---|---|
| `db.postgres.connection` | Postgres Connection | Reusable PostgreSQL connection (credential-based). |
| `db.sqlServer.connection` | SQL Server Connection | Reusable SQL Server connection (credential-based; supports Integrated Security). |
| `db.mysql.connection` | MySQL Connection | Reusable MySQL connection (credential-based). |
| `db.sqlite.connection` | SQLite Connection | Local, file-based connection isolated per workflow (`App_Data/{workflowId}/{database}.db`); no credential required. |

### Operations

| Key | Name | Description |
|---|---|---|
| `db.table` | Table | Selects a table from the upstream connection and loads column metadata. |
| `db.ensureTable` | Ensure Table | Creates the table from column definitions if it doesn't exist. |
| `db.select` | Select | Reads rows. |
| `db.insert` | Insert | Inserts rows. |
| `db.update` | Update | Updates rows. |
| `db.upsert` | Insert or Update | Inserts or updates (upsert). |
| `db.delete` | Delete | Deletes rows. |
| `db.executeQuery` | Execute Query | Runs an arbitrary SQL statement. |

## Communication

| Key | Name | Description |
|---|---|---|
| `email.send` | Send Email | Sends email over SMTP. |
| `telegram.message` | Telegram | Sends a Telegram message. |
| `slack.message` | Slack | Sends a Slack message. |
| `discord.message` | Discord | Sends to a Discord webhook. |
| `whatsapp.message` | WhatsApp | Sends a WhatsApp Cloud API message. |

## Developer

| Key | Name | Description |
|---|---|---|
| `code.javascript` | Code | Runs sandboxed JavaScript (Jint) over the incoming items. |

## AI

See [AI Agents & RAG](ai-agents.md) for the agent/sub-node architecture and RAG details.

### Agent

| Key | Name | Description |
|---|---|---|
| `ai.agent` | AI Agent | Tool-calling agent; consumes Model, Tool, and Memory sub-nodes. |

### Chat models (standalone)

Direct chat-completion nodes that call a provider and return its response:

| Key | Name |
|---|---|
| `openai.chat` | OpenAI |
| `azureopenai.chat` | Azure OpenAI |
| `anthropic.chat` | Anthropic Claude |
| `gemini.chat` | Google Gemini |
| `groq.chat` | Groq |
| `mistral.chat` | Mistral AI |
| `cohere.chat` | Cohere |
| `huggingface.chat` | Hugging Face |
| `openrouter.chat` | OpenRouter |
| `ollama.chat` | Ollama (local) |

### Model sub-nodes (for the AI Agent)

Each provider also has a `*.chatmodel` sub-node that plugs into the AI Agent's **Model** port: `openai.chatmodel`, `azureopenai.chatmodel`, `anthropic.chatmodel`, `gemini.chatmodel`, `groq.chatmodel`, `mistral.chatmodel`, `cohere.chatmodel`, `huggingface.chatmodel`, `openrouter.chatmodel`, `ollama.chatmodel`.

### Tools & memory sub-nodes

| Key | Name | Description |
|---|---|---|
| `tool.calculator` | Calculator | Math tool the agent can call. |
| `tool.httpRequest` | HTTP Request Tool | Lets the agent make HTTP calls. |
| `tool.mcp` | MCP Client | Connects the agent to a Model Context Protocol server's tools. |
| `rag.memory` | Vector Store: Memory | Provides the agent with retrieval memory over the vector store. |

### RAG (Vector Store)

| Key | Name | Description |
|---|---|---|
| `rag.insert` | Vector Store: Insert | Embeds text into the per-workspace SQLite vector store. |
| `rag.query` | Vector Store: Query | Semantic search over the vector store. |

Embeddings are generated by a bundled local model — no API key required. See [AI Agents & RAG](ai-agents.md).
