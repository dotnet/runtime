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

        [Theory]
        [MemberData(nameof(TestData.DuplicatePropertyJsonPayloads), MemberType = typeof(TestData))]
        public void DserializeJsonDocumentWithDuplicateProperties(string jsonPayload, bool isValidJson = false)
        {
            AssertDuplicateProperty<JsonDocument>(jsonPayload, isValidJson);
        }

        [Theory]
        [MemberData(nameof(TestData.DuplicatePropertyJsonPayloads), MemberType = typeof(TestData))]
        public void DserializeJsonDocumentArrayWithDuplicateProperties(string jsonPayload, bool isValidJson = false)
        {
            jsonPayload = $"[{jsonPayload}]";
            AssertDuplicateProperty<JsonDocument>(jsonPayload, isValidJson);
        }

        [Theory]
        [MemberData(nameof(TestData.DuplicatePropertyJsonPayloads), MemberType = typeof(TestData))]
        public static void DserializeJsonDocumentDeeplyNestedWithDuplicateProperties(string jsonPayload, bool isValidJson = false)
        {
            jsonPayload = $$"""{"p0":{"p1":{"p2":{"p3":{"p4":{"p5":{"p6":{"p7":{"p8":{"p9":{{jsonPayload}}} } } } } } } } } }""";
            AssertDuplicateProperty<JsonDocument>(jsonPayload, isValidJson);
        }

        [Theory]
        [MemberData(nameof(TestData.DuplicatePropertyJsonPayloads), MemberType = typeof(TestData))]
        public void DserializeJsonDocumentClassWithDuplicateProperties(string jsonPayload, bool isValidJson = false)
        {
            jsonPayload = $$"""{"Object":{{jsonPayload}}}""";
            AssertDuplicateProperty<JsonDocument>(jsonPayload, isValidJson);
        }

        private static void AssertDuplicateProperty<T>(string jsonPayload, bool isValidJson)
        {
            if (isValidJson)
            {
                _ = JsonSerializer.Deserialize<T>(jsonPayload, JsonTestSerializerOptions.DisallowDuplicateProperties); // Assert no throw
            }
            else
            {
                Exception ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(jsonPayload, JsonTestSerializerOptions.DisallowDuplicateProperties));
                Assert.Contains("Duplicate", ex.Message);
            }

            _ = JsonSerializer.Deserialize<T>(jsonPayload); // Assert no throw
        }

        [Fact]
        public void DserializeJsonDocument_CaseInsensitiveWithDuplicatePropertiesNoThrow()
        {
            string jsonPayload = """{"a": 1, "A": 2}""";

            JsonSerializerOptions options = JsonTestSerializerOptions.DisallowDuplicatePropertiesIgnoringCase;
            JsonDocument doc = JsonSerializer.Deserialize<JsonDocument>(jsonPayload, options);

            // JsonDocument is always case-sensitive
            Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("A").GetInt32());
        }

        [Fact]
        public void DserializeJsonDocumentWrapper_CaseInsensitiveWithDuplicatePropertiesNoThrow()
        {
            string jsonPayload = """{"document": {"a": 1, "A": 2}}""";

            JsonSerializerOptions options = JsonTestSerializerOptions.DisallowDuplicatePropertiesIgnoringCase;
            JsonDocumentClass obj = JsonSerializer.Deserialize<JsonDocumentClass>(jsonPayload, options);

            // JsonDocument is always case-sensitive
            Assert.Equal(1, obj.Document.RootElement.GetProperty("a").GetInt32());
            Assert.Equal(2, obj.Document.RootElement.GetProperty("A").GetInt32());
        }
    }
}
