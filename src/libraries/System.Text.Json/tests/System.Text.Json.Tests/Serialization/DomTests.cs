// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Provides basic tests for serializing To\From DOM types including JsonDocument, JsonElement and JsonNode.
    /// </summary>
    /// The test class <see cref="System.Text.Json.Tests.Serialization.MetadataTests"/> provides tests for the JsonTypeInfo and JsonContext permutations.
    /// The test class <see cref="JsonSerializerApiValidation"/> provides tests for input validation.
    public static class DomTests
    {
        private const string Escaped_PlusSign = "\"\\u002B\""; // A '+' sign is escaped as hex.

        private class MyPoco
        {
            public static MyPoco Create()
            {
                return new MyPoco { StringProp = "Hello", IntArrayProp = new int[] { 1, 2 } };
            }

            [JsonPropertyOrder(0)]
            public string StringProp { get; set; }

            [JsonPropertyOrder(1)]
            public int[] IntArrayProp { get; set; }

            public void Verify()
            {
                Assert.Equal("Hello", StringProp);
                Assert.Equal(1, IntArrayProp[0]);
                Assert.Equal(2, IntArrayProp[1]);
            }
        }

        public const string Json =
            "{\"StringProp\":\"Hello\",\"IntArrayProp\":[1,2]}";

        [Fact]
        public static void JsonDocumentDeserialize_Generic()
        {
            using JsonDocument dom = JsonDocument.Parse(Json);
            MyPoco obj = dom.Deserialize<MyPoco>();
            obj.Verify();
        }

        [Fact]
        public static void JsonDocumentDeserialize_NonGeneric()
        {
            using JsonDocument dom = JsonDocument.Parse(Json);
            MyPoco obj = (MyPoco)dom.Deserialize(typeof(MyPoco));
            obj.Verify();
        }

        [Fact]
        public static void JsonDocumentDeserialize_Null()
        {
            using JsonDocument dom = JsonDocument.Parse("null");
            MyPoco obj = dom.Deserialize<MyPoco>();
            Assert.Null(obj);
        }

        [Fact]
        public static void JsonElementDeserialize_Generic()
        {
            using JsonDocument document = JsonDocument.Parse(Json);
            JsonElement dom = document.RootElement;
            MyPoco obj = JsonSerializer.Deserialize<MyPoco>(dom);
            obj.Verify();
        }

        [Fact]
        public static void JsonElementDeserialize_NonGeneric()
        {
            using JsonDocument document = JsonDocument.Parse(Json);
            JsonElement dom = document.RootElement;
            MyPoco obj = (MyPoco)JsonSerializer.Deserialize(dom, typeof(MyPoco));
            obj.Verify();
        }

        [Fact]
        public static void JsonElementDeserialize_Null()
        {
            using JsonDocument document = JsonDocument.Parse("null");
            JsonElement dom = document.RootElement;
            MyPoco obj = dom.Deserialize<MyPoco>();
            Assert.Null(obj);
        }

        [Fact]
        public static void JsonElementDeserialize_FromChildElement()
        {
            using JsonDocument document = JsonDocument.Parse(Json);
            JsonElement dom = document.RootElement.GetProperty("IntArrayProp");
            int[] arr = JsonSerializer.Deserialize<int[]>(dom);
            Assert.Equal(1, arr[0]);
            Assert.Equal(2, arr[1]);
        }

        [Fact]
        public static void JsonNodeDeserialize_Generic()
        {
            JsonNode dom = JsonNode.Parse(Json);
            MyPoco obj = dom.Deserialize<MyPoco>();
            obj.Verify();
        }

        [Fact]
        public static void JsonNodeDeserialize_NonGeneric()
        {
            JsonNode dom = JsonNode.Parse(Json);
            MyPoco obj = (MyPoco)dom.Deserialize(typeof(MyPoco));
            obj.Verify();
        }

        [Fact]
        public static void JsonNodeDeserialize_Null()
        {
            JsonNode node = null;
            MyPoco obj = JsonSerializer.Deserialize<MyPoco>(node);
            Assert.Null(obj);
        }

        [Fact]
        public static void JsonElementDeserialize_FromChildNode()
        {
            JsonNode dom = JsonNode.Parse(Json)["IntArrayProp"];
            int[] arr = JsonSerializer.Deserialize<int[]>(dom);
            Assert.Equal(1, arr[0]);
            Assert.Equal(2, arr[1]);
        }

        [Fact]
        public static void SerializeToDocument()
        {
            MyPoco obj = MyPoco.Create();
            using JsonDocument dom = JsonSerializer.SerializeToDocument(obj);

            JsonElement stringProp = dom.RootElement.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());

            JsonElement[] elements = dom.RootElement.GetProperty("IntArrayProp").EnumerateArray().ToArray();
            Assert.Equal(JsonValueKind.Number, elements[0].ValueKind);
            Assert.Equal(1, elements[0].GetInt32());
            Assert.Equal(JsonValueKind.Number, elements[1].ValueKind);
            Assert.Equal(2, elements[1].GetInt32());
        }

        [Fact]
        public static void SerializeToElement()
        {
            MyPoco obj = MyPoco.Create();
            JsonElement dom = JsonSerializer.SerializeToElement(obj);

            JsonElement stringProp = dom.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());

            JsonElement[] elements = dom.GetProperty("IntArrayProp").EnumerateArray().ToArray();
            Assert.Equal(JsonValueKind.Number, elements[0].ValueKind);
            Assert.Equal(1, elements[0].GetInt32());
            Assert.Equal(JsonValueKind.Number, elements[1].ValueKind);
            Assert.Equal(2, elements[1].GetInt32());
        }

        [Fact]
        public static void SerializeToNode()
        {
            MyPoco obj = MyPoco.Create();
            JsonNode dom = JsonSerializer.SerializeToNode(obj);

            JsonNode stringProp = dom["StringProp"];
            Assert.True(stringProp is JsonValue);
            Assert.Equal("Hello", stringProp.AsValue().GetValue<string>());

            JsonNode arrayProp = dom["IntArrayProp"];
            Assert.IsType<JsonArray>(arrayProp);
            Assert.Equal(1, arrayProp[0].AsValue().GetValue<int>());
            Assert.Equal(2, arrayProp[1].AsValue().GetValue<int>());
        }

        [Fact]
        public static void SerializeToDocument_WithEscaping()
        {
            using JsonDocument document = JsonSerializer.SerializeToDocument("+");
            JsonElement dom = document.RootElement;
            Assert.Equal(JsonValueKind.String, dom.ValueKind);
            Assert.Equal(Escaped_PlusSign, dom.GetRawText());

            string json = dom.Deserialize<string>();
            Assert.Equal("+", json);
        }

        [Fact]
        public static void SerializeToElement_WithEscaping()
        {
            JsonElement dom = JsonSerializer.SerializeToElement("+");
            Assert.Equal(JsonValueKind.String, dom.ValueKind);
            Assert.Equal(Escaped_PlusSign, dom.GetRawText());

            string json = dom.Deserialize<string>();
            Assert.Equal("+", json);
        }

        [Fact]
        public static void SerializeToNode_WithEscaping()
        {
            JsonNode dom = JsonSerializer.SerializeToNode("+");
            Assert.Equal(Escaped_PlusSign, dom.ToJsonString());

            string json = dom.Deserialize<string>();
            Assert.Equal("+", json);
        }
    }
}
