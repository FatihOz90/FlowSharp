import { defineConfig } from 'vitepress'

export default defineConfig({
  base: '/FlowSharp/',
  title: "FlowSharp",
  description: "Node-based workflow automation platform built with C# / .NET 10 and Blazor",
  themeConfig: {
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Guide', link: '/guide/getting-started' },
      { text: 'Nodes', link: '/guide/built-in-nodes' },
      { text: 'Plugins', link: '/guide/plugin-development' }
    ],

    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'Getting Started', link: '/guide/getting-started' },
          { text: 'Architecture', link: '/guide/architecture' },
          { text: 'Configuration', link: '/guide/configuration' },
          { text: 'Database & Migrations', link: '/guide/database-migrations' },
          { text: 'Deployment', link: '/guide/deployment' }
        ]
      },
      {
        text: 'Building Workflows',
        items: [
          { text: 'Built-in Nodes', link: '/guide/built-in-nodes' },
          { text: 'Triggers & Scheduling', link: '/guide/triggers-and-scheduling' },
          { text: 'Expressions', link: '/guide/expressions' },
          { text: 'Credentials', link: '/guide/credentials' },
          { text: 'Webhooks', link: '/guide/webhooks' },
          { text: 'AI Agents & RAG', link: '/guide/ai-agents' },
          { text: 'Executions & Data', link: '/guide/executions-and-data' }
        ]
      },
      {
        text: 'Plugin System',
        items: [
          { text: 'Plugin Development', link: '/guide/plugin-development' },
          { text: 'Marketplace', link: '/guide/marketplace' }
        ]
      },
      {
        text: 'Operations',
        items: [
          { text: 'Roles & Permissions', link: '/guide/roles-and-permissions' },
          { text: 'Observability', link: '/guide/observability' }
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/FlowSharp/FlowSharp' }
    ],

    footer: {
      message: 'Released under the Elastic License 2.0 (ELv2).',
      copyright: 'Copyright © 2026 FlowSharp Authors'
    },

    search: {
      provider: 'local'
    }
  }
})
