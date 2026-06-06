# AI Agents & RAG

FlowSharp integrates **Microsoft Semantic Kernel** to run tool-calling AI agents, direct chat-completion nodes, and retrieval-augmented generation (RAG) over a local vector store.

## Two ways to use AI

- **Chat model nodes** (`openai.chat`, `anthropic.chat`, `gemini.chat`, …) — a single call to a provider that returns a completion. Simple, linear, no tools.
- **AI Agent** (`ai.agent`) — an orchestrator that can pick a model, call tools, and use retrieval memory in a loop until it produces an answer.

See [Built-in Nodes](built-in-nodes.md#ai) for the full list of providers.

## The AI Agent and sub-nodes

The AI Agent is configured by **connecting sub-nodes** to its special input ports rather than by stuffing everything into one node. The engine separates these sub-node connections from the main data flow and hands them to the agent executor.

```text
        ┌──────────────┐
Model ──▶              │
 Tool ──▶   AI Agent   ──▶ main output
 Tool ──▶              │
Memory──▶              │
        └──────────────┘
```

| Port | Sub-node types | Purpose |
|---|---|---|
| **Model** (required) | `*.chatmodel` (e.g. `openai.chatmodel`, `anthropic.chatmodel`) | The LLM the agent reasons with. Exactly one is required. |
| **Tool** | `tool.calculator`, `tool.httpRequest`, `tool.mcp` | Capabilities the agent may invoke. Connect as many as needed. |
| **Memory** | `rag.memory` | Retrieval memory backed by the vector store. |

The orchestration lives in [`SemanticKernelAgentExecutor`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Nodes/Ai/Agent/SemanticKernelAgentExecutor.cs): it maps the connected model to a provider, resolves the credential, binds the connected tools (including MCP servers), and calls the LLM. The generic workflow engine only knows the [`IAgentExecutor`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Nodes/Agents/IAgentExecutor.cs) contract, keeping provider specifics in the AI layer.

### Choosing the model

On a model sub-node, set the `model` parameter (e.g. `gpt-4o`, `claude-...`) — if left blank, the provider's default (or the Azure deployment name) is used. Attach a credential for the provider's API key. **Ollama** runs locally and needs no API key.

## Tools

- **Calculator** (`tool.calculator`) — safe arithmetic.
- **HTTP Request Tool** (`tool.httpRequest`) — lets the agent call external APIs (subject to the `HttpNodes` egress policy — see [Configuration](configuration.md#http-nodes-egress-safety)).
- **MCP Client** (`tool.mcp`) — connects to a [Model Context Protocol](https://modelcontextprotocol.io) server and exposes its tools to the agent.

## RAG (Vector Store)

FlowSharp ships a built-in vector store so you can ground answers in your own data.

| Node | Key | Role |
|---|---|---|
| Vector Store: Insert | `rag.insert` | Embeds text and stores the vectors. |
| Vector Store: Query | `rag.query` | Semantic search; returns the most relevant chunks. |
| Vector Store: Memory | `rag.memory` | Connects retrieval to the AI Agent's Memory port. |

### How it works

- **Local embeddings** — text is embedded with a **bundled local model**. No external embedding API or key is required.
- **Per-workspace SQLite store** — each workflow gets its own SQLite vector database under `Rag:DatabaseDirectory` (default `App_Data/rag`), so one workflow's data stays isolated from another's. Implementation: [`SqliteVectorStore`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Ai/SqliteVectorStore.cs).

### Typical RAG flow

1. **Ingest** — load documents (HTTP/CSV/Spreadsheet/etc.), split text, and feed it to `rag.insert`.
2. **Retrieve & answer** — at query time, either:
   - use `rag.query` to fetch context and build a prompt for a chat model node, or
   - connect `rag.memory` to an AI Agent so retrieval happens automatically during reasoning.

## Building a conversational assistant

Pair the **AI Chat UI** trigger (`chat.trigger`) with an AI Agent: connect a model, the tools you want, and optionally `rag.memory`. The chat interface streams the agent's response (the executor surfaces token deltas), giving you an interactive assistant grounded in your workflows and data.

See [Triggers & Scheduling](triggers-and-scheduling.md#ai-chat-ui-chattrigger) and [Configuration → RAG](configuration.md#rag-vector-store).
