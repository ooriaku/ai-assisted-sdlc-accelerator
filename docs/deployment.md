# Deployment Guide

## Azure Infrastructure

All Azure resources are defined as modular Bicep templates under `infra/`. A single `az deployment group create` command provisions the full environment.

### Resource Overview

```
Resource Group: aiharness-{env}-rg
│
├── Log Analytics Workspace       aiharness-{env}-logs
├── Key Vault                     aiharness-{env}-kv
│
├── Cosmos DB Account             aiharness-{env}-cosmos  (serverless)
│   ├── Database: AIHarness
│   ├── Container: WorkflowRuns   (partition key: /id)
│   ├── Container: Artifacts      (partition key: /workflowRunId)
│   └── Container: AuditLog       (partition key: /workflowRunId)
│
├── Service Bus Namespace         aiharness-{env}-sb  (Standard)
│   ├── Queue: agent-tasks        (lock: 10 min)
│   └── Queue: agent-results      (lock: 5 min)
│
├── Container Registry            aiharnessacr{env}  (Basic, admin disabled)
│
├── Storage Account               aiharness{env}stor
│   └── Blob Container: artifacts
│
└── Container Apps Environment    aiharness-{env}-cae
    ├── Container App: api        (external ingress port 8080, 1–5 replicas)
    └── Container App: worker     (internal, scale-to-zero 0–3 replicas)
```

### Bicep Module Breakdown

| Module | File | What it provisions |
|---|---|---|
| Log Analytics | `modules/log-analytics.bicep` | PerGB2018 SKU, 30-day retention; outputs `customerId` and `sharedKey` for Container Apps |
| Key Vault | `modules/key-vault.bicep` | Standard SKU, RBAC authorization model, 7-day soft delete |
| Cosmos DB | `modules/cosmos-db.bicep` | Serverless account; creates database and 3 containers with correct partition keys |
| Service Bus | `modules/service-bus.bicep` | Standard tier namespace and 2 queues |
| Container Registry | `modules/container-registry.bicep` | Basic SKU, `adminUserEnabled=false` — images pulled via managed identity |
| Storage Account | `modules/storage-account.bicep` | Standard_LRS, TLS 1.2 minimum, creates `artifacts` blob container |
| Container Apps | `modules/container-apps.bicep` | Managed environment wired to Log Analytics; API and Worker apps with SystemAssigned identity |
| Role Assignments | `modules/role-assignments.bicep` | Least-privilege RBAC grants to API and Worker identities |

### Role Assignments

| Identity | Role | Resource |
|---|---|---|
| API | Key Vault Secrets User | Key Vault |
| API | Cosmos DB Built-in Data Contributor | Cosmos DB account |
| API | AcrPull | Container Registry |
| API | Storage Blob Data Contributor | Storage Account |
| API | Azure Service Bus Data Sender | Service Bus namespace |
| Worker | Key Vault Secrets User | Key Vault |
| Worker | AcrPull | Container Registry |
| Worker | Azure Service Bus Data Receiver | Service Bus namespace |

The `role-assignments` module declares `dependsOn: [apps]` so the Container App managed identities exist before the role assignments are created.

## CI/CD Pipelines

### Workflow: `ci.yml` — Build, Test, Push

Triggers on every push to `main` or `feature/**` branches, and on pull requests to `main`.

**Job 1: `build-and-test`** (all triggers)
1. Checkout → setup .NET 9
2. `dotnet restore AIHarness.sln`
3. `dotnet build --no-restore -c Release`
4. `dotnet test --filter "Category!=Integration"` — excludes integration tests that require live Azure infrastructure
5. Upload TRX results and code coverage artifacts

**Job 2: `push-images`** (main branch pushes only, after `build-and-test`)
1. `azure/login@v2` via OIDC — no client secrets
2. `az acr login`
3. Build and push `aiharness-api` image tagged with `${{ github.sha }}` and `latest`
4. Build and push `aiharness-worker` image (same tags)

### Workflow: `deploy.yml` — Infrastructure + App Deploy

Triggers when `ci.yml` completes successfully on `main`, or manually via `workflow_dispatch` with an environment selector (`dev` / `prod`).

**Job 1: `guard`** — Verifies CI passed (skips on direct `workflow_dispatch`).

**Job 2: `deploy-infra`** (after guard)
1. `azure/login@v2` OIDC
2. `az deployment group create` with `infra/main.bicep` and the environment's `.bicepparam` file
3. Passes `imageTag=$SHA` so Container Apps launch the exact image built by CI

