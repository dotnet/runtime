// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonNodeToJsonElementTests
    {
        [Fact]
        public static void TestAsJsonElement()
        {
            var jsonObject = new JsonObject
            {
                { "text", "property value" },
                { "boolean", true },
                { "number", 15 },
                { "null node", (JsonNode) null },
                { "array", new JsonString[] { "value1", "value2"} }
            };

            JsonElement jsonElement = jsonObject.AsJsonElement();
            Assert.False(jsonElement.IsImmutable);

            JsonElement.ObjectEnumerator enumerator = jsonElement.EnumerateObject();

            enumerator.MoveNext();
            Assert.Equal("text", enumerator.Current.Name);
            Assert.Equal(JsonValueKind.String, enumerator.Current.Value.ValueKind);
            Assert.Equal("property value", enumerator.Current.Value.GetString());
           
            enumerator.MoveNext();
            Assert.Equal("boolean", enumerator.Current.Name);
            Assert.Equal(JsonValueKind.True, enumerator.Current.Value.ValueKind);
            Assert.True(enumerator.Current.Value.GetBoolean());
            
            enumerator.MoveNext();
            Assert.Equal("number", enumerator.Current.Name);
            Assert.Equal(15, enumerator.Current.Value.GetInt32());
            Assert.Equal(JsonValueKind.Number, enumerator.Current.Value.ValueKind);

            enumerator.MoveNext();
            Assert.Equal("null node", enumerator.Current.Name);
            Assert.Equal(JsonValueKind.Null, enumerator.Current.Value.ValueKind);

            enumerator.MoveNext();
            Assert.Equal("array", enumerator.Current.Name);
            Assert.Equal(2, enumerator.Current.Value.GetArrayLength());
            Assert.Equal(JsonValueKind.Array, enumerator.Current.Value.ValueKind);
            JsonElement.ArrayEnumerator innerEnumerator = enumerator.Current.Value.EnumerateArray();

            innerEnumerator.MoveNext();
            Assert.Equal(JsonValueKind.String, innerEnumerator.Current.ValueKind);
            Assert.Equal("value1", innerEnumerator.Current.GetString());
            
            innerEnumerator.MoveNext();
            Assert.Equal(JsonValueKind.String, innerEnumerator.Current.ValueKind);
            Assert.Equal("value2", innerEnumerator.Current.GetString());

            Assert.False(innerEnumerator.MoveNext());
            innerEnumerator.Dispose();

            Assert.False(enumerator.MoveNext());
            enumerator.Dispose();

            // Modifying JsonObject will change JsonElement:

            jsonObject["text"] = new JsonNumber("123");
            Assert.Equal(123, jsonElement.GetProperty("text").GetInt32());
        }

        [Fact]
        public static void TestArrayIterator()
        {
            JsonArray array = new JsonArray { 1, 2, 3 };
            JsonElement jsonNodeElement = array.AsJsonElement();
            IEnumerator enumerator = jsonNodeElement.EnumerateArray();
            array.Add(4);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public static void TestGetNode()
        {
            var jsonObject = new JsonObject
            {
                { "text", "property value" },
                { "boolean", true },
                { "number", 15 },
                { "null node", (JsonNode) null },
                { "array", new JsonString[] { "value1", "value2"} }
            };

            JsonElement jsonElement = jsonObject.AsJsonElement();
            JsonObject jsonObjectFromElement = (JsonObject)JsonNode.GetNode(jsonElement);

            Assert.Equal("property value", (JsonString)jsonObjectFromElement["text"]);

            // Modifying JsonObject will change JsonObjectFromElement:

            jsonObject["text"] = new JsonString("something different");
            Assert.Equal("something different", (JsonString)jsonObjectFromElement["text"]);

            // Modifying JsonObjectFromElement will change JsonObject:

            ((JsonBoolean)jsonObjectFromElement["boolean"]).Value = false;
            Assert.False(((JsonBoolean)jsonObject["boolean"]).Value);
        }
    }
}
