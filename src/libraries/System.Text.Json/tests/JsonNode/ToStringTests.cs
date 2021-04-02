// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static class ToStringTests
    {
        internal const string JsonWithWhitespace = "{\r\n  \"MyString\": \"Hello!\",\r\n  \"MyNull\": null,\r\n  \"MyBoolean\": false,\r\n  \"MyArray\": [\r\n    2,\r\n    3,\r\n    42\r\n  ],\r\n  \"MyInt\": 43,\r\n  \"MyDateTime\": \"2020-07-08T00:00:00\",\r\n  \"MyGuid\": \"ed957609-cdfe-412f-88c1-02daca1b4f51\",\r\n  \"MyObject\": {\r\n    \"MyString\": \"Hello!!\"\r\n  },\r\n  \"Child\": {\r\n    \"ChildProp\": 1\r\n  }\r\n}";

        [Fact]
        public static void NodeToString()
        {
            JsonNode node = JsonNode.Parse(JsonNodeTests.ExpectedDomJson);
            string json = node.ToString();
            Assert.Equal(JsonWithWhitespace, json);
        }

        [Fact]
        public static void NodeToString_StringValuesNotQuoted()
        {
            JsonNode node = JsonNode.Parse("\"Hello\"");
            string json = node.ToString();
            Assert.Equal("Hello", json);

            node = JsonValue.Create("Hello");
            json = node.ToString();
            Assert.Equal("Hello", json);
        }
    }
}
