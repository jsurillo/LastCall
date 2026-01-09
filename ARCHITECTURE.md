# ARCHITECTURE.md

# BrosCode LastCall — Architecture & Dependency Rules

This repo uses a 4-project architecture with strict dependency rules and a DTO-first boundary.
The Api project is the composition root and wires DI, but business logic stays in Business.

---

## 1) Solution layout

Repo root:
- README.md
- AGENT_RULES.md
- ARCHITECTURE.md
- TASK_PLAYBOOK.md
- src/
- tests/ (optional)

Solution path:
- src/BrosCode.LastCall.sln

Projects under src/:
- BrosCode.LastCall.Api
- BrosCode.LastCall.Business
- BrosCode.LastCall.Entity
- BrosCode.LastCall.Infrastructure

---

## 2) Project responsibilities

### BrosCode.LastCall.Api
Purpose: host application, HTTP endpoints, middleware, bootstrapping.

Contains:
- Program.cs
- Controllers
- Middleware
- Seed JSON files under Seed/
- API pipeline configuration (Swagger/OpenAPI, auth, CORS, etc.)

Composition root rule:
- Api wires dependencies by calling DI extension methods from other layers.
- Api does not contain business logic or persistence logic.

### BrosCode.LastCall.Business
Purpose: business logic and “DTO language” of the application.

Contains:
- DTOs
- Validators
- Converters
- Services (generic CRUD and specialized)
- AutoMapper profiles
- SignalR hubs (if used)

Key rule:
- DTOs that map directly to persisted entities inherit BaseDto.

### BrosCode.LastCall.Entity
Purpose: persistence boundary.

Contains:
- DbContext and EF Core model configuration
- Entities (persisted types)
- Repository pattern
- Unit of Work pattern
- EF Core migrations
- Entity-layer DI extension method (composition hook)

Key rules:
- All persisted entities inherit BaseEntity.
- RowVersion is configured as IsRowVersion() for all persisted entities.
- Audit timestamps are applied in SaveChanges/SaveChangesAsync.
- Provider configuration (UseNpgsql) belongs here, not in Api.

### BrosCode.LastCall.Infrastructure
Purpose: cross-cutting, reusable code with no dependency on other projects.

Contains:
- Helpers, extensions, utility services
- General-purpose code that does not reference Api/Business/Entity

Must NOT reference:
- Api
- Business
- Entity

---

## 3) Allowed project dependencies

Allowed:
- Api -> Business
- Api -> Entity (composition root only: call DI extension, EF CLI startup project)
- Business -> Entity
- Business -> Infrastructure (optional)
- Entity -> Infrastructure (optional)

Not allowed:
- Api -> Entity for persistence logic (controllers must not use repos/UoW directly)
- Api -> Infrastructure (direct) unless explicitly approved for trivial helper
- Entity -> Business
- Infrastructure -> anything

---

## 4) Standard request flow (DTO-first)

Read flow:
- Controller calls Business service
- Business service queries via UnitOfWork/Repository
- Entity is mapped to DTO using AutoMapper
- Controller returns DTO

Write flow:
- Controller receives DTO
- Controller calls Business service
- Business service maps DTO to Entity and persists via UnitOfWork
- Business service saves changes
- Controller returns DTO or identifier

---

## 5) BaseEntity / BaseDto invariants

BaseEntity:
- Required base type for all persisted entities (Id + audit + RowVersion)

BaseDto:
- Required base type for all DTOs mapped directly to persisted entities (Id + RowVersion + audit hidden from JSON)

---

## 6) Seeding framework

Seed data is defined as JSON files (system data).
- Seed file location: Api/Seed/
- File naming: <EntityName>.json
- Seeding is applied during model creation for entity types that opt-in to seeding.
- Missing seed files do not fail startup; they are skipped.
