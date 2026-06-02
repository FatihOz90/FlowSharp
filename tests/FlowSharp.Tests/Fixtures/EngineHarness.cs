using FlowSharp.Application.Nodes;
using FlowSharp.Application.Nodes.Agents;
using FlowSharp.Infrastructure.Workflows;
using FlowSharp.Infrastructure.Workflows.Expressions;
using FlowSharp.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FlowSharp.Tests.Fixtures;

/// <summary>
/// Verilen node tipleriyle gercek bir <see cref="WorkflowExecutionEngine"/> kurar.
/// Varsayilan olarak test node'larini icerir; AI executor mock'tur.
/// </summary>
public static class EngineHarness
{
    public static WorkflowExecutionEngine Create(
        IEnumerable<INodeType>? nodes = null,
        IAgentExecutor? agentExecutor = null)
    {
        var registry = new NodeRegistry(nodes ?? DefaultNodes());
        var evaluator = new ExpressionEvaluator();
        var agent = agentExecutor ?? Substitute.For<IAgentExecutor>();

        var services = new ServiceCollection();
        services.AddHttpClient("workflow-nodes");
        var provider = services.BuildServiceProvider();

        return new WorkflowExecutionEngine(
            registry, evaluator, agent, provider, NullLogger<WorkflowExecutionEngine>.Instance);
    }

    public static IEnumerable<INodeType> DefaultNodes() =>
    [
        new TestTriggerNode(),
        new TagNode(),
        new FailingNode(),
        new SplitNode()
    ];
}
