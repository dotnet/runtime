// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterDateTime : JsonValueConverter<DateTime>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out DateTime value)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                value = default;
                return false;
            }

            ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed, 'O') && span.Length == bytesConsumed;
        }

        public override void Write(DateTime value, ref Utf8JsonWriter writer)
        {
            // todo: use the appropriate DateTime method once available (https://github.com/dotnet/corefx/issues/34690)
            writer.WriteStringValue(value.ToString("O"));
        }

        public override void Write(Span<byte> escapedPropertyName, DateTime value, ref Utf8JsonWriter writer)
        {
            // todo: use the appropriate DateTime method once available (https://github.com/dotnet/corefx/issues/34690)
            writer.WriteString(escapedPropertyName, value.ToString("O"));
        }
    }
}
