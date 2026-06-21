# Snowflake Enterprise Egress Service

A C# .NET 8.0 console application for enterprise data egress from Snowflake, built with Snowflake.Data connector. Supports multiple authentication methods including Username/Password, OAuth, Key Pair, and External Browser authentication.

## Features

- **Multiple Authentication Methods**: Username/Password, OAuth, Key Pair Authentication, External Browser
- **Secure Credential Management**: Uses .NET User Secrets for local development (no credentials in code)
- **Data Egress Capabilities**:
  - Execute queries and retrieve results
  - Export data to CSV
  - Execute scalar queries
  - Execute non-queries (DDL/DML)
- **Logging**: Built-in structured logging with Microsoft.Extensions.Logging
- **Dependency Injection**: Follows modern .NET patterns with DI container

## Project Structure

```
SnowflakeDriver/
├── Models/
│   └── SnowflakeConfig.cs          # Configuration model with auth types
├── Services/
│   ├── ISnowflakeConnectionService.cs
│   ├── SnowflakeConnectionService.cs  # Manages Snowflake connections
│   └── SnowflakeQueryService.cs       # Query execution and data egress
├── Program.cs                      # Entry point with examples
├── appsettings.json               # Configuration (no secrets)
└── .gitignore                     # Protects sensitive files
```

## Setup Instructions

### 1. Prerequisites

- .NET 8.0 SDK
- Snowflake account (trial or enterprise)
- macOS, Linux, or Windows

### 2. Initial Configuration

Edit `appsettings.json` to add your Snowflake connection parameters (non-sensitive):

```json
{
  "Snowflake": {
    "Account": "your-account",
    "User": "your-username",
    "Database": "your-database",
    "Schema": "PUBLIC",
    "Warehouse": "COMPUTE_WH",
    "Role": "ACCOUNTADMIN",
    "AuthenticationType": "UsernamePassword"
  }
}
```

### 3. Secure Credential Setup

#### For Trial Account (Username/Password)

Use User Secrets to store your password securely:

```bash
cd /Users/yashvyas/Documents/SnowflakeDriver/SnowflakeDriver

# Set your password (NEVER commit this)
dotnet user-secrets set "Snowflake:Password" "your-password"

# Verify secrets are set
dotnet user-secrets list
```

#### For Corporate (OAuth)

```bash
dotnet user-secrets set "Snowflake:AuthenticationType" "OAuth"
dotnet user-secrets set "Snowflake:OAuthToken" "your-oauth-token"
```

#### For Key Pair Authentication

```bash
dotnet user-secrets set "Snowflake:AuthenticationType" "KeyPair"
dotnet user-secrets set "Snowflake:PrivateKeyPath" "/path/to/private-key.pem"
dotnet user-secrets set "Snowflake:PrivateKeyPassword" "key-password"  # Optional
```

#### For External Browser Authentication

```bash
dotnet user-secrets set "Snowflake:AuthenticationType" "ExternalBrowser"
```

### 4. Build and Run

```bash
# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

## Authentication Types

### 1. Username/Password (Default - For Trial Accounts)

**Configuration:**
```json
{
  "Snowflake": {
    "AuthenticationType": "UsernamePassword"
  }
}
```

**User Secrets:**
```bash
dotnet user-secrets set "Snowflake:Password" "your-password"
```

**Use Case**: Development, trial accounts, local testing

---

### 2. OAuth (For Corporate with ODBC Driver)

**Configuration:**
```json
{
  "Snowflake": {
    "AuthenticationType": "OAuth"
  }
}
```

**User Secrets:**
```bash
dotnet user-secrets set "Snowflake:OAuthToken" "your-oauth-token"
```

**Use Case**: Enterprise SSO, corporate environments

---

### 3. Key Pair Authentication

**Configuration:**
```json
{
  "Snowflake": {
    "AuthenticationType": "KeyPair"
  }
}
```

**User Secrets:**
```bash
dotnet user-secrets set "Snowflake:PrivateKeyPath" "/path/to/rsa-key.pem"
dotnet user-secrets set "Snowflake:PrivateKeyPassword" "optional-key-password"
```

**Setup:**
1. Generate RSA key pair:
   ```bash
   openssl genrsa 2048 | openssl pkcs8 -topk8 -inform PEM -out rsa_key.p8 -nocrypt
   openssl rsa -in rsa_key.p8 -pubout -out rsa_key.pub
   ```

2. Add public key to Snowflake user:
   ```sql
   ALTER USER your_username SET RSA_PUBLIC_KEY='MIIBIjANBg...';
   ```

**Use Case**: Service accounts, automated processes

---

### 4. External Browser Authentication

**Configuration:**
```json
{
  "Snowflake": {
    "AuthenticationType": "ExternalBrowser"
  }
}
```

**Use Case**: Interactive login with browser-based SSO

## Usage Examples

### Basic Query Execution

```csharp
var queryService = serviceProvider.GetRequiredService<SnowflakeQueryService>();

// Execute query and get results
var results = await queryService.ExecuteQueryAsync("SELECT * FROM MY_TABLE LIMIT 10");

// Execute scalar query
var count = await queryService.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MY_TABLE");

// Execute non-query (DDL/DML)
var rowsAffected = await queryService.ExecuteNonQueryAsync("DELETE FROM MY_TABLE WHERE ID < 100");
```

### Export to CSV

```csharp
var outputPath = "/path/to/output.csv";
await queryService.ExportToCsvAsync("SELECT * FROM MY_TABLE", outputPath);
```

### Connection Testing

```csharp
var connectionService = serviceProvider.GetRequiredService<ISnowflakeConnectionService>();
var isConnected = await connectionService.TestConnectionAsync();
```

## Security Best Practices

✅ **DO:**
- Use User Secrets for local development
- Use environment variables for production
- Use OAuth or Key Pair authentication in corporate environments
- Keep `appsettings.json` in source control (without secrets)
- Add `.gitignore` to protect sensitive files

❌ **DON'T:**
- Commit passwords or tokens to source control
- Hard-code credentials in code
- Share your user secrets file
- Use password authentication in production

## Transitioning to Corporate ODBC OAuth

When you're ready to test with corporate ODBC OAuth:

1. Install Snowflake ODBC driver on your machine
2. Update `AuthenticationType` to `OAuth`
3. Configure OAuth token acquisition (via your corporate SSO)
4. Test connection with corporate credentials

The service is designed to work with both Snowflake.Data and ODBC patterns, making the transition seamless.

## Troubleshooting

### Connection Failed

1. Verify your account identifier is correct:
   ```
   account = <account_identifier>  (e.g., "abc12345.us-east-1")
   ```

2. Check user secrets are set:
   ```bash
   dotnet user-secrets list
   ```

3. Verify Snowflake user has necessary privileges

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Dependencies

- **Snowflake.Data** (5.7.0): Official Snowflake .NET connector
- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.Configuration.Json**: JSON config provider
- **Microsoft.Extensions.Configuration.UserSecrets**: User secrets support
- **Microsoft.Extensions.Logging.Console**: Console logging

## License

Internal use only - Snowflake Enterprise Egress Service

## Next Steps

1. Configure your Snowflake credentials using user secrets
2. Run the application to test connectivity
3. Customize queries in `Program.cs` for your use case
4. Implement additional egress patterns (JSON export, Parquet, etc.)
5. Add error handling and retry logic for production
