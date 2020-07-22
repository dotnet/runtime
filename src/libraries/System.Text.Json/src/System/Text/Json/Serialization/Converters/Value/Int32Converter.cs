// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int32Converter : JsonConverter<int>
    {
        public Int32Converter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            // For performance, lift up the writer implementation.
            writer.WriteNumberValue((long)value);
        }

        internal override int ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetInt32WithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, int value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override int ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling)
        {
            if (reader.TokenType == JsonTokenType.String &&
                (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return reader.GetInt32WithQuotes();
            }

            return reader.GetInt32();
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, int value, JsonNumberHandling handling)
        {
            if ((JsonNumberHandling.WriteAsString & handling) != 0)
            {
                writer.WriteNumberValueAsString(value);
            }
            else
            {
                // For performance, lift up the writer implementation.
                writer.WriteNumberValue((long)value);
            }
        }
    }
}
