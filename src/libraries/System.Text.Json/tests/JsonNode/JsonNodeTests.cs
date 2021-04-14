// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Node.Tests
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
        }

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
    }
}
