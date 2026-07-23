// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace CdacUsageGraph.Docs;

/// <summary>
/// The canonical "meaning" text for each <c>Type.Field</c> descriptor and global, loaded from
/// <c>docs/design/datacontracts/data-descriptor-meanings.json</c>. The file shape is:
/// <code>
/// {
///   "_fields": { "Thread.Id": "Thread identifier", ... },
///   "_globals": { "ThreadStore": "Pointer to the thread store", ... }
/// }
/// </code>
/// </summary>
internal sealed class DocDescriptorMeanings
{
    private readonly Dictionary<string, string> _meanings;
    private readonly Dictionary<string, string> _globalMeanings;

    private DocDescriptorMeanings(
        Dictionary<string, string> meanings,
        Dictionary<string, string> globalMeanings)
    {
        _meanings = meanings;
        _globalMeanings = globalMeanings;
    }

    public static DocDescriptorMeanings Empty { get; } =
        new(new(), new());

    /// <summary>Loads the sidecar; returns <see cref="Empty"/> when the file does not exist.</summary>
    public static DocDescriptorMeanings Load(string path)
    {
        if (!File.Exists(path))
            return Empty;

        Dictionary<string, string> meanings = new(StringComparer.Ordinal);
        Dictionary<string, string> globalMeanings = new(StringComparer.Ordinal);

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonProperty top in doc.RootElement.EnumerateObject())
        {
            switch (top.Name)
            {
                case "_fields":
                    ReadStringMap(top.Value, meanings, validateDescriptorKeys: true);
                    break;
                case "_globals":
                    ReadStringMap(top.Value, globalMeanings, validateDescriptorKeys: false);
                    break;
                default:
                    throw new JsonException($"Unknown data-descriptor meanings section '{top.Name}'.");
            }
        }

        return new DocDescriptorMeanings(meanings, globalMeanings);
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
