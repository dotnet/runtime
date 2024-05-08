// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;

/// <summary>
///  Writes a FieldModel in the compact form of [offset, type] or just offset if type is null.
///  <see cref="https://github.com/dotnet/runtime/blob/main/docs/design/datacontracts/data_descriptor.md"/>.
/// </summary>
public class FieldModelJsonConverter : JsonConverter<DataDescriptorModel.FieldModel>
{
    public override DataDescriptorModel.FieldModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DataDescriptorModel.FieldModel value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value.Type))
        {
            writer.WriteNumberValue(value.Offset);
        }
        else
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Offset);
            writer.WriteStringValue(value.Type);
            writer.WriteEndArray();
        }
    }
}
