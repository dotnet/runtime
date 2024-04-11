// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.DataContractReader;
public partial class ContractDescriptorParser
{
    public const string TypeDescriptorSizeSigil = "!";

    public static CompactContractDescriptor? Parse(ReadOnlySpan<byte> json)
    {
        return JsonSerializer.Deserialize(json, ContractDescriptorContext.Default.CompactContractDescriptor);
    }

    [JsonSerializable(typeof(CompactContractDescriptor))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    [JsonSerializable(typeof(Dictionary<string, TypeDescriptor>))]
    [JsonSerializable(typeof(Dictionary<string, FieldDescriptor>))]
    [JsonSerializable(typeof(TypeDescriptor))]
    [JsonSerializable(typeof(FieldDescriptor))]
    [JsonSourceGenerationOptions(AllowTrailingCommas = true,
                                DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified, // contracts, types and globals are case sensitive
                                PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                                ReadCommentHandling = JsonCommentHandling.Skip)]
    internal sealed partial class ContractDescriptorContext : JsonSerializerContext
    {
    }

    public class CompactContractDescriptor
    {
        public int? Version { get; set; }
        public string? Baseline { get; set; }
        public Dictionary<string, int>? Contracts { get; set; }

        public Dictionary<string, TypeDescriptor>? Types { get; set; }

        // TODO: globals

        [JsonExtensionData]
        public Dictionary<string, object?>? Extras { get; set; }
    }

    [JsonConverter(typeof(TypeDescriptorConverter))]
    public class TypeDescriptor
    {
        public uint Size { get; set; }
        public Dictionary<string, FieldDescriptor>? Fields { get; set; }
    }

    // TODO: compact format needs a custom converter
    [JsonConverter(typeof(FieldDescriptorConverter))]
    public class FieldDescriptor
    {
        public string? Type { get; set; }
        public int Offset { get; set; }
    }

    internal sealed class TypeDescriptorConverter : JsonConverter<TypeDescriptor>
    {
        public override TypeDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            uint size = 0;
            Dictionary<string, FieldDescriptor>? fields = new();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        return new TypeDescriptor { Size = size, Fields = fields };
                    case JsonTokenType.PropertyName:
                        if (reader.GetString() == TypeDescriptorSizeSigil)
                        {
                            reader.Read();
                            size = reader.GetUInt32();
                            // FIXME: handle duplicates?
                        }
                        else
                        {
                            string? fieldName = reader.GetString();
                            reader.Read();
                            var field = JsonSerializer.Deserialize(ref reader, ContractDescriptorContext.Default.FieldDescriptor);
                            // FIXME: duplicates?
                            if (fieldName != null && field != null)
                                fields.Add(fieldName, field);
                            else
                                throw new JsonException();
                        }
                        break;
                    case JsonTokenType.Comment:
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
        public override FieldDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.String)
                return new FieldDescriptor { Offset = reader.GetInt32() };
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            int eltIdx = 0;
            string? type = null;
            int offset = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndArray:
                        return new FieldDescriptor { Type = type, Offset = offset };
                    case JsonTokenType.Comment:
                        // don't incrment eltIdx
                        continue;
                    default:
                        break;
                }
                switch (eltIdx)
                {
                    case 0:
                        {
                            // expect an offset - either a string or a number token
                            if (reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.String)
                                offset = reader.GetInt32();
                            else
                                throw new JsonException();
                            break;
                        }
                    case 1:
                        {
                            // expect a type - a string token
                            if (reader.TokenType == JsonTokenType.String)
                                type = reader.GetString();
                            else
                                throw new JsonException();
                            break;
                        }
                    default:
                        // too many elements
                        throw new JsonException();
                }
                eltIdx++;
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, FieldDescriptor value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

}
