// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed record RunArgumentsJson(
    string[] applicationArguments,
    string[]? runtimeArguments = null,
    IDictionary<string, string>? environmentVariables = null,
    bool forwardConsoleToWS = false,
    bool debugging = false
)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // using an explicit property because the deserializer doesn't like
    // extension data in the record constructor
    [property: JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }

    public void Save(string file)
    {
        string json = JsonSerializer.Serialize(this, s_jsonOptions);
        File.WriteAllText(file, json);
    }
}
