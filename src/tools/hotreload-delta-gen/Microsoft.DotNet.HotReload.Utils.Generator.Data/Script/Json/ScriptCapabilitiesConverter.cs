// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Microsoft.DotNet.HotReload.Utils.Generator.Script.Json;

/// Deserialize capabilities as either a JSON string value, or an array of JSON string values
public class ScriptCapabilitiesConverter : JsonConverter<string> {
    public override bool HandleNull => true;

    public ScriptCapabilitiesConverter() {}

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.Null => string.Empty,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.StartArray => ReadCapsArray (ref reader, options),
            _ => throw new JsonException(),
        };

    private static string ReadCapsArray(ref Utf8JsonReader reader, JsonSerializerOptions options) {
        var elems = JsonSerializer.Deserialize<string[]>(ref reader, options);
        if (elems == null)
            throw new JsonException();
        return string.Join(' ', elems);
    }
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
