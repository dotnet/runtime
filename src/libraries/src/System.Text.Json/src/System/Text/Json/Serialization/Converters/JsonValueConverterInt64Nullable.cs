// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterInt64Nullable : JsonValueConverter<long?>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out long? value)
        {
            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetInt64(out long rawValue))
            {
                value = default;
                return false;
            }

            value = rawValue;
            return true;
        }

        public override void Write(long? value, ref Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value.Value);
        }

        public override void Write(Span<byte> escapedPropertyName, long? value, ref Utf8JsonWriter writer)
        {
            writer.WriteNumber(escapedPropertyName, value.Value);
        }
    }
}
