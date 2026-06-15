namespace AIHarness.Core.Configuration;

public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";
    public required string Token { get; set; }
    public required string Owner { get; set; }
    public required string Repo { get; set; }
    public string DefaultBranch { get; set; } = "main";
}
