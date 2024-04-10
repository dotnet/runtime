// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    internal sealed partial class ContractDescriptorContext : JsonSerializerContext
    {
    }

    // TODO: fix the key names to use lowercase
    public class CompactContractDescriptor
    {
        public int Version { get; set; }
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

}
