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

        public class JsonElementClass : ITestClass
        {
            public JsonElement Number { get; set; }
            public JsonElement True { get; set; }
            public JsonElement False { get; set; }
            public JsonElement String { get; set; }
            public JsonElement Array { get; set; }
            public JsonElement Object { get; set; }
            public JsonElement Null { get; set; }

            public static readonly string s_json =
                @"{" +
                    @"""Number"" : 1," +
                    @"""True"" : true," +
                    @"""False"" : false," +
                    @"""String"" : ""Hello""," +
                    @"""Array"" : [2, false, true, ""Goodbye""]," +
                    @"""Object"" : {}," +
                    @"""Null"" : null" +
                @"}";

            public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

            public void Initialize()
            {
                Number = JsonDocument.Parse(@"1").RootElement.Clone();
                True = JsonDocument.Parse(@"true").RootElement.Clone();
                False = JsonDocument.Parse(@"false").RootElement.Clone();
                String = JsonDocument.Parse(@"""Hello""").RootElement.Clone();
                Array = JsonDocument.Parse(@"[2, false, true, ""Goodbye""]").RootElement.Clone();
                Object = JsonDocument.Parse(@"{}").RootElement.Clone();
                Null = JsonDocument.Parse(@"null").RootElement.Clone();
            }

            public void Verify()
            {
                Assert.Equal(JsonValueKind.Number, Number.ValueKind);
                Assert.Equal("1", Number.ToString());
                Assert.Equal(JsonValueKind.True, True.ValueKind);
                Assert.Equal("True", True.ToString());
                Assert.Equal(JsonValueKind.False, False.ValueKind);
                Assert.Equal("False", False.ToString());
                Assert.Equal(JsonValueKind.String, String.ValueKind);
                Assert.Equal("Hello", String.ToString());
                Assert.Equal(JsonValueKind.Array, Array.ValueKind);
                JsonElement[] elements = Array.EnumerateArray().ToArray();
                Assert.Equal(JsonValueKind.Number, elements[0].ValueKind);
                Assert.Equal("2", elements[0].ToString());
                Assert.Equal(JsonValueKind.False, elements[1].ValueKind);
                Assert.Equal("False", elements[1].ToString());
                Assert.Equal(JsonValueKind.True, elements[2].ValueKind);
                Assert.Equal("True", elements[2].ToString());
                Assert.Equal(JsonValueKind.String, elements[3].ValueKind);
                Assert.Equal("Goodbye", elements[3].ToString());
                Assert.Equal(JsonValueKind.Object, Object.ValueKind);
                Assert.Equal("{}", Object.ToString());
                Assert.Equal(JsonValueKind.Null, Null.ValueKind);
                Assert.Equal("", Null.ToString()); // JsonElement returns empty string for null.
            }
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

        public class JsonElementArrayClass : ITestClass
        {
            public JsonElement[] Array { get; set; }

            public static readonly string s_json =
                @"{" +
                    @"""Array"" : [" +
                        @"1, " +
                        @"true, " +
                        @"false, " +
                        @"""Hello""," +
                        @"[2, false, true, ""Goodbye""]," +
                        @"{}" +
                    @"]" +
                @"}";

            public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

            public void Initialize()
            {
                Array = new JsonElement[]
                {
                    JsonDocument.Parse(@"1").RootElement.Clone(),
                    JsonDocument.Parse(@"true").RootElement.Clone(),
                    JsonDocument.Parse(@"false").RootElement.Clone(),
                    JsonDocument.Parse(@"""Hello""").RootElement.Clone()
                };
            }

            public void Verify()
            {
                Assert.Equal(JsonValueKind.Number, Array[0].ValueKind);
                Assert.Equal("1", Array[0].ToString());
                Assert.Equal(JsonValueKind.True, Array[1].ValueKind);
                Assert.Equal("True", Array[1].ToString());
                Assert.Equal(JsonValueKind.False, Array[2].ValueKind);
                Assert.Equal("False", Array[2].ToString());
                Assert.Equal(JsonValueKind.String, Array[3].ValueKind);
                Assert.Equal("Hello", Array[3].ToString());
            }
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
