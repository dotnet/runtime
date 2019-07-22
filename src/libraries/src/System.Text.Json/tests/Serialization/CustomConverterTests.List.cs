// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
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
    }
}
