using System.ComponentModel;
using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Infrastructure.Storage;
using Microsoft.SemanticKernel;

namespace AIHarness.Agents.Plugins;

public sealed class DeploymentPlugin
{
    private readonly IArtifactRepository _artifacts;
    private readonly BlobArtifactStorage _blobStorage;

    public DeploymentPlugin(IArtifactRepository artifacts, BlobArtifactStorage blobStorage)
    {
        _artifacts = artifacts;
        _blobStorage = blobStorage;
    }

    [KernelFunction("save_deployment_artifact")]
    [Description("Persists the generated GitHub Actions CI/CD pipeline YAML to blob storage.")]
    public async Task<string> SaveDeploymentArtifactAsync(
        [Description("The workflow run ID")] string workflowRunId,
        [Description("GitHub Actions pipeline YAML content")] string pipelineYaml,
        CancellationToken cancellationToken = default)
    {
        var blobUri = await _blobStorage.UploadArtifactAsync(
            workflowRunId, "pipeline.yml", pipelineYaml, cancellationToken);

        await _artifacts.SaveAsync(new WorkflowArtifact
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowRunId = workflowRunId,
            ArtifactType = "deployment",
            BlobUrl = blobUri.ToString(),
            ContentSummary = pipelineYaml[..Math.Min(300, pipelineYaml.Length)]
        }, cancellationToken);

        return blobUri.ToString();
    }
}
