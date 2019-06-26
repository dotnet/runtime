// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonKeyValuePairConverter<TKey, TValue> : JsonConverter<KeyValuePair<TKey, TValue>>
    {
        private const string KeyName = "Key";
        private const string ValueName = "Value";

        private static readonly JsonEncodedText _keyName = JsonEncodedText.Encode(KeyName);
        private static readonly JsonEncodedText _valueName = JsonEncodedText.Encode(ValueName);

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

            string propertyName = reader.GetString();
            if (propertyName == KeyName)
            {
                k = ReadProperty<TKey>(ref reader, options);
                keySet = true;
            }
            else if (propertyName == ValueName)
            {
                v = ReadProperty<TValue>(ref reader, options);
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

            propertyName = reader.GetString();
            if (propertyName == ValueName)
            {
                v = ReadProperty<TValue>(ref reader, options);
                valueSet = true;
            }
            else if (propertyName == KeyName)
            {
                k = ReadProperty<TKey>(ref reader, options);
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

        private T ReadProperty<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            T k;
            Type typeToConvert = typeof(T);

            JsonConverter<T> keyConverter = options.GetConverter(typeToConvert) as JsonConverter<T>;
            if (keyConverter == null)
            {
                k = JsonSerializer.Deserialize<T>(ref reader, options);
            }
            else
            {
                reader.Read();
                k = keyConverter.Read(ref reader, typeToConvert, options);
            }

            return k;
        }

        private void WriteProperty<T>(Utf8JsonWriter writer, T value, JsonEncodedText name, JsonSerializerOptions options)
        {
            JsonConverter<T> keyConverter = options.GetConverter(typeof(T)) as JsonConverter<T>;
            if (keyConverter == null)
            {
                // todo: set property name on writer once that functionality exists
                // JsonSerializer.WriteValue<T>(writer, value, options);
                throw new NotImplementedException();
            }
            else
            {
                keyConverter.Write(writer, value, name, options);
            }
        }

        public override void Write(Utf8JsonWriter writer, KeyValuePair<TKey, TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteProperty(writer, value.Key, _keyName, options);
            WriteProperty(writer, value.Value, _valueName, options);
            writer.WriteEndObject();
        }

        public override void Write(Utf8JsonWriter writer, KeyValuePair<TKey, TValue> value, JsonEncodedText propertyName, JsonSerializerOptions options)
        {
            writer.WriteStartObject(propertyName);
            WriteProperty(writer, value.Key, _keyName, options);
            WriteProperty(writer, value.Value, _valueName, options);
            writer.WriteEndObject();
        }
    }
}
