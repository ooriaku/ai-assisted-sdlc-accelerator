using System.ComponentModel;
using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Infrastructure.Storage;
using Microsoft.SemanticKernel;

namespace AIHarness.Agents.Plugins;

public sealed class RequirementsPlugin
{
    private readonly IArtifactRepository _artifacts;
    private readonly BlobArtifactStorage _blobStorage;

    public RequirementsPlugin(IArtifactRepository artifacts, BlobArtifactStorage blobStorage)
    {
        _artifacts = artifacts;
        _blobStorage = blobStorage;
    }

    [KernelFunction("save_requirements_artifact")]
    [Description("Persists the structured requirements JSON to blob storage and records the artifact.")]
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
}
