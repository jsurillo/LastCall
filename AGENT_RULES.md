# AGENT_RULES.md

# BrosCode LastCall — Agent Rules (Coding Conventions)

These rules are mandatory for any agent or developer making changes in this repo.

---

## 1) Non-negotiables

1) Do not break build.
- Every change must end with: dotnet build
- If tests exist: dotnet test

2) No circular dependencies.
- Follow ARCHITECTURE.md dependency rules exactly.

3) DTO-only at boundaries.
- Controllers accept and return DTOs only.
- API contracts must never expose EF Entities.

4) Standard CRUD flow is mandatory.
- Controller -> Business Service -> UnitOfWork/Repository -> Entity
- Mapping happens in the Business layer using AutoMapper.

5) BaseEntity and BaseDto invariants are mandatory.
- All persisted entities MUST inherit BaseEntity.
- All DTOs that map directly to entities MUST inherit BaseDto (directly or indirectly). BaseDto is the required root for entity-backed DTOs.

6) RowVersion concurrency configuration is mandatory.
- Every persisted entity MUST have its RowVersion configured as IsRowVersion() in OnModelCreating.
- The agent must use one consistent approach:
  - Preferred: generic configuration applied to all BaseEntity-derived entity types (single implementation).
  - Allowed: explicit per-entity Property(x => x.RowVersion).IsRowVersion() lines, but the agent must keep them complete (no missing entity).

7) Elegant EF composition rule is mandatory.
- The API must not call UseNpgsql directly.
- The Entity project must expose an IServiceCollection extension method that registers:
  - DbContext
  - provider (UseNpgsql)
  - any Entity-layer services (UnitOfWork, repositories)
- The API calls that extension method in Program.cs.

8) API -> Entity reference is allowed only for composition root.
- Allowed uses:
  - calling the Entity DI extension method
  - EF CLI usage where Api is startup project and Entity is migration project
- Not allowed:
  - controllers calling repositories/UnitOfWork directly
  - entities used as API payloads

9) Infrastructure is dependency-free.
- Infrastructure must not reference Api, Business, or Entity.

10) No secrets in source control.
- Use environment variables, user-secrets, or local-only appsettings.Development.json excluded from git.

---

## 2) Naming & structure

Company: BrosCode
App: LastCall

Projects:
- BrosCode.LastCall.Api
- BrosCode.LastCall.Business
- BrosCode.LastCall.Entity
- BrosCode.LastCall.Infrastructure

Namespaces must match project names and folders.

---

## 3) BaseEntity rule (persistence invariant)

Every EF Core entity class that is persisted MUST inherit BaseEntity.

BaseEntity establishes:
- Guid Id primary key
- Audit fields (CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
- Optimistic concurrency token RowVersion (uint?)

Agent must NOT:
- Create an entity that does not inherit BaseEntity
- Recreate audit fields per entity
- Replace concurrency with SQL Server rowversion patterns

---

## 4) BaseDto rule (business invariant)

Every DTO that maps directly to a persisted entity MUST inherit BaseDto.

BaseDto establishes:
- Guid Id
- uint? RowVersion
- Audit fields ignored for JSON serialization

DTO hierarchies are allowed if the chain includes BaseDto.

---

## 5) Mapping rules (AutoMapper)

- AutoMapper lives in Business.
- Every entity-backed DTO must have:
  - Entity -> DTO mapping (reads)
  - DTO -> Entity mapping (writes)

Never map navigation graphs blindly. Use explicit DTO shape and explicit Includes.

---

## 6) Repository + UnitOfWork rules

- All persistence operations go through UnitOfWork + Repository.
- Controllers never call DbContext/Repo directly.
- Business services call repositories and SaveChanges/SaveChangesAsync.

---

## 7) Validation rules

- Use FluentValidation.
- Validate DTOs in the Business layer.
- Controllers do not contain business validation logic.

---

## 8) Seeding rules (JSON seed framework)

Seed data is stored as JSON files and applied using EF Core HasData.

Seed file convention:
- Location: Api/Seed/
- Name: <EntityName>.json

Seeding behavior:
- During model creation, seeding reads the JSON file for the entity type and calls HasData.
- If the file does not exist, seeding is skipped without failing.

---

## 9) Audit rules

Audit is enforced at DbContext SaveChanges / SaveChangesAsync:
- For Added entities: set CreatedDate
- For Added and Modified entities: set ModifiedDate

Audit fields are not set manually in controllers.

---

## 10) Agent execution checklist (required)

For any change request like: “add entity”, “add service”, “run migrations”, the agent must:

1) Make changes only in correct layer(s)
2) Create/modify entity and DTO following BaseEntity/BaseDto rules
3) Ensure RowVersion IsRowVersion configuration is applied (generic preferred)
4) Add or update AutoMapper mapping
5) Add or update validator
6) Update DbContext and EF configurations
7) Add migration and apply database update when schema changes
8) dotnet build
9) dotnet test (if present)
10) Summarize what changed and what commands were executed
