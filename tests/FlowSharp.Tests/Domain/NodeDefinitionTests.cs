using FluentAssertions;
using FlowSharp.Domain.Nodes;
using Xunit;

namespace FlowSharp.Tests.Domain;

public class NodeDefinitionTests
{
    private static NodeDefinition Make(
        IReadOnlyList<NodePort>? inputs = null,
        IReadOnlyList<NodePort>? outputs = null,
        NodeKind kind = NodeKind.Transform) =>
        new("k", "Name", NodeCategory.Core, kind, "desc", [], [], "icon",
            Inputs: inputs, Outputs: outputs);

    [Fact]
    public void Default_transform_has_single_main_input_and_output()
    {
        var def = Make();
        def.InputPorts.Should().ContainSingle().Which.Type.Should().Be(NodePortType.Main);
        def.OutputPorts.Should().ContainSingle().Which.Type.Should().Be(NodePortType.Main);
    }

    [Fact]
    public void Trigger_has_no_input_ports_by_default()
    {
        var def = Make(kind: NodeKind.Trigger);
        def.InputPorts.Should().BeEmpty();
    }

    [Fact]
    public void SubNode_detected_when_output_port_is_non_main()
    {
        var def = Make(outputs: [new NodePort("model", "Model", NodePortType.AiModel)]);
        def.IsSubNode.Should().BeTrue();
        def.SubOutputPorts.Should().ContainSingle();
        def.MainOutputPorts.Should().BeEmpty();
    }

    [Fact]
    public void Main_and_sub_ports_are_partitioned_by_type()
    {
        var def = Make(inputs:
        [
            NodePort.Main,
            new NodePort("tool", "Tool", NodePortType.AiTool)
        ]);
        def.MainInputPorts.Should().ContainSingle();
        def.SubInputPorts.Should().ContainSingle().Which.Type.Should().Be(NodePortType.AiTool);
    }

    [Fact]
    public void CategoryKey_falls_back_to_enum_when_no_explicit_name()
    {
        Make().CategoryKey.Should().Be(NodeCategory.Core.ToString());
    }
}
