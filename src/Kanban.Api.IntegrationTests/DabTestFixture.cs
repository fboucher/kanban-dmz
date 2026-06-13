using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;
using Xunit;

public class DabTestFixture : IAsyncLifetime
{
    public INetwork Network { get; private set; } = null!;
    public PostgreSqlContainer PostgresContainer { get; private set; } = null!;
    public IContainer DabContainer { get; private set; } = null!;
    public string DabBaseUrl { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepositoryRoot();

        // 1. Create a network
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        // 2. Start Postgres container
        PostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithNetwork(Network)
            .WithNetworkAliases("db")
            .WithDatabase("kanban")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await PostgresContainer.StartAsync();

        // 3. Initialize the database schema
        var initSqlPath = Path.Combine(repoRoot, "db", "init.sql");
        var initSql = await File.ReadAllTextAsync(initSqlPath);
        var execResult = await PostgresContainer.ExecScriptAsync(initSql);
        if (execResult.ExitCode != 0)
        {
            throw new Exception($"Failed to initialize Postgres schema: {execResult.Stderr}");
        }

        // 4. Start DAB container
        var configPath = Path.Combine(repoRoot, "dab", "dab-config.json");
        var configContent = await File.ReadAllTextAsync(configPath);
        var connectionString = "Host=db;Database=kanban;Username=postgres;Password=postgres;Port=5432";

        DabContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-databases/data-api-builder:latest")
            .WithNetwork(Network)
            .WithNetworkAliases("dab")
            .WithEnvironment("ConnectionStrings__kanban", connectionString)
            .WithResourceMapping(Encoding.UTF8.GetBytes(configContent), "/App/dab-config.json")
            .WithPortBinding(5000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5000))
            .Build();

        await DabContainer.StartAsync();

        var hostPort = DabContainer.GetMappedPublicPort(5000);
        DabBaseUrl = $"http://{DabContainer.Hostname}:{hostPort}/api/";
    }

    public async Task DisposeAsync()
    {
        if (DabContainer != null) await DabContainer.DisposeAsync();
        if (PostgresContainer != null) await PostgresContainer.DisposeAsync();
        if (Network != null) await Network.DisposeAsync();
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "CONTEXT.md")) || Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current)!;
        }
        throw new Exception("Repository root not found.");
    }
}
