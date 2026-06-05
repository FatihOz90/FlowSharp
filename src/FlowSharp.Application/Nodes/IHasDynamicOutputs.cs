using FlowSharp.Domain.Nodes;

namespace FlowSharp.Application.Nodes;

/// <summary>
/// Cikis portlari instance parametrelerine gore degisen node'lar bu arayuzu implemente eder
/// (orn. bir multiselect'te secilen her event icin ayri cikis portu). UI ve motor, statik
/// <see cref="NodeDefinition.OutputPorts"/> yerine <see cref="INodeCatalog.ResolveOutputs"/>
/// uzerinden bu portlari alir; node'a ozel UI kodu gerekmez.
/// </summary>
public interface IHasDynamicOutputs
{
    /// <summary>
    /// Verilen instance parametrelerine gore cikis portlarini uretir. En az bir port donmelidir.
    /// </summary>
    IReadOnlyList<NodePort> GetOutputs(IReadOnlyDictionary<string, string> parameters);
}
