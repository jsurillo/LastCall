# BrosCode.LastCall.MigrationsGen

## Purpose
BrosCode.LastCall.MigrationsGen is a design-time migration helper that:
- Diffs seed JSON files against snapshots
- Diffs SQL objects against snapshots
- Generates INSERT / UPDATE / DELETE / CREATE / DROP SQL
- Injects that SQL into the generated EF Core migration
- Updates snapshots after successful injection

This tool does NOT run at application startup.
This tool does NOT seed data at runtime.
This tool exists to make migrations deterministic and incremental.

## When to Use This Tool
Run this tool:
- After creating a new EF Core migration
- Whenever seed JSON or SQL objects have changed
- Before committing migrations to source control

## When NOT to Use This Tool
Do NOT:
- Run this tool without a migration
- Reference this project from Api or Business
- Use this tool to mutate the database directly
- Bypass migrations by running SQL manually

## Canonical Order of Operations
1. Create a migration:
   dotnet ef migrations add <Name> \
     -p src/BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj \
     -s src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj \
     -o Migrations

2. Run the migration generator:
   dotnet run --project src/BrosCode.LastCall.MigrationsGen -- \
     --migration <path-to-new-migration.cs> \
     --seed-dir <src/BrosCode.LastCall.Api/Seed> \
     --seed-snapshot-dir <src/BrosCode.LastCall.Entity/Seed/Snapshots> \
     --seed-map <src/BrosCode.LastCall.Entity/Seed/seed-map.json> \
     --sqlobjects-dir <src/BrosCode.LastCall.Api/SqlObjects> \
     --sqlobjects-snapshot-dir <src/BrosCode.LastCall.Entity/SqlObjects/Snapshots>

3. Review the generated migration:
   - Verify BEGIN/END SEED DATA region
   - Verify BEGIN/END SQL OBJECTS region
   - Ensure no unrelated code was modified

4. Commit:
   - Migration file
   - Updated snapshot files
   - Any new or modified seed JSON or SQL object files

## How It Works (High Level)
- JSON is normalized and diffed by Id (Guid)
- SQL objects are diffed by normalized file content
- Only deltas are emitted
- SQL is embedded using migrationBuilder.Sql
- Snapshots are updated only after successful generation

## Safety Guarantees
- No full re-seeding
- No runtime dependency
- No schema assumptions (multi-schema supported)
- No silent deletes (deletes are explicit in migrations)

## Enforcement
Skipping this step when seed or SQL objects change is a defect.
Migrations without updated snapshots are invalid.
If unsure, STOP and ask.
