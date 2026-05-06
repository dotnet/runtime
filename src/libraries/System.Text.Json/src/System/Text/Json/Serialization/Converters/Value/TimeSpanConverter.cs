// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class TimeSpanConverter : JsonPrimitiveConverter<TimeSpan>
    {
        private const int MinimumTimeSpanFormatLength = 1; // d
        private const int MaximumTimeSpanFormatLength = 26; // -dddddddd.hh:mm:ss.fffffff
        private const int MaximumEscapedTimeSpanFormatLength = JsonConstants.MaxExpansionFactorWhileEscaping * MaximumTimeSpanFormatLength;

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
            }

            return ReadCore(ref reader);
        }

        internal override TimeSpan ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return ReadCore(ref reader);
        }

        private static TimeSpan ReadCore(ref Utf8JsonReader reader)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName);

            if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, MinimumTimeSpanFormatLength, MaximumEscapedTimeSpanFormatLength))
            {
                ThrowHelper.ThrowFormatException(DataType.TimeSpan);
            }

            scoped ReadOnlySpan<byte> source;
            if (!reader.HasValueSequence && !reader.ValueIsEscaped)
            {
                source = reader.ValueSpan;
            }
            else
            {
                Span<byte> stackSpan = stackalloc byte[MaximumEscapedTimeSpanFormatLength];
                int bytesWritten = reader.CopyString(stackSpan);
                source = stackSpan.Slice(0, bytesWritten);
            }

            byte firstChar = source[0];
            if (!JsonHelpers.IsDigit(firstChar) && firstChar != '-')
            {
                // Note: Utf8Parser.TryParse allows for leading whitespace so we
                // need to exclude that case here.
                ThrowHelper.ThrowFormatException(DataType.TimeSpan);
            }

            bool result = Utf8Parser.TryParse(source, out TimeSpan tmpValue, out int bytesConsumed, 'c');

            // Note: Utf8Parser.TryParse will return true for invalid input so
            // long as it starts with an integer. Example: "2021-06-18" or
            // "1$$$$$$$$$$". We need to check bytesConsumed to know if the
            // entire source was actually valid.

            if (!result || source.Length != bytesConsumed)
            {
                ThrowHelper.ThrowFormatException(DataType.TimeSpan);
            }

            return tmpValue;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            Span<byte> output = stackalloc byte[MaximumTimeSpanFormatLength];

            bool result = Utf8Formatter.TryFormat(value, output, out int bytesWritten, 'c');
            Debug.Assert(result);

            writer.WriteStringValue(output.Slice(0, bytesWritten));
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            Span<byte> output = stackalloc byte[MaximumTimeSpanFormatLength];

            bool result = Utf8Formatter.TryFormat(value, output, out int bytesWritten, 'c');
            Debug.Assert(result);

            writer.WritePropertyName(output.Slice(0, bytesWritten));
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new()
        {
            Type = JsonSchemaType.String,
            Comment = "Represents a System.TimeSpan value.",
            Pattern = @"^-?(\d+\.)?\d{2}:\d{2}:\d{2}(\.\d{1,7})?$"
        };
    }
}
