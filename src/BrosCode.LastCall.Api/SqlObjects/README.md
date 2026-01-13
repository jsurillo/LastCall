# SQL Objects

Each SQL object is stored as a .sql file.

Naming convention (schema is part of name):
- Functions:   Functions/<Schema>.<Name>.sql
- Procedures:  Procedures/<Schema>.<Name>.sql

Each file contains the full CREATE OR REPLACE statement (or equivalent Postgres-safe statement).

Required header metadata at the very top of each file:
-- TYPE: FUNCTION|PROCEDURE
-- DROP: <exact DROP statement including signature>

Example:
-- TYPE: FUNCTION
-- DROP: DROP FUNCTION IF EXISTS ref.get_drink_type(uuid);

These objects are deployed via migrations (not at runtime).
