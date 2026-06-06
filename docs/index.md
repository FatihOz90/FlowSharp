---
layout: home

hero:
  name: "FlowSharp"
  text: "Node-Based Workflow Automation"
  tagline: "Build, run, and extend automations with C# / .NET 10, Blazor, and runtime Roslyn plugins."
  image:
    src: /assets/flowsharp-hero.png
    alt: FlowSharp workflow designer hero
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: Built-in Nodes
      link: /guide/built-in-nodes
    - theme: alt
      text: GitHub
      link: https://github.com/FlowSharp/FlowSharp

features:
  - icon: ⚡
    title: Real Executable Nodes
    details: HTTP, databases (Postgres / SQL Server / MySQL / SQLite), email, messaging, data transforms, and sandboxed JavaScript — connected on a visual canvas.
    link: /guide/built-in-nodes
  - icon: 🤖
    title: AI Agents & RAG
    details: Semantic Kernel agents with model, tool, and memory sub-nodes. Local-embedding vector store for retrieval. OpenAI, Azure, Anthropic, Gemini, Groq, Mistral, Ollama, and more.
    link: /guide/ai-agents
  - icon: 🔌
    title: Roslyn Hot-Plugins
    details: Drop C# source into the plugins folder; it compiles and loads at runtime into a collectible context — no rebuild, no restart.
    link: /guide/plugin-development
  - icon: 🪝
    title: Triggers & Webhooks
    details: Manual, schedule (cron), webhook, IMAP, chat, WhatsApp, workflow, and error triggers — with synchronous webhook responses.
    link: /guide/triggers-and-scheduling
  - icon: 🔐
    title: Secure by Design
    details: ASP.NET Core Identity, role/permission policies, AES-GCM encrypted credentials, owner-scoped data isolation, and SSRF egress controls.
    link: /guide/roles-and-permissions
  - icon: 📈
    title: Scales & Observable
    details: DB-backed queue, independent Web/Worker scaling, Redis live events, health probes, Serilog, and OpenTelemetry. Runs on Docker and Kubernetes.
    link: /guide/deployment
---
