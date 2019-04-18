// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterInt64 : JsonValueConverter<long>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out long value)
        {
            if (reader.TokenType != JsonTokenType.Number)
            {
                value = default;
                return false;
            }

            return reader.TryGetInt64(out value);
        }

        public override void Write(long value, Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value);
        }

        public override void Write(Span<byte> escapedPropertyName, long value, Utf8JsonWriter writer)
        {
            writer.WriteNumber(escapedPropertyName, value);
        }
    }
}
