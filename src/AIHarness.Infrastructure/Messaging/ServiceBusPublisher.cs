using System.Text.Json;
using AIHarness.Core.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace AIHarness.Infrastructure.Messaging;

public sealed class ServiceBusPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusOptions _options;

    public ServiceBusPublisher(ServiceBusClient client, IOptions<ServiceBusOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task PublishAgentTaskAsync<T>(T task, CancellationToken ct = default)
    {
        await using var sender = _client.CreateSender(_options.AgentTasksQueue);
        await sender.SendMessageAsync(
            new ServiceBusMessage(JsonSerializer.Serialize(task)) { ContentType = "application/json" }, ct);
    }

    public async Task PublishAgentResultAsync<T>(T result, CancellationToken ct = default)
    {
        await using var sender = _client.CreateSender(_options.AgentResultsQueue);
        await sender.SendMessageAsync(
            new ServiceBusMessage(JsonSerializer.Serialize(result)) { ContentType = "application/json" }, ct);
    }
}
