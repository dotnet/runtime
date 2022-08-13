// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int128Converter : JsonConverter<Int128>
    {
        public Int128Converter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override Int128 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInt128();
        }

        public override void Write(Utf8JsonWriter writer, Int128 value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override Int128 ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInt128WithQuotes();
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Int128 value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(value);
        }

        internal override Int128 ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String &&
                (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return reader.GetInt128WithQuotes();
            }

            return reader.GetInt128();
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, Int128 value, JsonNumberHandling handling)
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
