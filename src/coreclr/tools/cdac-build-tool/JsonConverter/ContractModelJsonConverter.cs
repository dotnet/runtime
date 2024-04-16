// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;

public class ContractModelJsonConverter : JsonConverter<DataDescriptorModel.ContractModel>
{
    public override DataDescriptorModel.ContractModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var version = reader.GetInt32();
        return new DataDescriptorModel.ContractModel { Version = version };
    }

    public override void Write(Utf8JsonWriter writer, DataDescriptorModel.ContractModel value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Version);
    }
}
