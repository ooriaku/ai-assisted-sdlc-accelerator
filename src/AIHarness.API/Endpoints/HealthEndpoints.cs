namespace AIHarness.API.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "healthy",
            Timestamp = DateTimeOffset.UtcNow
        }))
        .WithTags("Health")
        .ExcludeFromDescription();

        return app;
    }
}
