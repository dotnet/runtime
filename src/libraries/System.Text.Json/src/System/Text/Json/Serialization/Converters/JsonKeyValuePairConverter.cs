// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonKeyValuePairConverter : JsonConverterFactory
    {
        private readonly byte[] _keyName;
        private readonly byte[] _valueName;

        private readonly JsonEncodedText _encodedKeyName;
        private readonly JsonEncodedText _encodedValueName;

        public JsonKeyValuePairConverter(JsonSerializerOptions options)
        {
            JsonNamingPolicy namingPolicy = options.PropertyNamingPolicy;
            if (namingPolicy == null)
            {
                _keyName = new byte[] { (byte)'K', (byte)'e', (byte)'y' };
                _valueName = new byte[] { (byte)'V', (byte)'a', (byte)'l', (byte)'u', (byte)'e' };
            }
            else
            {
                string propertyName = namingPolicy.ConvertName("Key");
                if (propertyName == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNamingPolicyReturnNull(namingPolicy);
                }
                _keyName = JsonReaderHelper.s_utf8Encoding.GetBytes(propertyName);

                propertyName = namingPolicy.ConvertName("Value");
                if (propertyName == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNamingPolicyReturnNull(namingPolicy);
                }
                _valueName = JsonReaderHelper.s_utf8Encoding.GetBytes(propertyName);
            }

            // "encoder: null" is used since the literal values of "Key" and "Value" should not normally be escaped
            // unless a custom encoder is used that escapes these ASCII characters (rare).
            _encodedKeyName = JsonEncodedText.Encode(_keyName, encoder: null);
            _encodedValueName = JsonEncodedText.Encode(_valueName, encoder: null);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
                return false;

            Type generic = typeToConvert.GetGenericTypeDefinition();
            return (generic == typeof(KeyValuePair<,>));
        }

        [PreserveDependency(
            ".ctor(Byte[], Byte[], System.Text.Json.JsonEncodedText, System.Text.Json.JsonEncodedText)",
            "System.Text.Json.Serialization.Converters.JsonKeyValuePairConverter`2")]
        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(JsonKeyValuePairConverter<,>).MakeGenericType(new Type[] { keyType, valueType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { _keyName, _valueName, _encodedKeyName, _encodedValueName },
                culture: null);

            return converter;
        }
    }
}
