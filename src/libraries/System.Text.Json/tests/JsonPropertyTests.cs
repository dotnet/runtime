// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonPropertyTests
    {

        [Fact]
        public static void CheckByPassingNullWriter()
        {
            using (JsonDocument doc = JsonDocument.Parse("{\"First\":1}", default))
            {
                foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                {
                    AssertExtensions.Throws<ArgumentNullException>("writer", () => property.WriteTo(null));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void WriteObjectValidations(bool skipValidation)
        {
            var buffer = new ArrayBufferWriter<byte>(1024);
            using (JsonDocument doc = JsonDocument.Parse("{\"First\":1}", default))
            {
                JsonElement root = doc.RootElement;
                var options = new JsonWriterOptions
                {
                    SkipValidation = skipValidation,
                };
                using var writer = new Utf8JsonWriter(buffer, options);
                if (skipValidation)
                {
                    foreach (JsonProperty property in root.EnumerateObject())
                    {
                        property.WriteTo(writer);
                    }
                    writer.Flush();
                    AssertContents("\"First\":1", buffer);
                }
                else
                {
                    foreach (JsonProperty property in root.EnumerateObject())
                    {
                        Assert.Throws<InvalidOperationException>(() =>
                        {
                            property.WriteTo(writer);
                        });
                    }
                    writer.Flush();
                    AssertContents("", buffer);
                }
            }
        }

        [Fact]
        public static void WriteSimpleObject()
        {
            var buffer = new ArrayBufferWriter<byte>(1024);
            using (JsonDocument doc = JsonDocument.Parse("{\"First\":1, \"Number\":1e400}"))
            {
                using var writer = new Utf8JsonWriter(buffer);
                writer.WriteStartObject();
                foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
                {
                    prop.WriteTo(writer);
                }
                writer.WriteEndObject();
                writer.Flush();

                AssertContents("{\"First\":1,\"Number\":1e400}", buffer);
            }
        }

        private static void AssertContents(string expectedValue, ArrayBufferWriter<byte> buffer)
        {
            Assert.Equal(
                expectedValue,
                Encoding.UTF8.GetString(
                    buffer.WrittenSpan
#if NETFRAMEWORK
                        .ToArray()
#endif
                    ));
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("")]
        [InlineData(null)]
        public static void NameEquals_InvalidInstance_Throws(string text)
        {
            string ErrorMessage = new InvalidOperationException().Message;
            JsonProperty prop = default;
            AssertExtensions.Throws<InvalidOperationException>(() => prop.NameEquals(text), ErrorMessage);
            AssertExtensions.Throws<InvalidOperationException>(() => prop.NameEquals(text.AsSpan()), ErrorMessage);
            byte[] expectedGetBytes = text == null ? null : Encoding.UTF8.GetBytes(text);
            AssertExtensions.Throws<InvalidOperationException>(() => prop.NameEquals(expectedGetBytes), ErrorMessage);
        }

        [Theory]
        [InlineData("conne\\u0063tionId", "connectionId")]
        [InlineData("connectionId", "connectionId")]
        [InlineData("123", "123")]
        [InlineData("My name is \\\"Ahson\\\"", "My name is \"Ahson\"")]
        public static void NameEquals_UseGoodMatches_True(string propertyName, string otherText)
        {
            string jsonString = $"{{ \"{propertyName}\" : \"itsValue\" }}";
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                JsonElement jElement = doc.RootElement;
                JsonProperty property = jElement.EnumerateObject().First();
                byte[] expectedGetBytes = Encoding.UTF8.GetBytes(otherText);
                Assert.True(property.NameEquals(otherText));
                Assert.True(property.NameEquals(otherText.AsSpan()));
                Assert.True(property.NameEquals(expectedGetBytes));
            }
        }

        [Fact]
        public static void NameEquals_GivenPropertyAndValue_TrueForPropertyName()
        {
            string jsonString = $"{{ \"aPropertyName\" : \"itsValue\" }}";
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                JsonElement jElement = doc.RootElement;
                JsonProperty property = jElement.EnumerateObject().First();

                string text = "aPropertyName";
                byte[] expectedGetBytes = Encoding.UTF8.GetBytes(text);
                Assert.True(property.NameEquals(text));
                Assert.True(property.NameEquals(text.AsSpan()));
                Assert.True(property.NameEquals(expectedGetBytes));

                text = "itsValue";
                expectedGetBytes = Encoding.UTF8.GetBytes(text);
                Assert.False(property.NameEquals(text));
                Assert.False(property.NameEquals(text.AsSpan()));
                Assert.False(property.NameEquals(expectedGetBytes));
            }
        }
    }
}
