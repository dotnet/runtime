// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonNodeOptionsTests
    {
        [Fact]
        public static void TestParseFailsWhenExceedsMaxDepth()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                builder.Append("[");
            }

            for (int i = 0; i < 100; i++)
            {
                builder.Append("]");
            }

            // Test for default MaxDepth
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(builder.ToString()));
            JsonNodeOptions options = new JsonNodeOptions { MaxDepth = default };
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(builder.ToString(), options));

            // Test for MaxDepth of 5
            options = new JsonNodeOptions { MaxDepth = 5 };
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(builder.ToString(), options));
        }

        [Fact]
        public static void TestJsonCommentHandling()
        {
            string jsonStringWithComments = @"
            {
                // First comment
                ""firstProperty"": ""first value"", //Second comment
                ""secondProperty"" : ""second value""// Third comment
                // Last comment
            }";

            JsonNodeOptions options = new JsonNodeOptions { CommentHandling = default };
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(jsonStringWithComments, options));

            options = new JsonNodeOptions { CommentHandling = JsonCommentHandling.Disallow };
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(jsonStringWithComments, options));

            options = new JsonNodeOptions { CommentHandling = JsonCommentHandling.Skip };
            JsonObject jsonObject = (JsonObject)JsonNode.Parse(jsonStringWithComments, options);

            Assert.Equal("first value", jsonObject["firstProperty"]);
            Assert.Equal("second value", jsonObject["secondProperty"]);

            Assert.Equal(2, jsonObject.GetPropertyNames().Count);
            Assert.Equal(2, jsonObject.GetPropertyValues().Count);
        }

        [Fact]
        public static void TestTrailingCommas()
        {
            string jsonStringWithTrailingCommas = @"
            {
                ""firstProperty"": ""first value"",
                ""secondProperty"" : ""second value"",
            }";

            JsonNodeOptions options = new JsonNodeOptions { AllowTrailingCommas = default };
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(jsonStringWithTrailingCommas, options));

            options = new JsonNodeOptions { AllowTrailingCommas = false };
            Assert.ThrowsAny<JsonException>(() => JsonNode.Parse(jsonStringWithTrailingCommas, options));

            options = new JsonNodeOptions { AllowTrailingCommas = true };
            JsonObject jsonObject = (JsonObject) JsonNode.Parse(jsonStringWithTrailingCommas, options);

            Assert.Equal("first value", jsonObject["firstProperty"]);
            Assert.Equal("second value", jsonObject["secondProperty"]);

            Assert.Equal(2, jsonObject.GetPropertyNames().Count);
            Assert.Equal(2, jsonObject.GetPropertyValues().Count);
        }

        [Fact]
        public static void TestInvalidJsonNodeOptions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonNodeOptions()
            {
                CommentHandling = (JsonCommentHandling)Enum.GetNames(typeof(JsonCommentHandling)).Length
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonNodeOptions()
            {
                MaxDepth = -1
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonNodeOptions()
            {
                DuplicatePropertyNameHandling = (DuplicatePropertyNameHandlingStrategy)Enum.GetNames(typeof(DuplicatePropertyNameHandlingStrategy)).Length
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonNodeOptions()
            {
                DuplicatePropertyNameHandling = (DuplicatePropertyNameHandlingStrategy)(-1)
            });
        }

        [Fact]
        public static void TestDefaultJsonNodeOptions()
        {
            JsonNodeOptions defaultOptions = default;
            JsonNodeOptions newOptions = new JsonNodeOptions();

            Assert.Equal(defaultOptions.AllowTrailingCommas, newOptions.AllowTrailingCommas);
            Assert.Equal(defaultOptions.CommentHandling, newOptions.CommentHandling);
            Assert.Equal(defaultOptions.DuplicatePropertyNameHandling, newOptions.DuplicatePropertyNameHandling);
            Assert.Equal(defaultOptions.MaxDepth, newOptions.MaxDepth);

            Assert.False(defaultOptions.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, defaultOptions.CommentHandling);
            Assert.Equal(DuplicatePropertyNameHandlingStrategy.Replace, defaultOptions.DuplicatePropertyNameHandling);
            Assert.Equal(0, defaultOptions.MaxDepth);
        }
    }
}
