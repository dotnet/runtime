// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;
public class GlobalModelJsonConverter : JsonConverter<DataDescriptorModel.GlobalModel>
{
    public override DataDescriptorModel.GlobalModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DataDescriptorModel.GlobalModel value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value.Type))
        {
            // no type: just write 'value' or '[value]'
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            // there's a type. Write: [value, type] or [[value], type]
            writer.WriteStartArray();
            JsonSerializer.Serialize(writer, value.Value, options);
            writer.WriteStringValue(value.Type);
            writer.WriteEndArray();
        }
    }
}
