// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

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
    public static ContractDescriptor? ParseCompact(ReadOnlySpan<byte> json)
    {
        try
        {
            return ParseCompactCore(json);
        }
        catch (JsonException ex) when (ex.GetType() != typeof(JsonException))
        {
            throw new JsonException(ex.Message, ex);
        }
    }

    private static ContractDescriptor? ParseCompactCore(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(
            json,
            new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

        if (!reader.Read())
            throw new JsonException();
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var descriptor = new ContractDescriptor();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (reader.Read())
                    throw new JsonException();
                return descriptor;
            }
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string propertyName = reader.GetString() ?? throw new JsonException();
            ReadNext(ref reader);
            switch (propertyName)
            {
                case "version":
                    descriptor.Version = ReadNullableInt32(ref reader);
                    break;
                case "baseline":
                    descriptor.Baseline = ReadNullableString(ref reader);
                    break;
                case "contracts":
                    descriptor.Contracts = ReadStringDictionary(ref reader);
                    break;
                case "types":
                    descriptor.Types = ReadTypeDictionary(ref reader);
                    break;
                case "globals":
                    descriptor.Globals = ReadGlobalDictionary(ref reader);
                    break;
                case "subDescriptors":
                    descriptor.SubDescriptors = ReadGlobalDictionary(ref reader);
                    break;
                default:
                    descriptor.Extras ??= [];
                    using (JsonDocument extra = JsonDocument.ParseValue(ref reader))
                    {
                        descriptor.Extras[propertyName] = extra.RootElement.Clone();
                    }
                    break;
            }
        }

        throw new JsonException();
    }

    public class ContractDescriptor
    {
        public int? Version { get; set; }
        public string? Baseline { get; set; }
        public Dictionary<string, string>? Contracts { get; set; }

        public Dictionary<string, TypeDescriptor>? Types { get; set; }

        public Dictionary<string, GlobalDescriptor>? Globals { get; set; }

        public Dictionary<string, GlobalDescriptor>? SubDescriptors { get; set; }

        public Dictionary<string, JsonElement>? Extras { get; set; }

        public override string ToString()
        {
            return $"Version: {Version}, Baseline: {Baseline}, Contracts: {Contracts?.Count}, Types: {Types?.Count}, Globals: {Globals?.Count}, SubDescriptors: {SubDescriptors?.Count}";
        }

    }

    public class TypeDescriptor
    {
        public uint? Size { get; set; }
        public Dictionary<string, FieldDescriptor>? Fields { get; set; }
    }

    public class FieldDescriptor
    {
        public string? Type { get; set; }
        public int Offset { get; set; }
    }

    public class GlobalDescriptor
    {
        [MemberNotNullWhen(true, nameof(NumericValue))]
        public bool Indirect { get; set; }
        public string? Type { get; set; }

        // When the descriptor is indirect, NumericValue must be non-null to point to the actual data
        public ulong? NumericValue { get; set; }
        public string? StringValue { get; set; }
    }

    private static Dictionary<string, string>? ReadStringDictionary(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var values = new Dictionary<string, string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return values;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string name = reader.GetString() ?? throw new JsonException();
            ReadNext(ref reader);
            string value = reader.TokenType == JsonTokenType.String
                ? reader.GetString() ?? throw new JsonException()
                : throw new JsonException();
            values[name] = value;
        }

        throw new JsonException();
    }

    private static Dictionary<string, TypeDescriptor>? ReadTypeDictionary(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var types = new Dictionary<string, TypeDescriptor>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return types;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string name = reader.GetString() ?? throw new JsonException();
            ReadNext(ref reader);
            types[name] = ReadTypeDescriptor(ref reader);
        }

        throw new JsonException();
    }

    private static TypeDescriptor ReadTypeDescriptor(ref Utf8JsonReader reader)
    {
        // Almost a normal dictionary except:
        //  1. "!" specifies the type size.
        //  2. All other property names are case-sensitive field names.
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        uint? size = null;
        var fields = new Dictionary<string, FieldDescriptor>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new TypeDescriptor { Size = size, Fields = fields };
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string fieldName = reader.GetString() ?? throw new JsonException();
            ReadNext(ref reader);
            if (fieldName == TypeDescriptorSizeSigil)
            {
                uint newSize = reader.GetUInt32();
                if (size is not null)
                    throw new JsonException($"Size specified multiple times: {size} and {newSize}");
                size = newSize;
            }
            else if (!fields.TryAdd(fieldName, ReadFieldDescriptor(ref reader)))
            {
                throw new JsonException($"Duplicate field name: {fieldName}");
            }
        }

        throw new JsonException();
    }

    private static FieldDescriptor ReadFieldDescriptor(ref Utf8JsonReader reader)
    {
        // Compact field descriptors are either:
        //  1. number - no type, offset is given as the number.
        //  2. [number, string] - offset is the number and type name is the string.
        if (TryGetInt32FromToken(ref reader, out int offset))
            return new FieldDescriptor { Offset = offset };
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        ReadNext(ref reader);
        if (!TryGetInt32FromToken(ref reader, out offset))
            throw new JsonException();
        ReadNext(ref reader);
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();
        string type = reader.GetString() ?? throw new JsonException();
        ReadNext(ref reader);
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException();
        return new FieldDescriptor { Type = type, Offset = offset };
    }

    private static Dictionary<string, GlobalDescriptor>? ReadGlobalDictionary(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var globals = new Dictionary<string, GlobalDescriptor>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return globals;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string name = reader.GetString() ?? throw new JsonException();
            ReadNext(ref reader);
            globals[name] = ReadGlobalDescriptor(ref reader);
        }

        throw new JsonException();
    }

    private static GlobalDescriptor ReadGlobalDescriptor(ref Utf8JsonReader reader)
    {
        // Compact global descriptors have four forms:
        //  1. value - no type, direct value.
        //  2. [value] - no type, indirect value.
        //  3. [value, string] - typed direct value.
        //  4. [[value], string] - typed indirect value.
        // A value can be a string or number. Numeric strings are retained as strings and parsed as numbers.
        if (TryGetGlobalValueFromToken(ref reader, out GlobalValue directValue))
            return CreateGlobalDescriptor(directValue, indirect: false, type: null);
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        ReadNext(ref reader);
        if (TryGetGlobalValueFromToken(ref reader, out GlobalValue value))
        {
            ReadNext(ref reader);
            if (reader.TokenType == JsonTokenType.EndArray)
                return CreateGlobalDescriptor(value, indirect: true, type: null);
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException();

            string type = reader.GetString() ?? throw new JsonException();
            ReadNext(ref reader);
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();
            return CreateGlobalDescriptor(value, indirect: false, type);
        }

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        ReadNext(ref reader);
        if (!TryGetGlobalValueFromToken(ref reader, out value))
            throw new JsonException();
        ReadNext(ref reader);
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException();
        ReadNext(ref reader);
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();
        string indirectType = reader.GetString() ?? throw new JsonException();
        ReadNext(ref reader);
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException();
        return CreateGlobalDescriptor(value, indirect: true, indirectType);
    }

    private static GlobalDescriptor CreateGlobalDescriptor(GlobalValue value, bool indirect, string? type)
    {
        if (indirect && value.NumericValue is null)
            throw new JsonException("Indirect global value could not be converted to a number.");

        return new GlobalDescriptor
        {
            Type = type,
            NumericValue = value.NumericValue,
            StringValue = value.StringValue,
            Indirect = indirect,
        };
    }

    private static int? ReadNullableInt32(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (TryGetInt32FromToken(ref reader, out int value))
            return value;
        throw new JsonException();
    }

    private static string? ReadNullableString(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString();
        throw new JsonException();
    }

    private static void ReadNext(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
            throw new JsonException();
    }

    private struct GlobalValue
    {
        public ulong? NumericValue;
        public string? StringValue;
    }

    private static bool TryGetGlobalValueFromToken(ref Utf8JsonReader reader, out GlobalValue directGlobalValue)
    {
        bool foundNumeric = TryGetUInt64FromToken(ref reader, out ulong numericValue);
        bool foundString = TryGetStringFromToken(ref reader, out string stringValue);
        if (foundNumeric || foundString)
        {
            // this parsed as a valid direct global value
            directGlobalValue = new GlobalValue
            {
                NumericValue = foundNumeric ? numericValue : null,
                StringValue = foundString ? stringValue : null
            };
            return true;
        }
        directGlobalValue = default;
        return false;
    }

    private static bool TryGetStringFromToken(ref Utf8JsonReader reader, out string value)
    {
        value = string.Empty;
        if (reader.TokenType == JsonTokenType.String && reader.GetString() is string stringValue)
        {
            value = stringValue;
            return true;
        }
        return false;
    }

    // Somewhat flexible parsing of numbers, allowing json number tokens or strings as decimal or hex, possibly negated.
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

    // Somewhat flexible parsing of numbers, allowing json number tokens or strings as decimal or hex, possibly negated.
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
