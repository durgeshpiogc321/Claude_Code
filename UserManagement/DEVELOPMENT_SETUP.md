# Development Setup Guide

## Prerequisites

- .NET 8 SDK (or later)
- SQL Server (LocalDB, Express, or full version)
- Visual Studio 2022 / VS Code / Rider (optional)

## Database Configuration

### Option 1: Windows Authentication (Recommended for Development)

The default `appsettings.json` is configured to use Windows Authentication with SQL Server LocalDB:

```json
"DefaultConnection": "Server=(local);Database=db_MigratedLginMVC_13_1;Integrated Security=true;..."
```

No additional configuration needed. Run the migrations and you're ready to go.

### Option 2: SQL Authentication with User Secrets

For SQL Server authentication, use User Secrets to store credentials securely (never commit credentials to source control):

1. **Initialize User Secrets** (if not already done):
   ```bash
   cd src/LoginandRegisterMVC
   dotnet user-secrets init
   ```

2. **Set your connection string**:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=db_MigratedLginMVC_13_1;user id=YOUR_USER;password=YOUR_PASSWORD;TrustServerCertificate=true;MultipleActiveResultSets=true;Connect Timeout=120;Pooling=true;Max Pool Size=200"
   ```

3. **Example for local SQL Server**:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=db_MigratedLginMVC_13_1;user id=sa;password=YourPassword123;TrustServerCertificate=true;MultipleActiveResultSets=true;Connect Timeout=120;Pooling=true;Max Pool Size=200"
   ```

4. **Verify your secrets**:
   ```bash
   dotnet user-secrets list
   ```

### Database Migrations

Apply database migrations to create the schema:

```bash
cd src/LoginandRegisterMVC
dotnet ef database update
```

This will create the database and apply all migrations including:
- User table schema
- Password migration fields (PasswordV2, PasswordMigrated)

## Running the Application

```bash
cd src/LoginandRegisterMVC
dotnet run
```

The application will start on:
- HTTPS: https://localhost:62406
- HTTP: http://localhost:62407

## Default Admin Account

An admin user is automatically created on first login page access:
- **Email**: admin@demo.com
- **Password**: Admin@123
- **Role**: Admin

## Running Tests

```bash
# Run all tests
cd tests
dotnet test

# Run unit tests only
cd tests/LoginandRegisterMVC.UnitTests
dotnet test

# Run integration tests only
cd tests/LoginandRegisterMVC.IntegrationTests
dotnet test
```

## Security Notes

### User Secrets Location

User Secrets are stored outside your project directory:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

The `user_secrets_id` is stored in `LoginandRegisterMVC.csproj` as `<UserSecretsId>`.

### Production Deployment

User Secrets are **only for development**. For production:

1. **Azure App Service**: Use Application Settings (automatically overrides `appsettings.json`)
2. **Azure Key Vault**: Use Azure.Extensions.AspNetCore.Configuration.Secrets
3. **Docker/Kubernetes**: Use environment variables or Kubernetes Secrets
4. **Environment Variables**: Set `ConnectionStrings__DefaultConnection` environment variable

Example production environment variable:
```bash
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=db_MigratedLginMVC_13_1;user id=prod_user;password=prod_password;..."
```

## Troubleshooting

### Cannot connect to database

1. **Check SQL Server is running**:
   ```bash
   # Windows - Check LocalDB
   sqllocaldb info
   sqllocaldb start MSSQLLocalDB
   ```

2. **Verify connection string**:
   ```bash
   dotnet user-secrets list
   ```

3. **Test connection with sqlcmd**:
   ```bash
   sqlcmd -S localhost -U sa -P YourPassword123 -Q "SELECT @@VERSION"
   ```

### User Secrets not loading

1. Ensure you're in the correct directory (`src/LoginandRegisterMVC`)
2. Check `LoginandRegisterMVC.csproj` contains `<UserSecretsId>` element
3. Verify `ASPNETCORE_ENVIRONMENT` is set to `Development`

### Migration errors

1. **Drop and recreate database** (WARNING: data loss):
   ```bash
   dotnet ef database drop
   dotnet ef database update
   ```

2. **Reset migrations** (if corrupted):
   - Delete all files in `Migrations/` folder
   - Drop database
   - Run `dotnet ef migrations add InitialCreate`
   - Run `dotnet ef database update`

## Additional Resources

- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [SQL Server Connection Strings](https://www.connectionstrings.com/sql-server/)
