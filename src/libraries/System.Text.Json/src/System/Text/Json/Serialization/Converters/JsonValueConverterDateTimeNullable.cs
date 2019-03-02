// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterDateTimeNullable : JsonValueConverter<DateTime?>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out DateTime? value)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                value = default;
                return false;
            }

            ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            bool success = Utf8Parser.TryParse(span, out DateTime tempValue, out int bytesConsumed, 'O') && span.Length == bytesConsumed;
            value = tempValue;
            return success;
        }

        public override void Write(DateTime? value, ref Utf8JsonWriter writer)
        {
            Debug.Assert(value.HasValue); // nulls are filtered before calling here

            // todo: use the appropriate DateTime method once available.
            writer.WriteStringValue(value.Value.ToString("O"));
        }

        public override void Write(Span<byte> escapedPropertyName, DateTime? value, ref Utf8JsonWriter writer)
        {
            Debug.Assert(value.HasValue); // nulls are filtered before calling here

            // todo: use the appropriate DateTime method once available.
            writer.WriteString(escapedPropertyName, value.Value.ToString("O"));
        }
    }
}
