using AIHarness.Core.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIHarness.Worker;

public sealed class AgentTaskWorker : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<AgentTaskWorker> _logger;

    public AgentTaskWorker(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        ILogger<AgentTaskWorker> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor(_options.AgentTasksQueue, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 2,
            AutoCompleteMessages = false,
            // Agents can run for several minutes; keep lock alive
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10)
        });

        processor.ProcessMessageAsync += OnMessageAsync;
        processor.ProcessErrorAsync   += OnErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down
        }
        finally
        {
            await processor.StopProcessingAsync();
            await processor.DisposeAsync();
        }
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        _logger.LogInformation(
            "Processing agent task. Subject: {Subject}, MessageId: {MessageId}",
            args.Message.Subject, args.Message.MessageId);

        try
        {
            // Dispatch based on message subject — extend this as new task types are introduced
            _logger.LogDebug("Task payload preview: {Preview}",
                body.Length > 200 ? body[..200] : body);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process agent task {MessageId}", args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processor error. Source: {ErrorSource}, Namespace: {Namespace}",
            args.ErrorSource, args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }
}
