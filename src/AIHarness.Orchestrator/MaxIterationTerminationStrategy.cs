using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace AIHarness.Orchestrator;

/// <summary>
/// Terminates the agent group chat after a fixed number of turns.
/// </summary>
public sealed class MaxIterationTerminationStrategy : TerminationStrategy
{
    private readonly int _maxIterations;
    private int _turnCount;

    public MaxIterationTerminationStrategy(int maxIterations)
    {
        _maxIterations = maxIterations;
        MaximumIterations = maxIterations;
    }

    protected override Task<bool> ShouldAgentTerminateAsync(
        Agent agent,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken = default)
    {
        _turnCount++;
        return Task.FromResult(_turnCount >= _maxIterations);
    }
}
