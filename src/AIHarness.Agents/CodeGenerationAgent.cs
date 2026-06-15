using AIHarness.Agents.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AIHarness.Agents;

public sealed class CodeGenerationAgent
{
    public ChatCompletionAgent ChatAgent { get; }

    public CodeGenerationAgent(Kernel kernel, CodeGenerationPlugin plugin)
    {
        var definition = AgentDefinitionLoader.Load(nameof(CodeGenerationAgent));
        var agentKernel = kernel.Clone();
        agentKernel.Plugins.AddFromObject(plugin, nameof(CodeGenerationPlugin));

        ChatAgent = new ChatCompletionAgent
        {
            Name = definition.Name,
            Instructions = definition.Instructions,
            Kernel = agentKernel
        };
    }
}
