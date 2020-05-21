// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Encodings.Web;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class KeyValuePairConverter<TKey, TValue> : JsonValueConverter<KeyValuePair<TKey, TValue>>
    {
        private const string KeyNameCLR = "Key";
        private const string ValueNameCLR = "Value";

        // Property name for "Key" and "Value" with Options.PropertyNamingPolicy applied.
        private string _keyName = null!;
        private string _valueName = null!;

        // _keyName and _valueName as JsonEncodedText.
        private JsonEncodedText _keyNameEncoded;
        private JsonEncodedText _valueNameEncoded;

        // todo: https://github.com/dotnet/runtime/issues/32352
        // it is possible to cache the underlying converters since this is an internal converter and
        // an instance is created only once for each JsonSerializerOptions instance.

        internal override void Initialize(JsonSerializerOptions options)
        {
            JsonNamingPolicy? namingPolicy = options.PropertyNamingPolicy;

            if (namingPolicy == null)
            {
                _keyName = KeyNameCLR;
                _valueName = ValueNameCLR;
            }
            else
            {
                _keyName = namingPolicy.ConvertName(KeyNameCLR);
                _valueName = namingPolicy.ConvertName(ValueNameCLR);

                if (_keyName == null || _valueName == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(namingPolicy);
                }
            }

            JavaScriptEncoder? encoder = options.Encoder;
            _keyNameEncoded = JsonEncodedText.Encode(_keyName, encoder);
            _valueNameEncoded = JsonEncodedText.Encode(_valueName, encoder);
        }

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

            bool caseInsensitiveMatch = options.PropertyNameCaseInsensitive;

            string propertyName = reader.GetString()!;
            if (FoundKeyProperty(propertyName, caseInsensitiveMatch))
            {
                reader.ReadWithVerify();
                k = JsonSerializer.Deserialize<TKey>(ref reader, options, ref state, _keyName);
                keySet = true;
            }
            else if (FoundValueProperty(propertyName, caseInsensitiveMatch))
            {
                reader.ReadWithVerify();
                v = JsonSerializer.Deserialize<TValue>(ref reader, options, ref state, _valueName);
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
            if (!keySet && FoundKeyProperty(propertyName, caseInsensitiveMatch))
            {
                reader.ReadWithVerify();
                k = JsonSerializer.Deserialize<TKey>(ref reader, options, ref state, _keyName);
            }
            else if (!valueSet && FoundValueProperty(propertyName, caseInsensitiveMatch))
            {
                reader.ReadWithVerify();
                v = JsonSerializer.Deserialize<TValue>(ref reader, options, ref state, _valueName);
            }
            else
            {
                ThrowHelper.ThrowJsonException();
            }

            reader.ReadWithVerify();

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                ThrowHelper.ThrowJsonException();
            }

            value = new KeyValuePair<TKey, TValue>(k!, v!);
            return true;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, KeyValuePair<TKey, TValue> value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(_keyNameEncoded);
            JsonSerializer.Serialize(writer, value.Key, options, ref state, _keyName);

            writer.WritePropertyName(_valueNameEncoded);
            JsonSerializer.Serialize(writer, value.Value, options, ref state, _valueName);

            writer.WriteEndObject();
            return true;
        }

        private bool FoundKeyProperty(string propertyName, bool caseInsensitiveMatch)
        {
            return propertyName == _keyName ||
                (caseInsensitiveMatch && string.Equals(propertyName, _keyName, StringComparison.OrdinalIgnoreCase)) ||
                propertyName == KeyNameCLR;
        }

        private bool FoundValueProperty(string propertyName, bool caseInsensitiveMatch)
        {
            return propertyName == _valueName ||
                (caseInsensitiveMatch && string.Equals(propertyName, _valueName, StringComparison.OrdinalIgnoreCase)) ||
                propertyName == ValueNameCLR;
        }
    }
}
