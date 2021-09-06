// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class for wrapping string-based JsonSerializer methods which allows tests to run under different configurations.
    /// </summary>
    public abstract partial class JsonSerializerWrapperForString
    {
        private static readonly JsonSerializerOptions _optionsWithSmallBuffer = new JsonSerializerOptions { DefaultBufferSize = 1 };

        public static JsonSerializerWrapperForString SpanSerializer => new SpanSerializerWrapper();
        public static JsonSerializerWrapperForString StringSerializer => new StringSerializerWrapper();
        public static JsonSerializerWrapperForString AsyncStreamSerializer => new AsyncStreamSerializerWrapper();
        public static JsonSerializerWrapperForString AsyncStreamSerializerWithSmallBuffer => new AsyncStreamSerializerWrapperWithSmallBuffer();
        public static JsonSerializerWrapperForString SyncStreamSerializer => new SyncStreamSerializerWrapper();
        public static JsonSerializerWrapperForString ReaderWriterSerializer => new ReaderWriterSerializerWrapper();
        public static JsonSerializerWrapperForString DocumentSerializer => new DocumentSerializerWrapper();
        public static JsonSerializerWrapperForString ElementSerializer => new ElementSerializerWrapper();
        public static JsonSerializerWrapperForString NodeSerializer => new NodeSerializerWrapper();

        private class SpanSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => true; // a 'null' value is supported via implicit operator.

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, inputType, options);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes<T>(value, options);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, inputType, context);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json.AsSpan(), options));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), type, options));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), jsonTypeInfo));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), type, context));
            }
        }

        private class StringSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => true;

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, inputType, options));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, options));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, inputType, context));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, jsonTypeInfo));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json, options));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json, type, options));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json, jsonTypeInfo));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json, type, context));
            }
        }

        private class AsyncStreamSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => false;

            protected internal override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new();
                await JsonSerializer.SerializeAsync(stream, value, inputType, options);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new();
                await JsonSerializer.SerializeAsync<T>(stream, value, options);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                using MemoryStream stream = new();
                await JsonSerializer.SerializeAsync(stream, value, inputType, context);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                using MemoryStream stream = new();
                await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return await JsonSerializer.DeserializeAsync<T>((Stream)null, options ?? _optionsWithSmallBuffer);
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return await JsonSerializer.DeserializeAsync<T>(stream, options ?? _optionsWithSmallBuffer);
            }

            protected internal override async Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return await JsonSerializer.DeserializeAsync((Stream)null, type, options ?? _optionsWithSmallBuffer);
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return await JsonSerializer.DeserializeAsync(stream, type, options ?? _optionsWithSmallBuffer);
            }

            protected internal override async Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return await JsonSerializer.DeserializeAsync((Stream)null, jsonTypeInfo);
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo);
            }

            protected internal override async Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return await JsonSerializer.DeserializeAsync((Stream)null, type, context);
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return await JsonSerializer.DeserializeAsync(stream, type, context);
            }
        }

        private class AsyncStreamSerializerWrapperWithSmallBuffer : AsyncStreamSerializerWrapper
        {
            protected internal override bool SupportsNullValueOnDeserialize => false;

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                if (options == null)
                {
                    options = _optionsWithSmallBuffer;
                }

                return base.SerializeWrapper(value, inputType, options);
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                return base.SerializeWrapper<T>(value, options);
            }
        }

        private class SyncStreamSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => false;

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new();
                JsonSerializer.Serialize(stream, value, inputType, options);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new();
                JsonSerializer.Serialize<T>(stream, value, options);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                using MemoryStream stream = new();
                JsonSerializer.Serialize(stream, value, inputType, context);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                using MemoryStream stream = new();
                JsonSerializer.Serialize(stream, value, jsonTypeInfo);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>((Stream)null, options ?? _optionsWithSmallBuffer));
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize<T>(stream, options ?? _optionsWithSmallBuffer));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize((Stream)null, type, options ?? _optionsWithSmallBuffer));
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize(stream, type, options ?? _optionsWithSmallBuffer));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>((Stream)null, jsonTypeInfo));
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize<T>(stream, jsonTypeInfo));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                if (json is null)
                {
                    // Emulate a null Stream for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize((Stream)null, type, context));
                }

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize(stream, type, context));
            }
        }

        private class ReaderWriterSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => false;

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream))
                {
                    JsonSerializer.Serialize(writer, value, inputType, options);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream))
                {
                    JsonSerializer.Serialize<T>(writer, value, options);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream))
                {
                    JsonSerializer.Serialize(writer, value, inputType, context);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream))
                {
                    JsonSerializer.Serialize(writer, value, jsonTypeInfo);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize<T>(ref reader, options));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, type, options));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, jsonTypeInfo));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, type, context));
            }
        }

        private class DocumentSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => false;

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, inputType, options);
                return Task.FromResult(GetStringFromDocument(document));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, options);
                return Task.FromResult(GetStringFromDocument(document));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, inputType, context);
                return Task.FromResult(GetStringFromDocument(document));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, jsonTypeInfo);
                return Task.FromResult(GetStringFromDocument(document));
            }

            private string GetStringFromDocument(JsonDocument document)
            {
                // Emulate a null return value.
                if (document is null)
                {
                    return "null";
                }

                using MemoryStream stream = new();
                using (Utf8JsonWriter writer = new(stream))
                {
                    document.WriteTo(writer);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>(document: null));
                }

                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.Deserialize<T>(options));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(document: null, type));
                }

                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.Deserialize(type, options));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>(document: null, jsonTypeInfo));
                }

                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.Deserialize<T>(jsonTypeInfo));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(document: null, type, context));
                }

                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.Deserialize(type, context));
            }
        }

        private class ElementSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => false;

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, inputType, options);
                return Task.FromResult(GetStringFromElement(element));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, options);
                return Task.FromResult(GetStringFromElement(element));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, inputType, context);
                return Task.FromResult(GetStringFromElement(element));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, jsonTypeInfo);
                return Task.FromResult(GetStringFromElement(element));
            }

            private string GetStringFromElement(JsonElement element)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream))
                {
                    element.WriteTo(writer);
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.RootElement.Deserialize<T>(options));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.RootElement.Deserialize(type, options));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.RootElement.Deserialize<T>(jsonTypeInfo));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                using JsonDocument document = JsonDocument.Parse(json);
                return Task.FromResult(document.RootElement.Deserialize(type, context));
            }
        }

        private class NodeSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override bool SupportsNullValueOnDeserialize => true;

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, inputType, options);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, options);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, inputType, context);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, jsonTypeInfo);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null node for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>(node: null));
                }

                JsonNode node = JsonNode.Parse(json);
                return Task.FromResult(node.Deserialize<T>(options));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null node for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(node: null, type));
                }

                JsonNode node = JsonNode.Parse(json);
                return Task.FromResult(node.Deserialize(type, options));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null node for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(node: null, jsonTypeInfo));
                }

                JsonNode node = JsonNode.Parse(json);
                return Task.FromResult(node.Deserialize<T>(jsonTypeInfo));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(node: null, type));
                }

                JsonNode node = JsonNode.Parse(json);
                return Task.FromResult(node.Deserialize(type, context));
            }
        }
    }
}
