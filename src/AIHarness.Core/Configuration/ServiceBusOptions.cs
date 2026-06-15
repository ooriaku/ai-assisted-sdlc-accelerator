namespace AIHarness.Core.Configuration;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";
    public required string FullyQualifiedNamespace { get; set; }
    public string AgentTasksQueue { get; set; } = "agent-tasks";
    public string AgentResultsQueue { get; set; } = "agent-results";
}
