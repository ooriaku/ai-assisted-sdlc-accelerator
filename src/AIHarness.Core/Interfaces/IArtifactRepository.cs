using AIHarness.Core.Models;

namespace AIHarness.Core.Interfaces;

public interface IArtifactRepository
{
    Task<WorkflowArtifact> SaveAsync(WorkflowArtifact artifact, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowArtifact>> GetByWorkflowRunIdAsync(string workflowRunId, CancellationToken ct = default);
    Task<WorkflowArtifact?> GetByIdAsync(string id, CancellationToken ct = default);
}
