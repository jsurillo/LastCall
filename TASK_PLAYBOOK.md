# TASK_PLAYBOOK.md

# BrosCode LastCall — Task Playbook

This document defines the **canonical commands and procedures** that agents and developers
must follow when working in this repository.

If this document conflicts with AGENT_RULES.md or ARCHITECTURE.md, defer to those documents.

---

## 0. Prerequisites

- .NET SDK 10.x
- EF Core CLI tools

Install EF tools:
```
dotnet tool install --global dotnet-ef
```

Update if already installed:
```
dotnet tool update --global dotnet-ef
```

PostgreSQL:
- A PostgreSQL instance must be available (local Docker, local install, or remote)

---

## 1. Solution and project creation

From repo root:

```
mkdir src
cd src

dotnet new sln -n BrosCode.LastCall

dotnet new webapi   -n BrosCode.LastCall.Api
dotnet new classlib -n BrosCode.LastCall.Business
dotnet new classlib -n BrosCode.LastCall.Contracts
dotnet new classlib -n BrosCode.LastCall.Entity
dotnet new classlib -n BrosCode.LastCall.Infrastructure

dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj
dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj
dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Contracts/BrosCode.LastCall.Contracts.csproj
dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj
dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Infrastructure/BrosCode.LastCall.Infrastructure.csproj
```

---

## 2. Project references (mandatory)

From `src/` directory:

### Api
```
dotnet add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj reference BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj
dotnet add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj reference BrosCode.LastCall.Contracts/BrosCode.LastCall.Contracts.csproj
dotnet add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj reference BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj
```

### Business
```
dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj reference BrosCode.LastCall.Contracts/BrosCode.LastCall.Contracts.csproj
dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj reference BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj
dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj reference BrosCode.LastCall.Infrastructure/BrosCode.LastCall.Infrastructure.csproj
```

### Entity
```
dotnet add BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj reference BrosCode.LastCall.Infrastructure/BrosCode.LastCall.Infrastructure.csproj
```

### Contracts
- No project references allowed

### Infrastructure
- No project references allowed

---

## 3. NuGet package management (Central Package Management)

This repository uses **Central Package Management** via `Directory.Packages.props`.

### Rules
- NEVER add package versions to `.csproj`
- NEVER use `dotnet add package --version`
- ALL package versions live in `Directory.Packages.props`

### Correct process to add a package

1. Add an unversioned PackageReference to the appropriate `.csproj`:
```xml
<PackageReference Include="Package.Name" />
```

2. Add or update the version in `Directory.Packages.props`:
```xml
<PackageVersion Include="Package.Name" Version="X.Y.Z" />
```

3. Validate:
```
dotnet restore
dotnet build src/BrosCode.LastCall.sln
```

---

## 4. Required baseline packages

The following packages must be referenced (unversioned) in the appropriate projects:

### Api
- Microsoft.AspNetCore.OpenApi
- Scalar.AspNetCore

### Business
- AutoMapper
- AutoMapper.Extensions.Microsoft.DependencyInjection
- FluentValidation
- FluentValidation.DependencyInjectionExtensions

### Entity
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Relational
- Microsoft.EntityFrameworkCore.Design
- Npgsql.EntityFrameworkCore.PostgreSQL

Versions are defined **only** in `Directory.Packages.props`.

---

## 5. EF Core registration pattern

The Entity project MUST expose an IServiceCollection extension method, for example:
- `AddLastCallEntity()`

This method must:
- Register DbContext with `UseNpgsql`
- Register Repository and UnitOfWork

The Api `Program.cs` MUST call this extension method.
The Api MUST NOT call `UseNpgsql` directly.

---

## 6. OpenAPI + Scalar wiring

- OpenAPI is enabled via ASP.NET Core OpenAPI services
- Scalar is used as the API Reference UI
- Swagger / Swashbuckle MUST NOT be added

Controllers are automatically included in OpenAPI output.

---

## 7. Adding a new entity (canonical process)

When instructed to add a new entity:

1. Create Entity class inheriting `BaseEntity`
2. Add DbSet to DbContext
3. Ensure RowVersion is configured (generic preferred)
4. Create DTO inheriting `BaseDto` in Contracts
5. Add AutoMapper mapping in Business
6. Add FluentValidation validator (if applicable)
7. Add Business service method
8. Add Controller endpoint
9. Create migration
10. Run database update
11. Run `dotnet build`

---

## 8. EF Core migrations

Add a migration:
```
dotnet ef migrations add <MigrationName>   -p src/BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj   -s src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj   -o Migrations
```

Apply migrations:
```
dotnet ef database update   -p src/BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj   -s src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj
```

---

## 9. Build and run

```
dotnet build src/BrosCode.LastCall.sln
dotnet run --project src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj
```

---

## 10. Enforcement

Any deviation from this playbook is a defect.
If uncertain, STOP and ask for clarification.
