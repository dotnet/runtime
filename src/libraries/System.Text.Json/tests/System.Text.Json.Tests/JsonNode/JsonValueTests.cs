// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static class JsonValueTests
    {
        [Fact]
        public static void CreateFromNull()
        {
            Assert.Null(JsonValue.Create((object)null));
        }

        [Fact]
        public static void CreateFromNode_Fail()
        {
            Assert.Throws<ArgumentException>(() => JsonValue.Create(new JsonArray()));
            Assert.Throws<ArgumentException>(() => JsonValue.Create(new JsonObject()));
            Assert.Throws<ArgumentException>(() => JsonValue.Create(JsonValue.Create(42)));
        }

        private class Polymorphic_Base { }
        private class Polymorphic_Derived : Polymorphic_Base { }

        [Fact]
        public static void Polymorphic()
        {
            JsonValue value;

            Polymorphic_Base baseClass = new Polymorphic_Derived();
            value = JsonValue.Create(baseClass);
            Assert.Same(baseClass, value.GetValue<Polymorphic_Derived>());

            Polymorphic_Derived derivedClass = new Polymorphic_Derived();
            value = JsonValue.Create(derivedClass);
            Assert.Same(derivedClass, value.GetValue<Polymorphic_Base>());

            Assert.Same(derivedClass, value.GetValue<object>());
        }

        [Fact]
        public static void QuotedNumbers_Deserialize()
        {
            var options = new JsonSerializerOptions();
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals;

            JsonNode node = JsonSerializer.Deserialize<JsonNode>("\"42\"", options);
            Assert.IsAssignableFrom<JsonValue>(node);
            Assert.Throws<InvalidOperationException>(() => node.GetValue<int>());

            // A second pass is needed to obtain the quoted number.
            Assert.Equal(42, JsonSerializer.Deserialize<int>(node.ToJsonString(), options));

            node = JsonSerializer.Deserialize<JsonNode>("\"NaN\"", options);
            Assert.IsAssignableFrom<JsonValue>(node);
            Assert.Equal(double.NaN, JsonSerializer.Deserialize<double>(node.ToJsonString(), options));
            Assert.Equal(float.NaN, JsonSerializer.Deserialize<float>(node.ToJsonString(), options));
        }

        [Fact]
        public static void QuotedNumbers_Serialize()
        {
            var options = new JsonSerializerOptions();
            options.NumberHandling = JsonNumberHandling.WriteAsString;

            JsonValue obj = JsonValue.Create(42);
            string json = obj.ToJsonString(options);
            Assert.Equal("\"42\"", json);

            obj = JsonValue.Create(double.NaN);
            json = obj.ToJsonString(options);
            Assert.Equal("\"NaN\"", json);
        }

        [Fact]
        public static void FromElement()
        {
            using (JsonDocument document = JsonDocument.Parse("42"))
            {
                JsonValue jValue = JsonValue.Create(document.RootElement);
                Assert.Equal(42, jValue.GetValue<int>());
            }

            using (JsonDocument document = JsonDocument.Parse("null"))
            {
                JsonValue? jValue = JsonValue.Create(document.RootElement);
                Assert.Null(jValue);
            }
        }

        [Fact]
        public static void TryGetValue_FromString()
        {
            JsonValue jValue = JsonNode.Parse("\"MyString\"").AsValue();

            Assert.True(jValue.TryGetValue(out string _));
            Assert.False(jValue.TryGetValue(out char _));
            Assert.False(jValue.TryGetValue(out byte _));
            Assert.False(jValue.TryGetValue(out short _));
            Assert.False(jValue.TryGetValue(out int _));
            Assert.False(jValue.TryGetValue(out long _));
            Assert.False(jValue.TryGetValue(out sbyte _));
            Assert.False(jValue.TryGetValue(out ushort _));
            Assert.False(jValue.TryGetValue(out uint _));
            Assert.False(jValue.TryGetValue(out ulong _));
            Assert.False(jValue.TryGetValue(out bool _));
            Assert.False(jValue.TryGetValue(out float _));
            Assert.False(jValue.TryGetValue(out double _));
            Assert.False(jValue.TryGetValue(out decimal _));
            Assert.False(jValue.TryGetValue(out DateTime _));
            Assert.False(jValue.TryGetValue(out DateTimeOffset _));
            Assert.False(jValue.TryGetValue(out Guid _));
        }

        [Fact]
        public static void TryGetValue_FromNumber()
        {
            JsonValue jValue = JsonNode.Parse("42").AsValue();

            Assert.True(jValue.TryGetValue(out byte _));
            Assert.True(jValue.TryGetValue(out short _));
            Assert.True(jValue.TryGetValue(out int _));
            Assert.True(jValue.TryGetValue(out long _));
            Assert.True(jValue.TryGetValue(out sbyte _));
            Assert.True(jValue.TryGetValue(out ushort _));
            Assert.True(jValue.TryGetValue(out uint _));
            Assert.True(jValue.TryGetValue(out ulong _));
            Assert.True(jValue.TryGetValue(out float _));
            Assert.True(jValue.TryGetValue(out double _));
            Assert.True(jValue.TryGetValue(out decimal _));
            Assert.False(jValue.TryGetValue(out bool _));
            Assert.False(jValue.TryGetValue(out string _));
            Assert.False(jValue.TryGetValue(out char _));
            Assert.False(jValue.TryGetValue(out DateTime _));
            Assert.False(jValue.TryGetValue(out DateTimeOffset _));
            Assert.False(jValue.TryGetValue(out Guid _));
        }

        [Fact]
        public static void TryGetValue_FromGuid()
        {
            JsonValue jValue = JsonNode.Parse("\"ed957609-cdfe-412f-88c1-02daca1b4f51\"").AsValue();

            Assert.True(jValue.TryGetValue(out Guid _));
            Assert.True(jValue.TryGetValue(out string _));
            Assert.False(jValue.TryGetValue(out char _));
            Assert.False(jValue.TryGetValue(out byte _));
            Assert.False(jValue.TryGetValue(out short _));
            Assert.False(jValue.TryGetValue(out int _));
            Assert.False(jValue.TryGetValue(out long _));
            Assert.False(jValue.TryGetValue(out sbyte _));
            Assert.False(jValue.TryGetValue(out ushort _));
            Assert.False(jValue.TryGetValue(out uint _));
            Assert.False(jValue.TryGetValue(out ulong _));
            Assert.False(jValue.TryGetValue(out float _));
            Assert.False(jValue.TryGetValue(out double _));
            Assert.False(jValue.TryGetValue(out decimal _));
            Assert.False(jValue.TryGetValue(out bool _));
            Assert.False(jValue.TryGetValue(out DateTime _));
            Assert.False(jValue.TryGetValue(out DateTimeOffset _));
        }

        [Theory]
        [InlineData("\"2020-07-08T00:00:00\"")]
        [InlineData("\"2019-01-30T12:01:02+01:00\"")]
        public static void TryGetValue_FromDateTime(string json)
        {
            JsonValue jValue = JsonNode.Parse(json).AsValue();

            Assert.True(jValue.TryGetValue(out DateTime _));
            Assert.True(jValue.TryGetValue(out DateTimeOffset _));
            Assert.True(jValue.TryGetValue(out string _));
            Assert.False(jValue.TryGetValue(out char _));
            Assert.False(jValue.TryGetValue(out byte _));
            Assert.False(jValue.TryGetValue(out short _));
            Assert.False(jValue.TryGetValue(out int _));
            Assert.False(jValue.TryGetValue(out long _));
            Assert.False(jValue.TryGetValue(out sbyte _));
            Assert.False(jValue.TryGetValue(out ushort _));
            Assert.False(jValue.TryGetValue(out uint _));
            Assert.False(jValue.TryGetValue(out ulong _));
            Assert.False(jValue.TryGetValue(out float _));
            Assert.False(jValue.TryGetValue(out double _));
            Assert.False(jValue.TryGetValue(out decimal _));
            Assert.False(jValue.TryGetValue(out bool _));
            Assert.False(jValue.TryGetValue(out Guid _));
        }

        [Theory]
        [InlineData("\"A\"")]
        [InlineData("\" \"")]
        public static void FromElement_Char(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonValue jValue = JsonValue.Create(document.RootElement);
                Assert.Equal(json[1], jValue.GetValue<char>());

                bool success = jValue.TryGetValue(out char ch);
                Assert.True(success);
                Assert.Equal(json[1], ch);
            }
        }

        [Theory]
        [InlineData("\"A\"", "A")]
        [InlineData("\"AB\"", "AB")]
        public static void FromElement_ToElement(string json, string expected)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonValue jValue = JsonValue.Create(document.RootElement);

                // Obtain the internal element

                JsonElement element = jValue.GetValue<JsonElement>();
                Assert.Equal(expected, element.GetString());

                bool success = jValue.TryGetValue(out element);
                Assert.True(success);
                Assert.Equal(expected, element.GetString());
            }
        }

        [Theory]
        [InlineData("42")]
        [InlineData("\"AB\"")]
        [InlineData("\"\"")]
        public static void FromElement_Char_Fail(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonValue jValue = JsonValue.Create(document.RootElement);
                Assert.Throws<InvalidOperationException>(() => jValue.GetValue<char>());

                bool success = jValue.TryGetValue(out char ch);
                Assert.False(success);
            }
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        public static void FromElement_WrongNodeTypeThrows(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
                Assert.Throws<InvalidOperationException>(() => JsonValue.Create(document.RootElement));
        }

        [Fact]
        public static void WriteTo_Validation()
        {
            Assert.Throws<ArgumentNullException>(() => JsonValue.Create(42).WriteTo(null));
        }

        [Fact]
        public static void WriteTo()
        {
            const string Json = "42";

            JsonValue jObject = JsonNode.Parse(Json).AsValue();
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            jObject.WriteTo(writer);
            writer.Flush();

            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(Json, json);
        }
    }
}
