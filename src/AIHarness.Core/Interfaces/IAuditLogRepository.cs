using AIHarness.Core.Models;

namespace AIHarness.Core.Interfaces;

public interface IAuditLogRepository
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetByWorkflowRunIdAsync(string workflowRunId, CancellationToken ct = default);
}
