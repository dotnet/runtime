// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonNodeParseTests
    {
        private static string jsonSampleString = @"
            {
                ""text"": ""property value"",
                ""boolean true"": true,
                ""boolean false"": false,
                ""null"": null,
                ""int"": 17,
                ""double"": 3.14,
                ""scientific"": 3e100,
                ""array"" : [1,2,3],
                ""inner object"" : 
                {
                    ""inner property"" : ""value""
                }
            }";

        [Fact]
        public static void TestParseStringToJsonNode()
        {
            JsonNode node = JsonNode.Parse(jsonSampleString);

            var jsonObject = (JsonObject)node;
            Assert.Equal(9, jsonObject.PropertyNames.Count);
            Assert.Equal(9, jsonObject.PropertyValues.Count);
            Assert.Equal("property value", (JsonString)jsonObject["text"]);
            Assert.True(((JsonBoolean)jsonObject["boolean true"]).Value);
            Assert.False(((JsonBoolean)jsonObject["boolean false"]).Value);
            Assert.Null(jsonObject["null"]);
            Assert.Equal(17, ((JsonNumber)jsonObject["int"]).GetInt32());
            Assert.Equal(3.14, ((JsonNumber)jsonObject["double"]).GetDouble());
            Assert.Equal("3e100", ((JsonNumber)jsonObject["scientific"]).ToString());

            var innerArray = (JsonArray)jsonObject["array"];
            Assert.Equal(3, innerArray.Count);
            Assert.Equal(1, ((JsonNumber)innerArray[0]).GetInt32());
            Assert.Equal(2, ((JsonNumber)innerArray[1]).GetInt32());
            Assert.Equal(3, ((JsonNumber)innerArray[2]).GetInt32());

            var innerObject = (JsonObject)jsonObject["inner object"];
            Assert.Equal(1, innerObject.PropertyNames.Count);
            Assert.Equal(1, innerObject.PropertyValues.Count);
            Assert.Equal("value", (JsonString)innerObject["inner property"]);
        }
    }
}
