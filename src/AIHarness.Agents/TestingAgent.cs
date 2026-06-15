using AIHarness.Agents.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AIHarness.Agents;

public sealed class TestingAgent
{
    public ChatCompletionAgent ChatAgent { get; }

    public TestingAgent(Kernel kernel, TestingPlugin plugin)
    {
        var definition = AgentDefinitionLoader.Load(nameof(TestingAgent));
        var agentKernel = kernel.Clone();
        agentKernel.Plugins.AddFromObject(plugin, nameof(TestingPlugin));

        ChatAgent = new ChatCompletionAgent
        {
            Name = definition.Name,
            Instructions = definition.Instructions,
            Kernel = agentKernel
        };
    }
}
