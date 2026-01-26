// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Provides basic tests for serializing To\From DOM types including JsonDocument, JsonElement and JsonNode.
    /// </summary>
    /// The test class <see cref="System.Text.Json.Tests.Serialization.MetadataTests"/> provides tests for the JsonTypeInfo and JsonContext permutations.
    /// The test class <see cref="JsonSerializerApiValidation"/> provides tests for input validation.
    public static partial class DomTests
    {
        private const string Escaped_PlusSign = "\"\\u002B\""; // A '+' sign is escaped as hex.

        [JsonSerializable(typeof(MyPoco))]
        private partial class MyPocoContext : JsonSerializerContext
        {
        }

        private class MyPoco
        {
            public static MyPoco Create()
            {
                return new MyPoco { StringProp = "Hello", IntArrayProp = new int[] { 1, 2 } };
            }

            [JsonPropertyOrder(0)]
            public string StringProp { get; set; }

            [JsonPropertyOrder(1)]
            public int[] IntArrayProp { get; set; }

            public void Verify()
            {
                Assert.Equal("Hello", StringProp);
                Assert.Equal(1, IntArrayProp[0]);
                Assert.Equal(2, IntArrayProp[1]);
            }
        }

        public const string Json =
            "{\"StringProp\":\"Hello\",\"IntArrayProp\":[1,2]}";

        [Fact]
        public static void JsonDocumentDeserialize_Generic()
        {
            using JsonDocument dom = JsonDocument.Parse(Json);
            MyPoco obj = dom.Deserialize<MyPoco>();
            obj.Verify();
        }

        [Fact]
        public static void JsonDocumentDeserialize_NonGeneric()
        {
            using JsonDocument dom = JsonDocument.Parse(Json);
            MyPoco obj = (MyPoco)dom.Deserialize(typeof(MyPoco));
            obj.Verify();
        }

        [Fact]
        public static void JsonDocumentDeserialize_Null()
        {
            using JsonDocument dom = JsonDocument.Parse("null");
            MyPoco obj = dom.Deserialize<MyPoco>();
            Assert.Null(obj);
        }

        [Fact]
        public static void JsonElementDeserialize_Generic()
        {
            using JsonDocument document = JsonDocument.Parse(Json);
            JsonElement dom = document.RootElement;
            MyPoco obj = JsonSerializer.Deserialize<MyPoco>(dom);
            obj.Verify();
        }

        [Fact]
        public static void JsonElementDeserialize_NonGeneric()
        {
            using JsonDocument document = JsonDocument.Parse(Json);
            JsonElement dom = document.RootElement;
            MyPoco obj = (MyPoco)JsonSerializer.Deserialize(dom, typeof(MyPoco));
            obj.Verify();
        }

        [Fact]
        public static void JsonElementDeserialize_Null()
        {
            using JsonDocument document = JsonDocument.Parse("null");
            JsonElement dom = document.RootElement;
            MyPoco obj = dom.Deserialize<MyPoco>();
            Assert.Null(obj);
        }

        [Fact]
        public static void JsonElementDeserialize_FromChildElement()
        {
            using JsonDocument document = JsonDocument.Parse(Json);
            JsonElement dom = document.RootElement.GetProperty("IntArrayProp");
            int[] arr = JsonSerializer.Deserialize<int[]>(dom);
            Assert.Equal(1, arr[0]);
            Assert.Equal(2, arr[1]);
        }

        [Fact]
        public static void JsonNodeDeserialize_Generic()
        {
            JsonNode dom = JsonNode.Parse(Json);
            MyPoco obj = dom.Deserialize<MyPoco>();
            obj.Verify();
        }

        [Fact]
        public static void JsonNodeDeserialize_NonGeneric()
        {
            JsonNode dom = JsonNode.Parse(Json);
            MyPoco obj = (MyPoco)dom.Deserialize(typeof(MyPoco));
            obj.Verify();
        }

        [Fact]
        public static void JsonNodeDeserialize_Null()
        {
            JsonNode node = null;
            MyPoco obj = JsonSerializer.Deserialize<MyPoco>(node);
            Assert.Null(obj);
        }

        [Fact]
        public static void JsonElementDeserialize_FromChildNode()
        {
            JsonNode dom = JsonNode.Parse(Json)["IntArrayProp"];
            int[] arr = JsonSerializer.Deserialize<int[]>(dom);
            Assert.Equal(1, arr[0]);
            Assert.Equal(2, arr[1]);
        }

        [Fact]
        public static void SerializeToDocument()
        {
            MyPoco obj = MyPoco.Create();
            using JsonDocument dom = JsonSerializer.SerializeToDocument(obj);

            JsonElement stringProp = dom.RootElement.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());

            JsonElement[] elements = dom.RootElement.GetProperty("IntArrayProp").EnumerateArray().ToArray();
            Assert.Equal(JsonValueKind.Number, elements[0].ValueKind);
            Assert.Equal(1, elements[0].GetInt32());
            Assert.Equal(JsonValueKind.Number, elements[1].ValueKind);
            Assert.Equal(2, elements[1].GetInt32());
        }

        [Fact]
        public static void SerializeToElement()
        {
            MyPoco obj = MyPoco.Create();
            JsonElement dom = JsonSerializer.SerializeToElement(obj);

            JsonElement stringProp = dom.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());

            JsonElement[] elements = dom.GetProperty("IntArrayProp").EnumerateArray().ToArray();
            Assert.Equal(JsonValueKind.Number, elements[0].ValueKind);
            Assert.Equal(1, elements[0].GetInt32());
            Assert.Equal(JsonValueKind.Number, elements[1].ValueKind);
            Assert.Equal(2, elements[1].GetInt32());
        }

        [Fact]
        public static void SerializeToNode()
        {
            MyPoco obj = MyPoco.Create();
            JsonNode dom = JsonSerializer.SerializeToNode(obj);

            JsonNode stringProp = dom["StringProp"];
            Assert.True(stringProp is JsonValue);
            Assert.Equal("Hello", stringProp.AsValue().GetValue<string>());

            JsonNode arrayProp = dom["IntArrayProp"];
            Assert.IsType<JsonArray>(arrayProp);
            Assert.Equal(1, arrayProp[0].AsValue().GetValue<int>());
            Assert.Equal(2, arrayProp[1].AsValue().GetValue<int>());
        }

        [Theory]
        [InlineData(5)]
        [InlineData(32)]
        [InlineData(70)] // default max depth is 64
        public static void SerializeToNode_RespectsMaxDepth(int maxDepth)
        {
            var options = new JsonSerializerOptions { MaxDepth = maxDepth };

            RecursiveClass value = RecursiveClass.FromInt(maxDepth);
            JsonNode dom = JsonSerializer.SerializeToNode(value, options);

            value = RecursiveClass.FromInt(maxDepth + 1);
            Assert.Throws<JsonException>(() => JsonSerializer.SerializeToNode(value, options));
        }

        public class RecursiveClass
        {
            public RecursiveClass? Next { get; set; }
            public static RecursiveClass FromInt(int depth) => depth == 0 ? null : new RecursiveClass { Next = FromInt(depth - 1) };
            public static int ToInt(RecursiveClass value) => value is null ? 0 : 1 + ToInt(value.Next);
        }

        [Fact]
        public static void SerializeToDocument_WithEscaping()
        {
            using JsonDocument document = JsonSerializer.SerializeToDocument("+");
            JsonElement dom = document.RootElement;
            Assert.Equal(JsonValueKind.String, dom.ValueKind);
            Assert.Equal(Escaped_PlusSign, dom.GetRawText());

            string json = dom.Deserialize<string>();
            Assert.Equal("+", json);
        }

        [Fact]
        public static void SerializeToElement_WithEscaping()
        {
            JsonElement dom = JsonSerializer.SerializeToElement("+");
            Assert.Equal(JsonValueKind.String, dom.ValueKind);
            Assert.Equal(Escaped_PlusSign, dom.GetRawText());

            string json = dom.Deserialize<string>();
            Assert.Equal("+", json);
        }

        [Fact]
        public static void SerializeToNode_WithEscaping()
        {
            JsonNode dom = JsonSerializer.SerializeToNode("+");
            Assert.Equal(Escaped_PlusSign, dom.ToJsonString());

            string json = dom.Deserialize<string>();
            Assert.Equal("+", json);
        }

        [Fact]
        public static void SerializeToDocument_WithJsonTypeInfo()
        {
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = MyPoco.Create();
            using JsonDocument dom = JsonSerializer.SerializeToDocument(obj, typeInfo);

            JsonElement stringProp = dom.RootElement.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());
        }

        [Fact]
        public static void SerializeToDocument_WithJsonTypeInfo_NonGeneric()
        {
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = MyPoco.Create();
            using JsonDocument dom = JsonSerializer.SerializeToDocument(obj, typeInfo);

            JsonElement stringProp = dom.RootElement.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());
        }

        [Fact]
        public static void SerializeToElement_WithJsonTypeInfo()
        {
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = MyPoco.Create();
            JsonElement element = JsonSerializer.SerializeToElement(obj, typeInfo);

            JsonElement stringProp = element.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());
        }

        [Fact]
        public static void SerializeToElement_WithJsonTypeInfo_NonGeneric()
        {
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = MyPoco.Create();
            JsonElement element = JsonSerializer.SerializeToElement(obj, typeInfo);

            JsonElement stringProp = element.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());
        }

        [Fact]
        public static void SerializeToNode_WithJsonTypeInfo()
        {
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = MyPoco.Create();
            JsonNode node = JsonSerializer.SerializeToNode(obj, typeInfo);

            Assert.NotNull(node);
            Assert.Equal("Hello", node["StringProp"]?.GetValue<string>());
        }

        [Fact]
        public static void SerializeToNode_WithJsonTypeInfo_NonGeneric()
        {
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = MyPoco.Create();
            JsonNode node = JsonSerializer.SerializeToNode(obj, typeInfo);

            Assert.NotNull(node);
            Assert.Equal("Hello", node["StringProp"]?.GetValue<string>());
        }

        [Fact]
        public static void DeserializeFromSpan_WithJsonTypeInfo()
        {
            ReadOnlySpan<byte> utf8Json = """{"StringProp":"Hello","IntArrayProp":[1,2]}"""u8;
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = JsonSerializer.Deserialize(utf8Json, typeInfo);
            obj.Verify();
        }

        [Fact]
        public static void DeserializeFromSpan_WithJsonTypeInfo_NonGeneric()
        {
            ReadOnlySpan<byte> utf8Json = """{"StringProp":"Hello","IntArrayProp":[1,2]}"""u8;
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            object obj = JsonSerializer.Deserialize(utf8Json, typeInfo);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void SerializeToDocument_NullValue_WithJsonTypeInfo_NonGeneric()
        {
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            using JsonDocument dom = JsonSerializer.SerializeToDocument((object)null, typeInfo);
            Assert.Equal(JsonValueKind.Null, dom.RootElement.ValueKind);
        }

        [Fact]
        public static void SerializeToElement_NullValue_WithJsonTypeInfo_NonGeneric()
        {
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            JsonElement element = JsonSerializer.SerializeToElement((object)null, typeInfo);
            Assert.Equal(JsonValueKind.Null, element.ValueKind);
        }

        [Fact]
        public static void SerializeToNode_NullValue_WithJsonTypeInfo_NonGeneric()
        {
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            JsonNode node = JsonSerializer.SerializeToNode((object)null, typeInfo);
            Assert.Null(node);
        }

        [Fact]
        public static void DeserializeFromSpan_NullValue()
        {
            ReadOnlySpan<byte> utf8Json = "null"u8;
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = JsonSerializer.Deserialize(utf8Json, typeInfo);
            Assert.Null(obj);
        }

        [Fact]
        public static void DeserializeFromJsonDocument_WithJsonTypeInfo()
        {
            using JsonDocument doc = JsonDocument.Parse(Json);
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = doc.Deserialize(typeInfo);
            obj.Verify();
        }

        [Fact]
        public static void DeserializeFromJsonDocument_WithJsonTypeInfo_NonGeneric()
        {
            using JsonDocument doc = JsonDocument.Parse(Json);
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            object obj = doc.Deserialize(typeInfo);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromJsonElement_WithJsonTypeInfo()
        {
            using JsonDocument doc = JsonDocument.Parse(Json);
            JsonElement element = doc.RootElement;
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = element.Deserialize(typeInfo);
            obj.Verify();
        }

        [Fact]
        public static void DeserializeFromJsonElement_WithJsonTypeInfo_NonGeneric()
        {
            using JsonDocument doc = JsonDocument.Parse(Json);
            JsonElement element = doc.RootElement;
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            object obj = element.Deserialize(typeInfo);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromJsonNode_WithJsonTypeInfo()
        {
            JsonNode node = JsonNode.Parse(Json);
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = node.Deserialize(typeInfo);
            obj.Verify();
        }

        [Fact]
        public static void DeserializeFromJsonNode_WithJsonTypeInfo_NonGeneric()
        {
            JsonNode node = JsonNode.Parse(Json);
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            object obj = node.Deserialize(typeInfo);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromCharSpan_WithJsonTypeInfo()
        {
            ReadOnlySpan<char> jsonChars = Json.AsSpan();
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = JsonSerializer.Deserialize(jsonChars, typeInfo);
            obj.Verify();
        }

        [Fact]
        public static void DeserializeFromCharSpan_WithJsonTypeInfo_NonGeneric()
        {
            ReadOnlySpan<char> jsonChars = Json.AsSpan();
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            object obj = JsonSerializer.Deserialize(jsonChars, typeInfo);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void SerializeToUtf8Bytes_WithJsonTypeInfo_NonGeneric()
        {
            MyPoco obj = MyPoco.Create();
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(obj, typeInfo);
            string json = Encoding.UTF8.GetString(bytes);
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static void SerializeToStream_WithJsonTypeInfo_NonGeneric()
        {
            MyPoco obj = MyPoco.Create();
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            using MemoryStream stream = new();
            JsonSerializer.Serialize(stream, obj, typeInfo);
            
            stream.Position = 0;
            string json = new StreamReader(stream).ReadToEnd();
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static void SerializeToUtf8JsonWriter_WithJsonTypeInfo_NonGeneric()
        {
            MyPoco obj = MyPoco.Create();
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            using MemoryStream stream = new();
            using (Utf8JsonWriter writer = new(stream))
            {
                JsonSerializer.Serialize(writer, obj, typeInfo);
            }
            
            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static void DeserializeFromUtf8JsonReader_WithJsonTypeInfo()
        {
            byte[] utf8Json = Encoding.UTF8.GetBytes(Json);
            Utf8JsonReader reader = new(utf8Json);
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = JsonSerializer.Deserialize(ref reader, typeInfo);
            obj.Verify();
        }

        [Fact]
        public static void DeserializeFromUtf8JsonReader_WithJsonTypeInfo_NonGeneric()
        {
            byte[] utf8Json = Encoding.UTF8.GetBytes(Json);
            Utf8JsonReader reader = new(utf8Json);
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            object obj = JsonSerializer.Deserialize(ref reader, typeInfo);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromStream_WithJsonTypeInfo()
        {
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(Json));
            
            JsonTypeInfo<MyPoco> typeInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            MyPoco obj = JsonSerializer.Deserialize(stream, typeInfo);
            obj.Verify();
        }

        [Fact]
        public static void DeserializeFromStream_WithJsonTypeInfo_NonGeneric()
        {
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(Json));
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            object obj = JsonSerializer.Deserialize(stream, typeInfo);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void SerializeToDocument_WithJsonSerializerContext()
        {
            MyPoco obj = MyPoco.Create();
            
            using JsonDocument dom = JsonSerializer.SerializeToDocument(obj, typeof(MyPoco), MyPocoContext.Default);

            JsonElement stringProp = dom.RootElement.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());
        }

        [Fact]
        public static void SerializeToElement_WithJsonSerializerContext()
        {
            MyPoco obj = MyPoco.Create();
            
            JsonElement element = JsonSerializer.SerializeToElement(obj, typeof(MyPoco), MyPocoContext.Default);

            JsonElement stringProp = element.GetProperty("StringProp");
            Assert.Equal(JsonValueKind.String, stringProp.ValueKind);
            Assert.Equal("Hello", stringProp.ToString());
        }

        [Fact]
        public static void SerializeToNode_WithJsonSerializerContext()
        {
            MyPoco obj = MyPoco.Create();
            
            JsonNode node = JsonSerializer.SerializeToNode(obj, typeof(MyPoco), MyPocoContext.Default);

            Assert.NotNull(node);
            Assert.Equal("Hello", node["StringProp"]?.GetValue<string>());
        }

        [Fact]
        public static void DeserializeFromSpan_WithJsonSerializerContext()
        {
            ReadOnlySpan<byte> utf8Json = """{"StringProp":"Hello","IntArrayProp":[1,2]}"""u8;
            
            object obj = JsonSerializer.Deserialize(utf8Json, typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromCharSpan_WithJsonSerializerContext()
        {
            ReadOnlySpan<char> jsonChars = Json.AsSpan();
            
            object obj = JsonSerializer.Deserialize(jsonChars, typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromJsonDocument_WithJsonSerializerContext()
        {
            using JsonDocument doc = JsonDocument.Parse(Json);
            
            object obj = doc.Deserialize(typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromJsonElement_WithJsonSerializerContext()
        {
            using JsonDocument doc = JsonDocument.Parse(Json);
            JsonElement element = doc.RootElement;
            
            object obj = element.Deserialize(typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromJsonNode_WithJsonSerializerContext()
        {
            JsonNode node = JsonNode.Parse(Json);
            
            object obj = node.Deserialize(typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromUtf8JsonReader_WithJsonSerializerContext()
        {
            byte[] utf8Json = Encoding.UTF8.GetBytes(Json);
            Utf8JsonReader reader = new(utf8Json);
            
            object obj = JsonSerializer.Deserialize(ref reader, typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void DeserializeFromStream_WithJsonSerializerContext()
        {
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(Json));
            
            object obj = JsonSerializer.Deserialize(stream, typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }

        [Fact]
        public static void SerializeToUtf8Bytes_WithJsonSerializerContext()
        {
            MyPoco obj = MyPoco.Create();
            
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(obj, typeof(MyPoco), MyPocoContext.Default);
            string json = Encoding.UTF8.GetString(bytes);
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static void SerializeToStream_WithJsonSerializerContext()
        {
            MyPoco obj = MyPoco.Create();
            
            using MemoryStream stream = new();
            JsonSerializer.Serialize(stream, obj, typeof(MyPoco), MyPocoContext.Default);
            
            stream.Position = 0;
            string json = new StreamReader(stream).ReadToEnd();
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static void SerializeToUtf8JsonWriter_WithJsonSerializerContext()
        {
            MyPoco obj = MyPoco.Create();
            
            using MemoryStream stream = new();
            using (Utf8JsonWriter writer = new(stream))
            {
                JsonSerializer.Serialize(writer, obj, typeof(MyPoco), MyPocoContext.Default);
            }
            
            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static async Task SerializeAsyncToPipeWriter_WithJsonTypeInfo()
        {
            MyPoco obj = MyPoco.Create();
            
            JsonTypeInfo typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
            
            Pipe pipe = new();
            await JsonSerializer.SerializeAsync(pipe.Writer, obj, typeInfo);
            await pipe.Writer.CompleteAsync();
            
            ReadResult result = await pipe.Reader.ReadAsync();
            string json = Encoding.UTF8.GetString(result.Buffer.First.Span.ToArray());
            pipe.Reader.Complete();
            
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static async Task SerializeAsyncToPipeWriter_WithJsonSerializerContext()
        {
            MyPoco obj = MyPoco.Create();
            
            Pipe pipe = new();
            await JsonSerializer.SerializeAsync(pipe.Writer, obj, typeof(MyPoco), MyPocoContext.Default);
            await pipe.Writer.CompleteAsync();
            
            ReadResult result = await pipe.Reader.ReadAsync();
            string json = Encoding.UTF8.GetString(result.Buffer.First.Span.ToArray());
            pipe.Reader.Complete();
            
            Assert.Contains("Hello", json);
        }

        [Fact]
        public static async Task DeserializeAsyncFromPipeReader_WithJsonSerializerContext()
        {
            Pipe pipe = new();
            byte[] utf8Json = Encoding.UTF8.GetBytes(Json);
            await pipe.Writer.WriteAsync(utf8Json);
            await pipe.Writer.CompleteAsync();
            
            object obj = await JsonSerializer.DeserializeAsync(pipe.Reader, typeof(MyPoco), MyPocoContext.Default);
            Assert.IsType<MyPoco>(obj);
            ((MyPoco)obj).Verify();
        }
    }
}
