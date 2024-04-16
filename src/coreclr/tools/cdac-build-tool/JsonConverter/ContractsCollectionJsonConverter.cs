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
        DataDescriptorModel.ContractsCollctionBuilder builder = new();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                    return builder.Build();
                case JsonTokenType.PropertyName:
                    string? propertyName = reader.GetString();
                    if (propertyName is null)
                    {
                        throw new JsonException();
                    }
                    var contract = JsonSerializer.Deserialize<DataDescriptorModel.ContractModel>(ref reader, options);
                    builder.AddOrUpdateContract(propertyName, contract);
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
