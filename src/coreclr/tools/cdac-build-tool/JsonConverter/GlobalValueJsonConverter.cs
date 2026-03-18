// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;
public class GlobalValueJsonConverter : JsonConverter<DataDescriptorModel.GlobalValue>
{
    public override DataDescriptorModel.GlobalValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DataDescriptorModel.GlobalValue value, JsonSerializerOptions options)
    {
        switch (value.Kind)
        {
            case DataDescriptorModel.GlobalValue.KindEnum.Direct:
                // no type: just write value as a number.
                // we always write as a string containing a hex number
                writer.WriteStringValue($"0x{value.NumericValue:x}");
                break;
            case DataDescriptorModel.GlobalValue.KindEnum.Indirect:
                // pointer data index.  write as a 1-element array containing a decimal number
                writer.WriteStartArray();
                writer.WriteNumberValue(value.NumericValue);
                writer.WriteEndArray();
                break;
            case DataDescriptorModel.GlobalValue.KindEnum.String:
                // string data. write as a JSON string value
                writer.WriteStringValue(value.StringValue);
                break;
            default:
                throw new InvalidOperationException("Unknown GlobalValue type");
        }
    }
}
