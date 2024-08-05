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
    }
}
