// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

internal static class JsonToItemsReader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public static bool TryRead(string? jsonFilePath, TaskLoggingHelper log, [NotNullWhen(true)] out JsonModelRoot? json)
    {
        json = null;

        if (jsonFilePath is null)
        {
            log.LogError("no JsonFilePath specified");
            return false;
        }

        if (!File.Exists(jsonFilePath))
        {
            log.LogError($"Could not find JsonFilePath={jsonFilePath}");
            return false;
        }

        FileStream? file = null;
        try
        {
            try
            {
                file = File.OpenRead(jsonFilePath);
            }
            catch (FileNotFoundException exception)
            {
                log.LogErrorFromException(exception);
                return false;
            }

            json = GetJsonAsync(jsonFilePath, file, log).Result;
            if (json is null)
            {
                if (!log.HasLoggedErrors)
                {
                    log.LogError($"Failed to deserialize json from file {jsonFilePath}");
                }

                return false;
            }

            return true;
        }
        finally
        {
            file?.Dispose();
        }
    }

    private static async Task<JsonModelRoot?> GetJsonAsync(string jsonFilePath, FileStream file, TaskLoggingHelper log)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<JsonModelRoot>(file, s_jsonOptions).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            log.LogError($"Failed to deserialize json from file '{jsonFilePath}', JSON Path: {exception.Path}, Line: {exception.LineNumber}, Position: {exception.BytePositionInLine}");
            log.LogErrorFromException(exception, showStackTrace: false, showDetail: true, file: null);
            return null;
        }
    }
}

internal sealed class JsonModelRoot
{
    [JsonConverter(typeof(CaseInsensitiveDictionaryConverter))]
    public Dictionary<string, string>? Properties { get; set; }

    public Dictionary<string, JsonModelItem[]>? Items { get; set; }

    public ITaskItem[]? GetItems(string name)
    {
        if (Items is null || !Items.TryGetValue(name, out JsonModelItem[]? itemModels))
        {
            return null;
        }

        var items = new ITaskItem[itemModels.Length];
        for (int i = 0; i < itemModels.Length; i++)
        {
            JsonModelItem itemModel = itemModels[i];
            var item = new TaskItem(itemModel.Identity);
            if (itemModel.Metadata is not null)
            {
                foreach (KeyValuePair<string, string> metadata in itemModel.Metadata)
                {
                    item.SetMetadata(metadata.Key, metadata.Value);
                }
            }

            items[i] = item;
        }

        return items;
    }
}

[JsonConverter(typeof(JsonModelItemConverter))]
internal sealed class JsonModelItem
{
    public JsonModelItem(string identity, Dictionary<string, string>? metadata)
    {
        Identity = identity;
        Metadata = metadata;
    }

    public string Identity { get; }

    public Dictionary<string, string>? Metadata { get; }
}

internal sealed class CaseInsensitiveDictionaryConverter : JsonConverter<Dictionary<string, string>>
{
    public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Dictionary<string, string>? dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
        if (dictionary is null)
        {
            return null!;
        }

        return new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, string>? value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}

internal sealed class JsonModelItemConverter : JsonConverter<JsonModelItem>
{
    public override JsonModelItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                string? stringItem = reader.GetString();
                if (stringItem is null || stringItem.Length == 0)
                {
                    throw new JsonException("deserialized json string item was null or the empty string");
                }

                return new JsonModelItem(stringItem, metadata: null);

            case JsonTokenType.StartObject:
                Dictionary<string, string>? dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                if (dictionary is null)
                {
                    return null!;
                }

                var caseInsensitiveDictionary = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
                if (!caseInsensitiveDictionary.TryGetValue("Identity", out string? identity))
                {
                    throw new JsonException("deserialized json dictionary item did not have a non-empty Identity metadata");
                }

                if (identity is null || identity.Length == 0)
                {
                    throw new JsonException("deserialized json dictionary item did not have a non-empty Identity metadata");
                }

                caseInsensitiveDictionary.Remove("Identity");
                return new JsonModelItem(identity, caseInsensitiveDictionary);

            default:
                throw new NotSupportedException();
        }
    }

    public override void Write(Utf8JsonWriter writer, JsonModelItem value, JsonSerializerOptions options)
    {
        if (value.Metadata is null)
        {
            JsonSerializer.Serialize(writer, value.Identity);
        }
        else
        {
            JsonSerializer.Serialize(writer, value.Metadata);
        }
    }
}
