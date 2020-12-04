// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class JsonDocumentTests
    {
        [Fact]
        public void SerializeJsonDocument()
        {
            using JsonDocumentClass obj = new JsonDocumentClass();
            obj.Document = JsonSerializer.Deserialize<JsonDocument>(JsonDocumentClass.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj.Document);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(JsonDocumentClass.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj.Document = JsonSerializer.Deserialize<JsonDocument>(reserialized);
            obj.Verify();
        }

        public class JsonDocumentClass : ITestClass, IDisposable
        {
            public JsonDocument Document { get; set; }

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

            public readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

            public void Initialize()
            {
                Document = JsonDocument.Parse(s_json);
            }

            public void Verify()
            {
                JsonElement number = Document.RootElement.GetProperty("Number");
                JsonElement trueBool = Document.RootElement.GetProperty("True");
                JsonElement falseBool = Document.RootElement.GetProperty("False");
                JsonElement stringType = Document.RootElement.GetProperty("String");
                JsonElement arrayType = Document.RootElement.GetProperty("Array");
                JsonElement objectType = Document.RootElement.GetProperty("Object");
                JsonElement nullType = Document.RootElement.GetProperty("Null");

                Assert.Equal(JsonValueKind.Number, number.ValueKind);
                Assert.Equal("1", number.ToString());
                Assert.Equal(JsonValueKind.True, trueBool.ValueKind);
                Assert.Equal("True", true.ToString());
                Assert.Equal(JsonValueKind.False, falseBool.ValueKind);
                Assert.Equal("False", false.ToString());
                Assert.Equal(JsonValueKind.String, stringType.ValueKind);
                Assert.Equal("Hello", stringType.ToString());
                Assert.Equal(JsonValueKind.Array, arrayType.ValueKind);
                JsonElement[] elements = arrayType.EnumerateArray().ToArray();
                Assert.Equal(JsonValueKind.Number, elements[0].ValueKind);
                Assert.Equal("2", elements[0].ToString());
                Assert.Equal(JsonValueKind.False, elements[1].ValueKind);
                Assert.Equal("False", elements[1].ToString());
                Assert.Equal(JsonValueKind.True, elements[2].ValueKind);
                Assert.Equal("True", elements[2].ToString());
                Assert.Equal(JsonValueKind.String, elements[3].ValueKind);
                Assert.Equal("Goodbye", elements[3].ToString());
                Assert.Equal(JsonValueKind.Object, objectType.ValueKind);
                Assert.Equal("{}", objectType.ToString());
                Assert.Equal(JsonValueKind.Null, nullType.ValueKind);
                Assert.Equal("", nullType.ToString()); // JsonElement returns empty string for null.
            }

            public void Dispose()
            {
                Document.Dispose();
            }
        }

        [Fact]
        public void SerializeJsonElementArray()
        {
            using JsonDocumentArrayClass obj = new JsonDocumentArrayClass();
            obj.Document = JsonSerializer.Deserialize<JsonDocument>(JsonDocumentArrayClass.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj.Document);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(JsonDocumentArrayClass.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj.Document = JsonSerializer.Deserialize<JsonDocument>(reserialized);
            obj.Verify();
        }

        public class JsonDocumentArrayClass : ITestClass, IDisposable
        {
            public JsonDocument Document { get; set; }

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
                Document = JsonDocument.Parse(s_json);
            }

            public void Verify()
            {
                JsonElement[] array = Document.RootElement.GetProperty("Array").EnumerateArray().ToArray();

                Assert.Equal(JsonValueKind.Number, array[0].ValueKind);
                Assert.Equal("1", array[0].ToString());
                Assert.Equal(JsonValueKind.True, array[1].ValueKind);
                Assert.Equal("True", array[1].ToString());
                Assert.Equal(JsonValueKind.False, array[2].ValueKind);
                Assert.Equal("False", array[2].ToString());
                Assert.Equal(JsonValueKind.String, array[3].ValueKind);
                Assert.Equal("Hello", array[3].ToString());
            }

            public void Dispose()
            {
                Document.Dispose();
            }
        }

        [Theory,
            InlineData(5),
            InlineData(10),
            InlineData(20),
            InlineData(1024)]
        public void ReadJsonDocumentFromStream(int defaultBufferSize)
        {
            // Streams need to read ahead when they hit objects or arrays that are assigned to JsonElement or object.

            byte[] data = Encoding.UTF8.GetBytes(@"{""Data"":[1,true,{""City"":""MyCity""},null,""foo""]}");
            MemoryStream stream = new MemoryStream(data);
            JsonDocument obj = JsonSerializer.DeserializeAsync<JsonDocument>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result;

            data = Encoding.UTF8.GetBytes(@"[1,true,{""City"":""MyCity""},null,""foo""]");
            stream = new MemoryStream(data);
            obj = JsonSerializer.DeserializeAsync<JsonDocument>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result;

            // Ensure we fail with incomplete data
            data = Encoding.UTF8.GetBytes(@"{""Data"":[1,true,{""City"":""MyCity""},null,""foo""]");
            stream = new MemoryStream(data);
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<JsonDocument>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result);

            data = Encoding.UTF8.GetBytes(@"[1,true,{""City"":""MyCity""},null,""foo""");
            stream = new MemoryStream(data);
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<JsonDocument>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result);
        }
    }
}
