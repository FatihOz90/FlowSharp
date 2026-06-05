using FlowSharp.Domain.Nodes;

namespace FlowSharp.Application.Nodes;

public interface INodeCatalog
{
    IReadOnlyList<NodeDefinition> GetAll();

    IReadOnlyList<NodeDefinition> GetByCategory(NodeCategory category);

    IReadOnlyList<NodeDefinition> GetByCategory(string category);

    NodeDefinition? Find(string key);

    /// <summary>
    /// Bir node'un cikis portlarini cozer. Node <see cref="IHasDynamicOutputs"/> implemente
    /// ediyorsa portlar instance parametrelerinden uretilir; aksi halde statik
    /// <see cref="NodeDefinition.OutputPorts"/> doner.
    /// </summary>
    IReadOnlyList<NodePort> ResolveOutputs(string key, IReadOnlyDictionary<string, string> parameters);
}
