// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonKeyValuePairConverter<TKey, TValue> : JsonConverter<KeyValuePair<TKey, TValue>>
    {
        private readonly byte[] _keyName;
        private readonly byte[] _valueName;

        private readonly JsonEncodedText _encodedKeyName;
        private readonly JsonEncodedText _encodedValueName;

        public JsonKeyValuePairConverter(byte[] keyName, byte[] valueName, JsonEncodedText encodedKeyName, JsonEncodedText encodedValueName)
        {
            _keyName = keyName;
            _valueName = valueName;
            _encodedKeyName = encodedKeyName;
            _encodedValueName = encodedValueName;
        }

        public override KeyValuePair<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                ThrowHelper.ThrowJsonException();
            }

            TKey k = default;
            bool keySet = false;

            TValue v = default;
            bool valueSet = false;

            // Get the first property.
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowJsonException();
            }

            if (reader.ValueTextEquals(_valueName))
            {
                k = ReadProperty<TKey>(ref reader, typeToConvert, options);
                keySet = true;
            }
            else if (reader.ValueTextEquals(_keyName))
            {
                v = ReadProperty<TValue>(ref reader, typeToConvert, options);
                valueSet = true;
            }
            else
            {
                ThrowHelper.ThrowJsonException();
            }

            // Get the second property.
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowJsonException();
            }

            if (reader.ValueTextEquals(_valueName))
            {
                v = ReadProperty<TValue>(ref reader, typeToConvert, options);
                valueSet = true;
            }
            else if (reader.ValueTextEquals(_keyName))
            {
                k = ReadProperty<TKey>(ref reader, typeToConvert, options);
                keySet = true;
            }
            else
            {
                ThrowHelper.ThrowJsonException();
            }

            if (!keySet || !valueSet)
            {
                ThrowHelper.ThrowJsonException();
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                ThrowHelper.ThrowJsonException();
            }

            return new KeyValuePair<TKey, TValue>(k, v);
        }

        private T ReadProperty<T>(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T k;

            // Attempt to use existing converter first before re-entering through JsonSerializer.Deserialize().
            // The default converter for objects does not parse null objects as null, so it is not used here.
            if (typeToConvert != typeof(object) && (options?.GetConverter(typeToConvert) is JsonConverter<T> keyConverter))
            {
                reader.Read();
                k = keyConverter.Read(ref reader, typeToConvert, options);
            }
            else
            {
                k = JsonSerializer.Deserialize<T>(ref reader, options);
            }

            return k;
        }

        private void WriteProperty<T>(Utf8JsonWriter writer, T value, JsonEncodedText name, JsonSerializerOptions options)
        {
            Type typeToConvert = typeof(T);

            writer.WritePropertyName(name);

            // Attempt to use existing converter first before re-entering through JsonSerializer.Serialize().
            // The default converter for object does not support writing.
            if (typeToConvert != typeof(object) && (options?.GetConverter(typeToConvert) is JsonConverter<T> keyConverter))
            {
                keyConverter.Write(writer, value, options);
            }
            else
            {
                JsonSerializer.Serialize<T>(writer, value, options);
            }
        }

        public override void Write(Utf8JsonWriter writer, KeyValuePair<TKey, TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteProperty(writer, value.Key, _encodedKeyName, options);
            WriteProperty(writer, value.Value, _encodedValueName, options);
            writer.WriteEndObject();
        }
    }
}
