using System.Text.Json;
using FlowSharp.Domain.Common;

namespace FlowSharp.Domain.Workflows;

public sealed class Workflow : AuditableEntity
{
    public required string Name { get; set; }

    /// <summary>Bu workflow'u olusturan kullanicinin Identity Id'si. Sahiplik/izolasyon icin kullanilir.
    /// null ise sistem/eski kayit (yalniz Admin gorur).</summary>
    public string? OwnerId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int Version { get; set; } = 1;

    public JsonDocument Definition { get; set; } = JsonDocument.Parse("""{"nodes":[],"connections":[]}""");

    public ICollection<WorkflowExecution> Executions { get; } = [];
}
