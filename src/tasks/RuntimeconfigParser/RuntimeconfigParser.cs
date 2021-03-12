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


public class RuntimeconfigParserTask : Task
{
    /// <summary>
    /// The path to runtimeconfig.json file.
    /// </summary>
    [Required]
    public string RuntimeConfigFile { get; set; } = ""!;

    /// <summary>
    /// The path to the output binary file.
    /// </summary>
    [Required]
    public string OutputFile { get; set; } = ""!;

    /// <summary>
    /// List of reserved properties.
    /// </summary>
    public ITaskItem[] ReservedProperties { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        if (string.IsNullOrEmpty(RuntimeConfigFile))
        {
            throw new ArgumentException($"'{nameof(RuntimeConfigFile)}' is required.", nameof(RuntimeConfigFile));
        }

        if (string.IsNullOrEmpty(OutputFile))
        {
            throw new ArgumentException($"'{nameof(OutputFile)}' is required.", nameof(OutputFile));
        }

        Dictionary<string, string> configProperties = ConvertInputToDictionary (RuntimeConfigFile);

        if (ReservedProperties.Length != 0)
        {
            checkDuplicateProperties (configProperties, ReservedProperties);
        }

        var blobBuilder = new BlobBuilder();
        ConvertDictionaryToBlob (configProperties, blobBuilder);

        using var stream = File.OpenWrite(OutputFile);
        blobBuilder.WriteContentTo(stream);

        return true;
    }

    /// Reads a json file from the given path and extracts the "configProperties" key (assumed to be a string to string dictionary)
    private Dictionary<string, string> ConvertInputToDictionary(string inputFilePath)
    {
        var options = new JsonSerializerOptions {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        var jsonString = File.ReadAllText(inputFilePath);
        var parsedJson = JsonSerializer.Deserialize<Root>(jsonString, options);

        return parsedJson!.ConfigProperties;
    }

    /// Just write the dictionary out to a blob as a count followed by
    /// a length-prefixed UTF8 encoding of each key and value
    private void ConvertDictionaryToBlob (IReadOnlyDictionary<string, string> properties, BlobBuilder b)
    {
        int count = properties.Count;

        b.WriteCompressedInteger (count);
        foreach (var kvp in properties)
        {
            b.WriteSerializedString (kvp.Key);
            b.WriteSerializedString (kvp.Value);
        }
    }

    private void checkDuplicateProperties (IReadOnlyDictionary<string, string> properties, ITaskItem[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.ContainsKey (key.ItemSpec))
            {
                throw new ArgumentException($"Property '{key}' can't be set by the user!");
            }
        }
    }
}

public class Root {
    // the configProperties key
    [JsonPropertyName ("configProperties")]
    public Dictionary<string, string> ConfigProperties {get; set;} = new Dictionary<string, string> ();
    // everything other than configProperties
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData {get; set;} = new Dictionary<string, object> ();
}
