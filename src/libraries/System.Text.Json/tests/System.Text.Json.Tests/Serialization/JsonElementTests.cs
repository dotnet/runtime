// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class JsonElementTests
    {
        [Fact]
        public void SerializeJsonElement()
        {
            JsonElementClass obj = JsonSerializer.Deserialize<JsonElementClass>(JsonElementClass.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(JsonElementClass.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Deserialize<JsonElementClass>(reserialized);
            obj.Verify();
        }

        [Fact]
        public void SerializeJsonElementArray()
        {
            JsonElementArrayClass obj = JsonSerializer.Deserialize<JsonElementArrayClass>(JsonElementArrayClass.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(JsonElementArrayClass.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Deserialize<JsonElementArrayClass>(reserialized);
            obj.Verify();
        }

        [Theory,
            InlineData(5),
            InlineData(10),
            InlineData(20),
            InlineData(1024)]
        public void ReadJsonElementFromStream(int defaultBufferSize)
        {
            // Streams need to read ahead when they hit objects or arrays that are assigned to JsonElement or object.

            byte[] data = Encoding.UTF8.GetBytes(@"{""Data"":[1,true,{""City"":""MyCity""},null,""foo""]}");
            MemoryStream stream = new MemoryStream(data);
            JsonElement obj = JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result;

            data = Encoding.UTF8.GetBytes(@"[1,true,{""City"":""MyCity""},null,""foo""]");
            stream = new MemoryStream(data);
            obj = JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result;

            // Ensure we fail with incomplete data
            data = Encoding.UTF8.GetBytes(@"{""Data"":[1,true,{""City"":""MyCity""},null,""foo""]");
            stream = new MemoryStream(data);
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result);

            data = Encoding.UTF8.GetBytes(@"[1,true,{""City"":""MyCity""},null,""foo""");
            stream = new MemoryStream(data);
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result);
        }
    }
}
