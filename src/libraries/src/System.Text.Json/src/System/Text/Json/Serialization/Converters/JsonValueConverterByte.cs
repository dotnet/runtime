// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterByte : JsonValueConverter<byte>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out byte value)
        {
            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetInt32(out int rawValue) ||
                !JsonHelpers.IsInRangeInclusive(rawValue, byte.MinValue, byte.MaxValue))
            {
                value = default;
                return false;
            }

            value = (byte)rawValue;
            return true;
        }

        public override void Write(byte value, Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value);
        }

        public override void Write(Span<byte> escapedPropertyName, byte value, Utf8JsonWriter writer)
        {
            writer.WriteNumber(escapedPropertyName, value);
        }
    }
}
