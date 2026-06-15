using AIHarness.Agents;
using AIHarness.Agents.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace AIHarness.Orchestrator;

public static class OrchestratorServiceExtensions
{
    public static IServiceCollection AddSdlcOrchestrator(this IServiceCollection services)
    {
        // SK Kernel — IChatCompletionService is registered by Infrastructure layer
        services.AddKernel();

        // Agent plugins — scoped so each workflow run gets a fresh instance
        services.AddScoped<RequirementsPlugin>();
        services.AddScoped<CodeGenerationPlugin>();
        services.AddScoped<TestingPlugin>();
        services.AddScoped<DeploymentPlugin>();

        // Agent classes — each encapsulates its plugin and prompt
        services.AddScoped<RequirementsAgent>();
        services.AddScoped<CodeGenerationAgent>();
        services.AddScoped<TestingAgent>();
        services.AddScoped<DeploymentAgent>();

        // Orchestrator — scoped per workflow run
        services.AddScoped<SdlcOrchestrator>();

        return services;
    }
}
