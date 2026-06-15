# Agent Design

## The SDLC Pipeline

Four specialised Claude agents run in strict sequence. Each agent receives the full conversation history (so it can read the previous agent's output) and produces a structured artifact that it persists to Blob Storage via its plugin.

```
User requirements text
        │
        ▼
┌────────────────────┐   Requirements JSON
│  RequirementsAgent │ ──────────────────► Blob: requirements.json
│  claude-sonnet-4-6 │                     Cosmos: WorkflowArtifact
└────────────────────┘
        │ conversation history
        ▼
┌────────────────────┐   Code scaffold JSON
│ CodeGenerationAgent│ ──────────────────► Blob: code.json
│  claude-sonnet-4-6 │                     Cosmos: WorkflowArtifact
└────────────────────┘
        │ conversation history
        ▼
┌────────────────────┐   Test suite JSON
│   TestingAgent     │ ──────────────────► Blob: tests.json
│  claude-sonnet-4-6 │                     Cosmos: WorkflowArtifact
└────────────────────┘
        │ conversation history
        ▼
┌────────────────────┐   CI/CD + IaC JSON
│  DeploymentAgent   │ ──────────────────► Blob: deployment.json
│  claude-sonnet-4-6 │                     Cosmos: WorkflowArtifact
└────────────────────┘
```

## Agent Class Design

Each agent is a **sealed class** in `src/AIHarness.Agents/`. They are registered in the DI container as `Scoped` — a fresh instance is created per workflow run, ensuring kernel state and plugin instances don't leak between concurrent runs.

```
AIHarness.Agents/
├── RequirementsAgent.cs
├── CodeGenerationAgent.cs
├── TestingAgent.cs
├── DeploymentAgent.cs
├── AgentDefinition.cs        ← record: Name, Description, Version, Instructions
├── AgentDefinitionLoader.cs  ← parses YAML front matter from embedded .txt
└── Plugins/
    ├── RequirementsPlugin.cs
    ├── CodeGenerationPlugin.cs
    ├── TestingPlugin.cs
    └── DeploymentPlugin.cs
```

### Why Not Subclass `ChatCompletionAgent`?

`ChatCompletionAgent` in SK 1.77 is a **sealed class** with `init`-only properties — it cannot be subclassed or mutated after construction. Each agent class therefore wraps a `ChatCompletionAgent` instance, built in its constructor, and exposes it via `.ChatAgent`.

```csharp
// Pattern used by all four agents
public sealed class RequirementsAgent
{
    public ChatCompletionAgent ChatAgent { get; }

    public RequirementsAgent(Kernel kernel, RequirementsPlugin plugin)
    {
        var definition = AgentDefinitionLoader.Load(nameof(RequirementsAgent));
        var agentKernel = kernel.Clone();
        agentKernel.Plugins.AddFromObject(plugin, nameof(RequirementsPlugin));

        ChatAgent = new ChatCompletionAgent
        {
            Name = definition.Name,
            Instructions = definition.Instructions,
            Kernel = agentKernel
        };
    }
}
```

The kernel is **cloned** so that each agent has an isolated plugin set. The original shared kernel (registered via `services.AddKernel()`) is not modified.

## Prompt File System

Prompts are stored as plain-text files with YAML front matter, embedded into the assembly as resources.

### File Location

```
src/AIHarness.Agents/Prompts/
├── RequirementsAgent.txt
├── CodeGenerationAgent.txt
├── TestingAgent.txt
└── DeploymentAgent.txt
```

### File Format

```
---
name: RequirementsAgent
description: Transforms free-text requirements into structured JSON
version: 1.0
---

You are a senior business analyst and software architect.
When given free-text business requirements, produce a valid JSON document...
```

### Versioning

The `version` field in the front matter tracks prompt iterations independently of code changes. A prompt change produces a clean, isolated diff in git — reviewers see exactly what the model will receive before and after.

### `AgentDefinitionLoader`

The loader reads from embedded assembly resources, so prompts are always available regardless of the deployment environment.

```csharp
// Resource naming: AIHarness.Agents.Prompts.{AgentName}.txt
AgentDefinitionLoader.Load("RequirementsAgent")
// Returns AgentDefinition { Name, Description, Version, Instructions }
```

The loader:
1. Opens the embedded resource stream by name
2. Reads the file content
3. Splits on `---` to isolate the front matter block
4. Parses `key: value` pairs from the header
5. Returns the remainder as `Instructions`

If a resource is not found, it throws `InvalidOperationException` with the full list of available resource names — making misconfiguration easy to diagnose.

## Plugins

Each agent has exactly one `KernelFunction`-decorated plugin method. The Semantic Kernel function-calling feature invokes this automatically when the model emits a tool call in its response.

| Agent | Plugin class | Function name | What it does |
|---|---|---|---|
| RequirementsAgent | `RequirementsPlugin` | `save_requirements_artifact` | Uploads requirements JSON to blob, saves `WorkflowArtifact` to Cosmos |
| CodeGenerationAgent | `CodeGenerationPlugin` | `save_code_artifact` | Uploads code scaffold JSON to blob, saves artifact |
| TestingAgent | `TestingPlugin` | `save_test_artifact` | Uploads test suite JSON to blob, saves artifact |
| DeploymentAgent | `DeploymentPlugin` | `save_deployment_artifact` | Uploads CI/CD + IaC JSON to blob, saves artifact |

All plugins are scoped and injected by DI. They depend on `IArtifactRepository` and `BlobArtifactStorage`.

### Plugin Pattern

```csharp
[KernelFunction("save_requirements_artifact")]
[Description("Persists the structured requirements JSON to blob storage.")]
public async Task<string> SaveRequirementsArtifactAsync(
    [Description("The workflow run ID")] string workflowRunId,
    [Description("Structured requirements JSON")] string requirementsJson,
    CancellationToken cancellationToken = default)
{
    var blobUri = await _blobStorage.UploadArtifactAsync(
        workflowRunId, "requirements.json", requirementsJson, cancellationToken);

    await _artifacts.SaveAsync(new WorkflowArtifact
    {
        Id = Guid.NewGuid().ToString(),
        WorkflowRunId = workflowRunId,
        ArtifactType = "requirements",
        BlobUrl = blobUri.ToString(),
        ContentSummary = requirementsJson[..Math.Min(300, requirementsJson.Length)]
    }, cancellationToken);

    return blobUri.ToString();
}
```

Each agent's instructions tell it to **always** call its plugin function before returning. The final response is the blob URL, which becomes part of the conversation history for the next agent.

## SK Orchestration

### AgentGroupChat

The orchestrator creates a single `AgentGroupChat` with all four agents:

```csharp
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
```

### Selection Strategy

`SequentialSelectionStrategy` (built into SK) selects agents in the order they were added to the chat. The pipeline is therefore deterministic: Requirements → Code → Testing → Deployment.

### Termination Strategy

`MaxIterationTerminationStrategy` is a custom implementation (SK's internal `DefaultTerminationStrategy` is a private nested class and not accessible):

```csharp
public sealed class MaxIterationTerminationStrategy : TerminationStrategy
{
    private int _turnCount;

    protected override Task<bool> ShouldAgentTerminateAsync(
        Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken ct)
    {
        _turnCount++;
        return Task.FromResult(_turnCount >= _maxIterations);
    }
}
```

It counts turns and signals termination after exactly 4, preventing infinite loops.

### Conversation Seeding

Before invoking the chat, the orchestrator adds the user's requirements as the opening message:

```csharp
chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, run.Requirements));
```

This becomes visible to every agent as part of the conversation history, so each downstream agent can see both the original requirements and all prior agents' outputs.

### Status Progression

As each agent turn completes, the orchestrator updates the `WorkflowRun` in Cosmos:

| Turn | Status after |
|---|---|
| 1 (RequirementsAgent) | `RequirementsCapture` |
| 2 (CodeGenerationAgent) | `CodeGeneration` |
| 3 (TestingAgent) | `Testing` |
| 4 (DeploymentAgent) | `Deployment` |
| After all turns | `Completed` |
| On any exception | `Failed` (error message persisted) |

## Workflow State Machine

```
                 ┌──────────┐
    [Created] ──►│ Req.Cap. │──► [CodeGeneration] ──► [Testing] ──► [Deployment]
                 └──────────┘                                              │
                      │                                                    ▼
                      │◄──────────────────────────────────────────── [Completed]
                      │
                 (any stage)
                      │
                      ▼
                  [Failed]
```

The `WorkflowStatus` enum values map directly to these states. Consumers poll `GET /api/workflows/{id}` and check `status` until they see `Completed` or `Failed`.

## Adding a New Agent

1. Create `src/AIHarness.Agents/Prompts/MyNewAgent.txt` with front matter and instructions.
2. Create `src/AIHarness.Agents/Plugins/MyNewPlugin.cs` with a `[KernelFunction]` method.
3. Create `src/AIHarness.Agents/MyNewAgent.cs` following the established pattern.
4. Add a new `WorkflowStatus` value to `AIHarness.Core/Enums/WorkflowStatus.cs`.
5. Register `MyNewPlugin` and `MyNewAgent` as Scoped in `OrchestratorServiceExtensions`.
6. Inject `MyNewAgent` into `SdlcOrchestrator`, add `.ChatAgent` to the `AgentGroupChat`, and extend `statusProgression` and `MaxIterationTerminationStrategy(maxIterations: 5)`.
