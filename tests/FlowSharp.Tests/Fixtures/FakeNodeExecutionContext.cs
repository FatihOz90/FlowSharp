using System.Text.Json.Nodes;
using FlowSharp.Application.Abstractions;
using FlowSharp.Application.Nodes;
using FlowSharp.Application.Nodes.Expressions;
using FlowSharp.Infrastructure.Workflows.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSharp.Tests.Fixtures;

/// <summary>
/// Bir node'u motor olmadan tek basina test etmek icin hafif bir <see cref="INodeExecutionContext"/>.
/// Gercek <see cref="ExpressionEvaluator"/> kullanir; boylece {{ }} cozumlemesi de dolayli test edilir.
/// </summary>
public sealed class FakeNodeExecutionContext : INodeExecutionContext
{
    private readonly JsonObject parameters;
    private readonly IExpressionEvaluator evaluator;
    private readonly Inner inner;

    public FakeNodeExecutionContext(
        JsonObject? parameters = null,
        IReadOnlyList<NodeItem>? items = null,
        JsonObject? trigger = null,
        IReadOnlyDictionary<string, IReadOnlyList<NodeItem>>? nodeOutputs = null,
        IServiceProvider? services = null,
        ICredentialStore? credentialStore = null,
        HttpMessageHandler? httpHandler = null,
        string nodeKey = "test.node",
        string nodeName = "Test Node",
        int runIndex = 0)
    {
        this.parameters = parameters ?? new JsonObject();
        Items = items ?? [NodeItem.Empty()];
        Trigger = trigger;
        evaluator = new ExpressionEvaluator();

        var collection = new ServiceCollection();
        var httpBuilder = collection.AddHttpClient("workflow-nodes");
        if (httpHandler is not null)
        {
            httpBuilder.ConfigurePrimaryHttpMessageHandler(() => httpHandler);
        }

        if (credentialStore is not null)
        {
            collection.AddSingleton(credentialStore);
        }

        Services = services ?? collection.BuildServiceProvider();

        inner = new Inner(
            nodeKey, nodeName, this.parameters, Items,
            nodeOutputs ?? new Dictionary<string, IReadOnlyList<NodeItem>>(),
            Trigger, runIndex, evaluator, Services, Logs.Add, CancellationToken.None);
    }

    public List<string> Logs { get; } = [];

    public string NodeKey => inner.NodeKey;
    public string NodeName => inner.NodeName;
    public IReadOnlyList<NodeItem> Items { get; }
    public IServiceProvider Services { get; }
    public CancellationToken CancellationToken => CancellationToken.None;
    public JsonObject? Trigger { get; }
    public Guid? WorkflowId => inner.WorkflowId;

    public JsonNode? GetRawParameter(string name) => inner.GetRawParameter(name);
    public string? GetString(string name, int itemIndex = 0, string? defaultValue = null) => inner.GetString(name, itemIndex, defaultValue);
    public bool GetBoolean(string name, int itemIndex = 0, bool defaultValue = false) => inner.GetBoolean(name, itemIndex, defaultValue);
    public double GetNumber(string name, int itemIndex = 0, double defaultValue = 0) => inner.GetNumber(name, itemIndex, defaultValue);
    public int GetInt(string name, int itemIndex = 0, int defaultValue = 0) => inner.GetInt(name, itemIndex, defaultValue);
    public JsonNode? GetJson(string name, int itemIndex = 0) => inner.GetJson(name, itemIndex);
    public JsonNode? ResolveValue(JsonNode? value, int itemIndex = 0) => inner.ResolveValue(value, itemIndex);
    public Task<string?> GetCredentialAsync(string type, string name, string field) => inner.GetCredentialAsync(type, name, field);
    public void Log(string message) => inner.Log(message);

