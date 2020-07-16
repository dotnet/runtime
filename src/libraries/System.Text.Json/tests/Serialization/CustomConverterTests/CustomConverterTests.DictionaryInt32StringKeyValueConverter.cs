// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// Demonstrates a <see cref="Dictionary{int, string}"> converter using a JSON array containing KeyValuePair objects.
        /// Sample JSON: [{"Key":1,"Value":"One"},{"Key":2,"Value":"Two"}]
        /// </summary>
        internal class DictionaryInt32StringKeyValueConverter : JsonConverter<Dictionary<int, string>>
        {
            private JsonConverter<KeyValuePair<int, string>> _intToStringConverter;

            public DictionaryInt32StringKeyValueConverter(JsonSerializerOptions options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                _intToStringConverter = (JsonConverter<KeyValuePair<int, string>>)options.GetConverter(typeof(KeyValuePair<int, string>));

                // KeyValuePair<> converter is built-in.
                Debug.Assert(_intToStringConverter != null);
            }

            public override Dictionary<int, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                var value = new Dictionary<int, string>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return value;
                    }

                    KeyValuePair<int, string> kvpair = _intToStringConverter.Read(ref reader, typeof(KeyValuePair<int, string>), options);

                    value.Add(kvpair.Key, kvpair.Value);
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<int, string> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (KeyValuePair<int, string> item in value)
                {
                    _intToStringConverter.Write(writer, item, options);
                }

                writer.WriteEndArray();
            }
        }

        [Fact]
        public static void VerifyDictionaryInt32StringKeyValueConverter()
        {
            const string json = @"[{""Key"":1,""Value"":""ValueOne""},{""Key"":2,""Value"":""ValueTwo""}]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DictionaryInt32StringKeyValueConverter(options));

            Dictionary<int, string> dictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(json, options);
            Assert.Equal("ValueOne", dictionary[1]);
            Assert.Equal("ValueTwo", dictionary[2]);

            string jsonSerialized = JsonSerializer.Serialize(dictionary, options);
            Assert.Equal(json, jsonSerialized);
        }
    }
}
