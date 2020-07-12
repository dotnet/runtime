// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int16Converter : JsonConverter<short>
    {
        public override short Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String &&
                options != null &&
                (JsonNumberHandling.AllowReadingFromString & options.NumberHandling) != 0)
            {
                return reader.GetInt16WithQuotes();
            }

            return reader.GetInt16();
        }

        public override void Write(Utf8JsonWriter writer, short value, JsonSerializerOptions options)
        {
            if (options != null && ((JsonNumberHandling.WriteAsString & options.NumberHandling) != 0))
            {
                writer.WriteNumberValueAsString(value);
            }
            else
            {
                // For performance, lift up the writer implementation.
                writer.WriteNumberValue((long)value);
            }
        }

        internal override short ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetInt16WithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, short value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }
    }
}
