// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int128Converter : JsonPrimitiveConverter<Int128>
    {
        private const int MaxFormatLength = 40;
        private const int MaxEscapedFormatLength = MaxFormatLength * JsonConstants.MaxExpansionFactorWhileEscaping;

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

            return ReadCore(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, Int128 value, JsonSerializerOptions options)
        {
            WriteCore(writer, value);
        }

        private static Int128 ReadCore(ref Utf8JsonReader reader)
        {
            if (reader.ValueLength > MaxEscapedFormatLength)
            {
                ThrowHelper.ThrowFormatException(NumericType.Int128);
            }

#if NET8_0_OR_GREATER
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
#else
            Span<char> buffer = stackalloc char[MaxFormatLength];
#endif
            int written = reader.CopyString(buffer);
            if (!Int128.TryParse(buffer.Slice(0, written), out Int128 result))
            {
                ThrowHelper.ThrowFormatException(NumericType.Int128);
            }

            return result;
        }

        private static void WriteCore(Utf8JsonWriter writer, Int128 value)
        {
#if NET8_0_OR_GREATER
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
#else
            Span<char> buffer = stackalloc char[MaxFormatLength];
#endif
            bool formattedSuccessfully = value.TryFormat(buffer, out int written);
            Debug.Assert(formattedSuccessfully);
            writer.WriteRawValue(buffer.Slice(0, written));
        }

        internal override Int128 ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return ReadCore(ref reader);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Int128 value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
#if NET8_0_OR_GREATER
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
#else
            Span<char> buffer = stackalloc char[MaxFormatLength];
#endif
            bool formattedSuccessfully = value.TryFormat(buffer, out int bytesWritten);
            Debug.Assert(formattedSuccessfully);
            writer.WritePropertyName(buffer);
        }

        internal override Int128 ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String &&
                (JsonNumberHandling.AllowReadingFromString & handling) != 0)
            {
                return ReadCore(ref reader);
            }

            return Read(ref reader, Type, options);
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, Int128 value, JsonNumberHandling handling)
        {
            if ((JsonNumberHandling.WriteAsString & handling) != 0)
            {
#if NET8_0_OR_GREATER
                const byte Quote = JsonConstants.Quote;
                Span<byte> buffer = stackalloc byte[MaxFormatLength + 2];
#else
                const char Quote = (char)JsonConstants.Quote;
                Span<char> buffer = stackalloc char[MaxFormatLength + 2];
#endif
                buffer[0] = Quote;
                bool formattedSuccessfully = value.TryFormat(buffer.Slice(1), out int bytesWritten);
                Debug.Assert(formattedSuccessfully);

                int length = bytesWritten + 2;
                buffer[length - 1] = Quote;
                writer.WriteRawValue(buffer.Slice(0, length));
            }
            else
            {
                WriteCore(writer, value);
            }
        }
    }
}
