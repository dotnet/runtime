// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class SByteConverter : JsonConverter<sbyte>
    {
        public SByteConverter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override sbyte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetSByte();
        }

        public override void Write(Utf8JsonWriter writer, sbyte value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override sbyte ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetSByteWithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, sbyte value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override sbyte ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling)
        {
            if (reader.TokenType == JsonTokenType.String &&
                (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return reader.GetSByteWithQuotes();
            }

            return reader.GetSByte();
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, sbyte value, JsonNumberHandling handling)
        {
            if ((JsonNumberHandling.WriteAsString & handling) != 0)
            {
                writer.WriteNumberValueAsString(value);
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}
