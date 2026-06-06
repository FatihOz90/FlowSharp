# Plugin Development

FlowSharp has a **Roslyn-powered, hot-loadable** plugin system. You write new nodes in C#, drop the source into the `plugins/` directory, and FlowSharp compiles and loads them at runtime — no rebuild or restart of the main application required.

## How plugin loading works

The [`PluginManager`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Infrastructure/Plugins/PluginManager.cs):

1. Treats **every subfolder** of the configured `Plugins:Path` (default `plugins/`) as a separate plugin.
2. Compiles **all `.cs` files** in that folder (including nested subfolders) with the Roslyn C# compiler.
3. Loads the resulting assembly into a **collectible `AssemblyLoadContext`**, so a plugin can be reloaded or removed without restarting the app.
4. Registers every discovered `INodeType` into the node registry, and any node translations it provides.

A compile error in one plugin is reported (and surfaced on the Marketplace screen); the faulty plugin is skipped and the others keep working.

## Writing a node

Implement [`INodeType`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Nodes/INodeType.cs) directly, or inherit a convenience base class from `NodeType` / `PerItemNodeType`. `PerItemNodeType` handles iterating input items for you — you implement `ProcessItemAsync` for a single item.

### Example: `plugins/Sample/HelloNode.cs`

```csharp
using System.Text.Json.Nodes;
using FlowSharp.Application.Nodes;
using FlowSharp.Domain.Nodes;

namespace Community.Sample;

public sealed class HelloNode : PerItemNodeType
{
    public override NodeDefinition Definition { get; } = new(
        Key: "community.hello",
        DisplayName: "Hello (Plugin)",
        Category: NodeCategory.Core,
        Kind: NodeKind.Action,
        Description: "Adds a greeting to the item.",
        Parameters:
        [
            new NodeParameterDefinition("name", "Name", NodeParameterType.String, DefaultValue: "World")
        ],
        Tags: ["community"],
        Icon: "sparkles",
        Color: "#9b51e0");

    protected override Task<NodeItem?> ProcessItemAsync(INodeExecutionContext context, NodeItem item, int index)
    {
        var name = context.GetString("name", index) ?? "World";
        var output = (JsonObject)item.Json.DeepClone();
        output["greeting"] = $"Hello, {name}!";
        return Task.FromResult<NodeItem?>(NodeItem.From(output));
    }
}
```

The node appears in the palette the moment the plugin loads.

### The node definition

[`NodeDefinition`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Domain/Nodes/NodeDefinition.cs) describes everything the UI and engine need:

| Field | Purpose |
|---|---|
| `Key` | Stable identifier stored in workflow definitions (keep it unique and versioned-friendly). |
| `DisplayName` / `Description` | Palette text (localizable). |
| `Category` / `Kind` | Grouping and behavior class (Trigger, Action, Transform, Condition, Ai). |
| `Parameters` | Inputs rendered in the node panel (`String`, `Boolean`, `Number`, `Json`, `Credential`, …). |
| `Inputs` / `Outputs` | Ports. Defaults to a single `main` in/out; triggers default to no input. Use extra ports for branching or AI sub-nodes. |
| `Credentials` | Credential type keys this node uses. See [Credentials](credentials.md). |
| `Icon` / `Color` / `Tags` / `SubCategory` | Presentation and search. |

### Reading parameters and data

Inside `ExecuteAsync` / `ProcessItemAsync`, use the [`INodeExecutionContext`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Nodes/INodeExecutionContext.cs):

- `GetString` / `GetBoolean` / `GetNumber` / `GetInt` / `GetJson` — read parameters with [expressions](expressions.md) resolved for the given item index.
- `ResolveValue` — resolve expressions inside an arbitrary JSON structure.
- `GetCredentialAsync(type, name, field)` — read a decrypted credential field.
- `Items`, `Trigger`, `Services`, `Log` — input items, trigger payload, DI services, and run logging.

### Dynamic options and outputs

Implement `IHasDynamicOptions` to populate a dropdown at design time (e.g. list tables from a connected database), or `IHasDynamicOutputs` to vary output ports based on parameters (e.g. Switch).

### Credentials in a plugin

Expose a schema with `IProvidesCredentials` (next to the node) or `ICredentialType` (standalone). The designer renders the credential form from the schema. See [Credentials](credentials.md#credential-types-and-schemas).

## Local development loop

1. Create a folder under `plugins/` (e.g. `plugins/MyNode/`).
2. Add one or more `.cs` files implementing your node(s).
3. Start (or reload) FlowSharp — the plugin compiles and the node shows up in the palette.
4. Iterate; reload the plugin from the Marketplace screen to pick up changes without a full restart.

Reference examples ship in the repository's `plugins/` folder: `Sample`, `Template`, `EditFields`, `Deduplicate`, `Validate`, `JsonPathExtract`, `ExchangeRates`, `OpenWeather`.

## Security

::: danger Plugins run in-process with full privileges
Compiled plugin code runs with the same trust as FlowSharp itself. Only install plugins from **trusted sources**, and keep `plugins.manage` restricted to administrators. See [Roles & Permissions](roles-and-permissions.md).
:::

## Sharing plugins

Community plugins are hosted in a dedicated repository: **[FlowSharp Plugins](https://github.com/FlowSharp/plugins)**. Open a pull request there; approved plugins become installable from the in-app [Marketplace](marketplace.md).
