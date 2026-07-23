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
    private readonly Dictionary<string, List<string>> _suppress;

    private DocDescriptorOverrides(
        Dictionary<string, List<string>> supplement,
        Dictionary<string, List<string>> suppress)
    {
        _supplement = supplement;
        _suppress = suppress;
    }

    public static DocDescriptorOverrides Empty { get; } = new(new(), new());

    public static DocDescriptorOverrides Load(string path)
    {
        if (!File.Exists(path))
            return Empty;

        Dictionary<string, List<string>> supplement = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> suppress = new(StringComparer.Ordinal);

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonProperty top in doc.RootElement.EnumerateObject())
        {
            switch (top.Name)
            {
                case "_supplement":
                    ReadListMap(top.Value, supplement);
                    break;
                case "_suppress":
                    ReadListMap(top.Value, suppress);
                    break;
                default:
                    throw new JsonException($"Unknown data-descriptor override section '{top.Name}'.");
            }
        }

        return new DocDescriptorOverrides(supplement, suppress);
    }

    public IReadOnlyList<string> Supplement(string contractShort, string version) =>
        GetEntries(_supplement, contractShort, version);

    public IReadOnlyList<string> Suppress(string contractShort, string version) =>
        GetEntries(_suppress, contractShort, version);

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
        {
            List<string> list = new();
            foreach (JsonElement v in p.Value.EnumerateArray())
            {
                if (v.GetString() is string key)
                {
                    ValidateDescriptorKey(key, $"'{p.Name}' override");
                    list.Add(key);
                }
            }
            into[p.Name] = list;
        }
    }

    private static void ValidateDescriptorKey(string key, string context)
    {
        int dot = key.LastIndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || dot == key.Length - 1)
            throw new JsonException($"Invalid data-descriptor key '{key}' in {context}; expected 'Type.Field'.");
    }
}
