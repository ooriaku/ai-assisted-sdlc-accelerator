using System.ComponentModel;
using AIHarness.Core.Interfaces;
using AIHarness.Core.Models;
using AIHarness.Infrastructure.Storage;
using Microsoft.SemanticKernel;

namespace AIHarness.Agents.Plugins;

public sealed class TestingPlugin
{
    private readonly IArtifactRepository _artifacts;
    private readonly BlobArtifactStorage _blobStorage;

    public TestingPlugin(IArtifactRepository artifacts, BlobArtifactStorage blobStorage)
    {
        _artifacts = artifacts;
        _blobStorage = blobStorage;
    }

    [KernelFunction("save_test_artifact")]
    [Description("Persists generated xUnit test files JSON to blob storage.")]
    public async Task<string> SaveTestArtifactAsync(
        [Description("The workflow run ID")] string workflowRunId,
        [Description("Generated tests JSON with testFiles array and coverage list")] string testsJson,
        CancellationToken cancellationToken = default)
    {
        var blobUri = await _blobStorage.UploadArtifactAsync(
            workflowRunId, "tests.json", testsJson, cancellationToken);

        await _artifacts.SaveAsync(new WorkflowArtifact
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowRunId = workflowRunId,
            ArtifactType = "tests",
            BlobUrl = blobUri.ToString(),
            ContentSummary = testsJson[..Math.Min(300, testsJson.Length)]
        }, cancellationToken);

        return blobUri.ToString();
    }
}
