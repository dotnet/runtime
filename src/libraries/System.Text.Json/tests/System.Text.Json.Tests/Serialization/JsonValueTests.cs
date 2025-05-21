// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Nodes;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class JsonValueTests
    {
        [Fact]
        public static void DeserializeArrayInDictionary_JsonValue_Works()
        {
            string json = """
                { "names": ["Chuck"] }
                """;

            Dictionary<string, JsonValue> dict = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(json);
            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey("names"));
            Assert.NotNull(dict["names"]);
            Assert.Equal(JsonValueKind.Array, dict["names"].GetValueKind());

            // Also validate we can correctly get the content
            string arrayValue = dict["names"].ToString();
            Assert.Contains("Chuck", arrayValue);
        }

        [Fact]
        public static void DeserializeObjectInDictionary_JsonValue_Works()
        {
            string json = """
                { "person": {"name": "Chuck"} }
                """;

            Dictionary<string, JsonValue> dict = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(json);
            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey("person"));
            Assert.NotNull(dict["person"]);
            Assert.Equal(JsonValueKind.Object, dict["person"].GetValueKind());

            // Also validate we can correctly get the content
            string objectValue = dict["person"].ToString();
            Assert.Contains("Chuck", objectValue);
        }

        [Fact]
        public static void DeserializeArrayInDictionary_JsonNode_Works()
        {
            string json = """
                { "names": ["Chuck"] }
                """;

            Dictionary<string, JsonNode> dict = JsonSerializer.Deserialize<Dictionary<string, JsonNode>>(json);
            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey("names"));
            Assert.NotNull(dict["names"]);
            Assert.Equal(JsonValueKind.Array, dict["names"].GetValueKind());
            Assert.IsType<JsonArray>(dict["names"]);

            // Also validate we can correctly get the content
            string arrayValue = dict["names"].ToString();
            Assert.Contains("Chuck", arrayValue);
        }

        [Fact]
        public static void Issue113268_JsonValueHandlesArraysAndObjects()
        {
            // This test specifically covers the issue reported in dotnet/runtime#113268
            // where deserialization of Dictionary<string, JsonValue> containing arrays 
            // failed in .NET 9.0 but worked in .NET 8.0

            string json = """
                { "names": ["Chuck"] }
                """;

            Dictionary<string, JsonValue> dict = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(json);
            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey("names"));
            
            // The key issue is that this line would throw in .NET 9.0 before the fix
            JsonValue jsonValue = dict["names"]; 
            Assert.NotNull(jsonValue);
            
            // Additional validation
            Assert.Equal(JsonValueKind.Array, jsonValue.GetValueKind());
            
            // Test with an object too
            json = """
                { "person": {"name": "Chuck"} }
                """;

            dict = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(json);
            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey("person"));
            
            jsonValue = dict["person"];
            Assert.NotNull(jsonValue);
            Assert.Equal(JsonValueKind.Object, jsonValue.GetValueKind());
        }
    }
}