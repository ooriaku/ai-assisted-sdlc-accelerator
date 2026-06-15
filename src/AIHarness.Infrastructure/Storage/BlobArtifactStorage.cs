using System.Text;
using AIHarness.Core.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace AIHarness.Infrastructure.Storage;

public sealed class BlobArtifactStorage
{
    private readonly BlobServiceClient _blobClient;
    private readonly BlobStorageOptions _options;

    public BlobArtifactStorage(BlobServiceClient blobClient, IOptions<BlobStorageOptions> options)
    {
        _blobClient = blobClient;
        _options = options.Value;
    }

    public async Task<Uri> UploadArtifactAsync(
        string workflowRunId, string fileName, string content, CancellationToken ct = default)
    {
        var container = _blobClient.GetBlobContainerClient(_options.ArtifactsContainer);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobPath = $"{workflowRunId}/{fileName}";
        var blob = container.GetBlobClient(blobPath);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        return blob.Uri;
    }

    public async Task<string?> DownloadArtifactAsync(
        string workflowRunId, string fileName, CancellationToken ct = default)
    {
        var container = _blobClient.GetBlobContainerClient(_options.ArtifactsContainer);
        var blob = container.GetBlobClient($"{workflowRunId}/{fileName}");

        if (!await blob.ExistsAsync(ct))
            return null;

        var response = await blob.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }
}
