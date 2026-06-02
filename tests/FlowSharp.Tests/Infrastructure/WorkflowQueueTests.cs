using System.Text.Json;
using FluentAssertions;
using FlowSharp.Domain.Queue;
using FlowSharp.Infrastructure.Queue;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Infrastructure;

public class WorkflowQueueTests : IDisposable
{
    private readonly SqliteDbFixture db = new();

    public void Dispose() => db.Dispose();

    private SqliteWorkflowQueue NewQueue() => new(db.NewContext());

    private static JsonDocument Payload() => JsonDocument.Parse("""{"source":"manual"}""");

    [Fact]
    public async Task Enqueue_then_dequeue_returns_pending_job_and_locks_it()
    {
        var workflowId = Guid.NewGuid();
        var enqueued = await NewQueue().EnqueueAsync(workflowId, Payload());

        var dequeued = await NewQueue().DequeueAsync("worker-1", TimeSpan.FromMinutes(1));

        dequeued.Should().NotBeNull();
        dequeued!.Id.Should().Be(enqueued.Id);
        dequeued.Status.Should().Be(WorkflowJobStatus.Processing);
        dequeued.LockedBy.Should().Be("worker-1");
        dequeued.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Dequeue_returns_null_when_no_pending_jobs()
    {
        var result = await NewQueue().DequeueAsync("worker-x", TimeSpan.FromMinutes(1));
        result.Should().BeNull();
    }

    [Fact]
    public async Task Complete_marks_job_completed()
    {
        var job = await NewQueue().EnqueueAsync(Guid.NewGuid(), Payload());
        await NewQueue().DequeueAsync("w", TimeSpan.FromMinutes(1));

        await NewQueue().CompleteAsync(job.Id);

        await using var ctx = db.NewContext();
        var reloaded = await ctx.WorkflowJobs.FindAsync(job.Id);
        reloaded!.Status.Should().Be(WorkflowJobStatus.Completed);
        reloaded.LockedBy.Should().BeNull();
    }

    [Fact]
    public async Task Fail_requeues_until_max_attempts_then_dead_letters()
    {
        var job = await NewQueue().EnqueueAsync(Guid.NewGuid(), Payload());

        for (var attempt = 0; attempt < job.MaxAttempts; attempt++)
        {
            await NewQueue().DequeueAsync("w", TimeSpan.FromMinutes(1));
            await NewQueue().FailAsync(job.Id, "patladi");

            // Backoff gecikmesini atla: isi yeniden hemen alinabilir yap.
            await using var resetCtx = db.NewContext();
            var pending = await resetCtx.WorkflowJobs.FindAsync(job.Id);
            pending!.AvailableAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await resetCtx.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var reloaded = await ctx.WorkflowJobs.FindAsync(job.Id);
        reloaded!.Status.Should().Be(WorkflowJobStatus.DeadLetter);
        reloaded.LastError.Should().Be("patladi");
    }

    [Fact]
    public async Task Dequeue_respects_fifo_by_created_at()
    {
        var first = await NewQueue().EnqueueAsync(Guid.NewGuid(), Payload());
        await Task.Delay(10);
        await NewQueue().EnqueueAsync(Guid.NewGuid(), Payload());

        var dequeued = await NewQueue().DequeueAsync("w", TimeSpan.FromMinutes(1));
        dequeued!.Id.Should().Be(first.Id);
    }
}
