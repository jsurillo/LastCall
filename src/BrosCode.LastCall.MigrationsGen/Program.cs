using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BrosCode.LastCall.MigrationsGen;

internal static class Program
{
    private const int BatchSize = 200;
    private static readonly HashSet<string> AlwaysExcludedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "RowVersion"
    };

    private static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 1;
        }

        if (!TryParseArgs(args, out Options options, out string error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return 1;
        }

        try
        {
            Run(options);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static void Run(Options options)
    {
        ValidateInputs(options);

        var seedMap = LoadSeedMap(options.SeedMapPath);
        var seedResult = BuildSeedSql(options.SeedDir, options.SeedSnapshotDir, seedMap);
        var sqlObjectsResult = BuildSqlObjectsSql(options.SqlObjectsDir, options.SqlObjectsSnapshotDir);

        string migrationText = File.ReadAllText(options.MigrationPath);
        string updatedMigration = MigrationPatcher.Apply(
            migrationText,
            seedResult.SqlStatements,
            sqlObjectsResult.SqlStatements);

        File.WriteAllText(options.MigrationPath, updatedMigration, Encoding.UTF8);

        WriteSeedSnapshots(seedResult.Snapshots);
        WriteSqlObjectSnapshots(sqlObjectsResult.Snapshots);
    }

    private static void ValidateInputs(Options options)
    {
        if (!File.Exists(options.MigrationPath))
        {
            throw new InvalidOperationException($"Migration file not found: {options.MigrationPath}");
        }

        if (!Directory.Exists(options.SeedDir))
        {
            throw new InvalidOperationException($"Seed directory not found: {options.SeedDir}");
        }

        if (!File.Exists(options.SeedMapPath))
        {
            throw new InvalidOperationException($"Seed map not found: {options.SeedMapPath}");
        }

        if (!Directory.Exists(options.SqlObjectsDir))
        {
            throw new InvalidOperationException($"SqlObjects directory not found: {options.SqlObjectsDir}");
        }
    }

    private static Dictionary<string, SeedMapEntry> LoadSeedMap(string seedMapPath)
    {
        string json = File.ReadAllText(seedMapPath);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("seed-map.json must be a JSON object.");
        }

        var map = new Dictionary<string, SeedMapEntry>(StringComparer.Ordinal);
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"seed-map.json entry '{property.Name}' must be an object.");
            }

            string? schema = null;
            string? table = null;
            foreach (var inner in property.Value.EnumerateObject())
            {
                if (inner.NameEquals("Schema"))
                {
                    schema = inner.Value.GetString();
                }
                else if (inner.NameEquals("Table"))
                {
                    table = inner.Value.GetString();
                }
            }

            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(table))
            {
                throw new InvalidOperationException($"seed-map.json entry '{property.Name}' must include Schema and Table.");
            }

            map[property.Name] = new SeedMapEntry(schema, table);
        }

        return map;
    }

    private static SeedResult BuildSeedSql(
        string seedDir,
        string seedSnapshotDir,
        Dictionary<string, SeedMapEntry> seedMap)
    {
        var sqlStatements = new List<string>();
        var snapshots = new List<SeedSnapshot>();

        string[] seedFiles = Directory.GetFiles(seedDir, "*.json", SearchOption.TopDirectoryOnly);
        foreach (string seedFile in seedFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            string entityName = Path.GetFileNameWithoutExtension(seedFile);
            if (!seedMap.TryGetValue(entityName, out SeedMapEntry? mapEntry))
            {
                throw new InvalidOperationException($"Missing seed-map.json entry for entity '{entityName}'.");
            }

            SeedData current = LoadSeedData(seedFile);
            SeedData snapshot = LoadSeedSnapshot(seedSnapshotDir, entityName);

            var added = current.Items.Values.Where(i => !snapshot.Items.ContainsKey(i.Id)).ToList();
            var deleted = snapshot.Items.Values.Where(i => !current.Items.ContainsKey(i.Id)).ToList();
            var updated = current.Items.Values
                .Where(i => snapshot.Items.TryGetValue(i.Id, out SeedItem? prior) && prior.Normalized != i.Normalized)
                .ToList();

            sqlStatements.AddRange(BuildSeedSqlStatements(mapEntry, added, updated, deleted));

            snapshots.Add(new SeedSnapshot(
                Path.Combine(seedSnapshotDir, $"{entityName}.snapshot.json"),
                current.NormalizedArray));
        }

        return new SeedResult(sqlStatements, snapshots);
    }

    private static SeedData LoadSeedData(string path)
    {
        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        if (node is not JsonArray array)
        {
            throw new InvalidOperationException($"Seed file must be a JSON array: {path}");
        }

        var items = new Dictionary<Guid, SeedItem>();
        foreach (JsonNode? elementNode in array)
        {
            if (elementNode is not JsonObject obj)
            {
                throw new InvalidOperationException($"Seed file contains a non-object element: {path}");
            }

            if (!obj.TryGetPropertyValue("Id", out JsonNode? idNode) || idNode is null)
            {
                throw new InvalidOperationException($"Seed file item missing Id: {path}");
            }

            if (!Guid.TryParse(idNode.ToString(), out Guid id))
            {
                throw new InvalidOperationException($"Seed file item Id is not a Guid: {path}");
            }

            JsonObject normalized = NormalizeJsonObject(obj);
            string normalizedText = SerializeNormalized(normalized, false);

            if (!items.TryAdd(id, new SeedItem(id, normalized, normalizedText)))
            {
                throw new InvalidOperationException($"Duplicate Id '{id}' in seed file: {path}");
            }
        }

        JsonArray normalizedArray = new();
        foreach (SeedItem item in items.Values.OrderBy(i => i.Id))
        {
            normalizedArray.Add(item.Node);
        }

        string normalizedArrayText = SerializeNormalized(normalizedArray, true);
        return new SeedData(items, normalizedArrayText);
    }

    private static SeedData LoadSeedSnapshot(string seedSnapshotDir, string entityName)
    {
        string snapshotPath = Path.Combine(seedSnapshotDir, $"{entityName}.snapshot.json");
        if (!File.Exists(snapshotPath))
        {
            return new SeedData(new Dictionary<Guid, SeedItem>(), "[]");
        }

        return LoadSeedData(snapshotPath);
    }

    private static IReadOnlyList<string> BuildSeedSqlStatements(
        SeedMapEntry mapEntry,
        List<SeedItem> added,
        List<SeedItem> updated,
        List<SeedItem> deleted)
    {
        var statements = new List<string>();

        var upsertItems = added.Concat(updated).ToList();
        if (upsertItems.Count > 0)
        {
            foreach (var group in upsertItems.GroupBy(GetSeedColumnKey))
            {
                string[] columns = group.Key.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length == 0)
                {
                    columns = Array.Empty<string>();
                }

                foreach (var chunk in group.Chunk(BatchSize))
                {
                    statements.Add(BuildSeedUpsertStatement(mapEntry, columns, chunk));
                }
            }
        }

        if (deleted.Count > 0)
        {
            foreach (var chunk in deleted.OrderBy(d => d.Id).Chunk(BatchSize))
            {
                statements.Add(BuildSeedDeleteStatement(mapEntry, chunk));
            }
        }

        return statements;
    }

    private static string GetSeedColumnKey(SeedItem item)
    {
        var columns = item.Node
            .Select(p => p.Key)
            .Where(c => !string.Equals(c, "Id", StringComparison.OrdinalIgnoreCase))
            .Where(c => !AlwaysExcludedColumns.Contains(c))
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        return string.Join('|', columns);
    }

    private static string BuildSeedUpsertStatement(
        SeedMapEntry mapEntry,
        string[] columns,
        IReadOnlyList<SeedItem> items)
    {
        var allColumns = new List<string> { "Id" };
        allColumns.AddRange(columns);

        string columnList = string.Join(", ", allColumns.Select(QuoteIdent));
        string target = $"{QuoteIdent(mapEntry.Schema)}.{QuoteIdent(mapEntry.Table)}";

        var values = new StringBuilder();
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                values.Append(", ");
            }

            values.Append("(");
            values.Append(FormatSeedValues(items[i], allColumns));
            values.Append(")");
        }

        string updateSet;
        if (columns.Length == 0)
        {
            updateSet = "DO NOTHING";
        }
        else
        {
            string updates = string.Join(", ", columns.Select(c => $"{QuoteIdent(c)} = EXCLUDED.{QuoteIdent(c)}"));
            updateSet = $"DO UPDATE SET {updates}";
        }

        return $"INSERT INTO {target} ({columnList}) VALUES {values} ON CONFLICT ({QuoteIdent("Id")}) {updateSet};";
    }

    private static string FormatSeedValues(SeedItem item, IReadOnlyList<string> columns)
    {
        var values = new List<string>(columns.Count);
        foreach (string column in columns)
        {
            if (!item.Node.TryGetPropertyValue(column, out JsonNode? valueNode))
            {
                values.Add("NULL");
                continue;
            }

            values.Add(ToSqlLiteral(valueNode));
        }

        return string.Join(", ", values);
    }

    private static string BuildSeedDeleteStatement(SeedMapEntry mapEntry, IReadOnlyList<SeedItem> items)
    {
        string target = $"{QuoteIdent(mapEntry.Schema)}.{QuoteIdent(mapEntry.Table)}";
        string ids = string.Join(", ", items.Select(i => $"'{i.Id:D}'"));
        return $"DELETE FROM {target} WHERE {QuoteIdent("Id")} IN ({ids});";
    }

    private static SqlObjectsResult BuildSqlObjectsSql(string sqlObjectsDir, string sqlObjectsSnapshotDir)
    {
        var sqlStatements = new List<string>();
        var snapshots = new List<SqlObjectSnapshot>();

        var currentObjects = LoadSqlObjects(sqlObjectsDir, sqlObjectsSnapshotDir);
        var snapshotObjects = LoadSqlObjectSnapshots(sqlObjectsSnapshotDir);

        foreach (var current in currentObjects.Values)
        {
            if (!snapshotObjects.TryGetValue(current.Key, out SqlObjectInfo? prior))
            {
                sqlStatements.Add(EnsureSemicolon(current.NormalizedContent));
            }
            else if (!string.Equals(current.NormalizedContent, prior.NormalizedContent, StringComparison.Ordinal))
            {
                sqlStatements.Add(EnsureSemicolon(current.NormalizedContent));
            }

            snapshots.Add(new SqlObjectSnapshot(current.SnapshotPath, current.NormalizedContent));
        }

        foreach (var snapshot in snapshotObjects.Values)
        {
            if (!currentObjects.ContainsKey(snapshot.Key))
            {
                sqlStatements.Add(EnsureSemicolon(snapshot.DropStatement));
            }
        }

        return new SqlObjectsResult(sqlStatements, snapshots);
    }

    private static Dictionary<string, SqlObjectInfo> LoadSqlObjects(string sqlObjectsDir, string sqlObjectsSnapshotDir)
    {
        var result = new Dictionary<string, SqlObjectInfo>(StringComparer.Ordinal);

        LoadSqlObjectsFromFolder(
            Path.Combine(sqlObjectsDir, "Functions"),
            Path.Combine(sqlObjectsSnapshotDir, "Functions"),
            "FUNCTION",
            result);

        LoadSqlObjectsFromFolder(
            Path.Combine(sqlObjectsDir, "Procedures"),
            Path.Combine(sqlObjectsSnapshotDir, "Procedures"),
            "PROCEDURE",
            result);

        return result;
    }

    private static void LoadSqlObjectsFromFolder(
        string folder,
        string snapshotFolder,
        string expectedType,
        Dictionary<string, SqlObjectInfo> result)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(folder, "*.sql", SearchOption.TopDirectoryOnly))
        {
            var info = ParseSqlObject(file, snapshotFolder, expectedType);
            if (!result.TryAdd(info.Key, info))
            {
                throw new InvalidOperationException($"Duplicate SQL object '{info.Key}'.");
            }
        }
    }

    private static Dictionary<string, SqlObjectInfo> LoadSqlObjectSnapshots(string sqlObjectsSnapshotDir)
    {
        var result = new Dictionary<string, SqlObjectInfo>(StringComparer.Ordinal);

        LoadSqlObjectSnapshotsFromFolder(Path.Combine(sqlObjectsSnapshotDir, "Functions"), result);
        LoadSqlObjectSnapshotsFromFolder(Path.Combine(sqlObjectsSnapshotDir, "Procedures"), result);

        return result;
    }

    private static void LoadSqlObjectSnapshotsFromFolder(string folder, Dictionary<string, SqlObjectInfo> result)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(folder, "*.snapshot.sql", SearchOption.TopDirectoryOnly))
        {
            var info = ParseSqlObjectSnapshot(file);
            if (!result.TryAdd(info.Key, info))
            {
                throw new InvalidOperationException($"Duplicate SQL object snapshot '{info.Key}'.");
            }
        }
    }

    private static SqlObjectInfo ParseSqlObject(string path, string snapshotFolder, string expectedType)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        string[] nameParts = fileName.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length != 2)
        {
            throw new InvalidOperationException($"SQL object filename must be <Schema>.<Name>.sql: {path}");
        }

        string schema = nameParts[0];
        string name = nameParts[1];

        string normalizedContent = NormalizeSqlContent(File.ReadAllText(path));
        SqlHeader header = ParseSqlHeader(normalizedContent, path);
        if (!string.Equals(header.Type, expectedType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"SQL object header TYPE mismatch in {path}. Expected {expectedType}.");
        }

        string snapshotPath = Path.Combine(snapshotFolder, $"{schema}.{name}.snapshot.sql");
        string key = $"{header.Type}:{schema}.{name}";
        return new SqlObjectInfo(key, header.Type, schema, name, normalizedContent, header.DropStatement, snapshotPath);
    }

    private static SqlObjectInfo ParseSqlObjectSnapshot(string path)
    {
        string fileName = Path.GetFileName(path);
        if (!fileName.EndsWith(".snapshot.sql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid snapshot filename: {path}");
        }

        string baseName = fileName[..^".snapshot.sql".Length];
        string[] nameParts = baseName.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length != 2)
        {
            throw new InvalidOperationException($"SQL snapshot filename must be <Schema>.<Name>.snapshot.sql: {path}");
        }

        string schema = nameParts[0];
        string name = nameParts[1];

        string normalizedContent = NormalizeSqlContent(File.ReadAllText(path));
        SqlHeader header = ParseSqlHeader(normalizedContent, path);
        string key = $"{header.Type}:{schema}.{name}";
        return new SqlObjectInfo(key, header.Type, schema, name, normalizedContent, header.DropStatement, path);
    }

    private static SqlHeader ParseSqlHeader(string normalizedContent, string path)
    {
        string[] lines = normalizedContent.Split('\n');
        if (lines.Length < 2)
        {
            throw new InvalidOperationException($"SQL object is missing required header lines: {path}");
        }

        string typeLine = lines[0].Trim();
        string dropLine = lines[1].Trim();

        if (!typeLine.StartsWith("-- TYPE:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"SQL object missing TYPE header: {path}");
        }

        if (!dropLine.StartsWith("-- DROP:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"SQL object missing DROP header: {path}");
        }

        string type = typeLine["-- TYPE:".Length..].Trim().ToUpperInvariant();
        string drop = dropLine["-- DROP:".Length..].Trim();

        if (type != "FUNCTION" && type != "PROCEDURE")
        {
            throw new InvalidOperationException($"SQL object TYPE must be FUNCTION or PROCEDURE: {path}");
        }

        if (string.IsNullOrWhiteSpace(drop))
        {
            throw new InvalidOperationException($"SQL object DROP header is empty: {path}");
        }

        return new SqlHeader(type, drop);
    }

    private static string NormalizeSqlContent(string content)
    {
        string normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] lines = normalized.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();
        }

        return string.Join('\n', lines);
    }

    private static void WriteSeedSnapshots(IEnumerable<SeedSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshot.Path)!);
            File.WriteAllText(snapshot.Path, snapshot.Content, Encoding.UTF8);
        }
    }

    private static void WriteSqlObjectSnapshots(IEnumerable<SqlObjectSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshot.Path)!);
            File.WriteAllText(snapshot.Path, snapshot.Content, Encoding.UTF8);
        }
    }

    private static JsonObject NormalizeJsonObject(JsonObject obj)
    {
        var normalized = new JsonObject();
        foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            normalized[property.Key] = NormalizeJsonNode(property.Value);
        }

        return normalized;
    }

    private static JsonNode? NormalizeJsonNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            return NormalizeJsonObject(obj);
        }

        if (node is JsonArray arr)
        {
            var normalizedArray = new JsonArray();
            foreach (var item in arr)
            {
                normalizedArray.Add(NormalizeJsonNode(item));
            }

            return normalizedArray;
        }

        return node?.DeepClone();
    }

    private static string SerializeNormalized(JsonNode node, bool writeIndented)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented
        };

        return node.ToJsonString(options);
    }

    private static string ToSqlLiteral(JsonNode? node)
    {
        if (node is null)
        {
            return "NULL";
        }

        JsonElement element = JsonSerializer.SerializeToElement(node);
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return "NULL";
            case JsonValueKind.String:
                return SqlString(element.GetString() ?? string.Empty);
            case JsonValueKind.Number:
                return element.GetRawText();
            case JsonValueKind.True:
                return "TRUE";
            case JsonValueKind.False:
                return "FALSE";
            case JsonValueKind.Object:
            case JsonValueKind.Array:
                return SqlString(element.GetRawText());
            default:
                return SqlString(element.GetRawText());
        }
    }

    private static string SqlString(string value)
    {
        return $"'{value.Replace("'", "''")}'";
    }

    private static string QuoteIdent(string name)
    {
        return $"\"{name.Replace("\"", "\"\"")}\"";
    }

    private static string EnsureSemicolon(string sql)
    {
        string trimmed = sql.TrimEnd();
        return trimmed.EndsWith(';') ? trimmed : $"{trimmed};";
    }

    private static bool TryParseArgs(string[] args, out Options options, out string error)
    {
        options = new Options();
        error = string.Empty;

        if (args.Length % 2 != 0)
        {
            error = "Arguments must be provided as --key value pairs.";
            return false;
        }

        for (int i = 0; i < args.Length; i += 2)
        {
            string key = args[i];
            string value = args[i + 1];

            switch (key)
            {
                case "--migration":
                    options.MigrationPath = value;
                    break;
                case "--seed-dir":
                    options.SeedDir = value;
                    break;
                case "--seed-snapshot-dir":
                    options.SeedSnapshotDir = value;
                    break;
                case "--seed-map":
                    options.SeedMapPath = value;
                    break;
                case "--sqlobjects-dir":
                    options.SqlObjectsDir = value;
                    break;
                case "--sqlobjects-snapshot-dir":
                    options.SqlObjectsSnapshotDir = value;
                    break;
                default:
                    error = $"Unknown argument: {key}";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(options.MigrationPath) ||
            string.IsNullOrWhiteSpace(options.SeedDir) ||
            string.IsNullOrWhiteSpace(options.SeedSnapshotDir) ||
            string.IsNullOrWhiteSpace(options.SeedMapPath) ||
            string.IsNullOrWhiteSpace(options.SqlObjectsDir) ||
            string.IsNullOrWhiteSpace(options.SqlObjectsSnapshotDir))
        {
            error = "Missing required arguments.";
            return false;
        }

        return true;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/BrosCode.LastCall.MigrationsGen -- \\");
        Console.WriteLine("    --migration <path-to-new-migration.cs> \\");
        Console.WriteLine("    --seed-dir <src/BrosCode.LastCall.Api/Seed> \\");
        Console.WriteLine("    --seed-snapshot-dir <src/BrosCode.LastCall.Entity/Seed/Snapshots> \\");
        Console.WriteLine("    --seed-map <src/BrosCode.LastCall.Entity/Seed/seed-map.json> \\");
        Console.WriteLine("    --sqlobjects-dir <src/BrosCode.LastCall.Api/SqlObjects> \\");
        Console.WriteLine("    --sqlobjects-snapshot-dir <src/BrosCode.LastCall.Entity/SqlObjects/Snapshots>");
        Console.WriteLine();
        Console.WriteLine("Canonical workflow:");
        Console.WriteLine("1) Create migration:");
        Console.WriteLine("   dotnet ef migrations add <Name> \\");
        Console.WriteLine("     -p src/BrosCode.LastCall.Entity/BrosCode.LastCall.Entity.csproj \\");
        Console.WriteLine("     -s src/BrosCode.LastCall.Api/BrosCode.LastCall.Api.csproj \\");
        Console.WriteLine("     -o Migrations");
        Console.WriteLine();
        Console.WriteLine("2) Run generator (this tool).");
    }

    private sealed class Options
    {
        public string MigrationPath { get; set; } = string.Empty;
        public string SeedDir { get; set; } = string.Empty;
        public string SeedSnapshotDir { get; set; } = string.Empty;
        public string SeedMapPath { get; set; } = string.Empty;
        public string SqlObjectsDir { get; set; } = string.Empty;
        public string SqlObjectsSnapshotDir { get; set; } = string.Empty;
    }

    private sealed record SeedMapEntry(string Schema, string Table);

    private sealed record SeedItem(Guid Id, JsonObject Node, string Normalized);

    private sealed record SeedData(Dictionary<Guid, SeedItem> Items, string NormalizedArray);

    private sealed record SeedSnapshot(string Path, string Content);

    private sealed record SeedResult(List<string> SqlStatements, List<SeedSnapshot> Snapshots);

    private sealed record SqlObjectsResult(List<string> SqlStatements, List<SqlObjectSnapshot> Snapshots);

    private sealed record SqlObjectSnapshot(string Path, string Content);

    private sealed record SqlObjectInfo(
        string Key,
        string Type,
        string Schema,
        string Name,
        string NormalizedContent,
        string DropStatement,
        string SnapshotPath);

    private sealed record SqlHeader(string Type, string DropStatement);

    private static class MigrationPatcher
    {
        public static string Apply(string content, IReadOnlyList<string> seedSql, IReadOnlyList<string> sqlObjectsSql)
        {
            string newLine = DetectNewLine(content);
            if (!TryFindUpMethod(content, out int braceIndex, out string indent))
            {
                throw new InvalidOperationException("Could not locate Up(MigrationBuilder migrationBuilder) method.");
            }

            const string seedBegin = "// BEGIN SEED DATA (generated)";
            const string seedEnd = "// END SEED DATA (generated)";
            const string sqlBegin = "// BEGIN SQL OBJECTS (generated)";
            const string sqlEnd = "// END SQL OBJECTS (generated)";

            string seedRegion = BuildRegion(
                "SEED DATA",
                seedSql,
                indent,
                newLine);

            string sqlRegion = BuildRegion(
                "SQL OBJECTS",
                sqlObjectsSql,
                indent,
                newLine);

            bool hasSeed = content.Contains(seedBegin, StringComparison.Ordinal);
            bool hasSql = content.Contains(sqlBegin, StringComparison.Ordinal);

            string updated = content;
            if (hasSeed)
            {
                updated = ReplaceRegion(updated, seedRegion, seedBegin, seedEnd, newLine);
            }

            if (hasSql)
            {
                updated = ReplaceRegion(updated, sqlRegion, sqlBegin, sqlEnd, newLine);
            }

            if (!hasSeed && !hasSql)
            {
                string combined = seedRegion + newLine + sqlRegion;
                updated = InsertAfterBrace(updated, combined, braceIndex, newLine);
            }
            else if (hasSeed && !hasSql)
            {
                updated = InsertAfterRegion(updated, sqlRegion, seedEnd, newLine);
            }
            else if (!hasSeed && hasSql)
            {
                updated = InsertBeforeRegion(updated, seedRegion, sqlBegin, newLine);
            }

            return updated;
        }

        private static string ReplaceRegion(
            string content,
            string regionBlock,
            string beginMarker,
            string endMarker,
            string newLine)
        {
            int beginIndex = content.IndexOf(beginMarker, StringComparison.Ordinal);
            int endIndex = content.IndexOf(endMarker, StringComparison.Ordinal);

            if (beginIndex < 0 || endIndex < 0 || endIndex < beginIndex)
            {
                throw new InvalidOperationException($"Region markers are malformed for {beginMarker}.");
            }

            int beginLineStart = content.LastIndexOf(newLine, beginIndex, StringComparison.Ordinal);
            if (beginLineStart < 0)
            {
                beginLineStart = 0;
            }
            else
            {
                beginLineStart += newLine.Length;
            }

            int endLineEnd = content.IndexOf(newLine, endIndex, StringComparison.Ordinal);
            if (endLineEnd < 0)
            {
                endLineEnd = content.Length;
            }
            else
            {
                endLineEnd += newLine.Length;
            }

            return content[..beginLineStart] + regionBlock + content[endLineEnd..];
        }

        private static string InsertAfterBrace(string content, string regionBlock, int upBraceIndex, string newLine)
        {
            int insertIndex = content.IndexOf(newLine, upBraceIndex, StringComparison.Ordinal);
            if (insertIndex < 0)
            {
                insertIndex = upBraceIndex + 1;
                return content.Insert(insertIndex, newLine + regionBlock);
            }

            insertIndex += newLine.Length;
            return content.Insert(insertIndex, regionBlock + newLine);
        }

        private static string InsertAfterRegion(string content, string regionBlock, string endMarker, string newLine)
        {
            int endIndex = content.IndexOf(endMarker, StringComparison.Ordinal);
            if (endIndex < 0)
            {
                throw new InvalidOperationException($"Region end marker not found for {endMarker}.");
            }

            int endLineEnd = content.IndexOf(newLine, endIndex, StringComparison.Ordinal);
            if (endLineEnd < 0)
            {
                endLineEnd = content.Length;
                return content + newLine + regionBlock;
            }

            endLineEnd += newLine.Length;
            return content.Insert(endLineEnd, regionBlock + newLine);
        }

        private static string InsertBeforeRegion(string content, string regionBlock, string beginMarker, string newLine)
        {
            int beginIndex = content.IndexOf(beginMarker, StringComparison.Ordinal);
            if (beginIndex < 0)
            {
                throw new InvalidOperationException($"Region begin marker not found for {beginMarker}.");
            }

            int beginLineStart = content.LastIndexOf(newLine, beginIndex, StringComparison.Ordinal);
            if (beginLineStart < 0)
            {
                beginLineStart = 0;
            }
            else
            {
                beginLineStart += newLine.Length;
            }

            return content.Insert(beginLineStart, regionBlock + newLine);
        }

        private static bool TryFindUpMethod(string content, out int braceIndex, out string indent)
        {
            indent = "        ";
            braceIndex = -1;

            string signature = "protected override void Up(MigrationBuilder migrationBuilder)";
            int sigIndex = content.IndexOf(signature, StringComparison.Ordinal);
            if (sigIndex < 0)
            {
                signature = "public override void Up(MigrationBuilder migrationBuilder)";
                sigIndex = content.IndexOf(signature, StringComparison.Ordinal);
            }

            if (sigIndex < 0)
            {
                return false;
            }

            braceIndex = content.IndexOf('{', sigIndex);
            if (braceIndex < 0)
            {
                return false;
            }

            string newLine = DetectNewLine(content);
            int nextLineIndex = content.IndexOf(newLine, braceIndex, StringComparison.Ordinal);
            if (nextLineIndex >= 0)
            {
                int lineStart = nextLineIndex + newLine.Length;
                int lineEnd = content.IndexOf(newLine, lineStart, StringComparison.Ordinal);
                if (lineEnd < 0)
                {
                    lineEnd = content.Length;
                }

                string line = content[lineStart..lineEnd];
                string lineIndent = new string(line.TakeWhile(char.IsWhiteSpace).ToArray());
                if (!string.IsNullOrWhiteSpace(lineIndent))
                {
                    indent = lineIndent;
                }
            }

            return true;
        }

        private static string BuildRegion(
            string title,
            IReadOnlyList<string> sql,
            string indent,
            string newLine)
        {
            var sb = new StringBuilder();
            sb.Append(indent).Append("// BEGIN ").Append(title).Append(" (generated)").Append(newLine);

            if (sql.Count == 0)
            {
                sb.Append(indent).Append("// (no changes)").Append(newLine);
            }
            else
            {
                foreach (string statement in sql)
                {
                    sb.Append(indent).Append("migrationBuilder.Sql(@\"")
                        .Append(EscapeVerbatim(statement))
                        .AppendLine("\");");
                    sb.Append(newLine);
                }
            }

            sb.Append(indent).Append("// END ").Append(title).Append(" (generated)").Append(newLine);
            return sb.ToString();
        }

        private static string EscapeVerbatim(string value)
        {
            return value.Replace("\"", "\"\"");
        }

        private static string DetectNewLine(string content)
        {
            return content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        }
    }
}
