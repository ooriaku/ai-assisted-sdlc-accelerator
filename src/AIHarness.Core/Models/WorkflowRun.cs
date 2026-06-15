using AIHarness.Core.Enums;

namespace AIHarness.Core.Models;

public sealed record WorkflowRun
{
    public required string Id { get; init; }
    public required string ProjectName { get; init; }
    public required string Requirements { get; init; }
    public WorkflowStatus Status { get; init; } = WorkflowStatus.Created;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}
