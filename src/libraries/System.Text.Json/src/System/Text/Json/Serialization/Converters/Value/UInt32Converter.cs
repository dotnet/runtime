// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class UInt32Converter : JsonConverter<uint>
    {
        public UInt32Converter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetUInt32();
        }

        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        {
            // For performance, lift up the writer implementation.
            writer.WriteNumberValue((ulong)value);
        }

        internal override uint ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetUInt32WithQuotes();
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, uint value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(value);
        }

        internal override uint ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String &&
                (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return reader.GetUInt32WithQuotes();
            }

            return reader.GetUInt32();
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, uint value, JsonNumberHandling handling)
        {
            if ((JsonNumberHandling.WriteAsString & handling) != 0)
            {
                // TODO: [ActiveIssue("https://github.com/dotnet/runtime/issues/74001")]
                // TODO: Casting to ulong should not be required (TypeCode.UInt16/TypeCode.Byte)
                writer.WriteNumberValueAsString((ulong)value);
            }
            else
            {
                // For performance, lift up the writer implementation.
                writer.WriteNumberValue((ulong)value);
            }
        }
    }
}
