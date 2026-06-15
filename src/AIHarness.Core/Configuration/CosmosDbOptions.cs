namespace AIHarness.Core.Configuration;

public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";
    public required string AccountEndpoint { get; set; }
    public string? AccountKey { get; set; }
    public string DatabaseName { get; set; } = "AIHarness";
    public string WorkflowRunsContainer { get; set; } = "WorkflowRuns";
    public string ArtifactsContainer { get; set; } = "Artifacts";
    public string AuditLogContainer { get; set; } = "AuditLog";
}
