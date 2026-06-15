namespace AIHarness.Core.Models;

public sealed record WorkflowArtifact
{
    public required string Id { get; init; }
    public required string WorkflowRunId { get; init; }
    public required string ArtifactType { get; init; }
    public required string BlobUrl { get; init; }
    public string? ContentSummary { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
