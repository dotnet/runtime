// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterUInt32Nullable : JsonValueConverter<uint?>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out uint? value)
        {
            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetUInt32(out uint rawValue))
            {
                value = default;
                return false;
            }

            value = rawValue;
            return true;
        }

        public override void Write(uint? value, ref Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value.Value);
        }

        public override void Write(Span<byte> escapedPropertyName, uint? value, ref Utf8JsonWriter writer)
        {
            writer.WriteNumber(escapedPropertyName, value.Value);
        }
    }
}
