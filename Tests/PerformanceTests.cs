using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Xunit;

namespace Lab6.Tests;

public class PerformanceTests : IClassFixture<DockerApiFixture>
{
    private readonly DockerApiFixture _fixture;

    public PerformanceTests(DockerApiFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<string> RunK6Test(string scriptName)
    {
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var scriptPath = Path.Combine(rootDir, "k6", scriptName);
        
        var k6Container = new ContainerBuilder()
            .WithImage("grafana/k6:latest")
            .WithNetwork(_fixture.Network)
            .WithBindMount(scriptPath, $"/scripts/{scriptName}", AccessMode.ReadOnly)
            .WithEnvironment("BASE_URL", "http://api:8080")
            .WithCommand("run", $"/scripts/{scriptName}")
            // Wait strategy with explicit timeout to prevent infinite hangs
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged(".*http_req_duration.*", o => o.WithTimeout(TimeSpan.FromMinutes(3))))
            .Build();


        try 
        {
            await k6Container.StartAsync();
            var logs = await k6Container.GetLogsAsync();
            
            Console.WriteLine($"k6 STDOUT: {logs.Stdout}");
            Console.WriteLine($"k6 STDERR: {logs.Stderr}");

            return logs.Stdout + logs.Stderr;
        }
        finally
        {
            await k6Container.DisposeAsync();
        }
    }

    [Fact]
    public async Task SmokeTest()
    {
        var output = await RunK6Test("smoke.js");
        Assert.Contains("http_req_duration", output);
    }

    [Fact]
    public async Task LoadTest()
    {
        var output = await RunK6Test("load.js");
        Assert.Contains("http_req_duration", output);
    }

    [Fact]
    public async Task StressTest()
    {
        // Stress test takes longer, so we increase timeout if needed in RunK6Test
        var output = await RunK6Test("stress.js");
        Assert.Contains("http_req_duration", output);
    }
}
