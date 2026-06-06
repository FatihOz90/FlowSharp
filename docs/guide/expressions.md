# Expressions

Expressions let a node's parameters reference data produced earlier in the workflow. Anywhere a parameter accepts text, wrap a reference in double curly braces:

```text
{{ $json.email }}
Hello {{ $json.firstName }}, your order {{ $json.orderId }} shipped.
```

At execution time the engine resolves each expression against the **current item** and the outputs of nodes that already ran.

## Single expression vs. mixed text

- If the whole value is a **single** expression, the raw JSON type is preserved — a number stays a number, an object stays an object, an array stays an array.
- If the value mixes text and expressions, the result is a **string**.

```text
{{ $json.count }}              → 42        (number)
Count is {{ $json.count }}     → "Count is 42"  (string)
```

## Reference roots

| Root | Resolves to |
|---|---|
| `$json` | The current input item's JSON object. |
| `$item` | The current item wrapped as `{ "json": ... }` — use `$item.json.field`. |
| `$node["Node Name"].json` | The output of another node (by its instance name), aligned to the current item index. |
| `$trigger` | The trigger payload that started the run (e.g. webhook body). |
| `$now` | Current UTC timestamp, ISO 8601 (`O` format). |
| `$today` | Current UTC date, `yyyy-MM-dd`. |
| `$itemIndex` | Zero-based index of the current item. |
| `$runIndex` | Current run index (loop iterations). |

## Accessing nested data

Both dot and bracket notation work, and the two can be mixed. Numeric accessors index into arrays.

```text
{{ $json.customer.name }}
{{ $json["customer"]["name"] }}
{{ $json.items[0].sku }}
{{ $node["HTTP Request"].json.data[2].id }}
```

If a path doesn't resolve (missing field, out-of-range index), a single-expression value resolves to nothing and the engine raises an expression error; design your data so referenced fields exist, or branch with an [IF node](built-in-nodes.md#core-and-logic) first.

## Referencing other nodes

`$node["Name"]` reads the **first output port** of the named node, picking the item at the current index (or the last item if the upstream produced fewer). The name is the node's instance name on the canvas (case-insensitive), not its type key.

```text
{{ $node["Set"].json.status }}
{{ $node["Loop Over Items"].json.batchTotal }}
```

## Scope: expressions vs. the Code node

The expression engine is a **safe, path-based resolver** — it reads and navigates data; it does not evaluate arbitrary JavaScript, run functions, or do arithmetic. For transformations (string manipulation, math, mapping, custom logic) use the **Code** node (`code.javascript`), which runs sandboxed JavaScript with [Jint](https://github.com/sebastienros/jint) and has full access to the incoming items. See [Built-in Nodes](built-in-nodes.md#developer).

## Where expressions are resolved

Node parameters are resolved through the execution context helpers — `GetString`, `GetJson`, `ResolveValue`, etc. — each taking the current `itemIndex`, so `$json` and `$item` always refer to the item currently being processed. Nodes that process items individually (most transform nodes) resolve expressions once per item. See [`INodeExecutionContext`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Application/Nodes/INodeExecutionContext.cs).
