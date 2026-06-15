namespace AIHarness.Core.Configuration;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    public required string ApiKey { get; set; }
    public string OrchestratorModel { get; set; } = "claude-opus-4-8";
    public string AgentModel { get; set; } = "claude-sonnet-4-6";
    public int MaxTokens { get; set; } = 16000;
}
