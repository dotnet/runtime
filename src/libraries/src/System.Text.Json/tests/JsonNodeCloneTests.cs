// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonNodeCloneTests
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
                { "array", new JsonString[] { "value1", "value2"} }
            };

            var jsonObjectCopy = (JsonObject)jsonObject.Clone();

            jsonObject["text"] = (JsonString)"something different";
            Assert.Equal("property value", (JsonString)jsonObjectCopy["text"]);

            ((JsonBoolean)jsonObject["boolean"]).Value = false;
            Assert.True(((JsonBoolean)jsonObjectCopy["boolean"]).Value);

            Assert.Equal(2, jsonObjectCopy.GetJsonArrayPropertyValue("array").Count);
            jsonObject.GetJsonArrayPropertyValue("array").Add("value3");
            Assert.Equal(2, jsonObjectCopy.GetJsonArrayPropertyValue("array").Count);

            jsonObject.Add("new one", 123);
            Assert.Equal(4, jsonObjectCopy.PropertyNames.Count);
        }
    }
}
