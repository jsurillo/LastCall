# ARCHITECTURE.md

# BrosCode LastCall — Architecture & Dependency Rules

This document defines the architectural layout, responsibilities, and dependency rules
for the BrosCode LastCall solution. It must be followed exactly.

If this document conflicts with other guidance, defer to AGENT_RULES.md.

---

## 1. Solution overview

The solution uses a **5-project layered architecture** with strict dependency rules
and a **contracts-first boundary**.

Projects:

- BrosCode.LastCall.Api
- BrosCode.LastCall.Business
- BrosCode.LastCall.Contracts
- BrosCode.LastCall.Entity
- BrosCode.LastCall.Infrastructure

The Api project is the **composition root**.
Business owns orchestration and mapping.
Contracts define all DTO boundaries.
Entity owns persistence.
Infrastructure contains reusable, dependency-free utilities.

---

## 2. Project responsibilities

### BrosCode.LastCall.Api

Purpose:
- Host the application
- Expose HTTP endpoints
- Configure middleware and DI
- Expose OpenAPI + Scalar API Reference UI

Contains:
- Program.cs
- Controllers
- Middleware
- API-level configuration
- Seed JSON files (if used)

Rules:
- No business logic
- No persistence logic
- Controllers accept/return DTOs only
- No Swagger or Swashbuckle (Scalar only)

---

### BrosCode.LastCall.Business

Purpose:
- Business logic
- Application orchestration
- Validation
- Mapping between Entity and Contracts

Contains:
- Services
- Validators
- AutoMapper profiles
- Business rules

Rules:
- Business speaks in **Contracts DTOs**
- Mapping (Entity ↔ DTO) occurs ONLY here
- Business may reference Entity and Contracts
- Business may reference Infrastructure

---

### BrosCode.LastCall.Contracts

Purpose:
- Define the application contract surface (DTOs)

Contains:
- Request/response DTOs
- BaseDto
- Contract enums and lightweight value objects

Rules:
- No business logic
- No persistence logic
- No references to other projects
- Contracts are shared by Api and Business
- Entity MUST NOT reference Contracts

---

### BrosCode.LastCall.Entity

Purpose:
- Persistence boundary

Contains:
- DbContext
- EF Core model configuration
- Entities
- Repository pattern
- Unit of Work
- Migrations

Rules:
- All persisted entities inherit BaseEntity
- Default database schema is `app`
- Table names are plural
- RowVersion concurrency is enforced
- Audit fields are set in DbContext
- Entity may reference Infrastructure
- Entity MUST NOT reference Contracts or Business

PostgreSQL notes:
- RowVersion uses Postgres system column `xmin`
- Seeing `xmin` in migrations is expected

---

### BrosCode.LastCall.Infrastructure

Purpose:
- Cross-cutting utilities and helpers

Contains:
- Extensions
- Utilities
- Reusable helpers

Rules:
- Infrastructure MUST NOT reference:
  - Api
  - Business
  - Entity
  - Contracts

---

## 3. Allowed dependencies

Allowed:

- Api → Business
- Api → Contracts
- Api → Entity (composition root only)
- Business → Contracts
- Business → Entity
- Business → Infrastructure
- Entity → Infrastructure

Forbidden:

- Contracts → anything
- Infrastructure → anything
- Entity → Business
- Entity → Contracts
- Api → Infrastructure (unless explicitly approved)
- Api → Entity for persistence logic

---

## 4. Standard request flow

### Read flow
```
Controller
  → Business Service
    → UnitOfWork / Repository
      → Entity
    → Map Entity → DTO
→ Return DTO
```

### Write flow
```
Controller
  → DTO
  → Business Service
    → Map DTO → Entity
    → UnitOfWork / Repository
    → SaveChanges
→ Return DTO / Id
```

---

## 5. Package management

This repository uses **Central Package Management** via `Directory.Packages.props`.

Rules:
- All NuGet versions live in `Directory.Packages.props`
- `.csproj` files must contain unversioned PackageReferences only
- Package versions must never be specified in project files

---

## 6. API reference (OpenAPI)

- OpenAPI is enabled using ASP.NET Core OpenAPI support
- API reference UI is provided by **Scalar**
- Swagger / Swashbuckle are not used and must not be added

---

## 7. Enforcement

Architecture violations are defects.
If a change cannot comply with this document, STOP and ask for clarification.
