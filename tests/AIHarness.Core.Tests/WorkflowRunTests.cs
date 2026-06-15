using AIHarness.Core.Enums;
using AIHarness.Core.Models;
using FluentAssertions;

namespace AIHarness.Core.Tests;

public class WorkflowRunTests
{
    [Fact]
    public void WorkflowRun_DefaultStatus_IsCreated()
    {
        var run = new WorkflowRun
        {
            Id = "run-1",
            ProjectName = "Demo",
            Requirements = "Build a REST API"
        };

        run.Status.Should().Be(WorkflowStatus.Created);
    }

    [Fact]
    public void WorkflowRun_WithExpression_UpdatesStatusImmutably()
    {
        var run = new WorkflowRun
        {
            Id = "run-1",
            ProjectName = "Demo",
            Requirements = "Build a REST API"
        };

        var updated = run with { Status = WorkflowStatus.CodeGeneration };

        run.Status.Should().Be(WorkflowStatus.Created);
        updated.Status.Should().Be(WorkflowStatus.CodeGeneration);
        updated.Id.Should().Be(run.Id);
    }

    [Fact]
    public void WorkflowRun_CreatedAt_IsSetToUtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var run = new WorkflowRun { Id = "run-1", ProjectName = "Demo", Requirements = "req" };
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        run.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void WorkflowRun_Metadata_IsEmptyByDefault()
    {
        var run = new WorkflowRun { Id = "run-1", ProjectName = "Demo", Requirements = "req" };
        run.Metadata.Should().BeEmpty();
    }

    [Theory]
    [InlineData(WorkflowStatus.Created)]
    [InlineData(WorkflowStatus.RequirementsCapture)]
    [InlineData(WorkflowStatus.CodeGeneration)]
    [InlineData(WorkflowStatus.Testing)]
    [InlineData(WorkflowStatus.Deployment)]
    [InlineData(WorkflowStatus.Completed)]
    [InlineData(WorkflowStatus.Failed)]
    public void WorkflowStatus_AllValuesAreDefined(WorkflowStatus status)
    {
        Enum.IsDefined(status).Should().BeTrue();
    }
}
