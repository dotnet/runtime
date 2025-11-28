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
        private const int MaxUnescapedFormatLength = JsonConstants.MaximumFloatingPointConstantLength * JsonConstants.MaxExpansionFactorWhileEscaping;

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
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
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
            Span<byte> buffer = stackalloc byte[MaxFormatLength];
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
                const byte Quote = JsonConstants.Quote;
                Span<byte> buffer = stackalloc byte[MaxFormatLength + 2];
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
            scoped Span<byte> buffer;

            // Only checking for length 10 or less for constants
            if (reader.ValueIsEscaped)
            {
                if (reader.ValueLength > MaxUnescapedFormatLength)
                {
                    value = default;
                    return false;
                }

                buffer = stackalloc byte[MaxUnescapedFormatLength];
            }
            else
            {
                if (reader.ValueLength > JsonConstants.MaximumFloatingPointConstantLength)
                {
                    value = default;
                    return false;
                }

                buffer = stackalloc byte[JsonConstants.MaximumFloatingPointConstantLength];
            }

            int written = reader.CopyValue(buffer);

            if (written > JsonConstants.MaximumFloatingPointConstantLength)
            {
                value = default;
                return false;
            }

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
            bool success = Half.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

            // Half.TryParse is more lax with floating-point literals than other S.T.Json floating-point types
            // e.g: it parses "naN" successfully. Only succeed with the exact match.
            return success &&
                (!Half.IsNaN(result) || buffer.SequenceEqual(JsonConstants.NaNValue)) &&
                (!Half.IsPositiveInfinity(result) || buffer.SequenceEqual(JsonConstants.PositiveInfinityValue)) &&
                (!Half.IsNegativeInfinity(result) || buffer.SequenceEqual(JsonConstants.NegativeInfinityValue));
        }

        private static void Format(
            Span<byte> destination,
            Half value, out int written)
        {
            bool formattedSuccessfully = value.TryFormat(destination, out written, provider: CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully);
        }
    }
}
