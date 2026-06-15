namespace AIHarness.Core.Models;

public sealed record AuditEntry
{
    public required string Id { get; init; }
    public required string WorkflowRunId { get; init; }
    public required string AgentName { get; init; }
    public required string Action { get; init; }
    public required string Details { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
