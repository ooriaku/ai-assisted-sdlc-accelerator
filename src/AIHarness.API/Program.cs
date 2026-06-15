using AIHarness.API.Endpoints;
using AIHarness.Core.Configuration;
using AIHarness.Infrastructure.DependencyInjection;
using AIHarness.Infrastructure.KeyVault;
using AIHarness.Orchestrator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Key Vault configuration (production only, loaded before Options binding) ──
if (!builder.Environment.IsDevelopment())
{
    var kvUri = builder.Configuration["KeyVault:Uri"];
    if (!string.IsNullOrWhiteSpace(kvUri))
        builder.Configuration.AddKeyVaultConfiguration(new Uri(kvUri));
}

// ── Options binding ────────────────────────────────────────────────────────────
builder.Services
    .Configure<AnthropicOptions>(builder.Configuration.GetSection(AnthropicOptions.SectionName))
    .Configure<CosmosDbOptions>(builder.Configuration.GetSection(CosmosDbOptions.SectionName))
    .Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.SectionName))
    .Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.SectionName))
    .Configure<GitHubOptions>(builder.Configuration.GetSection(GitHubOptions.SectionName));

// ── Core services ──────────────────────────────────────────────────────────────
builder.Services.AddInfrastructure();
builder.Services.AddSdlcOrchestrator();

// ── OpenAPI ────────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// ── OpenAPI / Scalar UI ────────────────────────────────────────────────────────
app.MapOpenApi();
app.MapScalarApiReference();

// ── Endpoints ──────────────────────────────────────────────────────────────────
app.MapWorkflowEndpoints();
app.MapAgentEndpoints();
app.MapHealthEndpoints();

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }

// Extension method to wire Key Vault configuration source
public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddKeyVaultConfiguration(
        this IConfigurationBuilder builder, Uri vaultUri)
        => builder.Add(new KeyVaultConfigurationSource(vaultUri));
}
