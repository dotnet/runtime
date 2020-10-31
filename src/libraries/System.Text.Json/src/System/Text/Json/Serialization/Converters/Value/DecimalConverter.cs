// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DecimalConverter : JsonConverter<decimal>
    {
        public DecimalConverter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override decimal ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetDecimalWithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override decimal ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling)
        {
            if (reader.TokenType == JsonTokenType.String &&
                (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return reader.GetDecimalWithQuotes();
            }

            return reader.GetDecimal();
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, decimal value, JsonNumberHandling handling)
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
