using AIHarness.Agents.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AIHarness.Agents;

public sealed class RequirementsAgent
{
    public ChatCompletionAgent ChatAgent { get; }

    public RequirementsAgent(Kernel kernel, RequirementsPlugin plugin)
    {
        var definition = AgentDefinitionLoader.Load(nameof(RequirementsAgent));
        var agentKernel = kernel.Clone();
        agentKernel.Plugins.AddFromObject(plugin, nameof(RequirementsPlugin));

        ChatAgent = new ChatCompletionAgent
        {
            Name = definition.Name,
            Instructions = definition.Instructions,
            Kernel = agentKernel
        };
    }
}
