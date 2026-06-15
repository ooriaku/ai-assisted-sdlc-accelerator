using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Core.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace AIHarness.Infrastructure.Repositories;

public sealed class CosmosArtifactRepository : IArtifactRepository
{
    private readonly Container _container;

    public CosmosArtifactRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
    {
        var o = options.Value;
        _container = cosmosClient.GetContainer(o.DatabaseName, o.ArtifactsContainer);
    }

    public async Task<WorkflowArtifact> SaveAsync(WorkflowArtifact artifact, CancellationToken ct = default)
    {
        var response = await _container.UpsertItemAsync(
            artifact, new PartitionKey(artifact.WorkflowRunId), cancellationToken: ct);
        return response.Resource;
    }

    public async Task<IReadOnlyList<WorkflowArtifact>> GetByWorkflowRunIdAsync(
        string workflowRunId, CancellationToken ct = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.workflowRunId = @id ORDER BY c.createdAt ASC")
            .WithParameter("@id", workflowRunId);

        var results = new List<WorkflowArtifact>();
        using var iterator = _container.GetItemQueryIterator<WorkflowArtifact>(query);
        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync(ct))
                results.Add(item);
        }
        return results;
    }

    public async Task<WorkflowArtifact?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);

        using var iterator = _container.GetItemQueryIterator<WorkflowArtifact>(query);
        if (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync(ct))
                return item;
        }
        return null;
    }
}
