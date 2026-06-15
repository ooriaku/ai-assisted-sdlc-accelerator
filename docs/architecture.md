# System Architecture

## High-Level Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT / CI PIPELINE                        │
└────────────────────────────┬────────────────────────────────────────┘
                             │ POST /api/workflows
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      AIHarness.API  (Container App)                 │
│                                                                     │
│   ASP.NET Core 9 Minimal API                                        │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │
│   │  Workflow    │  │    Agent     │  │    Health    │             │
│   │  Endpoints   │  │  Endpoints   │  │   Endpoint   │             │
│   └──────┬───────┘  └──────────────┘  └──────────────┘             │
│          │ fire-and-forget (background DI scope)                    │
│          ▼                                                          │
│   ┌─────────────────────────────────────────┐                       │
│   │            SdlcOrchestrator              │                       │
│   │   AgentGroupChat (SK 1.77)               │                       │
│   │   SequentialSelectionStrategy            │                       │
│   │   MaxIterationTerminationStrategy(4)     │                       │
│   │                                         │                       │
│   │  ┌─────────┐ ┌──────┐ ┌───────┐ ┌────┐ │                       │
│   │  │ Req.    │ │ Code │ │ Test  │ │ Deploy│ │                     │
│   │  │ Agent   │ │ Agent│ │ Agent │ │ Agent │ │                     │
│   │  └─────────┘ └──────┘ └───────┘ └────┘ │                       │
│   └─────────────────────────────────────────┘                       │
└────────┬──────────────────────────────────┬────────────────────────┘
         │                                  │
         │ Cosmos DB SDK                    │ Blob SDK
         ▼                                  ▼
┌─────────────────┐              ┌────────────────────┐
│  Azure Cosmos DB│              │  Azure Blob Storage │
│  (serverless)   │              │  (artifacts)        │
│  WorkflowRuns   │              └────────────────────┘
│  Artifacts      │
│  AuditLog       │
└─────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                    AIHarness.Worker  (Container App)                │
│                                                                     │
│   BackgroundService → ServiceBusProcessor                           │
│   MaxConcurrentCalls=2, MaxAutoLockRenewalDuration=10min            │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             ▼
                   ┌─────────────────┐
                   │ Azure Service Bus│
                   │ agent-tasks     │
                   │ agent-results   │
                   └─────────────────┘
```

## Solution Layers

The solution is structured as six projects with a strict dependency hierarchy — each layer may only reference layers below it.

```
AIHarness.API  ──────────────────────────────────────────┐
AIHarness.Worker  ───────────────────────────────────────┤
  └── AIHarness.Orchestrator  ──────────────────────────┤
        └── AIHarness.Agents  ──────────────────────────┤
              └── AIHarness.Infrastructure  ────────────┤
                    └── AIHarness.Core (no deps)  ───────┘
