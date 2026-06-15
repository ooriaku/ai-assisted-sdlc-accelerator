using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Core.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace AIHarness.Infrastructure.Repositories;

public sealed class CosmosAuditLogRepository : IAuditLogRepository
{
    private readonly Container _container;

    public CosmosAuditLogRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
    {
        var o = options.Value;
        _container = cosmosClient.GetContainer(o.DatabaseName, o.AuditLogContainer);
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        await _container.CreateItemAsync(
            entry, new PartitionKey(entry.WorkflowRunId), cancellationToken: ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetByWorkflowRunIdAsync(
        string workflowRunId, CancellationToken ct = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.workflowRunId = @id ORDER BY c.timestamp ASC")
            .WithParameter("@id", workflowRunId);

        var results = new List<AuditEntry>();
        using var iterator = _container.GetItemQueryIterator<AuditEntry>(query);
        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync(ct))
                results.Add(item);
        }
        return results;
    }
}
