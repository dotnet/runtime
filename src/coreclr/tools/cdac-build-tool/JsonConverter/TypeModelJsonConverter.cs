// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;
public class TypeModelJsonConverter : JsonConverter<DataDescriptorModel.TypeModel>
{
    public const string SizePropertyname = "!";

    public override DataDescriptorModel.TypeModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DataDescriptorModel.TypeModel value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Size is not null)
        {
            writer.WriteNumber(SizePropertyname, value.Size.Value);
        }
        foreach (var (fieldName, field) in value.Fields)
        {
            writer.WritePropertyName(fieldName);
            JsonSerializer.Serialize(writer, field, options);
        }
        writer.WriteEndObject();
    }
}
