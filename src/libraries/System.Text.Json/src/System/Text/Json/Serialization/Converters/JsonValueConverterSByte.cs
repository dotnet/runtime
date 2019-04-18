// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterSByte : JsonValueConverter<sbyte>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out sbyte value)
        {
            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetInt32(out int rawValue) ||
                !JsonHelpers.IsInRangeInclusive(rawValue, sbyte.MinValue, sbyte.MaxValue))
            {
                value = default;
                return false;
            }

            value = (sbyte)reader.GetInt32();
            return true;
        }

        public override void Write(sbyte value, Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value);
        }

        public override void Write(Span<byte> escapedPropertyName, sbyte value, Utf8JsonWriter writer)
        {
            writer.WriteNumber(escapedPropertyName, value);
        }
    }
}
