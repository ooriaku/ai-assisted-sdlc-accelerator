namespace AIHarness.Agents;

public sealed record AgentDefinition
{
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0";
    public required string Instructions { get; init; }
}