    /// <summary>
    /// Gercek (internal) NodeExecutionContext'i sarmalar. InternalsVisibleTo gerektirmemek icin
    /// ayni davranisi tasiyan kucuk bir kopya degil, paramnetre okuma mantigini yeniden uretiriz.
    /// </summary>
    private sealed class Inner(
        string nodeKey,
        string nodeName,
        JsonObject parameters,
        IReadOnlyList<NodeItem> items,
        IReadOnlyDictionary<string, IReadOnlyList<NodeItem>> nodeOutputs,
        JsonObject? trigger,
        int runIndex,
        IExpressionEvaluator evaluator,
        IServiceProvider services,
        Action<string> logSink,
        CancellationToken cancellationToken)
    {
        public string NodeKey => nodeKey;
        public string NodeName => nodeName;
        public Guid? WorkflowId => null;

        public JsonNode? GetRawParameter(string name) =>
            parameters.TryGetPropertyValue(name, out var value) ? value : null;

        public string? GetString(string name, int itemIndex, string? defaultValue)
        {
            var raw = GetRawParameter(name);
            if (raw is null) return defaultValue;
            if (raw is JsonValue value && value.TryGetValue<string>(out var text))
            {
                var resolved = evaluator.EvaluateToString(text, BuildContext(itemIndex));
                return string.IsNullOrEmpty(resolved) ? defaultValue : resolved;
            }
            return raw.ToJsonString();
        }

        public bool GetBoolean(string name, int itemIndex, bool defaultValue)
        {
            var node = GetJson(name, itemIndex);
            return node switch
            {
                JsonValue v when v.TryGetValue<bool>(out var b) => b,
                JsonValue v when v.TryGetValue<string>(out var s) && bool.TryParse(s, out var parsed) => parsed,
                _ => defaultValue
            };
        }

        public double GetNumber(string name, int itemIndex, double defaultValue)
        {
            var node = GetJson(name, itemIndex);
            return node switch
            {
                JsonValue v when v.TryGetValue<double>(out var d) => d,
                JsonValue v when v.TryGetValue<string>(out var s) &&
                    double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => defaultValue
            };
        }

        public int GetInt(string name, int itemIndex, int defaultValue) => (int)GetNumber(name, itemIndex, defaultValue);

        public JsonNode? GetJson(string name, int itemIndex)
        {
            var raw = GetRawParameter(name);
            if (raw is null) return null;
            if (raw is JsonValue value && value.TryGetValue<string>(out var text) && evaluator.ContainsExpression(text))
            {
                return evaluator.EvaluateToNode(text, BuildContext(itemIndex))?.DeepClone();
            }
            return raw.DeepClone();
        }

        public JsonNode? ResolveValue(JsonNode? value, int itemIndex)
        {
            switch (value)
            {
                case null: return null;
                case JsonObject obj:
                    var result = new JsonObject();
                    foreach (var pair in obj) result[pair.Key] = ResolveValue(pair.Value, itemIndex);
                    return result;
                case JsonArray array:
                    var resolvedArray = new JsonArray();
                    foreach (var element in array) resolvedArray.Add(ResolveValue(element, itemIndex));
                    return resolvedArray;
                case JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text) && evaluator.ContainsExpression(text):
                    // Uretim koduyla ayni: parent'li dugumu kopyalayarak don (bkz. NodeExecutionContext).
                    return evaluator.EvaluateToNode(text, BuildContext(itemIndex))?.DeepClone();
                default:
                    return value.DeepClone();
            }
        }

        public async Task<string?> GetCredentialAsync(string type, string name, string field)
        {
            var store = services.GetService(typeof(ICredentialStore)) as ICredentialStore;
            if (store is null) { logSink("Credential store kayitli degil."); return null; }
            var data = Guid.TryParse(name, out var credId)
                ? await store.ResolveAsync(credId, null, cancellationToken)
                : await store.ResolveAsync(type, name, null, cancellationToken);
            if (data is null) { logSink($"Credential bulunamadi: {type}/{name}"); return null; }
            return data.TryGetValue(field, out var value) ? value : null;
        }

        public void Log(string message) => logSink(message);

        private ExpressionContext BuildContext(int itemIndex) => new()
        {
            CurrentItem = items.Count > 0 ? items[Math.Min(itemIndex, items.Count - 1)] : null,
            ItemIndex = itemIndex,
            RunIndex = runIndex,
            NodeOutputs = nodeOutputs,
            Trigger = trigger
        };
    }
}
