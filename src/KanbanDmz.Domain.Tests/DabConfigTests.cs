using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class DabConfigTests
{
    [Fact]
    public void DabConfig_ShouldHaveDataSourceHealthDisabled()
    {
        // Arrange
        // Find dab-config.json in the solution
        var currentDir = Directory.GetCurrentDirectory();
        // Traverse up to find the root folder containing the dab folder
        var rootDir = FindSolutionRoot(currentDir);
        var configPath = Path.Combine(rootDir, "dab", "dab-config.json");

        Assert.True(File.Exists(configPath), $"dab-config.json not found at {configPath}");

        // Act
        var json = File.ReadAllText(configPath);
        using var doc = JsonDocument.Parse(json);
        
        var dataSource = doc.RootElement.GetProperty("data-source");
        
        // Assert
        Assert.True(dataSource.TryGetProperty("health", out var healthProp), "data-source section is missing the 'health' property");
        Assert.True(healthProp.TryGetProperty("enabled", out var enabledProp), "health section is missing the 'enabled' property");
        Assert.False(enabledProp.GetBoolean(), "health check under data-source should be disabled to prevent Npgsql/SqlClient startup conflicts");
    }

    private string FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (dir.GetDirectories("dab").Any())
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("DAB configuration root directory not found.");
    }
}
