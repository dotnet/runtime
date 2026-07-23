// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace CdacUsageGraph.Docs;

/// <summary>
/// The canonical "meaning" text for each <c>Type.Field</c> descriptor and global, plus
/// per-contract <c>_supplement</c>/<c>_suppress</c> overrides, loaded from
/// <c>docs/design/datacontracts/data-descriptor-meanings.json</c>. The file shape is:
/// <code>
/// {
///   "_fields": { "Thread.Id": "Thread identifier", ... },
///   "_globals": { "ThreadStore": "Pointer to the thread store", ... },
///   "_supplement": { "Thread": ["ExtraType.ExtraField"] },
///   "_suppress":   { "Thread": ["FalsePositiveType.Field"] }
/// }
/// </code>
/// </summary>
internal sealed class DocDescriptorMeanings
{
    private readonly Dictionary<string, string> _meanings;
    private readonly Dictionary<string, string> _globalMeanings;
    private readonly Dictionary<string, List<string>> _supplement;
    private readonly Dictionary<string, List<string>> _suppress;

    private DocDescriptorMeanings(
        Dictionary<string, string> meanings,
        Dictionary<string, string> globalMeanings,
        Dictionary<string, List<string>> supplement,
        Dictionary<string, List<string>> suppress)
    {
        _meanings = meanings;
        _globalMeanings = globalMeanings;
        _supplement = supplement;
        _suppress = suppress;
    }

    public static DocDescriptorMeanings Empty { get; } =
        new(new(), new(), new(), new());

    /// <summary>Loads the sidecar; returns <see cref="Empty"/> when the file does not exist.</summary>
    public static DocDescriptorMeanings Load(string path)
    {
        if (!File.Exists(path))
            return Empty;

        Dictionary<string, string> meanings = new(StringComparer.Ordinal);
        Dictionary<string, string> globalMeanings = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> supplement = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> suppress = new(StringComparer.Ordinal);

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonProperty top in doc.RootElement.EnumerateObject())
        {
            switch (top.Name)
            {
                case "_fields":
                    ReadStringMap(top.Value, meanings, validateDescriptorKeys: true);
                    break;
                case "_supplement":
                    ReadListMap(top.Value, supplement);
                    break;
                case "_globals":
                    ReadStringMap(top.Value, globalMeanings, validateDescriptorKeys: false);
                    break;
                case "_suppress":
                    ReadListMap(top.Value, suppress);
                    break;
                default:
                    throw new JsonException($"Unknown data-descriptor meanings section '{top.Name}'.");
            }
        }

        return new DocDescriptorMeanings(
            meanings,
            globalMeanings,
            supplement,
            suppress);
    }

    /// <summary>The meaning for <paramref name="key"/> (<c>Type.Field</c>), or a TODO placeholder.</summary>
    public string Meaning(string key) =>
        _meanings.TryGetValue(key, out string? meaning)
            ? meaning
            : "_TODO: describe_";

    public string GlobalMeaning(string global) =>
        _globalMeanings.TryGetValue(global, out string? meaning)
            ? meaning
            : "_TODO: describe_";

    public IReadOnlyList<string> Supplement(string contractShort) =>
        _supplement.TryGetValue(contractShort, out List<string>? l) ? l : [];

    public IReadOnlyList<string> Suppress(string contractShort) =>
        _suppress.TryGetValue(contractShort, out List<string>? l) ? l : [];

    private static void ReadListMap(JsonElement obj, Dictionary<string, List<string>> into)
    {
        foreach (JsonProperty p in obj.EnumerateObject())
        {
            List<string> list = new();
            foreach (JsonElement v in p.Value.EnumerateArray())
            {
                if (v.GetString() is string s)
                {
                    ValidateDescriptorKey(s, $"'{p.Name}' override");
                    list.Add(s);
                }
            }
            into[p.Name] = list;
        }
    }

    private static void ReadStringMap(
        JsonElement obj,
        Dictionary<string, string> into,
        bool validateDescriptorKeys)
    {
        foreach (JsonProperty entry in obj.EnumerateObject())
        {
            if (validateDescriptorKeys)
                ValidateDescriptorKey(entry.Name, "meaning entry");
            into[entry.Name] = entry.Value.GetString() ?? string.Empty;
        }
    }

    private static void ValidateDescriptorKey(string key, string context)
    {
        int dot = key.LastIndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || dot == key.Length - 1)
            throw new JsonException($"Invalid data-descriptor key '{key}' in {context}; expected 'Type.Field'.");
    }
}
