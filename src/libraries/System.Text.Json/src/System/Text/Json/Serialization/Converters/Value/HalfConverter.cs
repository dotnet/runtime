// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

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
            int bufferLength = reader.ValueLength;

            Span<byte> byteBuffer = bufferLength <= JsonConstants.StackallocByteThreshold
                ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                : (rentedByteBuffer = ArrayPool<byte>.Shared.Rent(bufferLength));

            int written = reader.CopyValue(byteBuffer);
            byteBuffer = byteBuffer.Slice(0, written);

            bool success = TryParse(byteBuffer, out result);
            if (rentedByteBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedByteBuffer);
            }

            if (!success)
            {
                ThrowHelper.ThrowFormatException(NumericType.Half);
            }

            Debug.Assert(!Half.IsNaN(result) && !Half.IsInfinity(result));
            return result;
        }

        private static void WriteCore(Utf8JsonWriter writer, Half value)
        {
#if NET8_0_OR_GREATER
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
#else
            Span<char> buffer = stackalloc char[MaxFormatLength];
#endif
            Format(buffer, value, out int written);
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
            Format(buffer, value, out int written);
            writer.WritePropertyName(buffer.Slice(0, written));
        }

        internal override Half ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if ((JsonNumberHandling.AllowReadingFromString & handling) != 0)
                {
                    if (TryGetFloatingPointConstant(ref reader, out Half value))
                    {
                        return value;
                    }

                    return ReadCore(ref reader);
                }
                else if ((JsonNumberHandling.AllowNamedFloatingPointLiterals & handling) != 0)
                {
                    if (!TryGetFloatingPointConstant(ref reader, out Half value))
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
                Format(buffer.Slice(1), value, out int written);

                int length = written + 2;
                buffer[length - 1] = Quote;
                writer.WriteRawValue(buffer.Slice(0, length));
            }
            else if ((JsonNumberHandling.AllowNamedFloatingPointLiterals & handling) != 0)
            {
                WriteFloatingPointConstant(writer, value);
            }
            else
            {
                WriteCore(writer, value);
            }
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling numberHandling) =>
            GetSchemaForNumericType(JsonSchemaType.Number, numberHandling, isIeeeFloatingPoint: true);

        private static bool TryGetFloatingPointConstant(ref Utf8JsonReader reader, out Half value)
        {
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
            int written = reader.CopyValue(buffer);

            return JsonReaderHelper.TryGetFloatingPointConstant(buffer.Slice(0, written), out value);
        }

        private static void WriteFloatingPointConstant(Utf8JsonWriter writer, Half value)
        {
            if (Half.IsNaN(value))
            {
                writer.WriteNumberValueAsStringUnescaped(JsonConstants.NaNValue);
            }
            else if (Half.IsPositiveInfinity(value))
            {
                writer.WriteNumberValueAsStringUnescaped(JsonConstants.PositiveInfinityValue);
            }
            else if (Half.IsNegativeInfinity(value))
            {
                writer.WriteNumberValueAsStringUnescaped(JsonConstants.NegativeInfinityValue);
            }
            else
            {
                WriteCore(writer, value);
            }
        }

        private static bool TryParse(ReadOnlySpan<byte> buffer, out Half result)
        {
#if NET8_0_OR_GREATER
            bool success = Half.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);
#else
            // Half.TryFormat/TryParse(ROS<byte>) are not available on .NET 7
            // we need to use Half.TryFormat/TryParse(ROS<char>) in that case.
            char[]? rentedCharBuffer = null;

            Span<char> charBuffer = buffer.Length <= JsonConstants.StackallocCharThreshold
                ? stackalloc char[JsonConstants.StackallocCharThreshold]
                : (rentedCharBuffer = ArrayPool<char>.Shared.Rent(buffer.Length));

            int written = JsonReaderHelper.TranscodeHelper(buffer, charBuffer);

            bool success = Half.TryParse(charBuffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

            if (rentedCharBuffer != null)
            {
                ArrayPool<char>.Shared.Return(rentedCharBuffer);
            }
#endif

            // Half.TryParse is more lax with floating-point literals than other S.T.Json floating-point types
            // e.g: it parses "naN" successfully. Only succeed with the exact match.
            return success &&
                (!Half.IsNaN(result) || buffer.SequenceEqual(JsonConstants.NaNValue)) &&
                (!Half.IsPositiveInfinity(result) || buffer.SequenceEqual(JsonConstants.PositiveInfinityValue)) &&
                (!Half.IsNegativeInfinity(result) || buffer.SequenceEqual(JsonConstants.NegativeInfinityValue));
        }

        private static void Format(
#if NET8_0_OR_GREATER
            Span<byte> destination,
#else
            Span<char> destination,
#endif
            Half value, out int written)
        {
            bool formattedSuccessfully = value.TryFormat(destination, out written, provider: CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully);
        }
    }
}
