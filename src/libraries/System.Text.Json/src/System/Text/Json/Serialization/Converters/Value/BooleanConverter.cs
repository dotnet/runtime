// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class BooleanConverter : JsonPrimitiveConverter<bool>
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
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            ReadOnlySpan<byte> propertyName = reader.GetUnescapedSpan();
            if (!(Utf8Parser.TryParse(propertyName, out bool value, out int bytesConsumed)
                  && propertyName.Length == bytesConsumed))
            {
                ThrowHelper.ThrowFormatException(DataType.Boolean);
            }

            return value;
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, bool value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(value);
        }
    }
}
