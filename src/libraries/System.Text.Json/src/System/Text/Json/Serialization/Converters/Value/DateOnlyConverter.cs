// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DateOnlyConverter : JsonConverter<DateOnly>
    {
        public const int FormatLength = 10; // YYYY-MM-DD
        public const int MaxEscapedFormatLength = FormatLength * JsonConstants.MaxExpansionFactorWhileEscaping;

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
            }

            return ReadCore(ref reader);
        }

        internal override DateOnly ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadCore(ref reader);
        }

        private DateOnly ReadCore(ref Utf8JsonReader reader)
        {
            if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, FormatLength, MaxEscapedFormatLength))
            {
                ThrowHelper.ThrowFormatException(DataType.DateOnly);
            }

            ReadOnlySpan<byte> source = stackalloc byte[0];
            if (!reader.HasValueSequence && !reader.ValueIsEscaped)
            {
                source = reader.ValueSpan;
            }
            else
            {
                Span<byte> stackSpan = stackalloc byte[MaxEscapedFormatLength];
                int bytesWritten = reader.CopyString(stackSpan);
                source = stackSpan.Slice(0, bytesWritten);
            }

            if (!JsonHelpers.TryParseAsIso(source, out DateOnly value))
            {
                ThrowHelper.ThrowFormatException(DataType.DateOnly);
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            Span<char> buffer = stackalloc char[FormatLength];
            bool formattedSuccessfully = value.TryFormat(buffer, out int charsWritten, "O", CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully && charsWritten == FormatLength);
            writer.WriteStringValue(buffer);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            Span<char> buffer = stackalloc char[FormatLength];
            bool formattedSuccessfully = value.TryFormat(buffer, out int charsWritten, "O", CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully && charsWritten == FormatLength);
            writer.WritePropertyName(buffer);
        }
    }
}
