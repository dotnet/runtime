// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Tests;
using Xunit;

namespace System.Text.Json.Nodes.Tests
{
    public static class JsonValueTests
    {
        [Fact]
        public static void CreateFromNull()
        {
            Assert.Null(JsonValue.Create((bool?)null));
            Assert.Null(JsonValue.Create((string)null));
            Assert.Null(JsonValue.Create((JsonElement?)null));
            Assert.Null(JsonValue.Create(JsonElement.Parse("null")));

            Assert.Null(JsonValue.Create((object)null));
            Assert.Null(JsonValue.Create((bool?)null));
            Assert.Null(JsonValue.Create((string)null));
            Assert.Null(JsonValue.Create((JsonElement?)null));
            Assert.Null(JsonValue.Create(JsonElement.Parse("null")));
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

        [Fact]
        public static void TryGetValue_FromBoolean()
        {
            JsonValue jValue = JsonNode.Parse("true").AsValue();

            Assert.True(jValue.TryGetValue(out bool _));
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
            Assert.False(jValue.TryGetValue(out string _));
            Assert.False(jValue.TryGetValue(out char _));
            Assert.False(jValue.TryGetValue(out DateTime _));
            Assert.False(jValue.TryGetValue(out DateTimeOffset _));
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
        [InlineData("\"A\"", "A")]
        [InlineData("\"AB\"", "AB")]
        [InlineData("\"A\\u0022\"", "A\"")] // ValueEquals compares unescaped values
        public static void DeserializePrimitive_ToElement_String(string json, string expected)
        {
            DoTest(json, expected);

            // Test long strings
            string padding = new string('P', 256);
            DoTest(json[0] + padding + json.Substring(1), padding + expected);

            static void DoTest(string json, string expected)
            {
                JsonValue value = JsonSerializer.Deserialize<JsonValue>(json);
                JsonElement element = value.GetValue<JsonElement>();

                AssertExtensions.TrueExpression(element.ValueEquals(expected));

                bool success = value.TryGetValue(out element);
                Assert.True(success);
                AssertExtensions.TrueExpression(element.ValueEquals(expected));
            }
        }

        [Fact]
        public static void DeserializePrimitive_ToElement_Bool()
        {
            // true
            JsonValue value = JsonSerializer.Deserialize<JsonValue>("true");

            JsonElement element = value.GetValue<JsonElement>();
            Assert.Equal(JsonValueKind.True, element.ValueKind);

            bool success = value.TryGetValue(out element);
            Assert.True(success);
            Assert.Equal(JsonValueKind.True, element.ValueKind);

            // false
            value = JsonSerializer.Deserialize<JsonValue>("false");

            element = value.GetValue<JsonElement>();
            Assert.Equal(JsonValueKind.False, element.ValueKind);

            success = value.TryGetValue(out element);
            Assert.True(success);
            Assert.Equal(JsonValueKind.False, element.ValueKind);
        }

        [Fact]
        public static void DeserializePrimitive_ToElement_Number()
        {
            JsonValue value = JsonSerializer.Deserialize<JsonValue>("42");

            JsonElement element = value.GetValue<JsonElement>();
            Assert.Equal(42, element.GetInt32());

            bool success = value.TryGetValue(out element);
            Assert.True(success);
            Assert.Equal(42, element.GetInt32());
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

        [Theory]
        [InlineData("\"A\"")]
        [InlineData("\"AB\"")]
        [InlineData("\"A\\u0022\"")]
        [InlineData("42")]
        [InlineData("true")]
        [InlineData("false")]
        public static void DeserializePrimitive_WriteTo(string json)
        {
            byte[] utf8Json = Encoding.UTF8.GetBytes(json);
            JsonValue value = JsonSerializer.Deserialize<JsonValue>(utf8Json);

            var buffer = new ArrayBufferWriter<byte>(json.Length);
            using Utf8JsonWriter writer = new Utf8JsonWriter(buffer);
            value.WriteTo(writer);
            writer.Flush();

            AssertExtensions.SequenceEqual(utf8Json, buffer.WrittenSpan);
        }

        [Fact]
        public static void DeepCloneNotTrimmable()
        {
            var student = new Student()
            {
                Id = 1,
                Name = "test"
            };
            JsonValue jValue = JsonValue.Create(student);

            JsonNode clone = jValue.DeepClone();

            JsonNodeTests.AssertDeepEqual(jValue, clone);

            string originalJson = jValue.ToJsonString();
            string clonedJson = clone.ToJsonString();

            Assert.Equal(originalJson, clonedJson);
        }

        [Theory]
        [InlineData("42")]
        [InlineData("\"AB\"")]
        [InlineData("\"\"")]
        public static void DeepCloneTrimmable(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonValue jsonValue = JsonValue.Create(document.RootElement);
                JsonNode clone = jsonValue.DeepClone();

                JsonNodeTests.AssertDeepEqual(jsonValue, clone);
                string originalJson = jsonValue.ToJsonString();
                string clonedJson = clone.ToJsonString();

                Assert.Equal(originalJson, clonedJson);
            }
        }

        [Fact]
        public static void DeepEqualsComplexType()
        {
            var student = new Student()
            {
                Id = 10,
                Name = "test"
            };
            JsonValue jValue = JsonValue.Create(student);

            var jObject = new JsonObject();
            jObject.Add("Id", 10);
            jObject.Add("Name", "test");

            JsonNodeTests.AssertDeepEqual(jValue, jObject);
        }

        [Fact]
        public static void DeepEqualsPrimitiveType()
        {
            JsonNodeTests.AssertDeepEqual(JsonValue.Create(10), JsonValue.Create((uint)10));
            JsonNodeTests.AssertDeepEqual(JsonValue.Create(10), JsonValue.Create((ulong)10));
            JsonNodeTests.AssertDeepEqual(JsonValue.Create(10), JsonValue.Create((float)10));
            JsonNodeTests.AssertDeepEqual(JsonValue.Create(10), JsonValue.Create((decimal)10));
            JsonNodeTests.AssertDeepEqual(JsonValue.Create(10), JsonValue.Create((short)10));
            JsonNodeTests.AssertDeepEqual(JsonValue.Create(10), JsonValue.Create((ushort)10));

            Guid guid = Guid.Empty;
            JsonNodeTests.AssertDeepEqual(JsonValue.Create(guid), JsonValue.Create(guid.ToString()));
            JsonNodeTests.AssertNotDeepEqual(JsonValue.Create(10), JsonValue.Create("10"));
        }

        [Theory]
        [InlineData("-0.0", "0")]
        [InlineData("0", "0.0000e4")]
        [InlineData("0", "0.0000e-4")]
        [InlineData("1", "1.0")]
        [InlineData("1", "1e0")]
        [InlineData("1", "1.0000")]
        [InlineData("1", "1.0000e0")]
        [InlineData("1", "0.10000e1")]
        [InlineData("1", "10.0000e-1")]
        [InlineData("10001", "1.0001e4")]
        [InlineData("10001e-3", "1.0001e1")]
        [InlineData("1", "0.1e1")]
        [InlineData("0.1", "1e-1")]
        [InlineData("0.001", "1e-3")]
        [InlineData("1e9", "1000000000")]
        [InlineData("11", "1.100000000e1")]
        [InlineData("3.141592653589793", "3141592653589793E-15")]
        [InlineData("0.000000000000000000000000000000000000000001", "1e-42")]
        [InlineData("1000000000000000000000000000000000000000000", "1e42")]
        [InlineData("-1.1e3", "-1100")]
        [InlineData("79228162514264337593543950336", "792281625142643375935439503360e-1")] // decimal.MaxValue + 1
        [InlineData("79228162514.264337593543950336", "792281625142643375935439503360e-19")]
        [InlineData("1.75e+300", "1.75E+300")] // Variations in exponent casing
        [InlineData( // > 256 digits
            "1.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001" ,

            "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001" + "E-512")]
        public static void DeepEqualsNumericType(string leftStr, string rightStr)
        {
            JsonNode left = JsonNode.Parse(leftStr);
            JsonNode right = JsonNode.Parse(rightStr);

            JsonNodeTests.AssertDeepEqual(left, right);
        }

        [Theory]
        [InlineData("0", "1")]
        [InlineData("1", "-1")]
        [InlineData("1.1", "-1.1")]
        [InlineData("1.1e5", "-1.1e5")]
        [InlineData("0", "1e-1024")]
        [InlineData("1", "0.1")]
        [InlineData("1", "1.1")]
        [InlineData("1", "1e1")]
        [InlineData("1", "1.00001")]
        [InlineData("1", "1.0000e1")]
        [InlineData("1", "0.1000e-1")]
        [InlineData("1", "10.0000e-2")]
        [InlineData("10001", "1.0001e3")]
        [InlineData("10001e-3", "1.0001e2")]
        [InlineData("1", "0.1e2")]
        [InlineData("0.1", "1e-2")]
        [InlineData("0.001", "1e-4")]
        [InlineData("1e9", "1000000001")]
        [InlineData("11", "1.100000001e1")]
        [InlineData("0.000000000000000000000000000000000000000001", "1e-43")]
        [InlineData("1000000000000000000000000000000000000000000", "1e43")]
        [InlineData("-1.1e3", "-1100.1")]
        [InlineData("79228162514264337593543950336", "7922816251426433759354395033600e-1")] // decimal.MaxValue + 1
        [InlineData("79228162514.264337593543950336", "7922816251426433759354395033601e-19")]
        [InlineData("1.75e+300", "1.75E+301")] // Variations in exponent casing
        [InlineData("1e2147483647", "1e-2147483648")] // int.MaxValue, int.MinValue exponents
        [InlineData( // > 256 digits
            "1.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001",

            "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003" + "E-512")]
        public static void NotDeepEqualsNumericType(string leftStr, string rightStr)
        {
            JsonNode left = JsonNode.Parse(leftStr);
            JsonNode right = JsonNode.Parse(rightStr);

            JsonNodeTests.AssertNotDeepEqual(left, right);
        }

        [Theory]
        [InlineData(int.MinValue - 1L)]
        [InlineData(int.MaxValue + 1L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public static void DeepEquals_ExponentExceedsInt32_ThrowsArgumentOutOfRangeException(long exponent)
        {
            JsonNode node = JsonNode.Parse($"1e{exponent}");
            Assert.Throws<ArgumentOutOfRangeException>(() => JsonNode.DeepEquals(node, node));
        }

        [Fact]
        public static void DeepEqualsJsonElement()
        {
            using JsonDocument document1 = JsonDocument.Parse("10");

            JsonValue jsonValue1 = JsonValue.Create(document1.RootElement);

            JsonNodeTests.AssertDeepEqual(jsonValue1, JsonValue.Create(10));

            using JsonDocument document2 = JsonDocument.Parse("\"10\"");

            JsonValue jsonValue2 = JsonValue.Create(document2.RootElement);
            JsonNodeTests.AssertNotDeepEqual(jsonValue1, jsonValue2);
            JsonNodeTests.AssertDeepEqual(jsonValue2, JsonValue.Create("10"));
        }

        [Fact]
        public static void DeepEqualsJsonElement_Boolean()
        {
            JsonValue trueValue = JsonValue.Create(JsonElement.Parse("true"));
            JsonValue falseValue = JsonValue.Create(JsonElement.Parse("false"));

            JsonNodeTests.AssertNotDeepEqual(trueValue, falseValue);
            JsonNodeTests.AssertDeepEqual(trueValue, trueValue.DeepClone());
        }

        [Fact]
        public static void GetValueKind()
        {
            Assert.Equal(JsonValueKind.Object, JsonValue.Create(new Student()).GetValueKind());
            Assert.Equal(JsonValueKind.Array, JsonValue.Create(new Student[] { }).GetValueKind());

            using (JsonDocument document = JsonDocument.Parse("10"))
            {
                JsonValue jsonValue = JsonValue.Create(document.RootElement);
                Assert.Equal(JsonValueKind.Number, jsonValue.GetValueKind());
            }
        }

        [Theory]
        [InlineData(JsonNumberHandling.Strict, JsonValueKind.Number)]
        [InlineData(JsonNumberHandling.AllowReadingFromString, JsonValueKind.Number)]
        [InlineData(JsonNumberHandling.AllowNamedFloatingPointLiterals, JsonValueKind.Number)]
        [InlineData(JsonNumberHandling.WriteAsString, JsonValueKind.String)]
        [InlineData(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals, JsonValueKind.String)]
        public static void GetValueKind_NumberHandling(JsonNumberHandling numberHandling, JsonValueKind expectedKind)
        {
            JsonSerializerOptions options = new(JsonSerializerOptions.Default) { NumberHandling = numberHandling };
            JsonTypeInfo<int> typeInfo = options.GetTypeInfo<int>();
            JsonValue value = JsonValue.Create(42, typeInfo);
            Assert.Equal(expectedKind, value.GetValueKind());
        }

        [Fact]
        public static void DeepEquals_EscapedString()
        {
            JsonValue jsonValue = JsonValue.Create(JsonElement.Parse("\"It\'s alright\""));
            JsonValue escapedJsonValue = JsonValue.Create(JsonElement.Parse("\"It\\u0027s alright\""));
            JsonNodeTests.AssertDeepEqual(escapedJsonValue, jsonValue);
        }

        private class Student
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_ReturnExpectedTypeKind<T>(WrappedT<T> wrapped, JsonValueKind expectedKind)
        {
            T value = wrapped.Value;
            JsonNode node = JsonValue.Create(value);
            Assert.Equal(expectedKind, node.GetValueKind());
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_EqualThemselves<T>(WrappedT<T> wrapped, JsonValueKind _)
        {
            T value = wrapped.Value;
            JsonNode node = JsonValue.Create(value);
            Assert.True(JsonNode.DeepEquals(node, node));
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_EqualClonedValue<T>(WrappedT<T> wrapped, JsonValueKind _)
        {
            T value = wrapped.Value;
            JsonNode node = JsonValue.Create(value);
            JsonNode clone = node.DeepClone();

            Assert.True(JsonNode.DeepEquals(clone, clone));
            Assert.True(JsonNode.DeepEquals(node, clone));
            Assert.True(JsonNode.DeepEquals(clone, node));
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_EqualDeserializedValue<T>(WrappedT<T> wrapped, JsonValueKind _)
        {
            T value = wrapped.Value;
            JsonNode node = JsonValue.Create(value);
            JsonNode clone = JsonSerializer.Deserialize<JsonNode>(node.ToJsonString());

            Assert.True(JsonNode.DeepEquals(clone, clone));
            Assert.True(JsonNode.DeepEquals(node, clone));
            Assert.True(JsonNode.DeepEquals(clone, node));
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_DeepEquals_DifferentRepresentations<T>(WrappedT<T> wrapped, JsonValueKind _)
        {
            T value = wrapped.Value;
            string json = JsonSerializer.Serialize(value);
            JsonNode node = JsonSerializer.Deserialize<JsonNode>(json);
            JsonNode other = JsonSerializer.Deserialize<JsonArray>($"[{json}]")[0]; // JsonValueOfElement

            Assert.True(JsonNode.DeepEquals(other, other));
            Assert.True(JsonNode.DeepEquals(node, other));
            Assert.True(JsonNode.DeepEquals(other, node));
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_EqualClonedValue_DeserializedValue<T>(WrappedT<T> wrapped, JsonValueKind _)
        {
            T value = wrapped.Value;
            string json = JsonSerializer.Serialize(value);
            JsonNode node = JsonSerializer.Deserialize<JsonNode>(json);
            JsonNode clone = node.DeepClone();

            Assert.True(JsonNode.DeepEquals(clone, clone));
            Assert.True(JsonNode.DeepEquals(node, clone));
            Assert.True(JsonNode.DeepEquals(clone, node));
        }

        private static readonly HashSet<Type> s_convertibleTypes =
        [
            // True/False
            typeof(bool), typeof(bool?),

            // Number
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),

            typeof(byte?), typeof(sbyte?), typeof(short?), typeof(ushort?), typeof(int?), typeof(uint?),
            typeof(long?), typeof(ulong?), typeof(float?), typeof(double?), typeof(decimal?),

            // String
            typeof(char), typeof(char?),
            typeof(string),
            typeof(DateTimeOffset), typeof(DateTimeOffset?),
            typeof(DateTime), typeof(DateTime?),
            typeof(Guid), typeof(Guid?),
        ];

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_Conversion<T>(WrappedT<T> wrapped, JsonValueKind _)
        {
            T value = wrapped.Value;
            string json = JsonSerializer.Serialize(value);
            bool canGetValue = s_convertibleTypes.Contains(typeof(T));

            JsonValue jsonValue = JsonSerializer.Deserialize<JsonValue>(json)!;
            AssertExtensions.TrueExpression(jsonValue.TryGetValue(out T unused) == canGetValue);

            if (canGetValue)
            {
                // Assert no throw
                jsonValue.GetValue<T>();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonValue.GetValue<T>());
            }

            JsonValue jsonNode = (JsonValue)JsonSerializer.Deserialize<JsonNode>(json)!;
            AssertExtensions.TrueExpression(jsonNode.TryGetValue(out unused) == canGetValue);

            // Ensure the eager evaluation code path also produces the same result
            jsonNode = (JsonValue)JsonSerializer.Deserialize<JsonNode>(json, new JsonSerializerOptions { AllowDuplicateProperties = false })!;
            AssertExtensions.TrueExpression(jsonNode.TryGetValue(out unused) == canGetValue);
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveTypes))]
        public static void PrimitiveTypes_ReadOnlySequence<T>(WrappedT<T> wrapped, JsonValueKind _)
        {
            T value = wrapped.Value;

            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value);
            ReadOnlySequence<byte> seq = BufferFactory.Create([jsonBytes.AsMemory(0, 1), jsonBytes.AsMemory(1, jsonBytes.Length - 1)]);
            Utf8JsonReader reader = new Utf8JsonReader(seq);
            JsonValue jsonValueFromSequence = JsonSerializer.Deserialize<JsonValue>(ref reader)!;

            string jsonString = JsonSerializer.Serialize(value);
            JsonValue jsonValueFromString = JsonSerializer.Deserialize<JsonValue>(jsonString)!;

            AssertExtensions.TrueExpression(JsonNode.DeepEquals(jsonValueFromString, jsonValueFromSequence));
        }

        public static IEnumerable<object[]> GetPrimitiveTypes()
        {
            yield return Wrap(false, JsonValueKind.False);
            yield return Wrap(true, JsonValueKind.True);
            yield return Wrap((bool?)false, JsonValueKind.False);
            yield return Wrap((bool?)true, JsonValueKind.True);
            yield return Wrap((byte)42, JsonValueKind.Number);
            yield return Wrap((byte?)42, JsonValueKind.Number);
            yield return Wrap((sbyte)42, JsonValueKind.Number);
            yield return Wrap((sbyte?)42, JsonValueKind.Number);
            yield return Wrap((short)42, JsonValueKind.Number);
            yield return Wrap((short?)42, JsonValueKind.Number);
            yield return Wrap((ushort)42, JsonValueKind.Number);
            yield return Wrap((ushort?)42, JsonValueKind.Number);
            yield return Wrap(42, JsonValueKind.Number);
            yield return Wrap((int?)42, JsonValueKind.Number);
            yield return Wrap((uint)42, JsonValueKind.Number);
            yield return Wrap((uint?)42, JsonValueKind.Number);
            yield return Wrap((long)42, JsonValueKind.Number);
            yield return Wrap((long?)42, JsonValueKind.Number);
            yield return Wrap((ulong)42, JsonValueKind.Number);
            yield return Wrap((ulong?)42, JsonValueKind.Number);
            yield return Wrap(42.0f, JsonValueKind.Number);
            yield return Wrap((float?)42.0f, JsonValueKind.Number);
            yield return Wrap(42.0, JsonValueKind.Number);
            yield return Wrap((double?)42.0, JsonValueKind.Number);
            yield return Wrap(42.0m, JsonValueKind.Number);
            yield return Wrap((decimal?)42.0m, JsonValueKind.Number);
            yield return Wrap('A', JsonValueKind.String);
            yield return Wrap((char?)'A', JsonValueKind.String);
            yield return Wrap("A", JsonValueKind.String);
            yield return Wrap("A\u0041", JsonValueKind.String); // \u0041 == A
            yield return Wrap("A\u0022", JsonValueKind.String); // \u0022 == "
            yield return Wrap(new byte[] { 1, 2, 3 }, JsonValueKind.String);
            yield return Wrap(new DateTimeOffset(2024, 06, 20, 10, 29, 0, TimeSpan.Zero), JsonValueKind.String);
            yield return Wrap((DateTimeOffset?)new DateTimeOffset(2024, 06, 20, 10, 29, 0, TimeSpan.Zero), JsonValueKind.String);
            yield return Wrap(new DateTime(2024, 06, 20, 10, 29, 0), JsonValueKind.String);
            yield return Wrap((DateTime?)new DateTime(2024, 06, 20, 10, 29, 0), JsonValueKind.String);
            yield return Wrap(Guid.Empty, JsonValueKind.String);
            yield return Wrap((Guid?)Guid.Empty, JsonValueKind.String);
            yield return Wrap(new Uri("http://example.com"), JsonValueKind.String);
            yield return Wrap(new Version(1, 2, 3, 4), JsonValueKind.String);
            yield return Wrap(BindingFlags.Public, JsonValueKind.Number);
            yield return Wrap((BindingFlags?)BindingFlags.Public, JsonValueKind.Number);
#if NET
            yield return Wrap(Half.MaxValue, JsonValueKind.Number);
            yield return Wrap((Int128)42, JsonValueKind.Number);
            yield return Wrap((Int128)42, JsonValueKind.Number);
            yield return Wrap((Memory<byte>)new byte[] { 1, 2, 3 }, JsonValueKind.String);
            yield return Wrap((ReadOnlyMemory<byte>)new byte[] { 1, 2, 3 }, JsonValueKind.String);
            yield return Wrap(new DateOnly(2024, 06, 20), JsonValueKind.String);
            yield return Wrap(new TimeOnly(10, 29), JsonValueKind.String);
#endif
            static object[] Wrap<T>(T value, JsonValueKind expectedKind) => [new WrappedT<T> { Value = value }, expectedKind];
        }

        public class WrappedT<T>
        {
            public T Value;

            public override string ToString() => Value?.ToString();
        }

        [Theory]
        [InlineData("\"string\"")]
        [InlineData("42.0")]
        [InlineData("true")]
        [InlineData("false")]
        public static void PrimitiveTypes_ConverterThrows(string json)
        {
            JsonSerializerOptions opts = new JsonSerializerOptions
            {
                Converters = { new ThrowingConverter() }
            };

            JsonValue jsonValue = JsonSerializer.Deserialize<JsonValue>(json, opts);

            Assert.False(jsonValue.TryGetValue(out DummyClass unused));
            Assert.Throws<InvalidOperationException>(() => jsonValue.GetValue<DummyClass>());
        }

        private sealed class ThrowingConverter : JsonConverter<DummyClass>
        {
            public override DummyClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new JsonException();

            public override void Write(Utf8JsonWriter writer, DummyClass value, JsonSerializerOptions options) => Assert.Fail();
        }

        [Theory]
        [InlineData("\"string\"")]
        [InlineData("42.0")]
        [InlineData("true")]
        [InlineData("false")]
        public static void PrimitiveTypes_ConverterReturnsNull(string json)
        {
            JsonSerializerOptions opts = new JsonSerializerOptions
            {
                Converters = { new NullConverter() }
            };

            JsonValue jsonValue = JsonSerializer.Deserialize<JsonValue>(json, opts);

            Assert.False(jsonValue.TryGetValue(out DummyClass unused));
            Assert.Throws<InvalidOperationException>(() => jsonValue.GetValue<DummyClass>());
        }

        private sealed class NullConverter : JsonConverter<DummyClass>
        {
            public override DummyClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;

            public override void Write(Utf8JsonWriter writer, DummyClass value, JsonSerializerOptions options) => Assert.Fail();
        }

        [Theory]
        [InlineData("\"string\"")]
        [InlineData("42.0")]
        [InlineData("true")]
        [InlineData("false")]
        public static void PrimitiveTypes_NoTypeInfo(string json)
        {
            JsonSerializerOptions opts = new JsonSerializerOptions
            {
                TypeInfoResolver = new ExcludeType_TypeInfoResolver(typeof(DummyClass))
            };

            JsonValue jsonValue = JsonSerializer.Deserialize<JsonValue>(json, opts);

            Assert.False(jsonValue.TryGetValue(out DummyClass unused));
            Assert.Throws<InvalidOperationException>(() => jsonValue.GetValue<DummyClass>());
        }

        private class ExcludeType_TypeInfoResolver(Type excludeType) : IJsonTypeInfoResolver
        {
            private static readonly DefaultJsonTypeInfoResolver _defaultResolver = new DefaultJsonTypeInfoResolver();

            public Type ExcludeType { get; } = excludeType;

            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) =>
                type == ExcludeType ? null : _defaultResolver.GetTypeInfo(type, options);
        }

        private record DummyClass;
    }
}
