using AIHarness.Agents.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AIHarness.Agents;

public sealed class DeploymentAgent
{
    public ChatCompletionAgent ChatAgent { get; }

    public DeploymentAgent(Kernel kernel, DeploymentPlugin plugin)
    {
        var definition = AgentDefinitionLoader.Load(nameof(DeploymentAgent));
        var agentKernel = kernel.Clone();
        agentKernel.Plugins.AddFromObject(plugin, nameof(DeploymentPlugin));

        ChatAgent = new ChatCompletionAgent
        {
            Name = definition.Name,
            Instructions = definition.Instructions,
            Kernel = agentKernel
        };
    }
}
