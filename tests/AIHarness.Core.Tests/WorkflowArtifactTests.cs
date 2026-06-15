using AIHarness.Core.Models;
using FluentAssertions;

namespace AIHarness.Core.Tests;

public class WorkflowArtifactTests
{
    [Fact]
    public void WorkflowArtifact_Properties_AreInitialized()
    {
        var artifact = new WorkflowArtifact
        {
            Id = "art-1",
            WorkflowRunId = "run-1",
            ArtifactType = "requirements",
            BlobUrl = "https://storage.blob.core.windows.net/artifacts/run-1/requirements.json"
        };

        artifact.WorkflowRunId.Should().Be("run-1");
        artifact.ArtifactType.Should().Be("requirements");
        artifact.BlobUrl.Should().StartWith("https://");
    }

    [Fact]
    public void WorkflowArtifact_CreatedAt_IsSetToUtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var artifact = new WorkflowArtifact
        {
            Id = "art-1",
            WorkflowRunId = "run-1",
            ArtifactType = "code",
            BlobUrl = "https://example.com/blob"
        };
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        artifact.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }
}
