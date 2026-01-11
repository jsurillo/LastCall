# AGENT_RULES.md

# BrosCode LastCall — Agent Rules (Authoritative)

These rules are mandatory for any AI agent or developer making changes in this repository.
They define architecture, invariants, and acceptable behavior.
If a request conflicts with these rules, the agent must STOP and ask for clarification.

---

## 1) Non-negotiables

1. Do not break the build  
   - Every change must end with `dotnet build`  
   - If tests exist, also run `dotnet test`

2. No circular dependencies  
   - Follow `ARCHITECTURE.md` dependency rules exactly

3. DTO-only at boundaries  
   - Controllers accept and return DTOs only  
   - EF entities must never be exposed as API contracts

4. Standard request flow is mandatory  
   - Controller → Business Service → UnitOfWork / Repository → Entity  
   - Mapping occurs in the Business layer using AutoMapper

5. Infrastructure is dependency-free  
   - Infrastructure must not reference Api, Business, or Entity

6. No secrets in source control  
   - Use environment variables, user-secrets, or excluded local config files

---

## 2) Project structure invariants

Company: BrosCode  
Application: LastCall  

Projects:
- BrosCode.LastCall.Api
- BrosCode.LastCall.Business
- BrosCode.LastCall.Entity
- BrosCode.LastCall.Infrastructure

Namespaces must match project and folder structure.

---

## 3) API endpoint architecture rules

### Controllers vs Minimal APIs

- This repository uses **controller-based MVC APIs**
- **Minimal APIs are NOT allowed** unless explicitly requested

When a task mentions:
- “controller”
- “Controllers folder”
- “controller-based API”
- “move endpoints to controllers”

The agent MUST:
- Remove or avoid all minimal API endpoint mappings
- Configure the application so **controllers are the only HTTP endpoint mechanism**
- Use the standard ASP.NET MVC controller pipeline

The agent MUST NOT:
- Keep or add `MapGet`, `MapPost`, or other minimal API route mappings
- Mix minimal APIs and controllers
- Introduce hybrid endpoint models

---

## 4) Program.cs responsibilities

`Program.cs` is responsible only for:
- Hosting
- Dependency injection
- Middleware
- API surface wiring

Rules:
- No business endpoints in `Program.cs`
- Endpoints live only in controller classes under `Controllers/`
- Framework-level wiring is expected knowledge and must not require explicit user instruction

---

## 5) OpenAPI rules

- Existing OpenAPI configuration must be preserved exactly unless explicitly instructed otherwise
- The agent must NOT:
  - Replace OpenAPI with Swagger
  - Add Swagger UI
  - Add new OpenAPI-related packages
- Controllers must be automatically included in the existing OpenAPI surface

---

## 6) BaseEntity rule (persistence invariant)

All persisted entities MUST inherit `BaseEntity`.

`BaseEntity` defines:
- `Guid Id` as primary key
- Audit fields (`CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`)
- Optimistic concurrency token `RowVersion` (`uint?`)

The agent must NOT:
- Create persisted entities that do not inherit `BaseEntity`
- Re-declare audit fields on individual entities
- Use SQL Server–style `rowversion` patterns

---

## 7) RowVersion concurrency rule

- Every persisted entity MUST have its `RowVersion` configured as `IsRowVersion()` in `OnModelCreating`
- The agent must use **one consistent approach**:
  - Preferred: a generic configuration applied to all `BaseEntity`-derived types
  - Allowed: explicit per-entity configuration, but it must be complete (no omissions)

Missing concurrency configuration is considered a defect.

---

## 8) BaseDto rule (business invariant)

All DTOs that map directly to persisted entities MUST inherit `BaseDto`
(either directly or through a DTO inheritance chain).

`BaseDto` defines:
- `Guid Id`
- `uint? RowVersion`
- Audit fields ignored for JSON serialization

DTO hierarchies are allowed as long as `BaseDto` exists in the chain.

---

## 9) Mapping rules

- AutoMapper lives in the Business project
- Every entity-backed DTO must have:
  - Entity → DTO mapping (read)
  - DTO → Entity mapping (write)
- Do not map navigation graphs implicitly
- Queries must explicitly control shape and includes

---

## 10) Repository & UnitOfWork rules

- All persistence operations go through Repository + UnitOfWork
- Controllers must never call DbContext or repositories directly
- Business services own persistence orchestration

---

## 11) Database naming rules (mandatory)

When adding or modifying EF Core entities/migrations:

- Always use an explicit database schema.
  - Default schema is `app` (do not create tables in `public`).
- Always use plural table names.
  - `Drink` -> `Drinks`, `Order` -> `Orders`, etc.
- Enforce schema + pluralization in ONE central place (DbContext model configuration).
  - Do NOT sprinkle `ToTable(...)` across entity files unless there’s an exceptional case.
- If using Postgres row version concurrency:
  - Expect EF/Npgsql to use `xmin`.
  - Do not “fix” migrations just because you see `xmin`.

---

## 12) API → Entity reference exception

The Api project may reference the Entity project **only** for:
- Calling Entity DI extension methods
- EF Core CLI usage where Api is the startup project

The Api project must NOT:
- Call repositories or UnitOfWork directly
- Implement persistence logic
- Use entity types as API payloads

---

## 13) Seeding rules

- Seed data is defined via JSON files
- Location: `Api/Seed/`
- File naming: `<EntityName>.json`

Behavior:
- Seed files are loaded during model creation using `HasData`
- Missing seed files are skipped without failure

---

## 14) Audit rules

Audit is enforced at DbContext level:
- Added entities: set `CreatedDate`
- Added and modified entities: set `ModifiedDate`

Audit fields must not be manually set in controllers.

---

## 15) Agent execution checklist

For any task such as “add entity”, “add service”, or “run migrations”, the agent must:

1. Modify only the correct layer(s)
2. Enforce `BaseEntity` / `BaseDto` rules
3. Ensure `RowVersion` is configured correctly
4. Add or update AutoMapper mappings
5. Add or update validators if applicable
6. Update DbContext configuration
7. Run migrations if schema changes
8. Run `dotnet build`
9. Run `dotnet test` if tests exist
10. Report what changed and what commands were executed
