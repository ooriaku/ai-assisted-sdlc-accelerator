using AIHarness.Core.Models;

namespace AIHarness.Core.Interfaces;

public interface IWorkflowRepository
{
    Task<WorkflowRun> CreateAsync(WorkflowRun run, CancellationToken ct = default);
    Task<WorkflowRun?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<WorkflowRun> UpdateAsync(WorkflowRun run, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowRun>> ListAsync(int skip = 0, int take = 20, CancellationToken ct = default);
}
