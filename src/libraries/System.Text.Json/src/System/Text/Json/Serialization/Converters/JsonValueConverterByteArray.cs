// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterByteArray : JsonValueConverter<byte[]>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out byte[] value)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                value = default;
                return false;
            }

            return reader.TryGetBytesFromBase64(out value);
        }

        public override void Write(byte[] value, Utf8JsonWriter writer)
        {
            writer.WriteBase64StringValue(value);
        }

        public override void Write(JsonEncodedText propertyName, byte[] value, Utf8JsonWriter writer)
        {
            writer.WriteBase64String(propertyName, value);
        }
    }
}
