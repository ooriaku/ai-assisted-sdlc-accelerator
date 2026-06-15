using System.Reflection;

namespace AIHarness.Agents;

/// <summary>
/// Loads agent definitions from embedded Markdown prompt files.
/// Each file lives at src/AIHarness.Agents/Prompts/{AgentName}.md and uses YAML front matter
/// to declare name, description, and version — the body is the system prompt (Instructions).
/// </summary>
public static class AgentDefinitionLoader
{
    private static readonly Assembly _assembly = typeof(AgentDefinitionLoader).Assembly;

    public static AgentDefinition Load(string agentFileName)
    {
        var resourceName = $"AIHarness.Agents.Prompts.{agentFileName}.txt";
        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Prompt resource '{resourceName}' not found. " +
                $"Available: {string.Join(", ", _assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    private static AgentDefinition Parse(string content)
    {
        var lines = content.ReplaceLineEndings("\n").Split('\n');

        if (lines.Length < 2 || lines[0].Trim() != "---")
            throw new FormatException("Agent prompt file must begin with a YAML front matter block (---).");

        var frontMatterEnd = Array.IndexOf(lines, "---", 1);
        if (frontMatterEnd < 0)
            throw new FormatException("Agent prompt file front matter block is not closed with ---.");

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines[1..frontMatterEnd])
        {
            var sep = line.IndexOf(':');
            if (sep < 0) continue;
            var key = line[..sep].Trim();
            var value = line[(sep + 1)..].Trim();
            metadata[key] = value;
        }

        if (!metadata.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
            throw new FormatException("Agent prompt file front matter must include a non-empty 'name' field.");

        var instructions = string.Join('\n', lines[(frontMatterEnd + 1)..]).Trim();

        return new AgentDefinition
        {
            Name = name,
            Description = metadata.GetValueOrDefault("description", string.Empty),
            Version = metadata.GetValueOrDefault("version", "1.0"),
            Instructions = instructions
        };
    }
}
