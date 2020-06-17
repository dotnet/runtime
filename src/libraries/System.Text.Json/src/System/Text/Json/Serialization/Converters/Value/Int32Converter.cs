// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override int ReadWithQuotes(ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out int value, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                return value;
            }

            throw ThrowHelper.GetFormatException(NumericType.Int32);
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, int value, JsonSerializerOptions options, ref WriteStack state)
        {
            Span<byte> utf8PropertyName = stackalloc byte[JsonConstants.MaximumFormatInt64Length];

            bool result = Utf8Formatter.TryFormat(value, utf8PropertyName, out int bytesWritten);
            Debug.Assert(result);

            writer.WritePropertyName(utf8PropertyName.Slice(0, bytesWritten));
        }

        internal override bool CanBeDictionaryKey => true;
    }
}
