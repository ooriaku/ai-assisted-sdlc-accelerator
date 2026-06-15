namespace AIHarness.API.Endpoints;

public static class AgentEndpoints
{
    public static WebApplication MapAgentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/agents/status", () => Results.Ok(new
        {
            Agents = new[]
            {
                new { Name = "RequirementsAgent", Status = "ready" },
                new { Name = "CodeGenerationAgent", Status = "ready" },
                new { Name = "TestingAgent", Status = "ready" },
                new { Name = "DeploymentAgent", Status = "ready" }
            },
            Timestamp = DateTimeOffset.UtcNow
        })).WithTags("Agents").WithSummary("Returns readiness status of all SDLC agents");

        return app;
    }
}
