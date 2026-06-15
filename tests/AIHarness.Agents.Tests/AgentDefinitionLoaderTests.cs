using AIHarness.Agents;
using FluentAssertions;

namespace AIHarness.Agents.Tests;

public class AgentDefinitionLoaderTests
{
    [Theory]
    [InlineData("RequirementsAgent")]
    [InlineData("CodeGenerationAgent")]
    [InlineData("TestingAgent")]
    [InlineData("DeploymentAgent")]
    public void Load_KnownAgent_ParsesFrontMatterAndInstructions(string agentFile)
    {
        var definition = AgentDefinitionLoader.Load(agentFile);

        definition.Name.Should().NotBeNullOrWhiteSpace();
        definition.Instructions.Should().NotBeNullOrWhiteSpace();
        definition.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("RequirementsAgent", "RequirementsAgent")]
    [InlineData("CodeGenerationAgent", "CodeGenerationAgent")]
    [InlineData("TestingAgent", "TestingAgent")]
    [InlineData("DeploymentAgent", "DeploymentAgent")]
    public void Load_KnownAgent_NameMatchesExpected(string agentFile, string expectedName)
    {
        var definition = AgentDefinitionLoader.Load(agentFile);
        definition.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Load_UnknownAgent_ThrowsInvalidOperationException()
    {
        var act = () => AgentDefinitionLoader.Load("NonExistentAgent");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NonExistentAgent*");
    }

    [Theory]
    [InlineData("RequirementsAgent")]
    [InlineData("CodeGenerationAgent")]
    [InlineData("TestingAgent")]
    [InlineData("DeploymentAgent")]
    public void Load_KnownAgent_InstructionsDoNotContainFrontMatter(string agentFile)
    {
        var definition = AgentDefinitionLoader.Load(agentFile);

        // Front matter markers must not leak into the instructions
        definition.Instructions.Should().NotStartWith("---");
        definition.Instructions.Should().NotContain("name:");
        definition.Instructions.Should().NotContain("version:");
    }

    [Theory]
    [InlineData("RequirementsAgent")]
    [InlineData("CodeGenerationAgent")]
    [InlineData("TestingAgent")]
    [InlineData("DeploymentAgent")]
    public void Load_KnownAgent_InstructionsContainSaveArtifactCallInstruction(string agentFile)
    {
        var definition = AgentDefinitionLoader.Load(agentFile);

        // Every agent prompt must instruct the model to call its save_*_artifact function
        definition.Instructions.Should().Contain("save_");
    }
}
