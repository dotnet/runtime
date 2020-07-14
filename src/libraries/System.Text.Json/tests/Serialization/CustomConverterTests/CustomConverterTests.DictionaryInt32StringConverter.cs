// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// Demonstrates a <see cref="Dictionary{int, string}"> converter using a JSON object with property names representing keys.
        /// Sample JSON: {"1":"ValueOne","2":"ValueTwo"}
        /// </summary>
        internal class DictionaryInt32StringConverter : JsonConverter<Dictionary<int, string>>
        {
            public override Dictionary<int, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                var value = new Dictionary<int, string>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return value;
                    }

                    string keyAsString = reader.GetString();
                    if (!int.TryParse(keyAsString, out int keyAsInt))
                    {
                        throw new JsonException($"Unable to convert \"{keyAsString}\" to System.Int32.");
                    }

                    reader.Read();
                    string itemValue = reader.GetString();

                    value.Add(keyAsInt, itemValue);
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<int, string> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach (KeyValuePair<int, string> item in value)
                {
                    writer.WriteString(item.Key.ToString(), item.Value);
                }

                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void IntToStringDictionaryObjectConverter()
        {
            const string json = @"{""1"":""ValueOne"",""2"":""ValueTwo""}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DictionaryInt32StringConverter());

            Dictionary<int, string> dictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(json, options);
            Assert.Equal("ValueOne", dictionary[1]);
            Assert.Equal("ValueTwo", dictionary[2]);

            string jsonSerialized = JsonSerializer.Serialize(dictionary, options);
            Assert.Equal(json, jsonSerialized);
        }
    }
}
