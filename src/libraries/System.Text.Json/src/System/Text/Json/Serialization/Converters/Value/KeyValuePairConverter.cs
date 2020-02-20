// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class KeyValuePairConverter<TKey, TValue> : JsonValueConverter<KeyValuePair<TKey, TValue>>
    {
        private const string KeyName = "Key";
        private const string ValueName = "Value";

        // todo: https://github.com/dotnet/runtime/issues/1197
        // move these to JsonSerializerOptions and use the proper encoding.
        private static readonly JsonEncodedText _keyName = JsonEncodedText.Encode(KeyName, encoder: null);
        private static readonly JsonEncodedText _valueName = JsonEncodedText.Encode(ValueName, encoder: null);

        // todo: https://github.com/dotnet/runtime/issues/32352
        // it is possible to cache the underlying converters since this is an internal converter and
        // an instance is created only once for each JsonSerializerOptions instance.

        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options,
            ref ReadStack state,
            out KeyValuePair<TKey, TValue> value)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                ThrowHelper.ThrowJsonException();
            }

            TKey k = default!;
            bool keySet = false;

            TValue v = default!;
            bool valueSet = false;

            // Get the first property.
            reader.ReadWithVerify();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowJsonException();
            }

            string propertyName = reader.GetString()!;
            if (propertyName == KeyName)
            {
                reader.ReadWithVerify();
                k = JsonSerializer.Deserialize<TKey>(ref reader, options, ref state, KeyName);
                keySet = true;
            }
            else if (propertyName == ValueName)
            {
                reader.ReadWithVerify();
                v = JsonSerializer.Deserialize<TValue>(ref reader, options, ref state, ValueName);
                valueSet = true;
            }
            else
            {
                ThrowHelper.ThrowJsonException();
            }

            // Get the second property.
            reader.ReadWithVerify();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowJsonException();
            }

            propertyName = reader.GetString()!;
            if (propertyName == KeyName)
            {
                reader.ReadWithVerify();
                k = JsonSerializer.Deserialize<TKey>(ref reader, options, ref state, KeyName);
                keySet = true;
            }
            else if (propertyName == ValueName)
            {
                reader.ReadWithVerify();
                v = JsonSerializer.Deserialize<TValue>(ref reader, options, ref state, ValueName);
                valueSet = true;
            }
            else
            {
                ThrowHelper.ThrowJsonException();
            }

            if (!keySet || !valueSet)
            {
                ThrowHelper.ThrowJsonException();
            }

            reader.ReadWithVerify();

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                ThrowHelper.ThrowJsonException();
            }

            value = new KeyValuePair<TKey, TValue>(k, v);
            return true;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, KeyValuePair<TKey, TValue> value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(_keyName);
            JsonSerializer.Serialize(writer, value.Key, options, ref state, KeyName);

            writer.WritePropertyName(_valueName);
            JsonSerializer.Serialize(writer, value.Value, options, ref state, ValueName);

            writer.WriteEndObject();
            return true;
        }
    }
}
