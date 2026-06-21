using Snowflake.Data.Client;

namespace SnowflakeDriver.Services;

public interface ISnowflakeConnectionService
{
    SnowflakeDbConnection GetConnection();
    Task<bool> TestConnectionAsync();
}
