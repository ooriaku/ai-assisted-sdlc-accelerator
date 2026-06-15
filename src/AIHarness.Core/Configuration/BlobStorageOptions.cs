namespace AIHarness.Core.Configuration;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";
    public required string AccountUri { get; set; }
    public string ArtifactsContainer { get; set; } = "artifacts";
}
