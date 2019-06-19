// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonConverterSByte : JsonConverter<sbyte>
    {
        public override sbyte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetInt32(out int rawValue) ||
                !JsonHelpers.IsInRangeInclusive(rawValue, sbyte.MinValue, sbyte.MaxValue))
            {
                ThrowHelper.ThrowJsonException();
                return default;
            }

            return (sbyte)reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, sbyte value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        public override void Write(Utf8JsonWriter writer, sbyte value, JsonEncodedText propertyName, JsonSerializerOptions options)
        {
            writer.WriteNumber(propertyName, value);
        }
    }
}
