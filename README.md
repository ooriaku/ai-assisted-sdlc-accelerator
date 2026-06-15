# AI Harness — Enterprise Agentic SDLC Automation

An enterprise-grade multi-agent AI system that automates the full software development lifecycle — from requirements capture through code generation, test creation, and deployment pipeline generation — using Anthropic Claude models orchestrated by Microsoft Semantic Kernel on Azure.

## Overview

A single `POST /api/workflows` request triggers a sequential pipeline of four specialised Claude agents. Each agent reads the output of the previous, produces a structured artifact (JSON or YAML), persists it to Azure Blob Storage, and records a reference in Cosmos DB. The caller gets a workflow ID back immediately; status is polled asynchronously.

```
POST /api/workflows  →  RequirementsAgent  →  CodeGenerationAgent
                                          →  TestingAgent
                                          →  DeploymentAgent
                                          →  Completed (4 artifacts in Blob Storage)
```

## Documentation

| Document | Contents |
|---|---|
| [Architecture](docs/architecture.md) | Layers, design decisions, data flow, security model |
| [Agents](docs/agents.md) | Agent classes, prompt system, SDLC pipeline, SK orchestration |
| [Deployment](docs/deployment.md) | Azure infrastructure, Bicep IaC, GitHub Actions CI/CD |

## Technology Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 9 / C# 13 |
| AI | Anthropic Claude API (`claude-opus-4-8` orchestrator, `claude-sonnet-4-6` agents) |
| Orchestration | Microsoft Semantic Kernel 1.77 — `AgentGroupChat` |
| API | ASP.NET Core 9 Minimal API |
| Database | Azure Cosmos DB (serverless, NoSQL) |
| Messaging | Azure Service Bus (Standard tier) |
| Blob storage | Azure Blob Storage |
| Secrets | Azure Key Vault |
| Compute | Azure Container Apps |
| CI/CD | GitHub Actions with OIDC federated identity |
| IaC | Bicep (modular) |

## Quick Start (Local Docker)

**Prerequisites:** Docker Desktop running, an Anthropic API key.

```bash
# 1. Create your local env file
cp .env.template .env
# Edit .env — set Anthropic__ApiKey=sk-ant-api03-...

# 2. Start everything (Cosmos emulator takes ~90 seconds to become healthy)
docker compose up --build

# 3. Health check
curl http://localhost:5000/health

# 4. Submit a workflow
curl -X POST http://localhost:5000/api/workflows \
  -H "Content-Type: application/json" \
  -d '{"projectName":"Demo","requirements":"Build a task management REST API"}'

# 5. Poll status with the returned ID
curl http://localhost:5000/api/workflows/<id>

# 6. View artifacts once status is Completed
curl http://localhost:5000/api/workflows/<id>/artifacts
```

OpenAPI / Scalar UI is available at `http://localhost:5000/scalar`.

## Project Structure

```
ai-assisted-sdlc-project/
├── src/
│   ├── AIHarness.Core/               # Domain models, interfaces, configuration (no NuGet deps)
│   │   ├── Enums/WorkflowStatus.cs
│   │   ├── Models/                   # WorkflowRun, WorkflowArtifact, AuditEntry
│   │   ├── Interfaces/               # IWorkflowRepository, IArtifactRepository, IAuditLogRepository
│   │   └── Configuration/            # Options classes for each Azure service
│   │
│   ├── AIHarness.Infrastructure/     # Azure SDK clients, repositories, messaging
│   │   ├── DependencyInjection/      # AddInfrastructure() extension
│   │   ├── Repositories/             # CosmosDB implementations
│   │   ├── Storage/                  # BlobArtifactStorage
│   │   ├── Messaging/                # ServiceBusPublisher
│   │   └── KeyVault/                 # Custom IConfigurationSource
│   │
│   ├── AIHarness.Agents/             # Four SDLC agent classes + prompt system
│   │   ├── RequirementsAgent.cs
│   │   ├── CodeGenerationAgent.cs
│   │   ├── TestingAgent.cs
│   │   ├── DeploymentAgent.cs
│   │   ├── AgentDefinition.cs        # Typed record for parsed prompt metadata
│   │   ├── AgentDefinitionLoader.cs  # Reads .txt prompts from embedded resources
│   │   ├── Plugins/                  # KernelFunction plugins (one per agent)
│   │   └── Prompts/                  # *.txt prompt files (version-controlled)
│   │
│   ├── AIHarness.Orchestrator/       # SK AgentGroupChat pipeline
│   │   ├── SdlcOrchestrator.cs
│   │   ├── MaxIterationTerminationStrategy.cs
│   │   └── OrchestratorServiceExtensions.cs
│   │
│   ├── AIHarness.API/                # ASP.NET Core 9 Minimal API host
│   │   ├── Program.cs
│   │   └── Endpoints/               # WorkflowEndpoints, AgentEndpoints, HealthEndpoints
│   │
│   └── AIHarness.Worker/             # Service Bus consumer (BackgroundService)
│       ├── Program.cs
│       └── AgentTaskWorker.cs
│
├── tests/
│   ├── AIHarness.Core.Tests/         # Unit tests — WorkflowRun, WorkflowArtifact
│   ├── AIHarness.Agents.Tests/       # Unit tests — agent classes, prompt loader
│   └── AIHarness.Integration.Tests/  # Integration tests (requires running infrastructure)
│
├── infra/
│   ├── main.bicep                    # Orchestrates all modules
│   ├── modules/                      # One .bicep per Azure resource type
│   └── parameters/                   # dev.bicepparam, prod.bicepparam
│
├── .github/workflows/
│   ├── ci.yml                        # Build, test, push images to ACR
│   └── deploy.yml                    # Bicep deploy + containerapp update + smoke test
│
├── docker-compose.yml                # Local dev: Cosmos emulator + Azurite + API + Worker
├── .env.template                     # Copy to .env — add Anthropic__ApiKey
└── AIHarness.sln
```

## Configuration Reference

All configuration follows the Options Pattern and is injected via `IOptions<T>`.

| Section | Key | Description |
|---|---|---|
| `Anthropic` | `ApiKey` | Anthropic API key (`sk-ant-...`) |
| `Anthropic` | `OrchestratorModel` | Model for orchestration (default: `claude-opus-4-8`) |
| `Anthropic` | `AgentModel` | Model for agents (default: `claude-sonnet-4-6`) |
| `CosmosDb` | `AccountEndpoint` | Cosmos DB account URI |
| `CosmosDb` | `AccountKey` | Key (Development/emulator only; Production uses managed identity) |
| `ServiceBus` | `FullyQualifiedNamespace` | e.g. `myns.servicebus.windows.net` |
| `BlobStorage` | `AccountUri` | Blob service endpoint URI |
| `KeyVault` | `Uri` | Key Vault URI (production only; skipped in Development) |

In production, all Azure credentials use `DefaultAzureCredential` (managed identity). Secrets are loaded from Key Vault before the Options system binds, so they are transparent to application code.
# ai-assisted-sdlc-accelerator
