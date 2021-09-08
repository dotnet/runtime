// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Theory]
        [MemberData(nameof(GetCustomKeysAndExpectedPropertyNameEncodings))]
        public static void CustomKeyConverter_Serialization<TKey>(TKey key, string expectedKeyEncoding)
        {
            var options = new JsonSerializerOptions { Converters = { new EmbeddedJsonKeyConverter<TKey>() } };
            var value = new Dictionary<TKey, byte> { [key] = 42 };

            string expectedJson = $"{{\"{expectedKeyEncoding}\":42}}";
            string json = JsonSerializer.Serialize(value, options);

            Assert.Equal(expectedJson, json);
        }

        [Theory]
        [MemberData(nameof(GetCustomKeysAndExpectedPropertyNameEncodings))]
        public static void CustomKeyConverter_Deserialization<TKey>(TKey key, string expectedKeyEncoding)
        {
            var options = new JsonSerializerOptions { Converters = { new EmbeddedJsonKeyConverter<TKey>() } };

            string json = $"{{\"{expectedKeyEncoding}\":42}}";
            var deserializedValue = JsonSerializer.Deserialize<Dictionary<TKey, byte>>(json, options);

            Assert.Equal(1, deserializedValue.Count);
            Assert.Equal(key, deserializedValue.Keys.First());
        }

        public static IEnumerable<object[]> GetCustomKeysAndExpectedPropertyNameEncodings()
        {
            yield return Wrap("key");
            yield return Wrap(42);
            yield return Wrap(true);
            yield return Wrap(new KeyValuePair<string, bool>("x", false));
            yield return Wrap(new int[] { 1, 2, 3 });

            static object[] Wrap<TKey>(TKey key) => new object[] { key, JavaScriptEncoder.Default.Encode(JsonSerializer.Serialize(key)) };
        }

        [Fact]
        public static void ExtensionDataProperty_Serialization_IgnoreCustomStringKeyConverter()
        {
            var options = new JsonSerializerOptions { Converters = { new EmbeddedJsonKeyConverter<string>() } };
            var value = new PocoWithExtensionDataProperty();

            string expectedJson = @"{""key"":42}";
            string json = JsonSerializer.Serialize(value, options);

            Assert.Equal(expectedJson, json);
        }

        public class PocoWithExtensionDataProperty
        {
            [JsonExtensionData]
            public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object> { ["key"] = 42 };
        }

        [Theory]
        [InlineData(InvalidCustomKeyConverter.InvalidOperationType.DoNothing)]
        [InlineData(InvalidCustomKeyConverter.InvalidOperationType.HandleEntireParentObject)]
        [InlineData(InvalidCustomKeyConverter.InvalidOperationType.HandleEntireProperty)]
        public static void InvalidCustomKeyConverter_Serialization(InvalidCustomKeyConverter.InvalidOperationType invalidOperationType)
        {
            var options = new JsonSerializerOptions { Converters = { new InvalidCustomKeyConverter { Type = invalidOperationType } } };
            var value = new Dictionary<string, int> { ["key"] = 42 };

            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(value, options));
        }

        [Theory]
        [InlineData(InvalidCustomKeyConverter.InvalidOperationType.HandleEntireParentObject, typeof(JsonException))]
        [InlineData(InvalidCustomKeyConverter.InvalidOperationType.HandleEntireProperty, typeof(JsonException))]
        [InlineData(InvalidCustomKeyConverter.InvalidOperationType.ReturnNull, typeof(ArgumentNullException))]
        public static void InvalidCustomKeyConverter_Deserialization(InvalidCustomKeyConverter.InvalidOperationType invalidOperationType, Type exceptionType)
        {
            var options = new JsonSerializerOptions { Converters = { new InvalidCustomKeyConverter { Type = invalidOperationType } } };
            string json = @"{""key1"" : 1, ""key2"" : 2 }";

            Assert.Throws(exceptionType, () => JsonSerializer.Deserialize<Dictionary<string, int>>(json, options));
        }

        public class EmbeddedJsonKeyConverter<T> : JsonConverter<T>
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions _)
                => throw new NotSupportedException();

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions _)
                => throw new NotSupportedException();

            public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions _)
                => writer.WritePropertyName(JsonSerializer.Serialize(value));

            public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions _)
                => JsonSerializer.Deserialize<T>(reader.GetString());
        }

        public class InvalidCustomKeyConverter : JsonConverter<string>
        {
            public enum InvalidOperationType
            {
                DoNothing, HandleEntireProperty, HandleEntireParentObject, ReturnNull
            }

            public InvalidOperationType Type { get; init; }

            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new NotSupportedException();
            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
                => throw new NotSupportedException();

            public override void WriteAsPropertyName(Utf8JsonWriter writer, string value, JsonSerializerOptions _)
            {
                switch (Type)
                {
                    case InvalidOperationType.HandleEntireProperty:
                        writer.WriteString("key", value);
                        break;

                    case InvalidOperationType.HandleEntireParentObject:
                        writer.WriteEndObject();
                        break;

                    case InvalidOperationType.DoNothing:
                    default:
                        break;
                }
            }

            public override string ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions _)
            {
                switch (Type)
                {
                    case InvalidOperationType.HandleEntireProperty:
                        reader.Read();
                        reader.Skip();
                        break;

                    case InvalidOperationType.HandleEntireParentObject:
                        while (reader.TokenType != JsonTokenType.EndObject)
                        {
                            reader.Read();
                            reader.Skip();
                        }
                        break;

                    case InvalidOperationType.ReturnNull:
                        return null;

                    default:
                        break;
                }

                return "key";
            }
        }
    }
}
