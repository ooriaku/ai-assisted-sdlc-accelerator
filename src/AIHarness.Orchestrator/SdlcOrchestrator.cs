using AIHarness.Agents;
using AIHarness.Core.Enums;
using AIHarness.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIHarness.Orchestrator;

public sealed class SdlcOrchestrator
{
    private readonly RequirementsAgent _requirementsAgent;
    private readonly CodeGenerationAgent _codeGenerationAgent;
    private readonly TestingAgent _testingAgent;
    private readonly DeploymentAgent _deploymentAgent;
    private readonly IWorkflowRepository _workflows;
    private readonly ILogger<SdlcOrchestrator> _logger;

    public SdlcOrchestrator(
        RequirementsAgent requirementsAgent,
        CodeGenerationAgent codeGenerationAgent,
        TestingAgent testingAgent,
        DeploymentAgent deploymentAgent,
        IWorkflowRepository workflows,
        ILogger<SdlcOrchestrator> logger)
    {
        _requirementsAgent = requirementsAgent;
        _codeGenerationAgent = codeGenerationAgent;
        _testingAgent = testingAgent;
        _deploymentAgent = deploymentAgent;
        _workflows = workflows;
        _logger = logger;
    }

    public async Task RunAsync(string workflowRunId, CancellationToken ct = default)
    {
        var run = await _workflows.GetByIdAsync(workflowRunId, ct)
            ?? throw new InvalidOperationException($"Workflow run {workflowRunId} not found.");

        var chat = new AgentGroupChat(
            _requirementsAgent.ChatAgent,
            _codeGenerationAgent.ChatAgent,
            _testingAgent.ChatAgent,
            _deploymentAgent.ChatAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy(),
                TerminationStrategy = new MaxIterationTerminationStrategy(maxIterations: 4)
            }
        };

        chat.AddChatMessage(new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.User, run.Requirements));

        var agentIndex = 0;
        var statusProgression = new[]
        {
            WorkflowStatus.RequirementsCapture,
            WorkflowStatus.CodeGeneration,
            WorkflowStatus.Testing,
            WorkflowStatus.Deployment
        };

        try
        {
            await foreach (var message in chat.InvokeAsync(ct))
            {
                var preview = message.Content is { Length: > 0 }
                    ? message.Content[..Math.Min(120, message.Content.Length)]
                    : "(no text content)";

                _logger.LogInformation(
                    "Agent {AgentName} completed turn {Turn}. Preview: {Preview}",
                    message.AuthorName, agentIndex + 1, preview);

                if (agentIndex < statusProgression.Length)
                {
                    run = await _workflows.UpdateAsync(
                        run with { Status = statusProgression[agentIndex] }, ct);
                }

                agentIndex++;
            }

            run = await _workflows.UpdateAsync(
                run with { Status = WorkflowStatus.Completed, CompletedAt = DateTimeOffset.UtcNow }, ct);

            _logger.LogInformation("Workflow {WorkflowRunId} completed successfully.", workflowRunId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow {WorkflowRunId} failed.", workflowRunId);
            await _workflows.UpdateAsync(
                run with { Status = WorkflowStatus.Failed, ErrorMessage = ex.Message }, ct);
            throw;
        }
    }
}
