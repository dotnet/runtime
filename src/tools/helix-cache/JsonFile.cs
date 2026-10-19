// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace HelixCache;

internal static class JsonFile
{
    internal static T Read<T>(string path, JsonTypeInfo<T> type)
    {
        using FileStream stream = File.OpenRead(path);
        using JsonDocument document = JsonDocument.Parse(stream);
        using (Utf8JsonWriter validator = new(Stream.Null))
        {
            WriteCanonical(validator, document.RootElement);
        }
        return JsonSerializer.Deserialize(document.RootElement, type) ?? throw new InvalidDataException(SR.Get("InvalidField", path));
    }

    internal static void Write<T>(string path, T value, JsonTypeInfo<T> type)
    {
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, value, type);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    internal static string Digest(JsonElement value)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            WriteCanonical(writer, value);
        }
        return Convert.ToHexStringLower(SHA256.HashData(stream.GetBuffer().AsSpan(0, checked((int)stream.Length))));
    }

    internal static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                HashSet<string> names = new(StringComparer.Ordinal);
                foreach (JsonProperty property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!names.Add(property.Name))
                    {
                        throw new InvalidDataException(SR.Get("Duplicate", property.Name));
                    }
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in value.EnumerateArray())
                {
                    Require(item.ValueKind != JsonValueKind.Null, nameof(item));
                    WriteCanonical(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }

    internal static void ValidateVersion(int version)
    {
        if (version != Cache.ProtocolVersion)
        {
            throw new InvalidDataException(SR.Get("Version", version, Cache.ProtocolVersion));
        }
    }

    internal static void Require(bool condition, string field)
    {
        if (!condition)
        {
            throw new InvalidDataException(SR.Get("InvalidField", field));
        }
    }

    internal static void RelativePath(string path, bool allowEmpty = false)
    {
        Require(allowEmpty || !string.IsNullOrWhiteSpace(path), nameof(path));
        if (Path.IsPathRooted(path) || path.Split('/').Any(p => p is "." or "..") || path.Contains('\\'))
        {
            throw new InvalidDataException(SR.Get("RelativePath", path));
        }
    }
}
