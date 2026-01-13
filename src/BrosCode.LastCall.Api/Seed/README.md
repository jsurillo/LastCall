# Seed Data

Each system-data entity may have a corresponding JSON file in this folder.

Rules:
- Naming convention: <EntityName>.json (example: DrinkType.json)
- Every item MUST include Id (Guid). Id is the diff key.
- JSON arrays are the canonical source of truth for system data.

Suggested schema:
[
  { "Id": "GUID", "Code": "BEER", "Name": "Beer" }
]
