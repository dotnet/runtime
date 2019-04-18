// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterInt16 : JsonValueConverter<short>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out short value)
        {
            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetInt32(out int rawValue) ||
                !JsonHelpers.IsInRangeInclusive(rawValue, short.MinValue, short.MaxValue))
            {
                value = default;
                return false;
            }

            value = (short)rawValue;
            return true;
        }

        public override void Write(short value, Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value);
        }

        public override void Write(Span<byte> escapedPropertyName, short value, Utf8JsonWriter writer)
        {
            writer.WriteNumber(escapedPropertyName, value);
        }
    }
}
