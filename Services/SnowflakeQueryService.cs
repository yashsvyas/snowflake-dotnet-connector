using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;
using System.Data;

namespace SnowflakeDriver.Services;

public class SnowflakeQueryService
{
    private readonly ISnowflakeConnectionService _connectionService;
    private readonly ILogger<SnowflakeQueryService> _logger;

    public SnowflakeQueryService(
        ISnowflakeConnectionService connectionService,
        ILogger<SnowflakeQueryService> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
    {
        var results = new List<Dictionary<string, object>>();

        try
        {
            _logger.LogInformation("Executing query: {Query}", query);

            using var conn = _connectionService.GetConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value ?? DBNull.Value;
                }
                results.Add(row);
            }

            _logger.LogInformation("Query executed successfully. Rows returned: {RowCount}", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", query);
            throw;
        }

        return results;
    }

    public async Task<DataTable> ExecuteQueryToDataTableAsync(string query)
    {
        var dataTable = new DataTable();

        try
        {
            _logger.LogInformation("Executing query to DataTable: {Query}", query);

            using var conn = _connectionService.GetConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync();
            dataTable.Load(reader);

            _logger.LogInformation("Query executed successfully. Rows returned: {RowCount}", dataTable.Rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query to DataTable: {Query}", query);
            throw;
        }

        return dataTable;
    }

    public async Task<int> ExecuteNonQueryAsync(string query)
    {
        try
        {
            _logger.LogInformation("Executing non-query: {Query}", query);

            using var conn = _connectionService.GetConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Non-query executed successfully. Rows affected: {RowsAffected}", rowsAffected);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing non-query: {Query}", query);
            throw;
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(string query)
    {
        try
        {
            _logger.LogInformation("Executing scalar query: {Query}", query);

            using var conn = _connectionService.GetConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            var result = await cmd.ExecuteScalarAsync();

            _logger.LogInformation("Scalar query executed successfully. Result: {Result}", result);

            if (result == null || result == DBNull.Value)
                return default;

            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scalar query: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Export query results to CSV format
    /// </summary>
    public async Task ExportToCsvAsync(string query, string outputPath)
    {
        try
        {
            _logger.LogInformation("Exporting query results to CSV: {OutputPath}", outputPath);

            using var conn = _connectionService.GetConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync();
            using var writer = new StreamWriter(outputPath);

            // Write header
            var headers = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i));
            await writer.WriteLineAsync(string.Join(",", headers));

            // Write rows
            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                var values = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.IsDBNull(i) ? "" : EscapeCsvValue(reader.GetValue(i).ToString() ?? ""));
                await writer.WriteLineAsync(string.Join(",", values));
                rowCount++;
            }

            _logger.LogInformation("Successfully exported {RowCount} rows to {OutputPath}", rowCount, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to CSV: {OutputPath}", outputPath);
            throw;
        }
    }

    private static string EscapeCsvValue(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
