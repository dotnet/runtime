// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Extensions;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int128Converter : JsonPrimitiveConverter<Int128>
    {
        private const int MaxFormatLength = 40;

        public Int128Converter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override Int128 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(reader.TokenType);
            }

            return reader.GetInt128();
        }

        public override void Write(Utf8JsonWriter writer, Int128 value, JsonSerializerOptions options)
        {
            WriteCore(writer, value);
        }

        private static void WriteCore(Utf8JsonWriter writer, Int128 value)
        {
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
            Format(buffer, value, out int written);
            writer.WriteRawValue(buffer.Slice(0, written));
        }

        internal override Int128 ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return reader.GetInt128();
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Int128 value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
            Format(buffer, value, out int written);
            writer.WritePropertyName(buffer.Slice(0, written));
        }

        internal override Int128 ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String &&
                (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return reader.GetInt128();
            }

            return Read(ref reader, Type, options);
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, Int128 value, JsonNumberHandling handling)
        {
            if ((JsonNumberHandling.WriteAsString & handling) != 0)
            {
                const byte Quote = JsonConstants.Quote;
                Span<byte> buffer = stackalloc byte[MaxFormatLength + 2];
                buffer[0] = Quote;
                Format(buffer.Slice(1), value, out int written);

                int length = written + 2;
                buffer[length - 1] = Quote;
                writer.WriteRawValue(buffer.Slice(0, length));
            }
            else
            {
                WriteCore(writer, value);
            }
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling numberHandling) =>
            GetSchemaForNumericType(JsonSchemaType.Integer, numberHandling);

        private static void Format(
            Span<byte> destination,
            Int128 value, out int written)
        {
            bool formattedSuccessfully = value.TryFormat(destination, out written, provider: CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully);
        }
    }
}
