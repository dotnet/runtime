// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection.Metadata;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class RuntimeConfigParserTask : Task
{
    /// <summary>
    /// The path to runtimeconfig.json file.
    /// </summary>
    [Required]
    public string RuntimeConfigFile { get; set; } = "";

    /// <summary>
    /// The path to the output binary file.
    /// </summary>
    [Required]
    public string OutputFile { get; set; } = "";

    /// <summary>
    /// List of properties reserved for the host.
    /// </summary>
    public ITaskItem[] RuntimeConfigReservedProperties { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        if (string.IsNullOrEmpty(RuntimeConfigFile))
        {
            Log.LogError($"'{nameof(RuntimeConfigFile)}' is required.");
            return false;
        }

        if (string.IsNullOrEmpty(OutputFile))
        {
            Log.LogError($"'{nameof(OutputFile)}' is required.");
            return false;
        }

        if (!TryConvertInputToDictionary(RuntimeConfigFile, out Dictionary<string, string> configProperties))
        {
            return false;
        }

        if (RuntimeConfigReservedProperties.Length != 0)
        {
            CheckDuplicateProperties(configProperties, RuntimeConfigReservedProperties);
        }

        var blobBuilder = new BlobBuilder();
        ConvertDictionaryToBlob(configProperties, blobBuilder);

        Directory.CreateDirectory(Path.GetDirectoryName(OutputFile!)!);
        using var stream = File.OpenWrite(OutputFile);
        blobBuilder.WriteContentTo(stream);

        return !Log.HasLoggedErrors;
    }

    /// Reads a json file from the given path and extracts the "configProperties" key (assumed to be a string to string dictionary)
    private bool TryConvertInputToDictionary(string inputFilePath, out Dictionary<string, string> result)
    {
        var init_result = new Dictionary<string, string>();
        init_result.Clear();
        result = init_result;

        var options = new JsonSerializerOptions {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters =
            {
                new StringConverter()
            }
        };

        var jsonString = File.ReadAllText(inputFilePath);
        var parsedJson = JsonSerializer.Deserialize<Root>(jsonString, options);

        if (parsedJson == null)
        {
            Log.LogError("Wasn't able to parse the json file successfully.");
            return false;
        }
        if (parsedJson.RuntimeOptions == null)
        {
            Log.LogError("Key runtimeOptions wasn't found in the json file.");
            return false;
        }
        if (parsedJson.RuntimeOptions.ConfigProperties == null)
        {
            Log.LogError("Key runtimeOptions->configProperties wasn't found in the json file.");
            return false;
        }

        result = parsedJson.RuntimeOptions.ConfigProperties;
        return true;
    }

    /// Just write the dictionary out to a blob as a count followed by
    /// a length-prefixed UTF8 encoding of each key and value
    private void ConvertDictionaryToBlob(IReadOnlyDictionary<string, string> properties, BlobBuilder builder)
    {
        int count = properties.Count;

        builder.WriteCompressedInteger(count);
        foreach (var kvp in properties)
        {
            builder.WriteSerializedString(kvp.Key);
            builder.WriteSerializedString(kvp.Value);
        }
    }

    private void CheckDuplicateProperties(IReadOnlyDictionary<string, string> properties, ITaskItem[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.ContainsKey(key.ItemSpec))
            {
                Log.LogError($"Property '{key}' can't be set by the user!");
            }
        }
    }
}

public class RuntimeOption
{
    // the configProperties key
    [JsonPropertyName("configProperties")]
    public Dictionary<string, string> ConfigProperties { get; set; } = new Dictionary<string, string>();
    // everything other than configProperties
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionDataSub { get; set; } = new Dictionary<string, object>();
}

public class Root
{
    // the runtimeOptions key
    [JsonPropertyName("runtimeOptions")]
    public RuntimeOption RuntimeOptions { get; set; } = new RuntimeOption();
    // everything other than runtimeOptions
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionDataRoot { get; set; } = new Dictionary<string, object>();
}

public class StringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                var stringValueInt = reader.GetInt32();
                return stringValueInt.ToString();
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (stringValue != null)
                {
                    return stringValue;
                }
                break;
            default:
                throw new System.Text.Json.JsonException();
        }

        throw new System.Text.Json.JsonException();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
