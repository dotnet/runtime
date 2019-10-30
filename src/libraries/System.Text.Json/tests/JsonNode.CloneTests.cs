// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonNodeTests
    { 
        [Fact]
        public static void TestCloneJsonArray()
        {
            var jsonArray = new JsonArray { "value1", "value2" };
            var jsonArrayCopy = jsonArray.Clone() as JsonArray;
            Assert.Equal(2, jsonArrayCopy.Count);
            jsonArray.Add("value3");
            Assert.Equal(2, jsonArrayCopy.Count);
        }

        [Fact]
        public static void TestDeepCloneJsonArray()
        {
            JsonArray inner = new JsonArray { 1, 2, 3 };
            JsonArray outer = new JsonArray { inner };
            JsonArray outerClone = (JsonArray)outer.Clone();
            ((JsonArray) outerClone[0]).Add(4);

            Assert.Equal(3, inner.Count);
        }

        [Fact]
        public static void TestCloneJsonNode()
        {
            var jsonObject = new JsonObject
            {
                { "text", "property value" },
                { "boolean", true },
                { "number", 15 },
                { "array", new JsonArray { "value1", "value2"} },
                { "null", null }
            };

            var jsonObjectCopy = (JsonObject)jsonObject.Clone();
            Assert.Equal(5, jsonObjectCopy.GetPropertyNames().Count);
            Assert.Equal(5, jsonObjectCopy.GetPropertyValues().Count);

            jsonObject["text"] = "something different";
            Assert.Equal("property value", jsonObjectCopy["text"]);

            ((JsonBoolean)jsonObject["boolean"]).Value = false;
            Assert.True(((JsonBoolean)jsonObjectCopy["boolean"]).Value);

            Assert.Equal(2, jsonObjectCopy.GetJsonArrayPropertyValue("array").Count);
            jsonObject.GetJsonArrayPropertyValue("array").Add("value3");
            Assert.Equal(2, jsonObjectCopy.GetJsonArrayPropertyValue("array").Count);

            Assert.IsType<JsonNull>(jsonObjectCopy["null"]);

            jsonObject.Add("new one", 123);
            Assert.Equal(5, jsonObjectCopy.GetPropertyNames().Count);
        }

        [Fact]
        public static void TestCloneJsonNodeInJsonElement()
        {
            var jsonObject = new JsonObject
            {
                { "text", "value" },
                { "boolean", true },
                { "array", new JsonArray { "value1", "value2"} }
            };

            JsonElement jsonElement = jsonObject.AsJsonElement();

            var jsonObjectCloneFromElement = (JsonObject)JsonNode.DeepCopy(jsonElement);

            Assert.Equal(3, jsonObjectCloneFromElement.GetPropertyNames().Count);
            Assert.Equal(3, jsonObjectCloneFromElement.GetPropertyValues().Count);

            Assert.Equal("value", jsonObjectCloneFromElement["text"]);
            Assert.Equal(true, jsonObjectCloneFromElement["boolean"]);

            // Modifying should not change the clone and vice versa:
            
            jsonObjectCloneFromElement["boolean"] = false;
            Assert.Equal(false, jsonObjectCloneFromElement["boolean"]);
            Assert.Equal(true, jsonObject["boolean"]);

            jsonObjectCloneFromElement.GetJsonArrayPropertyValue("array").Add("value3");
            Assert.Equal(3, jsonObjectCloneFromElement.GetJsonArrayPropertyValue("array").Count);
            Assert.Equal(2, jsonObject.GetJsonArrayPropertyValue("array").Count);

            jsonObject["text"] = "different value";
            Assert.Equal("different value", jsonObject["text"]);
            Assert.Equal("value", jsonObjectCloneFromElement["text"]);

            jsonObject.GetJsonArrayPropertyValue("array").Remove("value2");
            Assert.Equal(1, jsonObject.GetJsonArrayPropertyValue("array").Count);
            Assert.Equal(3, jsonObjectCloneFromElement.GetJsonArrayPropertyValue("array").Count);
        }
    }
}
