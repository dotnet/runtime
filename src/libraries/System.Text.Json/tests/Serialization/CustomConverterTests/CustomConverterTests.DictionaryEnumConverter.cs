// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// Demonstrates a <see cref="Dictionary{TKey, TValue}"> converter where TKey is an <see cref="Enum"/> with the string
        /// version of the Enum is used.
        /// Sample JSON for <see cref="Dictionary{MyEnum, object}">: {"MyEnumValue":{"MyProperty":"myValue"}}
        /// Sample JSON for <see cref="Dictionary{MyEnum, int}">: {"MyEnumValue":42}
        /// </summary>
        /// <remarks>
        /// A case-insensitive parse is performed for the Enum value.
        /// A <see cref="JsonException"/> is thrown when deserializing if the Enum value is not found.
        /// </remarks>
        internal sealed class DictionaryEnumConverter : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                if (!typeToConvert.IsGenericType)
                {
                    return false;
                }

                if (typeToConvert.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                {
                    return false;
                }

                return typeToConvert.GetGenericArguments()[0].IsEnum;
            }

            public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
            {
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];

                JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                    typeof(DictionaryEnumConverterInner<,>).MakeGenericType(new Type[] { keyType, valueType }),
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: new object[] { options },
                    culture: null);

                return converter;
            }

            private class DictionaryEnumConverterInner<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>> where TKey : struct, Enum
            {
                private readonly JsonConverter<TValue> _valueConverter;
                private Type _keyType;
                private Type _valueType;

                public DictionaryEnumConverterInner(JsonSerializerOptions options)
                {
                    // For performance, use the existing converter if available.
                    _valueConverter = (JsonConverter<TValue>)options.GetConverter(typeof(TValue));

                    // Cache the key and value types.
                    _keyType = typeof(TKey);
                    _valueType = typeof(TValue);
                }

                public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        throw new JsonException();
                    }

                    Dictionary<TKey, TValue> value = new Dictionary<TKey, TValue>();

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            return value;
                        }

                        // Get the key.
                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            throw new JsonException();
                        }

                        string propertyName = reader.GetString();

                        // For performance, parse with ignoreCase:false first.
                        if (!Enum.TryParse(propertyName, ignoreCase: false, out TKey key) &&
                            !Enum.TryParse(propertyName, ignoreCase: true, out key))
                        {
                            throw new JsonException($"Unable to convert \"{propertyName}\" to Enum \"{_keyType}\".");
                        }

                        // Get the value.
                        TValue v;
                        if (_valueConverter != null)
                        {
                            reader.Read();
                            v = _valueConverter.Read(ref reader, _valueType, options);
                        }
                        else
                        {
                            v = JsonSerializer.Deserialize<TValue>(ref reader, options);
                        }

                        // Add to dictionary.
                        value.Add(key, v);
                    }

                    throw new JsonException();
                }

                public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
                {
                    writer.WriteStartObject();

                    foreach (KeyValuePair<TKey, TValue> kvp in value)
                    {
                        writer.WritePropertyName(kvp.Key.ToString());

                        if (_valueConverter != null)
                        {
                            _valueConverter.Write(writer, kvp.Value, options);
                        }
                        else
                        {
                            JsonSerializer.Serialize(writer, kvp.Value, options);
                        }
                    }

                    writer.WriteEndObject();
                }
            }
        }

        [Fact]
        public static void VerifyDictionaryEnumToIntConverter()
        {
            const string Json = @"{""One"":1}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DictionaryEnumConverter());

            Dictionary<MyEnum, int> obj = JsonSerializer.Deserialize<Dictionary<MyEnum, int>>(Json, options);
            Assert.Equal(1, obj.Count);
            Assert.Equal(1, obj[MyEnum.One]);

            string jsonRoundTripped = JsonSerializer.Serialize(obj, options);
            Assert.Equal(Json, jsonRoundTripped);
        }

        [Fact]
        public static void VerifyDictionaryEnumToIntConverterCaseInsensitive()
        {
            const string Json = @"{""one"":1}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DictionaryEnumConverter());

            Dictionary<MyEnum, int> obj = JsonSerializer.Deserialize<Dictionary<MyEnum, int>>(Json, options);
            Assert.Equal(1, obj.Count);
            Assert.Equal(1, obj[MyEnum.One]);

            // The serialized JSON is cased per the enum's actual vales.
            string jsonRoundTripped = JsonSerializer.Serialize(obj, options);
            Assert.Equal(@"{""One"":1}", jsonRoundTripped);
        }

        [Fact]
        public static void VerifyDictionaryEnumToObjectConverter()
        {
            const string Json = @"{""One"":{""Value"":""test""}}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DictionaryEnumConverter());

            Dictionary<MyEnum, Entity> obj = JsonSerializer.Deserialize<Dictionary<MyEnum, Entity>>(Json, options);
            Assert.Equal(1, obj.Count);
            Assert.Equal("test", obj[MyEnum.One].Value);

            string jsonRoundTripped = JsonSerializer.Serialize(obj, options);
            Assert.Equal(Json, jsonRoundTripped);
        }

        [Fact]
        public static void VerifyDictionaryEnumConverterFail()
        {
            const string Json = @"{""BAD"":2}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DictionaryEnumConverter());

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<MyEnum, int>>(Json, options));
            Assert.Contains($"Unable to convert \"BAD\" to Enum \"{typeof(MyEnum)}\".", ex.Message);
        }
    }
}