**Job 3: `deploy-apps`** (after `deploy-infra`)
1. `az containerapp update` — updates the API container app to the new image SHA
2. `az containerapp update` — updates the Worker container app
3. Smoke test: resolves the API FQDN from Container Apps, calls `/health` with 5 retries

## First-Time Setup

### 1. Create the Resource Group

```bash
az group create --name aiharness-dev-rg --location eastus
```

### 2. Register an App Registration for GitHub OIDC

```bash
APP_ID=$(az ad app create --display-name "aiharness-github-oidc" --query appId -o tsv)
az ad sp create --id $APP_ID

# Grant Contributor on the resource group
SUBSCRIPTION=$(az account show --query id -o tsv)
az role assignment create \
  --role Contributor \
  --assignee $APP_ID \
  --scope /subscriptions/$SUBSCRIPTION/resourceGroups/aiharness-dev-rg
```

### 3. Add the Federated Credential

In the Azure Portal → App registrations → `aiharness-github-oidc` → Certificates & secrets → Federated credentials → Add:

| Field | Value |
|---|---|
| Federated credential scenario | GitHub Actions deploying Azure resources |
| Organisation | your GitHub org or username |
| Repository | `ai-assisted-sdlc-project` |
| Entity type | Branch |
| Branch | `main` |
| Name | `aiharness-main` |

### 4. Configure GitHub Secrets

In your repository → Settings → Secrets and variables → Actions:

| Secret | Value |
|---|---|
| `AZURE_CLIENT_ID` | `$APP_ID` from step 2 |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| `AZURE_RESOURCE_GROUP` | `aiharness-dev-rg` |
| `ACR_NAME` | `aiharnessacrdev` |
| `ACR_REGISTRY` | `aiharnessacrdev.azurecr.io` |

### 5. First Deployment

Push to `main` — CI builds and pushes images, then Deploy provisions infrastructure and updates Container Apps.

Alternatively, trigger manually:

```bash
# Provision infrastructure
az deployment group create \
  --resource-group aiharness-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam

# Build and push images (requires Docker running and az acr login)
ACR=aiharnessacrdev.azurecr.io
SHA=$(git rev-parse HEAD)
az acr login --name aiharnessacrdev

docker build -f src/AIHarness.API/Dockerfile    -t $ACR/aiharness-api:$SHA    -t $ACR/aiharness-api:latest    .
docker build -f src/AIHarness.Worker/Dockerfile -t $ACR/aiharness-worker:$SHA -t $ACR/aiharness-worker:latest .
docker push $ACR/aiharness-api:$SHA && docker push $ACR/aiharness-api:latest
docker push $ACR/aiharness-worker:$SHA && docker push $ACR/aiharness-worker:latest
```

### 6. Add the Anthropic API Key to Key Vault

```bash
az keyvault secret set \
  --vault-name aiharness-dev-kv \
  --name "Anthropic--ApiKey" \
  --value "sk-ant-api03-..."
```

Key Vault secret names use `--` as the namespace separator. The `KeyVaultConfigurationSource` maps this back to `Anthropic:ApiKey` when loading configuration.

## Environment Parameters

| Parameter | `dev.bicepparam` | `prod.bicepparam` |
|---|---|---|
| `location` | `eastus` | `eastus` |
| `environment` | `dev` | `prod` |
| `prefix` | `aiharness` | `aiharness` |
| `imageTag` | `latest` | overridden by CI to `$SHA` |

## Post-Deployment Verification

```bash
# Get the API URL
API_URL=$(az containerapp show \
  --name aiharness-dev-api \
  --resource-group aiharness-dev-rg \
  --query "properties.configuration.ingress.fqdn" -o tsv)

# Health check
curl https://$API_URL/health
# Expected: {"status":"healthy"}

# Submit a workflow
curl -X POST https://$API_URL/api/workflows \
  -H "Content-Type: application/json" \
  -d '{"projectName":"Smoke Test","requirements":"Build a simple CRUD API for tasks"}'

# Poll until Completed
curl https://$API_URL/api/workflows/<returned-id>
```

## Scaling Notes

| Component | Behaviour |
|---|---|
| API Container App | Min 1 replica — always warm; scales to 5 on HTTP load |
| Worker Container App | Scale-to-zero (0 replicas at idle); scales to 3 on Service Bus queue depth |
| Cosmos DB | Serverless — no capacity to provision; billed per request unit |
| Service Bus | Standard tier — supports dead-letter queues and message lock renewal |
