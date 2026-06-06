# Marketplace

FlowSharp includes an in-app marketplace where **administrators** browse, install, update, and remove plugins at runtime — no restart or rebuild. It's the UI front-end for the [plugin system](plugin-development.md).

Access requires the `plugins.manage` permission, which is `Admin`-only because plugins run trusted, in-process code. See [Roles & Permissions](roles-and-permissions.md).

## Installing from GitHub

1. **Point at a repository** — enter a GitHub URL: a repo root (`https://github.com/owner/repo`) or a subpath (`https://github.com/owner/repo/tree/main/path`). The default repository comes from `Plugins:OfficialMarketplaceUrl`.
2. **Browse** — the manager downloads the repository as a zip and lists installable plugins. Each **top-level folder containing `.cs` files** is one plugin; if the repository root itself contains `.cs` files, the whole repo is treated as a single plugin.
3. **Install** — the selected plugin's source is written into your `Plugins:Path` directory (default `plugins/`).
4. **Compile & load** — Roslyn compiles the source in-process into a collectible `AssemblyLoadContext`.
5. **Register** — the new `INodeType` implementations are added to the registry and appear in the designer palette immediately.

Compile errors are shown on the Marketplace screen; a faulty plugin is skipped and does not affect others already loaded.

## Managing installed plugins

- **Reload** — recompile a plugin after its source changed, without restarting the app.
- **Remove** — unload the plugin and delete its folder from `Plugins:Path`.

## Configuration

```jsonc
{
  "Plugins": {
    "Path": "plugins",
    "OfficialMarketplaceUrl": "https://github.com/FlowSharp/plugins"
  }
}
```

| Key | Description |
|---|---|
| `Plugins:Path` | Directory where installed plugins live (absolute, or relative to the content root). In Docker this is a mounted volume so plugins persist across container restarts. |
| `Plugins:OfficialMarketplaceUrl` | The default GitHub repository browsed by the marketplace. |

::: tip Persist the plugins volume
In containerized deployments, mount `Plugins:Path` to a durable volume (the default `docker-compose.yml` does this with `flowsharp_plugins`) so installed plugins survive redeploys. See [Deployment](deployment.md).
:::

## Security

::: danger Only install from trusted sources
Plugins execute in-process with full privileges. Review plugin source before installing, restrict `plugins.manage` to administrators, and prefer the official, reviewed repository.
:::

Implementation: [`PluginManager`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Plugins/PluginManager.cs).
