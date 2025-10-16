// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Nodes.Tests
{
    public static partial class JsonNodeTests
    {
        [Fact]
        public static void JsonTypes_Deserialize()
        {
            Assert.IsType<JsonObject>(JsonSerializer.Deserialize<JsonNode>("{}"));
            Assert.IsType<JsonObject>(JsonNode.Parse("{}"));
            Assert.IsType<JsonObject>(JsonNode.Parse(ToUtf8("{}")));
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("{}"));

            Assert.IsType<JsonArray>(JsonSerializer.Deserialize<JsonNode>("[]"));
            Assert.IsType<JsonArray>(JsonNode.Parse("[]"));
            Assert.IsType<JsonArray>(JsonNode.Parse(ToUtf8("[]")));
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("[]"));

            Assert.IsAssignableFrom<JsonValue>(JsonSerializer.Deserialize<JsonNode>("true"));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse("true"));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse(ToUtf8("true")));
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("true"));

            Assert.IsAssignableFrom<JsonValue>(JsonSerializer.Deserialize<JsonNode>("0"));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse("0"));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse(ToUtf8("0")));
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("0"));

            Assert.IsAssignableFrom<JsonValue>(JsonSerializer.Deserialize<JsonNode>("1.2"));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse("1.2"));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse(ToUtf8("1.2")));
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("1.2"));

            Assert.IsAssignableFrom<JsonValue>(JsonSerializer.Deserialize<JsonNode>("\"str\""));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse("\"str\""));
            Assert.IsAssignableFrom<JsonValue>(JsonNode.Parse(ToUtf8("\"str\"")));
            Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>("\"str\""));

            JsonType_Deserializes_Null<JsonNode>();
            JsonType_Deserializes_Null<JsonArray>();
            JsonType_Deserializes_Null<JsonObject>();
        }

        [Fact]
        public static void Parse_AllowMultipleValues_TrailingJson()
        {
            var options = new JsonReaderOptions { AllowMultipleValues = true };
            var reader = new Utf8JsonReader("[null,false,42,{},[1]]             [43]"u8, options);

            JsonNode node = JsonNode.Parse(ref reader);
            Assert.Equal("[null,false,42,{},[1]]", node.ToJsonString());
            Assert.Equal(JsonTokenType.EndArray, reader.TokenType);

            Assert.True(reader.Read());
            node = JsonNode.Parse(ref reader);
            Assert.Equal("[43]", node.ToJsonString());

            Assert.False(reader.Read());
        }


        [Fact]
        public static void Parse_AllowMultipleValues_TrailingContent()
        {
            var options = new JsonReaderOptions { AllowMultipleValues = true };
            var reader = new Utf8JsonReader("[null,false,42,{},[1]]             <NotJson/>"u8, options);

            JsonNode node = JsonNode.Parse(ref reader);
            Assert.Equal("[null,false,42,{},[1]]", node.ToJsonString());
            Assert.Equal(JsonTokenType.EndArray, reader.TokenType);

            JsonTestHelper.AssertThrows<JsonException>(ref reader, (ref Utf8JsonReader reader) => reader.Read());
        }

        private static void JsonType_Deserializes_Null<TNode>() where TNode : JsonNode
        {
            Assert.Null(JsonSerializer.Deserialize<TNode>("null"));
            Assert.Collection(JsonSerializer.Deserialize<TNode[]>("[null]"), Assert.Null);
            Assert.Collection(JsonSerializer.Deserialize<IReadOnlyDictionary<string, TNode>>("{ \"Value\": null }"), kv => Assert.Null(kv.Value));
            Assert.Null(JsonSerializer.Deserialize<ObjectWithNodeProperty<TNode>>("{ \"Value\": null }").Value);
        }
        private record ObjectWithNodeProperty<TNode>(TNode Value) where TNode : JsonNode;

        [Fact]
        public static void AsMethods_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("{}").AsArray());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("{}").AsValue());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("[]").AsObject());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("[]").AsValue());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("1").AsArray());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("1").AsObject());
        }

        [Fact]
        public static void NullHandling()
        {
            var options = new JsonSerializerOptions();
            JsonNode obj = JsonSerializer.Deserialize<JsonNode>("null", options);
            Assert.Null(obj);
        }

        private static byte[] ToUtf8(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        [Fact]
        public static void GetValue_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("{}").GetValue<object>());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("[]").GetValue<object>());
        }

        [Fact]
        public static void GetValueKind()
        {
            Assert.Equal(JsonValueKind.Object, JsonNode.Parse("{}").GetValueKind());
            Assert.Equal(JsonValueKind.Array, JsonNode.Parse("[]").GetValueKind());
            Assert.Equal(JsonValueKind.Number, JsonNode.Parse("12").GetValueKind());
            Assert.Equal(JsonValueKind.String, JsonNode.Parse("\"12\"").GetValueKind());
            Assert.Equal(JsonValueKind.True, JsonNode.Parse("true").GetValueKind());
            Assert.Equal(JsonValueKind.False, JsonNode.Parse("false").GetValueKind());
        }

        [Fact]
        public static void GetPropertyName()
        {
            JsonNode jsonNode = JsonNode.Parse("{\"a\" : \"b\"}");
            Assert.Equal("a", jsonNode["a"].GetPropertyName());

            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("[]").GetPropertyName());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("5").GetPropertyName());
        }

        [Fact]
        public static void GetElementIndex()
        {
            JsonNode jsonNode = JsonNode.Parse("[90, \"str\", true, false]");
            Assert.Equal(0, jsonNode[0].GetElementIndex());
            Assert.Equal(1, jsonNode[1].GetElementIndex());
            Assert.Equal(2, jsonNode[2].GetElementIndex());
            Assert.Equal(3, jsonNode[3].GetElementIndex());

            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("{}").GetElementIndex());
            Assert.Throws<InvalidOperationException>(() => JsonNode.Parse("5").GetElementIndex());
        }


        [Fact]
        public static void ReplaceWith()
        {
            JsonNode jsonNode = JsonNode.Parse("[90, 2, 3]");
            jsonNode[1].ReplaceWith(12);
            jsonNode[2].ReplaceWith("str");

            Assert.Equal(12, jsonNode[1].GetValue<int>());
            Assert.Equal("str", jsonNode[2].GetValue<string>());

            Assert.Equal("[90,12,\"str\"]", jsonNode.ToJsonString());

            jsonNode = JsonNode.Parse("{\"a\": \"b\"}");
            jsonNode["a"].ReplaceWith("c");
            Assert.Equal("{\"a\":\"c\"}", jsonNode.ToJsonString());
        }

        [Theory]
        [InlineData("null", "  null       ")]
        [InlineData("false", "   false  ")]
        [InlineData("true", "   true  ")]
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
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001",

            "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001" + "E-512")]
        [InlineData("\"\"", "   \"\"")]
        [InlineData("\"ABC\"", "\"\\u0041\\u0042\\u0043\"")]
        [InlineData("{ }", " {    }")]
        [InlineData("""{ "x" : 1, "y" : 2 }""", """{ "x" : 1, "y" : 2 }""")]
        [InlineData("""{ "x" : 1, "y" : 2 }""", """{ "y" : 2, "x" : 1 }""")]
        [InlineData("""[]""", """ [ ]""")]
        [InlineData("""[1, 2, 3]""", """ [1,  2,  3  ]""")]
        [InlineData("""[null, false, 3.14, "ABC", { "x" : 1, "y" : 2 }, []]""",
            """[null, false, 314e-2, "\u0041\u0042\u0043", { "y" : 2, "x" : 1 }, [ ] ]""")]
        public static void DeepEquals_EqualValuesReturnTrue(string value1, string value2)
        {
            JsonNode node1 = JsonNode.Parse(value1);
            JsonNode node2 = JsonNode.Parse(value2);

            AssertDeepEqual(node1, node2);
        }

        [Theory]
        // Kind mismatch
        [InlineData("null", "false")]
        [InlineData("null", "42")]
        [InlineData("null", "\"str\"")]
        [InlineData("null", "{}")]
        [InlineData("null", "[]")]
        [InlineData("false", "42")]
        [InlineData("false", "\"str\"")]
        [InlineData("false", "{}")]
        [InlineData("false", "[]")]
        [InlineData("42", "\"str\"")]
        [InlineData("42", "{}")]
        [InlineData("42", "[]")]
        [InlineData("\"str\"", "{}")]
        [InlineData("\"str\"", "[]")]
        [InlineData("{}", "[]")]
        // Value mismatch
        [InlineData("false", "true")]
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
        [InlineData("\"\"", "   \" \"")]
        [InlineData("\"ABC\"", "   \"ABc\"")]
        [InlineData("[1]", "[]")]
        [InlineData("[1]", "[2]")]
        [InlineData("[1,2,3]", "[1,2,3,4]")]
        [InlineData("[1,2,3]", "[1,3,2]")]
        [InlineData("{}", """{ "Prop" : null }""")]
        [InlineData("""{ "Prop" : 1 }""", """{ "Prop" : null }""")]
        [InlineData("""{ "Prop1" : 1 }""", """{ "Prop1" : 1, "Prop2" : 2 }""")]
        [InlineData("""{ "Prop1" : 1, "Prop2": {} }""", """{ "Prop1" : 1, "Prop2" : 2 }""")]
        [InlineData("""{ "Prop1" : 1, "Prop2": {}, "Prop3": false }""", """{ "Prop1" : 1, "Prop2" : { "c" : null }, "Prop3" : false }""")]
        [InlineData("""{ "Prop1" : 1, "Prop2": {}, "Prop3": false }""", """{ "Prop1" : 1, "Prop3" : true, "Prop2" : {} }""")]
        // Regression tests for https://github.com/dotnet/runtime/issues/112769
        [InlineData("""{"test1":null}""", """{"test2":null}""")]
        [InlineData("""{"test1":null, "test2":null}""", """{"test3":null, "test4":null}""")]
        [InlineData("""{"test1":null, "test2":null}""", """{"test3":null}""")]
        [InlineData("""{"test1":null}""", """{"test2":[null]}""")]
        public static void DeepEquals_NotEqualValuesReturnFalse(string value1, string value2)
        {
            JsonNode obj1 = JsonNode.Parse(value1);
            JsonNode obj2 = JsonNode.Parse(value2);

            AssertNotDeepEqual(obj1, obj2);
        }
    }
}
