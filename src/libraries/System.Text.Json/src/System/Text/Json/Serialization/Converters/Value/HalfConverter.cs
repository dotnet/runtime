// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Extensions;
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

            return reader.GetHalf();
        }

        public override void Write(Utf8JsonWriter writer, Half value, JsonSerializerOptions options)
        {
            WriteCore(writer, value);
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
            return reader.GetHalf();
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

                    return reader.GetHalf();
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

        private static void Format(
            Span<byte> destination,
            Half value, out int written)
        {
            bool formattedSuccessfully = value.TryFormat(destination, out written, provider: CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully);
        }
    }
}
