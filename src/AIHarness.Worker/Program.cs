using AIHarness.Core.Configuration;
using AIHarness.Infrastructure.DependencyInjection;
using AIHarness.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .Configure<AnthropicOptions>(builder.Configuration.GetSection(AnthropicOptions.SectionName))
    .Configure<CosmosDbOptions>(builder.Configuration.GetSection(CosmosDbOptions.SectionName))
    .Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.SectionName))
    .Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.SectionName));

builder.Services.AddInfrastructure();
builder.Services.AddHostedService<AgentTaskWorker>();

var host = builder.Build();
host.Run();
