using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lab6.Tests;

public class DockerApiFixture : IAsyncLifetime
{
    private INetwork _network = null!;
    private PostgreSqlContainer _dbContainer = null!;
    private IContainer _apiContainer = null!;
    private IFutureDockerImage _apiImage = null!;

    public string BaseUrl { get; private set; } = string.Empty;
    public INetwork Network => _network;

    public async ValueTask InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName($"lab6-perf-network-{Guid.NewGuid():N}")
            .Build();
        await _network.CreateAsync();

        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("lab6db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithNetwork(_network)
            .WithNetworkAliases("db")
            .Build();
        await _dbContainer.StartAsync();

        var apiDbConnectionString = "Host=db;Port=5432;Database=lab6db;Username=postgres;Password=postgres";

        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        
        _apiImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(rootDir)
            .WithDockerfile("Api/Dockerfile")
            .WithName("lab6-api-perf:latest")
            .WithCleanUp(true)
            .Build();
        await _apiImage.CreateAsync();

        _apiContainer = new ContainerBuilder()
            .WithImage(_apiImage.FullName)
            .WithNetwork(_network)
            .WithNetworkAliases("api")
            .WithPortBinding(8080, true)
            .DependsOn(_dbContainer)
            .WithEnvironment("ConnectionStrings__DefaultConnection", apiDbConnectionString)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(8080))

            .Build();
        await _apiContainer.StartAsync();


        var host = _apiContainer.Hostname;
        var port = _apiContainer.GetMappedPublicPort(8080);
        BaseUrl = $"http://{host}:{port}";
    }

    public async ValueTask DisposeAsync()
    {
        if (_apiContainer != null)
            await _apiContainer.DisposeAsync();

        if (_dbContainer != null)
            await _dbContainer.DisposeAsync();

        if (_network != null)
            await _network.DeleteAsync();
    }
}
