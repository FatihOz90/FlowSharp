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

public partial class Executions
{
    [Inject] public ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private List<WorkflowExecution>? executions;

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        var (currentUserId, isAdmin) = await FlowSharp.Web.Security.CurrentUser.ResolveAsync(AuthenticationStateProvider);

        var query = DbContext.WorkflowExecutions
            .Include(e => e.Workflow)
            .AsQueryable();
        if (!isAdmin)
        {
            // Yalniz oturum sahibinin workflow'larina ait yurutmeler.
            query = query.Where(e => e.Workflow != null && e.Workflow.OwnerId == currentUserId);
        }

        executions = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .AsNoTracking()
            .ToListAsync();
    }

    private static string Duration(WorkflowExecution e) =>
        e.StartedAt is { } s && e.FinishedAt is { } f ? $"{(f - s).TotalSeconds:0.0} sn" : "—";
}
