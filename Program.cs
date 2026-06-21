using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnowflakeDriver.Models;
using SnowflakeDriver.Services;

namespace SnowflakeDriver;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>() // Load user secrets
            .AddEnvironmentVariables() // Allow override via environment variables
            .Build();

        // Setup dependency injection
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .Configure<SnowflakeConfig>(configuration.GetSection("Snowflake"))
            .AddSingleton<ISnowflakeConnectionService, SnowflakeConnectionService>()
            .AddSingleton<SnowflakeQueryService>()
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("=== Snowflake Enterprise Egress Service ===");
        logger.LogInformation("");

        try
        {
            // Test connection
            var connectionService = serviceProvider.GetRequiredService<ISnowflakeConnectionService>();
            logger.LogInformation("Testing connection to Snowflake...");

            var isConnected = await connectionService.TestConnectionAsync();

            if (!isConnected)
            {
                logger.LogError("Failed to connect to Snowflake. Please check your configuration.");
                logger.LogInformation("");
                logger.LogInformation("To set up credentials, run:");
                logger.LogInformation("  dotnet user-secrets set \"Snowflake:Account\" \"your-account\"");
                logger.LogInformation("  dotnet user-secrets set \"Snowflake:User\" \"your-username\"");
                logger.LogInformation("  dotnet user-secrets set \"Snowflake:Password\" \"your-password\"");
                return;
            }

            logger.LogInformation("");
            logger.LogInformation("=== Running Sample Queries ===");
            logger.LogInformation("");

            // Execute sample queries
            var queryService = serviceProvider.GetRequiredService<SnowflakeQueryService>();

            // Example 1: Get current timestamp
            logger.LogInformation("Example 1: Getting current timestamp...");
            var timestamp = await queryService.ExecuteScalarAsync<DateTime>("SELECT CURRENT_TIMESTAMP()");
            logger.LogInformation("Current Snowflake timestamp: {Timestamp}", timestamp);
            logger.LogInformation("");

            // Example 2: Query with results
            logger.LogInformation("Example 2: Querying available databases...");
            var databases = await queryService.ExecuteQueryAsync("SHOW DATABASES");
            logger.LogInformation("Found {Count} databases", databases.Count);
            foreach (var db in databases.Take(5))
            {
                logger.LogInformation("  - {Database}", db.GetValueOrDefault("name", "N/A"));
            }
            logger.LogInformation("");

            // Example 3: Query to DataTable
            logger.LogInformation("Example 3: Getting warehouse info...");
            var warehouses = await queryService.ExecuteQueryToDataTableAsync("SHOW WAREHOUSES");
            logger.LogInformation("Found {Count} warehouses", warehouses.Rows.Count);
            logger.LogInformation("");

            // Example 4: Export to CSV (commented out by default)
            // var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "export.csv");
            // await queryService.ExportToCsvAsync("SELECT * FROM YOUR_TABLE LIMIT 100", outputPath);
            // logger.LogInformation("Data exported to: {Path}", outputPath);

            logger.LogInformation("=== All operations completed successfully ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during execution");
        }
    }
}
