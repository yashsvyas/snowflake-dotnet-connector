namespace SnowflakeDriver.Models;

public class SnowflakeConfig
{
    public string Account { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.UsernamePassword;

    // OAuth specific
    public string? OAuthToken { get; set; }

    // Key Pair Authentication specific
    public string? PrivateKeyPath { get; set; }
    public string? PrivateKeyPassword { get; set; }
}

public enum AuthenticationType
{
    UsernamePassword,
    OAuth,
    KeyPair,
    ExternalBrowser
}
