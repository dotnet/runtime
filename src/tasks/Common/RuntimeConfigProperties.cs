// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

internal static class RuntimeConfigProperties
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters =
        {
            new StringConverter()
        }
    };

    public static Dictionary<string, string> Read(string? path, params string[] reservedProperties)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return new Dictionary<string, string>();
        }

        Root? root = JsonSerializer.Deserialize<Root>(File.ReadAllText(path), s_jsonOptions);
        Dictionary<string, string>? properties = root?.RuntimeOptions?.ConfigProperties;
        if (properties is null)
        {
            throw new InvalidDataException($"The runtime configuration '{path}' does not contain runtimeOptions.configProperties.");
        }

        foreach (string key in reservedProperties)
        {
            ThrowIfReserved(properties, key, path);
        }

        return properties;
    }

    public static string ToCStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\a':
                    builder.Append("\\a");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\v':
                    builder.Append("\\v");
                    break;
                case < ' ':
                case '\u007F':
                    builder.Append('\\');
                    builder.Append(Convert.ToString(c, 8).PadLeft(3, '0'));
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static void ThrowIfReserved(Dictionary<string, string> properties, string key, string path)
    {
        if (properties.ContainsKey(key))
        {
            throw new InvalidDataException($"The runtime configuration '{path}' cannot set the host-reserved property '{key}'.");
        }
    }

    private sealed class RuntimeOptions
    {
        [JsonPropertyName("configProperties")]
        public Dictionary<string, string>? ConfigProperties { get; set; }
    }

    private sealed class Root
    {
        [JsonPropertyName("runtimeOptions")]
        public RuntimeOptions? RuntimeOptions { get; set; }
    }

    private sealed class StringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt32().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.String => reader.GetString()!,
                _ => throw new JsonException($"Runtime configuration property values must be strings, booleans, or 32-bit integers, not {reader.TokenType}.")
            };

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }
}
