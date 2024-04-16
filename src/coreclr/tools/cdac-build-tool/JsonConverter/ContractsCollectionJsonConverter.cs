// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;

public sealed class ContractsCollectionModelJsonConverter : CompactCollectionModelJsonConverter<DataDescriptorModel.ContractsCollectionModel, DataDescriptorModel.ContractModel>
{
    public override DataDescriptorModel.ContractsCollectionModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                    return new DataDescriptorModel.ContractsCollectionModel(builtContracts.ToDictionary());
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
}
