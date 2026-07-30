// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for IEEE 754 floating-point types that share the same JSON number representation,
    /// such as <see cref="BFloat16"/> and the IEEE 754 decimal types.
    /// </summary>
    internal sealed class Ieee754FloatingPointConverter<T> : JsonPrimitiveConverter<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        // Values that need more room than this are formatted into a pooled buffer.
        // Formatting of the IEEE 754 decimal types only uses scientific notation when it is
        // required or more compact, so large-magnitude values expand to many digits.
        private const int StackBufferLength = 64;
        // This buffer only ever holds one of the named literals "NaN", "Infinity" or "-Infinity",
        // so it is sized from their maximum length rather than from any numeric format length.
        private const int MaxEscapedNamedLiteralLength = JsonConstants.MaximumFloatingPointConstantLength * JsonConstants.MaxExpansionFactorWhileEscaping;

        private readonly NumericType _numericType;

        public Ieee754FloatingPointConverter(NumericType numericType)
        {
            _numericType = numericType;
            IsInternalConverterForNumberType = true;
        }

        internal override bool IsIeeeFloatingPointConverter => true;

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (options?.NumberHandling is not null and not JsonNumberHandling.Strict)
            {
                return ReadNumberWithCustomHandling(ref reader, options.NumberHandling, options);
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(reader.TokenType);
            }

            return ReadCore(ref reader, isStringValue: false);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (options?.NumberHandling is not null and not JsonNumberHandling.Strict)
            {
                WriteNumberWithCustomHandling(writer, value, options.NumberHandling);
                return;
            }

            WriteCore(writer, value);
        }

        internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return ReadCore(ref reader, isStringValue: true);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            ValidateFinite(value);

            Span<byte> stackBuffer = stackalloc byte[StackBufferLength];
            byte[]? rented = Format(value, stackBuffer, out ReadOnlySpan<byte> formatted);
            writer.WritePropertyName(formatted);
            Return(rented);
        }

        internal override T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if ((JsonNumberHandling.AllowReadingFromString & handling) != 0)
                {
                    if (TryGetFloatingPointConstant(ref reader, out T value))
                    {
                        return value;
                    }

                    return ReadCore(ref reader, isStringValue: true);
                }
                else if ((JsonNumberHandling.AllowNamedFloatingPointLiterals & handling) != 0)
                {
                    if (!TryGetFloatingPointConstant(ref reader, out T value))
                    {
                        ThrowHelper.ThrowFormatException(_numericType);
                    }

                    return value;
                }
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(reader.TokenType);
            }

            return ReadCore(ref reader, isStringValue: false);
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
        {
            if ((JsonNumberHandling.WriteAsString & handling) != 0)
            {
                Span<byte> stackBuffer = stackalloc byte[StackBufferLength];
                byte[]? rented = Format(value, stackBuffer, out ReadOnlySpan<byte> formatted);
                writer.WriteNumberValueAsStringUnescaped(formatted);
                Return(rented);
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

        internal override JsonValueType GetSupportedJsonValueTypes(JsonNumberHandling numberHandling) =>
            GetSupportedJsonValueTypesForNumericType(numberHandling);

        // Reads the current token as a number. When the token is a string or property name,
        // non-finite results are rejected: the underlying parsers accept named literals such as "naN"
        // that are not valid JSON, and a string that overflows to infinity is rejected for consistency
        // with float and double. Number tokens are already constrained to the JSON number grammar by
        // the reader, so they are permitted to overflow to infinity per IEEE 754.
        private T ReadCore(ref Utf8JsonReader reader, bool isStringValue)
        {
            byte[]? rentedByteBuffer = null;
            int bufferLength = reader.ValueLength;

            Span<byte> byteBuffer = bufferLength <= JsonConstants.StackallocByteThreshold
                ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                : (rentedByteBuffer = ArrayPool<byte>.Shared.Rent(bufferLength));

            int written = reader.CopyValue(byteBuffer);
            byteBuffer = byteBuffer.Slice(0, written);

            bool success = TryParse(byteBuffer, out T result) && (!isStringValue || T.IsFinite(result));
            Return(rentedByteBuffer);

            if (!success)
            {
                ThrowHelper.ThrowFormatException(_numericType);
            }

            return result;
        }

        private static void WriteCore(Utf8JsonWriter writer, T value)
        {
            ValidateFinite(value);

            Span<byte> stackBuffer = stackalloc byte[StackBufferLength];
            byte[]? rented = Format(value, stackBuffer, out ReadOnlySpan<byte> formatted);
            writer.WriteRawValue(formatted);
            Return(rented);
        }

        // NaN and infinity have no representation in the JSON number grammar. Reject them with the
        // same error Utf8JsonWriter reports for float and double, which points callers at
        // JsonNumberHandling.AllowNamedFloatingPointLiterals.
        private static void ValidateFinite(T value)
        {
            if (!T.IsFinite(value))
            {
                ThrowHelper.ThrowArgumentException_ValueNotSupported();
            }
        }

        private static void WriteFloatingPointConstant(Utf8JsonWriter writer, T value)
        {
            if (T.IsNaN(value))
            {
                writer.WriteNumberValueAsStringUnescaped(JsonConstants.NaNValue);
            }
            else if (T.IsPositiveInfinity(value))
            {
                writer.WriteNumberValueAsStringUnescaped(JsonConstants.PositiveInfinityValue);
            }
            else if (T.IsNegativeInfinity(value))
            {
                writer.WriteNumberValueAsStringUnescaped(JsonConstants.NegativeInfinityValue);
            }
            else
            {
                WriteCore(writer, value);
            }
        }

        private static bool TryGetFloatingPointConstant(ref Utf8JsonReader reader, out T value)
        {
            scoped Span<byte> buffer;

            if (reader.ValueIsEscaped)
            {
                if (reader.ValueLength > MaxEscapedNamedLiteralLength)
                {
                    value = default;
                    return false;
                }

                buffer = stackalloc byte[MaxEscapedNamedLiteralLength];
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
            return TryGetFloatingPointConstant(buffer.Slice(0, written), out value);
        }

        private static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out T value)
        {
            if (span.SequenceEqual(JsonConstants.NaNValue))
            {
                value = T.NaN;
                return true;
            }

            if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
            {
                value = T.PositiveInfinity;
                return true;
            }

            if (span.SequenceEqual(JsonConstants.NegativeInfinityValue))
            {
                value = T.NegativeInfinity;
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryParse(ReadOnlySpan<byte> buffer, out T result) =>
            T.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

        // Formats value into initialBuffer, growing into a pooled buffer if necessary.
        // Returns the rented array, if any, which the caller must return.
        private static byte[]? Format(T value, Span<byte> initialBuffer, out ReadOnlySpan<byte> formatted)
        {
            byte[]? rented = null;
            Span<byte> destination = initialBuffer;
            int written;

            while (!value.TryFormat(destination, out written, format: default, provider: CultureInfo.InvariantCulture))
            {
                byte[]? toReturn = rented;
                rented = ArrayPool<byte>.Shared.Rent(destination.Length * 2);
                destination = rented;
                Return(toReturn);
            }

            formatted = destination.Slice(0, written);
            return rented;
        }

        private static void Return(byte[]? rented)
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
