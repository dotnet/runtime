// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        // A List{T} converter that used CreateConverter().
        private class ListConverter : JsonConverterFactory
        {
            int _offset;

            public ListConverter(int offset)
            {
                _offset = offset;
            }

            public override bool CanConvert(Type typeToConvert)
            {
                if (!typeToConvert.IsGenericType)
                    return false;

                Type generic = typeToConvert.GetGenericTypeDefinition();
                if (generic != typeof(List<>))
                    return false;

                Type arg = typeToConvert.GetGenericArguments()[0];
                return arg == typeof(int) ||
                    arg == typeof(long);
            }

            public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
            {
                Type elementType = type.GetGenericArguments()[0];

                JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                    typeof(ListConverter<>).MakeGenericType(elementType),
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    new object[] { _offset },
                    culture: null);

                return converter;
            }
        }

        // Demonstrates List<T>; Adds offset to each integer or long to verify converter ran.
        private class ListConverter<T> : JsonConverter<List<T>>
        {
            private int _offset;
            public ListConverter(int offset)
            {
                _offset = offset;
            }

            public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                var value = new List<T>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return value;
                    }

                    if (reader.TokenType != JsonTokenType.Number)
                    {
                        throw new JsonException();
                    }

                    if (typeof(T) == typeof(int))
                    {
                        int element = reader.GetInt32();
                        IList list = value;
                        list.Add(element + _offset);
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        long element = reader.GetInt64();
                        IList list = value;
                        list.Add(element + _offset);
                    }
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (T item in value)
                {
                    if (item is int)
                    {
                        writer.WriteNumberValue((int)(object)item - _offset);
                    }
                    else if (item is long)
                    {
                        writer.WriteNumberValue((long)(object)item - _offset);
                    }
                    else
                    {
                        Assert.True(false);
                    }
                }

                writer.WriteEndArray();
            }
        }

        [Fact]
        public static void ListConverterOpenGeneric()
        {
            const string json = "[1,2,3]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ListConverter(10));

            {
                List<int> list = JsonSerializer.Deserialize<List<int>>(json, options);
                Assert.Equal(11, list[0]);
                Assert.Equal(12, list[1]);
                Assert.Equal(13, list[2]);

                string jsonSerialized = JsonSerializer.Serialize(list, options);
                Assert.Equal(json, jsonSerialized);
            }

            {
                List<long> list = JsonSerializer.Deserialize<List<long>>(json, options);
                Assert.Equal(11, list[0]);
                Assert.Equal(12, list[1]);
                Assert.Equal(13, list[2]);

                string jsonSerialized = JsonSerializer.Serialize(list, options);
                Assert.Equal(json, jsonSerialized);
            }
        }

        [Fact]
        public static void ListConverterClosedGeneric()
        {
            const string json = "[1,2,3]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ListConverter<int>(10));

            List<int> list = JsonSerializer.Deserialize<List<int>>(json, options);
            Assert.Equal(11, list[0]);
            Assert.Equal(12, list[1]);
            Assert.Equal(13, list[2]);

            string jsonSerialized = JsonSerializer.Serialize(list, options);
            Assert.Equal(json, jsonSerialized);
        }

        /// <summary>
        /// Demonstrates polymorphic IList converter.
        /// </summary>
        private class IListConverter : JsonConverter<IList>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(IList).IsAssignableFrom(typeToConvert);
            }

            public override IList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                var value = new List<int>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return value;
                    }

                    int element = reader.GetInt32();
                    value.Add(element + 10);
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, IList value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (int item in value)
                {
                    writer.WriteNumberValue(item - 10);
                }

                writer.WriteEndArray();
            }
        }

        [Fact]
        public static void ListConverterPolymorphic()
        {
            const string json = "[1,2,3]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new IListConverter());

            IList list = JsonSerializer.Deserialize<IList>(json, options);
            Assert.Equal(11, list[0]);
            Assert.Equal(12, list[1]);
            Assert.Equal(13, list[2]);

            List<int> contraVariantList = JsonSerializer.Deserialize<List<int>>(json, options);
            Assert.Equal(11, contraVariantList[0]);
            Assert.Equal(12, contraVariantList[1]);
            Assert.Equal(13, contraVariantList[2]);

            string jsonSerialized = JsonSerializer.Serialize(list, options);
            Assert.Equal(json, jsonSerialized);

            jsonSerialized = JsonSerializer.Serialize(contraVariantList, options);
            Assert.Equal(json, jsonSerialized);
        }

        private class MyModelWithConverterAttributes
        {
            [JsonConverter(typeof(ListToStringElementsConverter))]
            public List<string> ItemsList { get; set; }

            [JsonConverter(typeof(ListToArrayElementsConverter))]
            public string[] ItemsArray { get; set; }

            [JsonConverter(typeof(ListToDictionaryElementsConverter))]
            public Dictionary<string, string> ItemsDictionary { get; set; }
        }

        private class MyModelWithNoConverterAttributes
        {
            public List<string> ItemsList { get; set; }
            public string[] ItemsArray { get; set; }
            public Dictionary<string, string> ItemsDictionary { get; set; }
        }

        [Fact]
        public static void CustomListWithJsonConverterAttribute()
        {
            const string Json =
                @"{""ItemsList"":[""hello"",1,true]," +
                @"""ItemsArray"":[""hello"",1,true]," +
                @"""ItemsDictionary"":{""hello"":""hello"",""1"":1,""true"":true}}";

            // Baseline failure (no JsonConverterAttributes).
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyModelWithNoConverterAttributes>(Json));

            // Success case.
            MyModelWithConverterAttributes obj;

            void Verify()
            {
                Assert.Equal("hello", obj.ItemsList[0]);
                Assert.Equal("1", obj.ItemsList[1]);
                Assert.Equal("True", obj.ItemsList[2]);

                Assert.Equal("hello", obj.ItemsArray[0]);
                Assert.Equal("1", obj.ItemsArray[1]);
                Assert.Equal("True", obj.ItemsArray[2]);

                Assert.Equal("hello", obj.ItemsDictionary["hello"]);
                Assert.Equal("1", obj.ItemsDictionary["1"]);
                Assert.Equal("True", obj.ItemsDictionary["true"]);
            }

            obj = JsonSerializer.Deserialize<MyModelWithConverterAttributes>(Json);
            Verify();

            string jsonRoundTripped = JsonSerializer.Serialize<MyModelWithConverterAttributes>(obj);
            Assert.Equal(
                @"{""ItemsList"":[""hello"",""1"",""True""]," +
                @"""ItemsArray"":[""hello"",""1"",""True""]," +
                @"""ItemsDictionary"":{""hello"":""hello"",""1"":""1"",""true"":""True""}}",
                jsonRoundTripped);
 
            obj = JsonSerializer.Deserialize<MyModelWithConverterAttributes>(jsonRoundTripped);
            Verify();
        }

        /// <summary>
        /// This converter coerces JSON array items into <see cref="List{string}"/> when deserializing.
        /// </summary>
        class ListToStringElementsConverter : JsonConverter<List<string>>
        {
            public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.EnumerateArray().Select(e => e.ToString()).ToList();
                }
            }

            public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }

        /// <summary>
        /// This converter coerces JSON array items into <see cref="string[]"/> when deserializing.
        /// </summary>
        class ListToArrayElementsConverter : JsonConverter<string[]>
        {
            public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.EnumerateArray().Select(e => e.ToString()).ToArray();
                }
            }

            public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }

        /// <summary>
        /// This converter coerces JSON property values into <see cref="Dictionary{string, string}"> when deserializing.
        /// </summary>
        class ListToDictionaryElementsConverter : JsonConverter<Dictionary<string, string>>
        {
            public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var dictionary = new Dictionary<string, string>();

                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                    {
                        dictionary.Add(property.Name, property.Value.ToString());
                    }
                }

                return dictionary;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}
