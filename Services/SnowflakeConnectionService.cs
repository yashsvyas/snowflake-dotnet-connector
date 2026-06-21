using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snowflake.Data.Client;
using SnowflakeDriver.Models;

namespace SnowflakeDriver.Services;

public class SnowflakeConnectionService : ISnowflakeConnectionService
{
    private readonly SnowflakeConfig _config;
    private readonly ILogger<SnowflakeConnectionService> _logger;

    public SnowflakeConnectionService(
        IOptions<SnowflakeConfig> config,
        ILogger<SnowflakeConnectionService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public SnowflakeDbConnection GetConnection()
    {
        var connectionString = BuildConnectionString();
        _logger.LogInformation("Creating Snowflake connection for account: {Account}", _config.Account);
        return new SnowflakeDbConnection { ConnectionString = connectionString };
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing Snowflake connection...");
            using var conn = GetConnection();
            await conn.OpenAsync();

            _logger.LogInformation("Connection successful!");

            // Test a simple query
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CURRENT_VERSION(), CURRENT_USER(), CURRENT_ACCOUNT()";
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                _logger.LogInformation("Snowflake Version: {Version}", reader.GetString(0));
                _logger.LogInformation("Current User: {User}", reader.GetString(1));
                _logger.LogInformation("Current Account: {Account}", reader.GetString(2));
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Snowflake");
            return false;
        }
    }

    private string BuildConnectionString()
    {
        var builder = new System.Text.StringBuilder();

        builder.Append($"account={_config.Account};");
        builder.Append($"user={_config.User};");

        switch (_config.AuthenticationType)
        {
            case AuthenticationType.UsernamePassword:
                if (string.IsNullOrEmpty(_config.Password))
                {
                    throw new InvalidOperationException(
                        "Password is required for UsernamePassword authentication. " +
                        "Please set it using user secrets: " +
                        "dotnet user-secrets set \"Snowflake:Password\" \"your-password\"");
                }
                builder.Append($"password={_config.Password};");
                break;

            case AuthenticationType.OAuth:
                if (string.IsNullOrEmpty(_config.OAuthToken))
                {
                    throw new InvalidOperationException("OAuth token is required for OAuth authentication");
                }
                builder.Append($"authenticator=oauth;token={_config.OAuthToken};");
                break;

            case AuthenticationType.KeyPair:
                if (string.IsNullOrEmpty(_config.PrivateKeyPath))
                {
                    throw new InvalidOperationException("Private key path is required for KeyPair authentication");
                }
                builder.Append($"authenticator=snowflake_jwt;");
                builder.Append($"private_key_file={_config.PrivateKeyPath};");
                if (!string.IsNullOrEmpty(_config.PrivateKeyPassword))
                {
                    builder.Append($"private_key_pwd={_config.PrivateKeyPassword};");
                }
                break;

            case AuthenticationType.ExternalBrowser:
                builder.Append("authenticator=externalbrowser;");
                break;

            default:
                throw new InvalidOperationException($"Unsupported authentication type: {_config.AuthenticationType}");
        }

        if (!string.IsNullOrEmpty(_config.Database))
            builder.Append($"db={_config.Database};");

        if (!string.IsNullOrEmpty(_config.Schema))
            builder.Append($"schema={_config.Schema};");

        if (!string.IsNullOrEmpty(_config.Warehouse))
            builder.Append($"warehouse={_config.Warehouse};");

        if (!string.IsNullOrEmpty(_config.Role))
            builder.Append($"role={_config.Role};");

        return builder.ToString();
    }
}