```

### AIHarness.Core

Pure domain layer with zero NuGet dependencies. Everything else in the solution depends on it; it depends on nothing.

| Type | Purpose |
|---|---|
| `WorkflowRun` | Sealed record representing one SDLC pipeline execution |
| `WorkflowArtifact` | Sealed record linking a generated artifact to a workflow run |
| `AuditEntry` | Sealed record for the immutable audit trail |
| `WorkflowStatus` | Enum — `Created → RequirementsCapture → CodeGeneration → Testing → Deployment → Completed / Failed` |
| `IWorkflowRepository` | CRUD + list interface |
| `IArtifactRepository` | Save + query by workflow run |
| `IAuditLogRepository` | Append-only audit log |
| `*Options` classes | Strongly-typed configuration (AnthropicOptions, CosmosDbOptions, …) |

All models are **sealed records** with `init`-only properties. Updates use C# `with` expressions, making state transitions explicit and traceable.

### AIHarness.Infrastructure

Azure SDK bindings, repository implementations, and the Key Vault configuration source. Registered via `AddInfrastructure()`.

| Component | Technology | Notes |
|---|---|---|
| `CosmosWorkflowRepository` | Azure Cosmos DB SDK 3.61 | Partition key `/id`, camelCase serialization |
| `CosmosArtifactRepository` | Azure Cosmos DB SDK 3.61 | Partition key `/workflowRunId` |
| `CosmosAuditLogRepository` | Azure Cosmos DB SDK 3.61 | Partition key `/workflowRunId` |
| `BlobArtifactStorage` | Azure Blob Storage SDK 12.29 | `UploadArtifactAsync` returns `Uri` |
| `ServiceBusPublisher` | Azure Service Bus SDK 7.20 | JSON-serialized messages to `agent-tasks` |
| `KeyVaultConfigurationSource` | Azure Key Vault Secrets SDK 4.11 | Custom `IConfigurationSource`; maps `--` → `:` in key names |
| Anthropic → SK bridge | Anthropic SDK 12.29 built-in `AsIChatClient()` | Bridges to SK via `AsChatCompletionService()` |

**Authentication strategy:** All Azure SDK clients use `DefaultAzureCredential` in production. In Development, the Cosmos client uses the emulator master key with SSL validation bypassed; Blob uses the Azurite well-known storage key. This branching lives entirely in `InfrastructureServiceExtensions` — application code is unaware of it.

### AIHarness.Agents

Four domain-specific agent classes, each encapsulating a `ChatCompletionAgent` from Semantic Kernel, plus a prompt loading system. See [agents.md](agents.md) for detailed coverage.

### AIHarness.Orchestrator

Wires the four agent classes into a Semantic Kernel `AgentGroupChat` pipeline. Registered via `AddSdlcOrchestrator()`.

| Component | Purpose |
|---|---|
| `SdlcOrchestrator` | Creates the `AgentGroupChat`, seeds it with requirements, advances `WorkflowStatus` in Cosmos after each agent turn |
| `MaxIterationTerminationStrategy` | Custom `TerminationStrategy` that stops the chat after exactly 4 turns (one per agent) |
| `OrchestratorServiceExtensions` | Registers `AddKernel()`, 4 plugins, 4 agent classes, `SdlcOrchestrator` — all Scoped |

### AIHarness.API

ASP.NET Core 9 Minimal API with three endpoint groups:

| Route | Method | Description |
|---|---|---|
| `/api/workflows` | POST | Create workflow run; fire-and-forget orchestrator; return 201 |
| `/api/workflows/{id}` | GET | Poll status |
| `/api/workflows` | GET | Paginated list |
| `/api/workflows/{id}/artifacts` | GET | List artifacts for a completed run |
| `/api/agents/status` | GET | Agent readiness check |
| `/health` | GET | Liveness probe |

OpenAPI schema is served at `/openapi/v1.json`; Scalar UI at `/scalar`.

### AIHarness.Worker

A `BackgroundService` that subscribes to the Azure Service Bus `agent-tasks` queue. Designed for long-running tasks that are offloaded from the API process. Currently handles message acknowledgement and logs; extended by adding dispatch cases in `OnMessageAsync`.

| Setting | Value |
|---|---|
| `MaxConcurrentCalls` | 2 |
| `MaxAutoLockRenewalDuration` | 10 minutes |
| On success | `CompleteMessageAsync` |
| On failure | `AbandonMessageAsync` (message returns to queue for retry) |

## Key Design Decisions

### Fire-and-Forget Orchestration

`POST /api/workflows` returns `201 Created` immediately. The orchestrator runs in a background DI scope (`IServiceScopeFactory.CreateAsyncScope`) launched via `Task.Run`. This allows the HTTP client to poll rather than wait — agent pipelines can take several minutes.

```csharp
_ = Task.Run(async () =>
{
    await using var scope = scopeFactory.CreateAsyncScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<SdlcOrchestrator>();
    await orchestrator.RunAsync(run.Id, CancellationToken.None);
});
```

### Scoped Agent Lifecycle

All four agent classes, their plugins, and `SdlcOrchestrator` are registered as **Scoped**. Each workflow run resolves them in an isolated DI scope, ensuring state (plugin instances, SK kernel clones) does not leak between concurrent runs.

### Immutable Domain Model

`WorkflowRun` and `WorkflowArtifact` are sealed records. Status transitions are expressed as `run with { Status = … }` — the old record is preserved in Cosmos, and the new record is written as an upsert. This gives a natural audit trail without a separate history table.

### Prompt Files as First-Class Code

Agent prompts live in `src/AIHarness.Agents/Prompts/*.txt` as embedded assembly resources. Each file has YAML front matter (`name`, `description`, `version`) followed by the instructions body. `AgentDefinitionLoader` parses the front matter at startup. Prompts are version-controlled as plain text, producing clean PR diffs.

### No Custom AI Adapter

Anthropic SDK 12.29 ships `AnthropicClientExtensions.AsIChatClient()`, which returns a `Microsoft.Extensions.AI.IChatClient`. This is bridged into Semantic Kernel via `IChatClient.AsChatCompletionService()`. No hand-written adapter class is needed.

### Key Vault Before Options Binding

In non-Development environments, the Key Vault configuration source is added to `IConfigurationBuilder` before `Configure<TOptions>()` runs. Secrets loaded from Key Vault are therefore available to the Options system and appear identical to `appsettings.json` values from the application's perspective.

## Data Flow

```
1. Client sends POST /api/workflows
   { projectName: "...", requirements: "..." }

2. API creates WorkflowRun (status: Created) in Cosmos DB
   → returns 201 with run ID

3. Background scope resolves SdlcOrchestrator

4. Orchestrator creates AgentGroupChat with 4 ChatCompletionAgents

5. Chat is seeded: [User] "Build a task management REST API..."

6. RequirementsAgent turn
   → Claude produces requirements JSON
   → RequirementsPlugin.save_requirements_artifact() called
      → BlobArtifactStorage uploads JSON → returns blob URI
      → CosmosArtifactRepository saves WorkflowArtifact
   → Cosmos WorkflowRun status → RequirementsCapture

7. CodeGenerationAgent turn  (same pattern, status → CodeGeneration)

8. TestingAgent turn          (same pattern, status → Testing)

9. DeploymentAgent turn       (same pattern, status → Deployment)

10. Orchestrator sets status → Completed, CompletedAt = UtcNow

11. Client polls GET /api/workflows/{id} → sees Completed
    GET /api/workflows/{id}/artifacts    → 4 artifact records with blob URLs
```

## Security Model

| Concern | Mechanism |
|---|---|
| Azure resource access | `DefaultAzureCredential` → Managed Identity in Container Apps |
| Role assignments | Bicep `role-assignments.bicep` — least-privilege RBAC (e.g. `Cosmos DB Built-in Data Contributor`, `Key Vault Secrets User`) |
| Secrets at rest | Azure Key Vault; never in appsettings.json or environment variables in production |
| CI/CD identity | GitHub OIDC federated credential — no client secrets stored in GitHub |
| Container registry | ACR admin disabled; pull via managed identity |
| TLS | Container Apps enforces HTTPS on the external ingress; internal traffic is within the managed environment |
| Local dev SSL bypass | Cosmos emulator SSL validation is bypassed only when `IHostEnvironment.IsDevelopment()` is true |

## NuGet Dependency Map

| Project | Key Packages |
|---|---|
| `AIHarness.Core` | (none) |
| `AIHarness.Infrastructure` | `Anthropic 12.29`, `Microsoft.Extensions.AI 10.7`, `Microsoft.SemanticKernel.Abstractions 1.77`, `Microsoft.Azure.Cosmos 3.61`, `Azure.Messaging.ServiceBus 7.20`, `Azure.Storage.Blobs 12.29`, `Azure.Security.KeyVault.Secrets 4.11`, `Azure.Identity 1.21`, `Octokit 14.0`, `Newtonsoft.Json 13.0` |
| `AIHarness.Agents` | `Microsoft.SemanticKernel 1.77`, `Microsoft.SemanticKernel.Agents.Core 1.77` |
| `AIHarness.Orchestrator` | `Microsoft.SemanticKernel 1.77`, `Microsoft.SemanticKernel.Agents.Core 1.77` |
| `AIHarness.API` | `Microsoft.AspNetCore.OpenApi 9.0.6`, `Scalar.AspNetCore` |
| `AIHarness.Worker` | `Azure.Messaging.ServiceBus 7.20`, `Microsoft.Extensions.Hosting 9.0` |
