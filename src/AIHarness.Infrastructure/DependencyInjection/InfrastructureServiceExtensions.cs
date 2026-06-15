using Anthropic;
using AIHarness.Core.Configuration;
using AIHarness.Core.Interfaces;
using AIHarness.Infrastructure.Messaging;
using AIHarness.Infrastructure.Repositories;
using AIHarness.Infrastructure.Storage;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIHarness.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    // Well-known Azurite development storage key (public, not a secret)
    private const string AzuriteDevKey =
        "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // ── Anthropic SDK client ───────────────────────────────────────────
        services.AddSingleton<AnthropicClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
            return new AnthropicClient(new Anthropic.Core.ClientOptions { ApiKey = opts.ApiKey });
        });

        // ── M.E.AI IChatClient (Anthropic SDK built-in integration) ────────
        services.AddSingleton<IChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
            var client = sp.GetRequiredService<AnthropicClient>();
            return client.AsIChatClient(opts.AgentModel, opts.MaxTokens);
        });

        // ── SK IChatCompletionService (bridge from IChatClient) ────────────
        services.AddSingleton<IChatCompletionService>(sp =>
            sp.GetRequiredService<IChatClient>().AsChatCompletionService(sp));

        // ── Azure Cosmos DB ────────────────────────────────────────────────
        // Development: emulator uses a well-known key + self-signed cert
        // Production:  managed identity via DefaultAzureCredential
        services.AddSingleton<CosmosClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            var env = sp.GetRequiredService<IHostEnvironment>();

            var clientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            if (env.IsDevelopment())
            {
                // Emulator certificate is self-signed — bypass SSL validation locally only
                clientOptions.HttpClientFactory = () => new HttpClient(
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                clientOptions.ConnectionMode = ConnectionMode.Gateway;

                return new CosmosClient(opts.AccountEndpoint, opts.AccountKey, clientOptions);
            }

            return new CosmosClient(opts.AccountEndpoint, new DefaultAzureCredential(), clientOptions);
        });

        // ── Azure Service Bus ──────────────────────────────────────────────
        services.AddSingleton<ServiceBusClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
            return new ServiceBusClient(opts.FullyQualifiedNamespace, new DefaultAzureCredential());
        });

        // ── Azure Blob Storage ─────────────────────────────────────────────
        // Development: Azurite uses a well-known dev storage key
        // Production:  managed identity via DefaultAzureCredential
        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            var env = sp.GetRequiredService<IHostEnvironment>();

            if (env.IsDevelopment())
            {
                return new BlobServiceClient(
                    new Uri(opts.AccountUri),
                    new StorageSharedKeyCredential("devstoreaccount1", AzuriteDevKey));
            }

            return new BlobServiceClient(new Uri(opts.AccountUri), new DefaultAzureCredential());
        });

        // ── Repositories (scoped: one per request) ─────────────────────────
        services.AddScoped<IWorkflowRepository, CosmosWorkflowRepository>();
        services.AddScoped<IArtifactRepository, CosmosArtifactRepository>();
        services.AddScoped<IAuditLogRepository, CosmosAuditLogRepository>();

        // ── Supporting services ────────────────────────────────────────────
        services.AddSingleton<ServiceBusPublisher>();
        services.AddSingleton<BlobArtifactStorage>();

        return services;
    }
}
