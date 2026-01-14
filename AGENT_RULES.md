# AGENT_RULES.md

# BrosCode LastCall — Agent Rules (Authoritative)

This document defines **non-negotiable rules** for all human and AI contributors.
If any instruction conflicts with this file, the agent **MUST STOP** and ask for clarification.

This is the highest authority for implementation decisions in this repository.

---

## 1. Core invariants

1. **Build safety**
   - Every change MUST end with `dotnet build`
   - If tests exist, also run `dotnet test`

2. **No circular dependencies**
   - Project references MUST follow `ARCHITECTURE.md` exactly

3. **Contracts-first boundaries**
   - Controllers accept and return DTOs ONLY
   - DTOs live in `BrosCode.LastCall.Contracts`
   - EF entities MUST NOT cross layer boundaries

4. **Mandatory request flow**
   ```
   Controller → Business → UnitOfWork / Repository → Entity
   ```
   - Mapping occurs ONLY in Business

5. **Infrastructure isolation**
   - Infrastructure MUST NOT reference:
     - Api
     - Business
     - Entity
     - Contracts

6. **No secrets in repository**
   - Use environment variables, user-secrets, or excluded local config files

---

## 2. Solution structure (fixed, with tooling exception)

The solution MUST contain these core projects:

- BrosCode.LastCall.Api
- BrosCode.LastCall.Business
- BrosCode.LastCall.Contracts
- BrosCode.LastCall.Entity
- BrosCode.LastCall.Infrastructure

Rules:
- Namespaces MUST match project and folder structure
- No additional projects **except** the tooling exception below

### Tooling exception (explicitly approved)
One additional project is allowed for **design-time tooling only** (migration-time generators, codegen, etc.):

- Allowed tooling project name (recommended): `BrosCode.LastCall.MigrationsGen`

Tooling rules:
- Tooling project MUST NOT be required at runtime (Api must run without it).
- Tooling project MUST NOT introduce runtime dependencies into Api/Business/Entity.
- Tooling project MUST NOT be referenced by Api or Business.
- Tooling project MAY reference:
  - Entity (optional, only if needed)
  - Infrastructure (optional)
- Tooling project MUST NOT reference:
  - Api (as a project reference)
  - Contracts
- Tooling project reads Api artifacts (e.g., Seed JSON, SqlObjects) via filesystem paths only.

---

## 3. Central Package Management (MANDATORY)

This repository uses **Central Package Management** via `Directory.Packages.props`.

### Rules
- ALL NuGet package versions live in `Directory.Packages.props`
- `.csproj` files MUST NOT contain versioned `PackageReference`
- Agents MUST NOT introduce `Version="..."` in `.csproj`
- Agents MUST NOT run:
  ```
  dotnet add <project> package <pkg> --version X
  ```

### Correct process
1. Add unversioned reference to `.csproj`
   ```xml
   <PackageReference Include="Package.Name" />
   ```
2. Add or update the version in `Directory.Packages.props`
3. Run:
   - `dotnet restore`
   - `dotnet build`

Violations are defects.

---

## 4. API architecture rules

### Controllers only
- APIs MUST be controller-based MVC
- Minimal APIs are NOT allowed

Forbidden:
- `MapGet`, `MapPost`, etc.
- Hybrid endpoint styles

---

## 5. OpenAPI / API reference

- API reference UI is **Scalar**
- Swagger and Swashbuckle are explicitly forbidden
- Use ASP.NET Core OpenAPI + Scalar only

---

## 6. Entity layer rules

### BaseEntity (mandatory)
All persisted entities MUST inherit `BaseEntity`.

`BaseEntity` defines:
- `Guid Id`
- Audit fields (`Created*`, `Modified*`)
- `RowVersion` concurrency token

### Concurrency
- RowVersion MUST be configured as a concurrency token
- Prefer a generic configuration applied to all BaseEntity-derived entities

### Auditing
- Audit fields are set in DbContext `SaveChanges` / `SaveChangesAsync`
- Controllers MUST NOT set audit fields manually

---

## 7. Repository & UnitOfWork rules

- All persistence goes through Repository + UnitOfWork
- Controllers MUST NOT access DbContext or repositories
- UnitOfWork MUST be DbContext-centric:
  - Expose generic `Repository<TEntity>()`
  - Do NOT expose per-entity properties (e.g., `Drinks`)

---

## 8. DTO (Contracts) rules

- All DTOs mapped to persisted entities MUST inherit `BaseDto`
- `BaseDto` lives in `BrosCode.LastCall.Contracts`
- Entity project MUST NOT reference Contracts
- Mapping is owned by Business

---

## 9. EF Core & PostgreSQL conventions

- The application supports **multiple database schemas**
- Every table MUST be mapped to an explicit schema (do not rely on `public`)
- Table names MUST be plural
- Do NOT override table names unless required
- Postgres row version uses `xmin` (this is expected)

---

## 10. Controller route naming rules

- URL paths MUST use nouns only.
- HTTP verbs express intent (GET, POST, PUT, DELETE).
- Route names are INTERNAL identifiers and are not part of the URL.

Naming rules:
- Only routes that represent a single resource identity are named.
- Pattern: <ResourceName>ById
  Example: DrinkById, UserById, OrderById
- Collection routes and non-identity routes MUST NOT be named.

Usage:
- POST endpoints that create resources MUST return:
  CreatedAtRoute("<ResourceName>ById", new { id = created.Id }, created)

---

## 11. Enforcement

Any violation of this document is considered a defect.
If unsure, STOP and ask for clarification.
