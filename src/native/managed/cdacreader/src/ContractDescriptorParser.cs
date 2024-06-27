// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
///   A parser for the JSON representation of a contract descriptor.
/// </summary>
/// <remarks>
/// <see href="https://github.com/dotnet/runtime/blob/main/docs/design/datacontracts/data_descriptor.md">See design doc</see> for the format.
/// </remarks>
public partial class ContractDescriptorParser
{
    // data_descriptor.md uses a distinguished property name to indicate the size of a type
    public const string TypeDescriptorSizeSigil = "!";

    /// <summary>
    ///  Parses the "compact" representation of a contract descriptor.
    /// </summary>
    // Workaround for https://github.com/dotnet/runtime/issues/101205
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Root))]
    public static ContractDescriptor? ParseCompact(ReadOnlySpan<byte> json)
    {
        return JsonSerializer.Deserialize(json, ContractDescriptorContext.Default.ContractDescriptor);
    }

    [JsonSerializable(typeof(ContractDescriptor))]
    [JsonSerializable(typeof(int?))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    [JsonSerializable(typeof(Dictionary<string, TypeDescriptor>))]
    [JsonSerializable(typeof(Dictionary<string, FieldDescriptor>))]
    [JsonSerializable(typeof(Dictionary<string, GlobalDescriptor>))]
    [JsonSerializable(typeof(TypeDescriptor))]
    [JsonSerializable(typeof(FieldDescriptor))]
    [JsonSerializable(typeof(GlobalDescriptor))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSourceGenerationOptions(AllowTrailingCommas = true,
                                DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified, // contracts, types and globals are case sensitive
                                PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                                ReadCommentHandling = JsonCommentHandling.Skip,
                                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
                                UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
                                Converters = [typeof(TypeDescriptorConverter),
                                            typeof(FieldDescriptorConverter),
                                            typeof(GlobalDescriptorConverter)])]
    internal sealed partial class ContractDescriptorContext : JsonSerializerContext
    {
    }

    public class ContractDescriptor
    {
        public int? Version { get; set; }
        public string? Baseline { get; set; }
        public Dictionary<string, int>? Contracts { get; set; }

        public Dictionary<string, TypeDescriptor>? Types { get; set; }

        public Dictionary<string, GlobalDescriptor>? Globals { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extras { get; set; }

        public override string ToString()
        {
            return $"Version: {Version}, Baseline: {Baseline}, Contracts: {Contracts?.Count}, Types: {Types?.Count}, Globals: {Globals?.Count}";
        }

    }

    [JsonConverter(typeof(TypeDescriptorConverter))]
    public class TypeDescriptor
    {
        public uint? Size { get; set; }
        public Dictionary<string, FieldDescriptor>? Fields { get; set; }
    }

    [JsonConverter(typeof(FieldDescriptorConverter))]
    public class FieldDescriptor
    {
        public string? Type { get; set; }
        public int Offset { get; set; }
    }

    [JsonConverter(typeof(GlobalDescriptorConverter))]
    public class GlobalDescriptor
    {
        public string? Type { get; set; }
        public ulong Value { get; set; }
        public bool Indirect { get; set; }
    }

    internal sealed class TypeDescriptorConverter : JsonConverter<TypeDescriptor>
    {
        // Almost a normal dictionary converter except:
        //  1. looks for a special key "!" to set the Size property
        //  2. field names are property names, but treated case-sensitively
        public override TypeDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            uint? size = null;
            Dictionary<string, FieldDescriptor>? fields = new();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        return new TypeDescriptor { Size = size, Fields = fields };
                    case JsonTokenType.PropertyName:
                        string? fieldNameOrSizeSigil = reader.GetString();
                        reader.Read(); // read the next value: either a number or a field descriptor
                        if (fieldNameOrSizeSigil == TypeDescriptorSizeSigil)
                        {
                            uint newSize = reader.GetUInt32();
                            if (size is not null)
                            {
                                throw new JsonException($"Size specified multiple times: {size} and {newSize}");
                            }
                            size = newSize;
                        }
                        else
                        {
                            string? fieldName = fieldNameOrSizeSigil;
                            var field = JsonSerializer.Deserialize(ref reader, ContractDescriptorContext.Default.FieldDescriptor);
                            if (fieldName is null || field is null)
                                throw new JsonException();
                            if (!fields.TryAdd(fieldName, field))
                            {
                                throw new JsonException($"Duplicate field name: {fieldName}");
                            }
                        }
                        break;
                    case JsonTokenType.Comment:
                        // unexpected - we specified to skip comments.  but let's ignore anyway
                        break;
                    default:
                        throw new JsonException();
                }
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, TypeDescriptor value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class FieldDescriptorConverter : JsonConverter<FieldDescriptor>
    {
        // Compact Field descriptors are either a number or a two element array
        // 1. number - no type, offset is given as the number
        // 2. [number, string] - offset is given as the number, type name is given as the string
        public override FieldDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (TryGetInt32FromToken(ref reader, out int offset))
                return new FieldDescriptor { Offset = offset };
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            reader.Read();
            //   [number, string]
            //    ^ we're here
            if (!TryGetInt32FromToken(ref reader, out offset))
                throw new JsonException();
            reader.Read(); // string
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException();
            string? type = reader.GetString();
            reader.Read(); // end of array
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();
            return new FieldDescriptor { Type = type, Offset = offset };
        }

        public override void Write(Utf8JsonWriter writer, FieldDescriptor value, JsonSerializerOptions options)
        {
            throw new JsonException();
        }
    }

    internal sealed class GlobalDescriptorConverter : JsonConverter<GlobalDescriptor>
    {
        public override GlobalDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // four cases:
            // 1. number - no type, direct value, given value
            // 2. [number] - no type, indirect value, given aux data ptr
            // 3. [number, string] - type, direct value, given value
            // 4. [[number], string] - type, indirect value, given aux data ptr

            // Case 1: number
            if (TryGetUInt64FromToken(ref reader, out ulong valueCase1))
                return new GlobalDescriptor { Value = valueCase1 };
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            reader.Read();
            // we're in case 2 or 3 or 4:
            // case 2: [number]
            //          ^ we're here
            // case 3: [number, string]
            //          ^ we're here
            // case 4: [[number], string]
            //          ^ we're here
            if (TryGetUInt64FromToken(ref reader, out ulong valueCase2or3))
            {
                // case 2 or 3
                // case 2: [number]
                //          ^ we're here
                // case 3: [number, string]
                //          ^ we're here
                reader.Read(); // end of array (case 2) or string (case 3)
                if (reader.TokenType == JsonTokenType.EndArray) // it was case 2
                {
                    return new GlobalDescriptor { Value = valueCase2or3, Indirect = true };
                }
                if (reader.TokenType == JsonTokenType.String) // it was case 3
                {
                    string? type = reader.GetString();
                    reader.Read(); // end of array for case 3
                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new JsonException();
                    return new GlobalDescriptor { Type = type, Value = valueCase2or3 };
                }
                throw new JsonException();
            }
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // case 4: [[number], string]
                //          ^ we're here
                reader.Read(); // number
                if (!TryGetUInt64FromToken(ref reader, out ulong value))
                    throw new JsonException();
                reader.Read(); // end of inner array
                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException();
                reader.Read(); // string
                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException();
                string? type = reader.GetString();
                reader.Read(); // end of outer array
                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException();
                return new GlobalDescriptor { Type = type, Value = value, Indirect = true };
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, GlobalDescriptor value, JsonSerializerOptions options)
        {
            throw new JsonException();
        }
    }

    // Somewhat flexible parsing of numbers, allowing json number tokens or strings as decimal or hex, possibly negatated.
    private static bool TryGetUInt64FromToken(ref Utf8JsonReader reader, out ulong value)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt64(out value))
                return true;
            if (reader.TryGetInt64(out long signedValue))
            {
                value = (ulong)signedValue;
                return true;
            }
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (s == null)
            {
                value = 0u;
                return false;
            }
            if (ulong.TryParse(s, out value))
                return true;
            if (long.TryParse(s, out long signedValue))
            {
                value = (ulong)signedValue;
                return true;
            }
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                ulong.TryParse(s.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out value))
            {
                return true;
            }
            if (s.StartsWith("-0x", StringComparison.OrdinalIgnoreCase) &&
                ulong.TryParse(s.AsSpan(3), System.Globalization.NumberStyles.HexNumber, null, out ulong negValue))
            {
                value = ~negValue + 1; // two's complement
                return true;
            }
        }
        value = 0;
        return false;
    }

    // Somewhat flexible parsing of numbers, allowing json number tokens or strings as either decimal or hex, possibly negated
    private static bool TryGetInt32FromToken(ref Utf8JsonReader reader, out int value)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            value = reader.GetInt32();
            return true;
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (s == null)
            {
                value = 0;
                return false;
            }
            if (int.TryParse(s, out value))
            {
                return true;
            }
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(s.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out value))
            {
                return true;
            }
            if (s.StartsWith("-0x", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(s.AsSpan(3), System.Globalization.NumberStyles.HexNumber, null, out int negValue))
            {
                value = -negValue;
                return true;
            }
        }
        value = 0;
        return false;
    }
}
