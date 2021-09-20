// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetBoolean();
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }

        internal override bool ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ReadOnlySpan<byte> propertyName = reader.GetSpan();
            if (Utf8Parser.TryParse(propertyName, out bool value, out int bytesConsumed)
                && propertyName.Length == bytesConsumed)
            {
                return value;
            }

            throw ThrowHelper.GetFormatException(DataType.Boolean);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, bool value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(value);
        }
    }
}
