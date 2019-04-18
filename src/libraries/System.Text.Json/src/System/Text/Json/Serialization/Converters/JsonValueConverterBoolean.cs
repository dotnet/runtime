// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterBoolean : JsonValueConverter<bool>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out bool value)
        {
            if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
            {
                value = default;
                return false;
            }

            value = reader.GetBoolean();
            return true;
        }

        public override void Write(bool value, Utf8JsonWriter writer)
        {
            writer.WriteBooleanValue(value);
        }

        public override void Write(Span<byte> escapedPropertyName, bool value, Utf8JsonWriter writer)
        {
            writer.WriteBoolean(escapedPropertyName, value);
        }
    }
}
