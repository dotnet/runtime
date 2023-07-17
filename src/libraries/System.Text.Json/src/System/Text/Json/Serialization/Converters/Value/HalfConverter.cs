// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class HalfConverter : JsonPrimitiveConverter<Half>
    {
        private const int MaxFormatLength = 20;

        public HalfConverter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override Half Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(reader.TokenType);
            }

            return ReadCore(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, Half value, JsonSerializerOptions options)
        {
            WriteCore(writer, value);
        }

        private static Half ReadCore(ref Utf8JsonReader reader)
        {
            Half result;

            byte[]? rentedByteBuffer = null;
            char[]? rentedCharBuffer = null;
            int bufferLength = reader.ValueLength;

            try
            {
                Span<byte> byteBuffer = bufferLength <= JsonConstants.StackallocByteThreshold
                    ? stackalloc byte[JsonConstants.StackallocCharThreshold]
                    : (rentedByteBuffer = ArrayPool<byte>.Shared.Rent(bufferLength));

                int written = reader.CopyValue(byteBuffer);
                byteBuffer = byteBuffer.Slice(0, written);

                if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName)
                {
                    if (JsonReaderHelper.TryGetFloatingPointConstant(byteBuffer, out result))
                    {
                        return result;
                    }
                }

#if NET8_0_OR_GREATER
                bool success = Half.TryParse(byteBuffer, out result);
#else
                // Half.TryParse(ROS<byte>) is not available on .NET 7, only Half.TryParse(ROS<char>);
                // we need to transcode here instead of letting CopyValue do it for us because
                // TryGetFloatingPointConstant only accepts ROS<byte>.

                Span<char> charBuffer = stackalloc char[MaxFormatLength];
                written = JsonReaderHelper.TranscodeHelper(byteBuffer, charBuffer);
                bool success = Half.TryParse(charBuffer.Slice(0, written), out result);
#endif
                if (!success)
                {
                    ThrowHelper.ThrowFormatException(NumericType.Half);
                }

                return result;
            }
            finally
            {
                if (rentedByteBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedByteBuffer);
                }

                if (rentedCharBuffer != null)
                {
                    ArrayPool<char>.Shared.Return(rentedCharBuffer);
                }
            }
        }

        private static void WriteCore(Utf8JsonWriter writer, Half value)
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

        internal override Half ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return ReadCore(ref reader);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Half value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
#if NET8_0_OR_GREATER
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
#else
            Span<char> buffer = stackalloc char[MaxFormatLength];
#endif
            bool formattedSuccessfully = value.TryFormat(buffer, out int written);
            Debug.Assert(formattedSuccessfully);
            writer.WritePropertyName(buffer.Slice(0, written));
        }

        internal override Half ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if ((JsonNumberHandling.AllowReadingFromString & handling) != 0)
                {
                    return ReadCore(ref reader);
                }
                else if ((JsonNumberHandling.AllowNamedFloatingPointLiterals & handling) != 0)
                {
                    if (!JsonReaderHelper.TryGetFloatingPointConstant(default, out Half value))
                    {
                        ThrowHelper.ThrowFormatException(NumericType.Half);
                    }

                    return value;
                }
            }

            return Read(ref reader, Type, options);
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, Half value, JsonNumberHandling handling)
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
                bool formattedSuccessfully = value.TryFormat(buffer.Slice(1), out int written);
                Debug.Assert(formattedSuccessfully);

                int length = written + 2;
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
