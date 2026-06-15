using System.ComponentModel;
using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Infrastructure.Storage;
using Microsoft.SemanticKernel;

namespace AIHarness.Agents.Plugins;

public sealed class CodeGenerationPlugin
{
    private readonly IArtifactRepository _artifacts;
    private readonly BlobArtifactStorage _blobStorage;

    public CodeGenerationPlugin(IArtifactRepository artifacts, BlobArtifactStorage blobStorage)
    {
        _artifacts = artifacts;
        _blobStorage = blobStorage;
    }

    [KernelFunction("save_code_artifact")]
    [Description("Persists the generated code JSON (project tree + file contents) to blob storage.")]
    public async Task<string> SaveCodeArtifactAsync(
        [Description("The workflow run ID")] string workflowRunId,
        [Description("Generated code JSON with projectTree and files arrays")] string codeJson,
        CancellationToken cancellationToken = default)
    {
        var blobUri = await _blobStorage.UploadArtifactAsync(
            workflowRunId, "generated-code.json", codeJson, cancellationToken);

        await _artifacts.SaveAsync(new WorkflowArtifact
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowRunId = workflowRunId,
            ArtifactType = "code",
            BlobUrl = blobUri.ToString(),
            ContentSummary = codeJson[..Math.Min(300, codeJson.Length)]
        }, cancellationToken);

        return blobUri.ToString();
    }
}
