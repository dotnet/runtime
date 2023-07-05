// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace System
{
    internal sealed class BinaryDataConverter : JsonConverter<BinaryData>
    {
        public sealed override BinaryData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return BinaryData.FromBytes(reader.GetBytesFromBase64());
        }

        public sealed override void Write(Utf8JsonWriter writer, BinaryData value, JsonSerializerOptions options)
        {
            writer.WriteBase64StringValue(value.ToMemory().Span);
        }
    }
}
