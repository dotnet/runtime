// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ByteArrayConverter : JsonConverter<byte[]?>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.GetBytesFromBase64();
        }

        public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteBase64StringValue(value);
            }
        }
    }
}
