// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedString(reader.TokenType);
            }

            var source = reader.GetSpan();

            bool result = Utf8Parser.TryParse(source, out TimeSpan tmpValue, out int bytesConsumed, 'c');

            // Note: Utf8Parser.TryParse will return true for invalid input so
            // long as it starts with an integer. Example: "2021-06-18" or
            // "1$$$$$$$$$$". We need to check bytesConsumed to know if the
            // entire source was actually valid.

            if (result && source.Length == bytesConsumed)
            {
                return tmpValue;
            }

            result = Utf8Parser.TryParse(source, out tmpValue, out bytesConsumed, 'g');

            if (result && source.Length == bytesConsumed)
            {
                return tmpValue;
            }

            throw ThrowHelper.GetFormatException();
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            const int MaximumTimeSpanFormatLength = 26; // -dddddddd.hh:mm:ss.fffffff

            Span<byte> output = stackalloc byte[MaximumTimeSpanFormatLength];

            bool result = Utf8Formatter.TryFormat(value, output, out int bytesWritten, 'c');
            Debug.Assert(result);

            writer.WriteStringValue(output.Slice(0, bytesWritten));
        }
    }
}
