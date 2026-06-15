using AIHarness.Agents;
using AIHarness.Agents.Plugins;
using AIHarness.Core.Configuration;
using AIHarness.Core.Interfaces;
using AIHarness.Infrastructure.Storage;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;

namespace AIHarness.Agents.Tests;

public class AgentClassTests
{
    private static Kernel BuildKernel() => Kernel.CreateBuilder().Build();

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IArtifactRepository>().Object);
        services.AddSingleton(new BlobServiceClient(new Uri("https://fakestorage.blob.core.windows.net")));
        services.AddSingleton<IOptions<BlobStorageOptions>>(
            Options.Create(new BlobStorageOptions
            {
                AccountUri = "https://fakestorage.blob.core.windows.net",
                ArtifactsContainer = "test"
            }));
        services.AddSingleton<BlobArtifactStorage>();
        services.AddScoped<RequirementsPlugin>();
        services.AddScoped<CodeGenerationPlugin>();
        services.AddScoped<TestingPlugin>();
        services.AddScoped<DeploymentPlugin>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void RequirementsAgent_ChatAgent_HasCorrectName()
    {
        var sp = BuildServiceProvider();
        var agent = new RequirementsAgent(BuildKernel(), sp.GetRequiredService<RequirementsPlugin>());
        agent.ChatAgent.Name.Should().Be("RequirementsAgent");
    }

    [Fact]
    public void CodeGenerationAgent_ChatAgent_HasCorrectName()
    {
        var sp = BuildServiceProvider();
        var agent = new CodeGenerationAgent(BuildKernel(), sp.GetRequiredService<CodeGenerationPlugin>());
        agent.ChatAgent.Name.Should().Be("CodeGenerationAgent");
    }

    [Fact]
    public void TestingAgent_ChatAgent_HasCorrectName()
    {
        var sp = BuildServiceProvider();
        var agent = new TestingAgent(BuildKernel(), sp.GetRequiredService<TestingPlugin>());
        agent.ChatAgent.Name.Should().Be("TestingAgent");
    }

    [Fact]
    public void DeploymentAgent_ChatAgent_HasCorrectName()
    {
        var sp = BuildServiceProvider();
        var agent = new DeploymentAgent(BuildKernel(), sp.GetRequiredService<DeploymentPlugin>());
        agent.ChatAgent.Name.Should().Be("DeploymentAgent");
    }

    [Fact]
    public void RequirementsAgent_ChatAgent_HasNonEmptyInstructions()
    {
        var sp = BuildServiceProvider();
        var agent = new RequirementsAgent(BuildKernel(), sp.GetRequiredService<RequirementsPlugin>());
        agent.ChatAgent.Instructions.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RequirementsAgent_ChatAgent_HasRequirementsPlugin()
    {
        var sp = BuildServiceProvider();
        var agent = new RequirementsAgent(BuildKernel(), sp.GetRequiredService<RequirementsPlugin>());
        agent.ChatAgent.Kernel.Plugins.Should().ContainSingle(p => p.Name == "RequirementsPlugin");
    }

    [Fact]
    public void CodeGenerationAgent_ChatAgent_HasCodeGenerationPlugin()
    {
        var sp = BuildServiceProvider();
        var agent = new CodeGenerationAgent(BuildKernel(), sp.GetRequiredService<CodeGenerationPlugin>());
        agent.ChatAgent.Kernel.Plugins.Should().ContainSingle(p => p.Name == "CodeGenerationPlugin");
    }

    [Fact]
    public void TestingAgent_ChatAgent_HasTestingPlugin()
    {
        var sp = BuildServiceProvider();
        var agent = new TestingAgent(BuildKernel(), sp.GetRequiredService<TestingPlugin>());
        agent.ChatAgent.Kernel.Plugins.Should().ContainSingle(p => p.Name == "TestingPlugin");
    }

    [Fact]
    public void DeploymentAgent_ChatAgent_HasDeploymentPlugin()
    {
        var sp = BuildServiceProvider();
        var agent = new DeploymentAgent(BuildKernel(), sp.GetRequiredService<DeploymentPlugin>());
        agent.ChatAgent.Kernel.Plugins.Should().ContainSingle(p => p.Name == "DeploymentPlugin");
    }

    [Fact]
    public void RequirementsAgent_OriginalKernel_IsNotModified()
    {
        var sp = BuildServiceProvider();
        var kernel = BuildKernel();
        var originalCount = kernel.Plugins.Count;

        new RequirementsAgent(kernel, sp.GetRequiredService<RequirementsPlugin>());

        kernel.Plugins.Count.Should().Be(originalCount);
    }
}
