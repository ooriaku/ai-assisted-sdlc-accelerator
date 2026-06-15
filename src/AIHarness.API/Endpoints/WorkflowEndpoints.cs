using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Orchestrator;

namespace AIHarness.API.Endpoints;

public static class WorkflowEndpoints
{
    public static WebApplication MapWorkflowEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workflows").WithTags("Workflows");

        group.MapPost("/", async (
            CreateWorkflowRequest request,
            IWorkflowRepository repo,
            IServiceScopeFactory scopeFactory,
            CancellationToken ct) =>
        {
            var run = await repo.CreateAsync(new WorkflowRun
            {
                Id = Guid.NewGuid().ToString("N"),
                ProjectName = request.ProjectName,
                Requirements = request.Requirements
            }, ct);

            // Fire-and-forget in a background DI scope so the request returns immediately
            _ = Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<SdlcOrchestrator>();
                try
                {
                    await orchestrator.RunAsync(run.Id, CancellationToken.None);
                }
                catch
                {
                    // Errors are persisted to CosmosDB by the orchestrator; suppress here
                }
            });

            return Results.Created($"/api/workflows/{run.Id}", run);
        })
        .WithName("CreateWorkflow")
        .WithSummary("Create and execute an SDLC workflow run");

        group.MapGet("/{id}", async (string id, IWorkflowRepository repo, CancellationToken ct) =>
            await repo.GetByIdAsync(id, ct) is { } run
                ? Results.Ok(run)
                : Results.NotFound());

        group.MapGet("/", async (
            int skip,
            int take,
            IWorkflowRepository repo,
            CancellationToken ct) =>
            Results.Ok(await repo.ListAsync(skip, take, ct)));

        group.MapGet("/{id}/artifacts", async (
            string id,
            IArtifactRepository artifacts,
            CancellationToken ct) =>
            Results.Ok(await artifacts.GetByWorkflowRunIdAsync(id, ct)));

        return app;
    }
}

public sealed record CreateWorkflowRequest(string ProjectName, string Requirements);
