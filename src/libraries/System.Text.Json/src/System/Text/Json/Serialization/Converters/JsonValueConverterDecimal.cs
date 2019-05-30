// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterDecimal : JsonValueConverter<decimal>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out decimal value)
        {
            if (reader.TokenType != JsonTokenType.Number)
            {
                value = default;
                return false;
            }

            return reader.TryGetDecimal(out value);
        }

        public override void Write(decimal value, Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value);
        }

        public override void Write(JsonEncodedText propertyName, decimal value, Utf8JsonWriter writer)
        {
            writer.WriteNumber(propertyName, value);
        }
    }
}
