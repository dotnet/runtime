// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;

/// <summary>
///  Parses or serializes contracts in the "compact" JSON format: as a single
///  object for the whole collection where each contract name is a property name and
///  the value is the version number.
/// </summary>
public class ContractCollectionModelJsonConverter : JsonConverter<DataDescriptorModel.ContractCollectionModel>
{
    public override DataDescriptorModel.ContractCollectionModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Dictionary<string, DataDescriptorModel.ContractBuilder> contracts = new();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                    var builtContracts = contracts.Select((kvp) => (kvp.Key, kvp.Value.Build()));
                    return new DataDescriptorModel.ContractCollectionModel(builtContracts.ToDictionary());
                case JsonTokenType.PropertyName:
                    string? propertyName = reader.GetString();
                    if (propertyName is null)
                    {
                        throw new JsonException();
                    }
                    reader.Read();
                    int version = reader.GetInt32();
                    contracts.Add(propertyName, new DataDescriptorModel.ContractBuilder { Version = version });
                    break;
                case JsonTokenType.Comment:
                    break;
                default:
                    throw new JsonException();
            }
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DataDescriptorModel.ContractCollectionModel value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (name, contract) in value.Contracts)
        {
            writer.WriteNumber(name, contract.Version);

        }
        writer.WriteEndObject();
    }
}
