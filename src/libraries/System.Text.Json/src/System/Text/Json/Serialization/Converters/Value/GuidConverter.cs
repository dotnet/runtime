// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class GuidConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetGuid();
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        internal override Guid ReadWithQuotes(ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out Guid value, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                return value;
            }

            throw ThrowHelper.GetFormatException(DataType.Guid);
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options, ref WriteStack state)
        {
            Span<byte> utf8PropertyName = stackalloc byte[JsonConstants.MaximumFormatGuidLength];

            bool result = Utf8Formatter.TryFormat(value, utf8PropertyName, out int bytesWritten);
            Debug.Assert(result);

            writer.WritePropertyName(utf8PropertyName);
        }

        internal override bool CanBeDictionaryKey => true;
    }
}
