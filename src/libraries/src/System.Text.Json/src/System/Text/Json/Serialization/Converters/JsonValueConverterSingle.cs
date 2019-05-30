// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterSingle : JsonValueConverter<float>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out float value)
        {
            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetDouble(out double rawValue) ||
                !JsonHelpers.IsInRangeInclusive(rawValue, float.MinValue, float.MaxValue))
            {
                value = default;
                return false;
            }

            value = (float)rawValue;
            return true;
        }

        public override void Write(float value, Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(value);
        }

        public override void Write(JsonEncodedText propertyName, float value, Utf8JsonWriter writer)
        {
            writer.WriteNumber(propertyName, value);
        }
    }
}
