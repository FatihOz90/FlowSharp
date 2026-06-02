using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowSharp.Domain.Workflows;
using FlowSharp.Infrastructure.Data;

namespace FlowSharp.Web.Components.Pages;

public partial class Home
{
    [Inject] public required ApplicationDbContext DbContext { get; set; }
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private int workflowCount, activeCount, executionCount, failedCount;
    private List<WorkflowExecution> recent = [];

    protected override async Task OnInitializedAsync()
    {
        var (currentUserId, isAdmin) = await FlowSharp.Web.Security.CurrentUser.ResolveAsync(AuthenticationStateProvider);

        // Sahiplik filtresi: Admin tum kayitlari, digerleri yalniz kendi workflow'larini/yurutmelerini gorur.
        var workflows = DbContext.Workflows.AsQueryable();
        var executions = DbContext.WorkflowExecutions.AsQueryable();
        if (!isAdmin)
        {
            workflows = workflows.Where(w => w.OwnerId == currentUserId);
            executions = executions.Where(e => e.Workflow != null && e.Workflow.OwnerId == currentUserId);
        }

        workflowCount = await workflows.CountAsync();
        activeCount = await workflows.CountAsync(w => w.IsActive);
        executionCount = await executions.CountAsync();
        failedCount = await executions.CountAsync(e => e.Status == WorkflowExecutionStatus.Failed);
        recent = await executions
            .Include(e => e.Workflow)
            .OrderByDescending(e => e.CreatedAt)
            .Take(8)
            .AsNoTracking()
            .ToListAsync();
    }

    private static string Duration(WorkflowExecution e) =>
        e.StartedAt is { } s && e.FinishedAt is { } f ? $"{(f - s).TotalSeconds:0.0} sn" : "—";
}
