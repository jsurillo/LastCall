# TASK_PLAYBOOK.md

# BrosCode LastCall – Task Playbook
(Commands the Agent Must Run)

This file defines the canonical commands the agent must use when scaffolding,
modifying structure, adding entities, or running migrations.
The agent must not invent alternative commands unless explicitly instructed.

This playbook assumes PostgreSQL.
DI and provider configuration must be registered in the Entity project via an IServiceCollection extension method,
and called from Api Program.cs.

---

## 0) Prerequisites

Install .NET SDK 10.x

Install EF Core CLI tools:

dotnet tool install --global dotnet-ef

If already installed:

dotnet tool update --global dotnet-ef

PostgreSQL requirement:
- Have a PostgreSQL server available (local Docker, local install, or remote).

---

## 1) Create solution and projects (from repo root)

mkdir src
cd src

dotnet new sln -n BrosCode.LastCall

dotnet new webapi -n BrosCode.LastCall.Api
dotnet new classlib -n BrosCode.LastCall.Business
dotnet new classlib -n BrosCode.LastCall.Entity
dotnet new classlib -n BrosCode.LastCall.Infrastructure

dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj
dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj
dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj
dotnet sln BrosCode.LastCall.sln add BrosCode.LastCall.Infrastructure/BrosCode.LastCall.Infrastructure.csproj

---

## 2) Add project references (enforce architecture)

From src directory:

dotnet add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj reference BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj
dotnet add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj reference BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj

dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj reference BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj
dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj reference BrosCode.LastCall.Infrastructure/BrosCode.LastCall.Infrastructure.csproj

dotnet add BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj reference BrosCode.LastCall.Infrastructure/BrosCode.LastCall.Infrastructure.csproj

---

## 3) Add NuGet packages (baseline)

Business layer:

dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj package AutoMapper
dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj package FluentValidation
dotnet add BrosCode.LastCall.Business/BrosCode.LastCall.Business.csproj package FluentValidation.DependencyInjectionExtensions

Entity layer (EF Core + PostgreSQL provider):

dotnet add BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj package Microsoft.EntityFrameworkCore
dotnet add BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj package Microsoft.EntityFrameworkCore.Design

API layer:

dotnet add BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj package Swashbuckle.AspNetCore

---

## 4) Local PostgreSQL using Docker (optional but recommended)

Create a disposable local database:

docker run --name lastcall-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -e POSTGRES_DB=lastcall -p 5432:5432 -d postgres:16

Example connection string:

Host=localhost;Port=5432;Database=lastcall;Username=postgres;Password=postgres

Stop and remove:

docker stop lastcall-postgres
docker rm lastcall-postgres

---

## 5) EF registration (required pattern)

The Entity project must expose an IServiceCollection extension method, for example:
- AddEntityLayer or AddLastCallEntity

That extension method must:
- register DbContext using UseNpgsql
- register UnitOfWork and repositories

The Api Program.cs must call that extension method (Api should not call UseNpgsql directly).

---

## 6) Standard procedure: Add a new Entity

When the user says “Add entity X”, the agent must follow AGENT_RULES.md, including:
- Entity inherits BaseEntity
- Entity-backed DTO inherits BaseDto
- Add AutoMapper mapping
- Add validator
- Use Business service + UnitOfWork for persistence
- Ensure RowVersion IsRowVersion() is applied for that entity (generic preferred)

Implementation locations:

1. Entity class: src/BrosCode.LastCall.Entity/Entity
2. DbContext registration/config: src/BrosCode.LastCall.Entity/Context
3. DTO: src/BrosCode.LastCall.Business/Dtos
4. AutoMapper profile/mapping: src/BrosCode.LastCall.Business/Mapping
5. Validator: src/BrosCode.LastCall.Business/Validators
6. Service: src/BrosCode.LastCall.Business/Services
7. Controller: src/BrosCode.LastCall.Api/Controllers

Create and apply migration:

dotnet ef migrations add Add<EntityName> -p src/BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj -s src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj -o Migrations

dotnet ef database update -p src/BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj -s src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj

---

## 7) Run migrations (apply latest)

dotnet ef database update -p src/BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj -s src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj

---

## 8) Build and run

dotnet build src/BrosCode.LastCall.sln

dotnet run --project src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj

---

## 9) Git workflow (recommended)

git checkout -b feature/<short-task-name>
git add .
git commit -m "<meaningful message>"
git push -u origin feature/<short-task-name>
