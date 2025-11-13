# .NET Migration Project

This project represents the successful migration of a legacy .NET Framework MVC application to .NET 8.

## Original Project
- **Framework**: .NET Framework 4.7.2
- **MVC Version**: ASP.NET MVC 5
- **Entity Framework**: 6.4.4
- **Authentication**: Forms Authentication

## Migrated Project  
- **Framework**: .NET 8
- **MVC Version**: ASP.NET Core MVC 8
- **Entity Framework**: EF Core 8.0.10
- **Authentication**: Cookie Authentication

## Architecture
- **Single Layer Architecture**: Controllers, Views, Models, Services, Data
- **Database**: SQL Server with EF Core migrations
- **UI Framework**: Bootstrap 4.5.2 (preserved exactly)
- **Testing**: Unit Tests and Integration Tests with NUnit

## Features Preserved
- ✅ User Registration and Login
- ✅ Role-based Authorization (Admin/User)
- ✅ Password Hashing (SHA1 - preserved for compatibility)
- ✅ Session Management
- ✅ Form Validation
- ✅ Responsive UI with Bootstrap 4.5.2
- ✅ Database Migrations History

## Migration Highlights
- **100% UI Fidelity**: All styling and layouts preserved exactly
- **Database Compatibility**: All migrations converted and history preserved
- **Modern Patterns**: Dependency injection, async/await, nullable reference types
- **SSL/TLS Configuration**: Automatic certificate trust handling
- **Testing Coverage**: Comprehensive unit and integration tests

## How to Run
1. Update connection string in `appsettings.json`
2. Run `dotnet ef database update`
3. Run `dotnet run`
4. Navigate to `/Users/Login`

Default admin credentials:
- Email: admin@demo.com  
- Password: Admin@123
