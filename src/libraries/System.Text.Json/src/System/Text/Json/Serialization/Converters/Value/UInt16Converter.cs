// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class UInt16Converter : JsonConverter<ushort>
    {
        public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetUInt16();
        }

        public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options)
        {
            // For performance, lift up the writer implementation.
            writer.WriteNumberValue((long)value);
        }

        internal override ushort ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetUInt16WithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }
    }
}
