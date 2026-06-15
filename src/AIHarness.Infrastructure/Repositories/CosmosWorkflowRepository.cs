using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Core.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace AIHarness.Infrastructure.Repositories;

public sealed class CosmosWorkflowRepository : IWorkflowRepository
{
    private readonly Container _container;

    public CosmosWorkflowRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
    {
        var o = options.Value;
        _container = cosmosClient.GetContainer(o.DatabaseName, o.WorkflowRunsContainer);
    }

    public async Task<WorkflowRun> CreateAsync(WorkflowRun run, CancellationToken ct = default)
    {
        var response = await _container.CreateItemAsync(run, new PartitionKey(run.Id), cancellationToken: ct);
        return response.Resource;
    }

    public async Task<WorkflowRun?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<WorkflowRun>(id, new PartitionKey(id), cancellationToken: ct);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<WorkflowRun> UpdateAsync(WorkflowRun run, CancellationToken ct = default)
    {
        var updated = run with { UpdatedAt = DateTimeOffset.UtcNow };
        var response = await _container.UpsertItemAsync(updated, new PartitionKey(updated.Id), cancellationToken: ct);
        return response.Resource;
    }

    public async Task<IReadOnlyList<WorkflowRun>> ListAsync(int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c ORDER BY c.createdAt DESC OFFSET @skip LIMIT @take")
            .WithParameter("@skip", skip)
            .WithParameter("@take", take);

        var results = new List<WorkflowRun>();
        using var iterator = _container.GetItemQueryIterator<WorkflowRun>(query);
        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync(ct))
                results.Add(item);
        }
        return results;
    }
}
