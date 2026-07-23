// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace CdacUsageGraph.Docs;

/// <summary>
/// Per-contract descriptor additions and suppressions loaded from
/// <c>docs/design/datacontracts/data-descriptor-overrides.json</c>.
/// Contract-wide entries use the contract short name; version-specific entries use
/// <c>Contract@version</c> and are combined with any contract-wide entries.
/// </summary>
public sealed class DocDescriptorOverrides
{
    private readonly Dictionary<string, List<string>> _supplement;
    private readonly Dictionary<string, Dictionary<string, string>> _supplementTypes;
    private readonly Dictionary<string, List<string>> _suppress;

    private DocDescriptorOverrides(
        Dictionary<string, List<string>> supplement,
        Dictionary<string, Dictionary<string, string>> supplementTypes,
        Dictionary<string, List<string>> suppress)
    {
        _supplement = supplement;
        _supplementTypes = supplementTypes;
        _suppress = suppress;
    }

    public static DocDescriptorOverrides Empty { get; } = new(new(), new(), new());

    public static DocDescriptorOverrides Load(string path)
    {
        if (!File.Exists(path))
            return Empty;

        Dictionary<string, List<string>> supplement = new(StringComparer.Ordinal);
        Dictionary<string, Dictionary<string, string>> supplementTypes = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> suppress = new(StringComparer.Ordinal);

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonProperty top in doc.RootElement.EnumerateObject())
        {
            switch (top.Name)
            {
                case "_supplement":
                    ReadSupplementMap(top.Value, supplement, supplementTypes);
                    break;
                case "_suppress":
                    ReadListMap(top.Value, suppress);
                    break;
                default:
                    throw new JsonException($"Unknown data-descriptor override section '{top.Name}'.");
            }
        }

        return new DocDescriptorOverrides(supplement, supplementTypes, suppress);
    }

    public IReadOnlyList<string> Supplement(string contractShort, string version) =>
        GetEntries(_supplement, contractShort, version);

    public IReadOnlyList<string> Suppress(string contractShort, string version) =>
        GetEntries(_suppress, contractShort, version);

    public string? SupplementNativeType(string contractShort, string version, string key)
    {
        if (_supplementTypes.TryGetValue($"{contractShort}@{version}", out Dictionary<string, string>? versionTypes) &&
            versionTypes.TryGetValue(key, out string? versionType))
        {
            return versionType;
        }

        return _supplementTypes.TryGetValue(contractShort, out Dictionary<string, string>? contractTypes) &&
            contractTypes.TryGetValue(key, out string? contractType)
            ? contractType
            : null;
    }

    private static List<string> GetEntries(
        Dictionary<string, List<string>> entries,
        string contractShort,
        string version)
    {
        entries.TryGetValue(contractShort, out List<string>? contractEntries);
        entries.TryGetValue($"{contractShort}@{version}", out List<string>? versionEntries);

        return (contractEntries, versionEntries) switch
        {
            (null, null) => [],
            (not null, null) => contractEntries,
            (null, not null) => versionEntries,
            _ => [.. contractEntries, .. versionEntries],
        };
    }

    private static void ReadListMap(JsonElement obj, Dictionary<string, List<string>> into)
    {
        foreach (JsonProperty p in obj.EnumerateObject())
            into[p.Name] = ReadList(p.Value, $"'{p.Name}' override");
    }

    private static void ReadSupplementMap(
        JsonElement obj,
        Dictionary<string, List<string>> into,
        Dictionary<string, Dictionary<string, string>> types)
    {
        foreach (JsonProperty p in obj.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.Array)
            {
                into[p.Name] = ReadList(p.Value, $"'{p.Name}' supplement");
                continue;
            }

            if (p.Value.ValueKind != JsonValueKind.Object)
                throw new JsonException($"Expected an array or object for '{p.Name}' supplement.");

            Dictionary<string, string> typedEntries = new(StringComparer.Ordinal);
            foreach (JsonProperty entry in p.Value.EnumerateObject())
            {
                ValidateDescriptorKey(entry.Name, $"'{p.Name}' supplement");
                if (entry.Value.GetString() is not string nativeType || nativeType.Length == 0)
                    throw new JsonException($"Expected a native type for '{entry.Name}' in '{p.Name}' supplement.");
                typedEntries.Add(entry.Name, nativeType);
            }

            into[p.Name] = [.. typedEntries.Keys];
            types[p.Name] = typedEntries;
        }
    }

    private static List<string> ReadList(JsonElement entries, string context)
    {
        if (entries.ValueKind != JsonValueKind.Array)
            throw new JsonException($"Expected an array for {context}.");

        List<string> list = new();
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            if (entry.GetString() is not string key)
                throw new JsonException($"Expected a descriptor key in {context}.");
            ValidateDescriptorKey(key, context);
            list.Add(key);
        }

        return list;
    }

    private static void ValidateDescriptorKey(string key, string context)
    {
        int dot = key.LastIndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || dot == key.Length - 1)
            throw new JsonException($"Invalid data-descriptor key '{key}' in {context}; expected 'Type.Field'.");
    }
}
