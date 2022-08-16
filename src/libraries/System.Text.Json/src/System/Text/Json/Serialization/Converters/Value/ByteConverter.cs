// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ByteConverter : JsonConverter<byte>
    {
        public ByteConverter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override byte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetByte();
        }

        public override void Write(Utf8JsonWriter writer, byte value, JsonSerializerOptions options)
        {
            // TODO: [ActiveIssue("https://github.com/dotnet/runtime/issues/74001")]
            // TODO: Casting to long should not be required
            writer.WriteNumberValue((long)value);
        }

        internal override byte ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetByteWithQuotes();
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, byte value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            // TODO: [ActiveIssue("https://github.com/dotnet/runtime/issues/74001")]
            // TODO: Casting to long should not be required
            writer.WritePropertyName((long)value);
        }

        internal override byte ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return reader.GetByteWithQuotes();
            }

            return reader.GetByte();
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, byte value, JsonNumberHandling handling)
        {
            // TODO: [ActiveIssue("https://github.com/dotnet/runtime/issues/74001")]
            // TODO: Casting to long should not be required
            if ((JsonNumberHandling.WriteAsString & handling) != 0)
            {
                writer.WriteNumberValueAsString((long)value);
            }
            else
            {
                writer.WriteNumberValue((long)value);
            }
        }
    }
}
