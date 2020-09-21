// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Fact]
        public static void CustomDerivedTypeConverter()
        {
            string json =
                @"{
                    ""ListWrapper"": [1, 2, 3],
                    ""List"": [4, 5, 6],
                    ""DictionaryWrapper"": {""key"": 1},
                    ""Dictionary"": {""key"": 2}
                }";

            // Without converters, we deserialize as is.
            DerivedTypesWrapper wrapper = JsonSerializer.Deserialize<DerivedTypesWrapper>(json);
            int expected = 1;
            foreach (int value in wrapper.ListWrapper)
            {
                Assert.Equal(expected++, value);
            }
            foreach (int value in wrapper.List)
            {
                Assert.Equal(expected++, value);
            }
            Assert.Equal(1, wrapper.DictionaryWrapper["key"]);
            Assert.Equal(2, wrapper.Dictionary["key"]);

            string serialized = JsonSerializer.Serialize(wrapper);
            Assert.Contains(@"""ListWrapper"":[1,2,3]", serialized);
            Assert.Contains(@"""List"":[4,5,6]", serialized);
            Assert.Contains(@"""DictionaryWrapper"":{""key"":1}", serialized);
            Assert.Contains(@"""Dictionary"":{""key"":2}", serialized);

            // With converters, we expect no values in the wrappers per converters' implementation.
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new ListWrapperConverter());
            options.Converters.Add(new DictionaryWrapperConverter());

            DerivedTypesWrapper customWrapper = JsonSerializer.Deserialize<DerivedTypesWrapper>(json, options);
            Assert.Null(customWrapper.ListWrapper);
            expected = 4;
            foreach (int value in customWrapper.List)
            {
                Assert.Equal(expected++, value);
            }
            Assert.Null(customWrapper.DictionaryWrapper);
            Assert.Equal(2, customWrapper.Dictionary["key"]);

            // Clear metadata for serialize.
            options = new JsonSerializerOptions();
            options.Converters.Add(new ListWrapperConverter());
            options.Converters.Add(new DictionaryWrapperConverter());

            serialized = JsonSerializer.Serialize(wrapper, options);
            Assert.Contains(@"""ListWrapper"":[]", serialized);
            Assert.Contains(@"""List"":[4,5,6]", serialized);
            Assert.Contains(@"""DictionaryWrapper"":{}", serialized);
            Assert.Contains(@"""Dictionary"":{""key"":2}", serialized);
        }

        [Fact]
        public static void CustomUnsupportedDictionaryConverter()
        {
            string json = @"{""DictionaryWrapper"": {""1"": 1}}";

            UnsupportedDerivedTypesWrapper_Dictionary wrapper = new UnsupportedDerivedTypesWrapper_Dictionary
            {
                DictionaryWrapper = new UnsupportedDictionaryWrapper()
            };
            wrapper.DictionaryWrapper[new int[,] { }] = 1;

            // Without converter, we throw.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<UnsupportedDerivedTypesWrapper_Dictionary>(json));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(wrapper));

            // With converter, we expect no values in the wrapper per converter's implementation.
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new UnsupportedDictionaryWrapperConverter());

            UnsupportedDerivedTypesWrapper_Dictionary customWrapper = JsonSerializer.Deserialize<UnsupportedDerivedTypesWrapper_Dictionary>(json, options);
            Assert.Null(customWrapper.DictionaryWrapper);

            // Clear metadata for serialize.
            options = new JsonSerializerOptions();
            options.Converters.Add(new UnsupportedDictionaryWrapperConverter());
            Assert.Equal(@"{""DictionaryWrapper"":{}}", JsonSerializer.Serialize(wrapper, options));
        }

        [Fact]
        public static void CustomUnsupportedIEnumerableConverter()
        {
            string json = @"{""IEnumerableWrapper"": [""1"", ""2"", ""3""]}";

            UnsupportedDerivedTypesWrapper_IEnumerable wrapper = new UnsupportedDerivedTypesWrapper_IEnumerable
            {
                IEnumerableWrapper = new StringIEnumerableWrapper(new List<string> { "1", "2", "3" }),
            };

            // Without converter, we throw on deserialize.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<UnsupportedDerivedTypesWrapper_IEnumerable>(json));
            // Without converter, we serialize as is.
            Assert.Equal(@"{""IEnumerableWrapper"":[""1"",""2"",""3""]}", JsonSerializer.Serialize(wrapper));

            // With converter, we expect no values in the wrapper per converter's implementation.
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new UnsupportedIEnumerableWrapperConverter());

            UnsupportedDerivedTypesWrapper_IEnumerable customWrapper = JsonSerializer.Deserialize<UnsupportedDerivedTypesWrapper_IEnumerable>(json, options);
            Assert.Null(customWrapper.IEnumerableWrapper);

            // Clear metadata for serialize.
            options = new JsonSerializerOptions();
            options.Converters.Add(new UnsupportedIEnumerableWrapperConverter());
            Assert.Equal(@"{""IEnumerableWrapper"":[]}", JsonSerializer.Serialize(wrapper, options));
        }
    }

    public class ListWrapper : List<int> { }

    public class DictionaryWrapper : Dictionary<string, int> { }

    public class UnsupportedDictionaryWrapper : Dictionary<int[,], int> { }

    public class DerivedTypesWrapper
    {
        public ListWrapper ListWrapper { get; set; }
        public List<int> List { get; set; }
        public DictionaryWrapper DictionaryWrapper { get; set; }
        public Dictionary<string, int> Dictionary { get; set; }
    }

    public class UnsupportedDerivedTypesWrapper_Dictionary
    {
        public UnsupportedDictionaryWrapper DictionaryWrapper { get; set; }
    }

    public class UnsupportedDerivedTypesWrapper_IEnumerable
    {
        public StringIEnumerableWrapper IEnumerableWrapper { get; set; }
    }

    public class ListWrapperConverter : JsonConverter<ListWrapper>
    {
        public override ListWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return null;
                }
            }

            throw new JsonException();
        }
        public override void Write(Utf8JsonWriter writer, ListWrapper value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
    }

    public class UnsupportedIEnumerableWrapperConverter : JsonConverter<StringIEnumerableWrapper>
    {
        public override StringIEnumerableWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return null;
                }
            }

            throw new JsonException();
        }
        public override void Write(Utf8JsonWriter writer, StringIEnumerableWrapper value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
    }

    public class DictionaryWrapperConverter : JsonConverter<DictionaryWrapper>
    {
        public override DictionaryWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return null;
                }
            }

            throw new JsonException();
        }
        public override void Write(Utf8JsonWriter writer, DictionaryWrapper value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }

    public class UnsupportedDictionaryWrapperConverter : JsonConverter<UnsupportedDictionaryWrapper>
    {
        public override UnsupportedDictionaryWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return null;
                }
            }

            throw new JsonException();
        }
        public override void Write(Utf8JsonWriter writer, UnsupportedDictionaryWrapper value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
