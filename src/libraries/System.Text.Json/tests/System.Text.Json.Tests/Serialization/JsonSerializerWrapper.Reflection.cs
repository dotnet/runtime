// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class JsonSerializerWrapper
    {
        // Ensure that the reflection-based serializer testing abstraction roots KeyValuePair<,>
        // which is required by many tests in the reflection test suite.
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        protected JsonSerializerWrapper()
        {
        }

        public static JsonSerializerWrapper SpanSerializer { get; } = new SpanSerializerWrapper();
        public static JsonSerializerWrapper StringSerializer { get; } = new StringSerializerWrapper();
        public static StreamingJsonSerializerWrapper AsyncStreamSerializer { get; } = new AsyncStreamSerializerWrapper();
        public static StreamingJsonSerializerWrapper AsyncStreamSerializerWithSmallBuffer { get; } = new AsyncStreamSerializerWrapper(forceSmallBufferInOptions: true, forceBomInsertions: true);
        public static StreamingJsonSerializerWrapper SyncStreamSerializer { get; } = new SyncStreamSerializerWrapper();
        public static StreamingJsonSerializerWrapper SyncStreamSerializerWithSmallBuffer { get; } = new SyncStreamSerializerWrapper(forceSmallBufferInOptions: true, forceBomInsertions: true);
        public static JsonSerializerWrapper ReaderWriterSerializer { get; } = new ReaderWriterSerializerWrapper();
        public static JsonSerializerWrapper DocumentSerializer { get; } = new DocumentSerializerWrapper();
        public static JsonSerializerWrapper ElementSerializer { get; } = new ElementSerializerWrapper();
        public static JsonSerializerWrapper NodeSerializer { get; } = new NodeSerializerWrapper();
        public static JsonSerializerWrapper PipeSerializer { get; } = new PipelinesSerializerWrapper();

        private class SpanSerializerWrapper : JsonSerializerWrapper
        {
            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override bool SupportsNullValueOnDeserialize => true; // a 'null' value is supported via implicit operator.

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, inputType, options);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes<T>(value, options);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, inputType, context);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            public override Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
                return Task.FromResult(Encoding.UTF8.GetString(result));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json.AsSpan(), options));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), type, options));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), type, context));
            }

            public override Task<object> DeserializeWrapper(string json, JsonTypeInfo jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), jsonTypeInfo));
            }
        }

        private class StringSerializerWrapper : JsonSerializerWrapper
        {
            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override bool SupportsNullValueOnDeserialize => true;

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, inputType, options));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, options));
            }

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, inputType, context));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, jsonTypeInfo));
            }

            public override Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Serialize(value, jsonTypeInfo));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json, options));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json, type, options));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json, jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json, type, context));
            }

            public override Task<object> DeserializeWrapper(string value, JsonTypeInfo jsonTypeInfo)
            {
                return Task.FromResult(JsonSerializer.Deserialize(value, jsonTypeInfo));
            }
        }

        private class AsyncStreamSerializerWrapper : StreamingJsonSerializerWrapper
        {
            private readonly bool _forceSmallBufferInOptions;
            private readonly bool _forceBomInsertions;

            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override bool IsAsyncSerializer => true;
            public override bool ForceSmallBufferInOptions => _forceSmallBufferInOptions;

            public AsyncStreamSerializerWrapper(bool forceSmallBufferInOptions = false, bool forceBomInsertions = false)
            {
                _forceSmallBufferInOptions = forceSmallBufferInOptions;
                _forceBomInsertions = forceBomInsertions;
            }

            private JsonSerializerOptions? ResolveOptionsInstance(JsonSerializerOptions? options)
                => _forceSmallBufferInOptions ? JsonSerializerOptionsSmallBufferMapper.ResolveOptionsInstanceWithSmallBuffer(options) : options;

            private Stream ResolveReadStream(Stream stream)
                => stream is not null && _forceBomInsertions ? new Utf8BomInsertingStream(stream) : stream;

            public override Task SerializeWrapper<T>(Stream utf8Json, T value, JsonSerializerOptions options = null)
            {
                return JsonSerializer.SerializeAsync<T>(utf8Json, value, ResolveOptionsInstance(options));
            }

            public override Task SerializeWrapper(Stream utf8Json, object value, Type inputType, JsonSerializerOptions options = null)
            {
                return JsonSerializer.SerializeAsync(utf8Json, value, inputType, ResolveOptionsInstance(options));
            }

            public override Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                return JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);
            }

            public override Task SerializeWrapper(Stream stream, object value, JsonTypeInfo jsonTypeInfo)
            {
                return JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);
            }

            public override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerContext context)
            {
                return JsonSerializer.SerializeAsync(stream, value, inputType, context);
            }

            public override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync<T>(ResolveReadStream(utf8Json), ResolveOptionsInstance(options));
            }

            public override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync(ResolveReadStream(utf8Json), returnType, ResolveOptionsInstance(options));
            }

            public override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return await JsonSerializer.DeserializeAsync<T>(ResolveReadStream(utf8Json), jsonTypeInfo);
            }

            public override async Task<object> DeserializeWrapper(Stream utf8Json, JsonTypeInfo jsonTypeInfo)
            {
                return await JsonSerializer.DeserializeAsync(ResolveReadStream(utf8Json), jsonTypeInfo);
            }

            public override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerContext context)
            {
                return await JsonSerializer.DeserializeAsync(ResolveReadStream(utf8Json), returnType, context);
            }
        }

        private class SyncStreamSerializerWrapper : StreamingJsonSerializerWrapper
        {
            private readonly bool _forceSmallBufferInOptions;
            private readonly bool _forceBomInsertions;

            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override bool IsAsyncSerializer => false;
            public override bool ForceSmallBufferInOptions => _forceSmallBufferInOptions;

            public SyncStreamSerializerWrapper(bool forceSmallBufferInOptions = false, bool forceBomInsertions = false)
            {
                _forceSmallBufferInOptions = forceSmallBufferInOptions;
                _forceBomInsertions = forceBomInsertions;
            }

            private JsonSerializerOptions? ResolveOptionsInstance(JsonSerializerOptions? options)
                => _forceSmallBufferInOptions ? JsonSerializerOptionsSmallBufferMapper.ResolveOptionsInstanceWithSmallBuffer(options) : options;

            private Stream ResolveReadStream(Stream stream)
                => stream is not null && _forceBomInsertions ? new Utf8BomInsertingStream(stream) : stream;

            public override Task SerializeWrapper<T>(Stream utf8Json, T value, JsonSerializerOptions options = null)
            {
                JsonSerializer.Serialize<T>(utf8Json, value, ResolveOptionsInstance(options));
                return Task.CompletedTask;
            }

            public override Task SerializeWrapper(Stream utf8Json, object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonSerializer.Serialize(utf8Json, value, inputType, ResolveOptionsInstance(options));
                return Task.CompletedTask;
            }

            public override Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonSerializer.Serialize(stream, value, jsonTypeInfo);
                return Task.CompletedTask;
            }

            public override Task SerializeWrapper(Stream stream, object value, JsonTypeInfo jsonTypeInfo)
            {
                JsonSerializer.Serialize(stream, value, jsonTypeInfo);
                return Task.CompletedTask;
            }

            public override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerContext context)
            {
                JsonSerializer.Serialize(stream, value, inputType, context);
                return Task.CompletedTask;
            }

            public override Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null)
            {
                T result = JsonSerializer.Deserialize<T>(ResolveReadStream(utf8Json), ResolveOptionsInstance(options));
                return Task.FromResult(result);
            }

            public override Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
            {
                object result = JsonSerializer.Deserialize(ResolveReadStream(utf8Json), returnType, ResolveOptionsInstance(options));
                return Task.FromResult(result);
            }

            public override Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo)
            {
                T result = JsonSerializer.Deserialize<T>(ResolveReadStream(utf8Json), jsonTypeInfo);
                return Task.FromResult(result);
            }

            public override Task<object> DeserializeWrapper(Stream utf8Json, JsonTypeInfo jsonTypeInfo)
            {
                object result = JsonSerializer.Deserialize(ResolveReadStream(utf8Json), jsonTypeInfo);
                return Task.FromResult(result);
            }

            public override Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerContext context)
            {
                object result = JsonSerializer.Deserialize(ResolveReadStream(utf8Json), returnType, context);
                return Task.FromResult(result);
            }
        }

        private class ReaderWriterSerializerWrapper : JsonSerializerWrapper
        {
            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream, OptionsHelpers.GetWriterOptions(options)))
                {
                    JsonSerializer.Serialize(writer, value, inputType, options);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream, OptionsHelpers.GetWriterOptions(options)))
                {
                    JsonSerializer.Serialize<T>(writer, value, options);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream, OptionsHelpers.GetWriterOptions(context?.Options)))
                {
                    JsonSerializer.Serialize(writer, value, inputType, context);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream, OptionsHelpers.GetWriterOptions(jsonTypeInfo?.Options)))
                {
                    JsonSerializer.Serialize(writer, value, jsonTypeInfo);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            public override Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream, OptionsHelpers.GetWriterOptions(jsonTypeInfo?.Options)))
                {
                    JsonSerializer.Serialize(writer, value, jsonTypeInfo);
                }

                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json), OptionsHelpers.GetReaderOptions(options));
                return Task.FromResult(JsonSerializer.Deserialize<T>(ref reader, options));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json), OptionsHelpers.GetReaderOptions(options));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, type, options));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json), OptionsHelpers.GetReaderOptions(jsonTypeInfo?.Options));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, JsonTypeInfo jsonTypeInfo)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json), OptionsHelpers.GetReaderOptions(jsonTypeInfo?.Options));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json), OptionsHelpers.GetReaderOptions(context?.Options));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, type, context));
            }
        }

        private class DocumentSerializerWrapper : JsonSerializerWrapper
        {
            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, inputType, options);
                return Task.FromResult(GetStringFromDocument(document));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, options);
                return Task.FromResult(GetStringFromDocument(document));
            }

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, inputType, context);
                return Task.FromResult(GetStringFromDocument(document));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonDocument document = JsonSerializer.SerializeToDocument(value, jsonTypeInfo);
                return Task.FromResult(GetStringFromDocument(document));
            }

            public override Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
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

            public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>(document: null, options));
                }

                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(options));
                return Task.FromResult(document.Deserialize<T>(options));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(document: null, type, options));
                }

                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(options));
                return Task.FromResult(document.Deserialize(type, options));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>(document: null, jsonTypeInfo));
                }

                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(jsonTypeInfo?.Options));
                return Task.FromResult(document.Deserialize(jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, JsonTypeInfo jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(document: null, jsonTypeInfo));
                }

                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(jsonTypeInfo?.Options));
                return Task.FromResult(document.Deserialize(jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(document: null, type, context));
                }

                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(context?.Options));
                return Task.FromResult(document.Deserialize(type, context));
            }
        }

        private class ElementSerializerWrapper : JsonSerializerWrapper
        {
            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, inputType, options);
                return Task.FromResult(GetStringFromElement(element, options));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, options);
                return Task.FromResult(GetStringFromElement(element, options));
            }

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, inputType, context);
                return Task.FromResult(GetStringFromElement(element, context?.Options));
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, jsonTypeInfo);
                return Task.FromResult(GetStringFromElement(element, jsonTypeInfo?.Options));
            }

            public override Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
            {
                JsonElement element = JsonSerializer.SerializeToElement(value, jsonTypeInfo);
                return Task.FromResult(GetStringFromElement(element, jsonTypeInfo?.Options));
            }

            private string GetStringFromElement(JsonElement element, JsonSerializerOptions options)
            {
                using MemoryStream stream = new MemoryStream();
                using (Utf8JsonWriter writer = new(stream, OptionsHelpers.GetWriterOptions(options)))
                {
                    element.WriteTo(writer);
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(options));
                return Task.FromResult(document.RootElement.Deserialize<T>(options));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(options));
                return Task.FromResult(document.RootElement.Deserialize(type, options));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(jsonTypeInfo?.Options));
                return Task.FromResult(document.RootElement.Deserialize<T>(jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, JsonTypeInfo jsonTypeInfo)
            {
                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(jsonTypeInfo?.Options));
                return Task.FromResult(document.RootElement.Deserialize(jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(context?.Options));
                return Task.FromResult(document.RootElement.Deserialize(type, context));
            }
        }

        private class NodeSerializerWrapper : JsonSerializerWrapper
        {
            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override bool SupportsNullValueOnDeserialize => true;

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, inputType, options);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, options);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, inputType, context);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, jsonTypeInfo);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            public override Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
            {
                JsonNode node = JsonSerializer.SerializeToNode(value, jsonTypeInfo);

                // Emulate a null return value.
                if (node is null)
                {
                    return Task.FromResult("null");
                }

                return Task.FromResult(node.ToJsonString());
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null node for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize<T>(node: null, options));
                }

                JsonNode node = JsonNode.Parse(json, OptionsHelpers.GetNodeOptions(options), OptionsHelpers.GetDocumentOptions(options));
                return Task.FromResult(node.Deserialize<T>(options));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                if (json is null)
                {
                    // Emulate a null node for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(node: null, type, options));
                }

                JsonNode node = JsonNode.Parse(json, OptionsHelpers.GetNodeOptions(options), OptionsHelpers.GetDocumentOptions(options));
                return Task.FromResult(node.Deserialize(type, options));
            }

            public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null node for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(node: null, jsonTypeInfo));
                }

                JsonNode node = JsonNode.Parse(json, OptionsHelpers.GetNodeOptions(jsonTypeInfo?.Options), OptionsHelpers.GetDocumentOptions(jsonTypeInfo?.Options));
                return Task.FromResult(node.Deserialize<T>(jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, JsonTypeInfo jsonTypeInfo)
            {
                if (json is null)
                {
                    // Emulate a null node for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(node: null, jsonTypeInfo));
                }

                JsonNode node = JsonNode.Parse(json, OptionsHelpers.GetNodeOptions(jsonTypeInfo?.Options), OptionsHelpers.GetDocumentOptions(jsonTypeInfo?.Options));
                return Task.FromResult(node.Deserialize(jsonTypeInfo));
            }

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                if (json is null)
                {
                    // Emulate a null document for API validation tests.
                    return Task.FromResult(JsonSerializer.Deserialize(node: null, type, context?.Options));
                }

                JsonNode node = JsonNode.Parse(json, OptionsHelpers.GetNodeOptions(context?.Options), OptionsHelpers.GetDocumentOptions(context?.Options));
                return Task.FromResult(node.Deserialize(type, context));
            }
        }

        private static class OptionsHelpers
        {
            public static JsonWriterOptions GetWriterOptions(JsonSerializerOptions? options)
            {
                return options is null
                    ? default
                    : new JsonWriterOptions
                    {
                        Encoder = options.Encoder,
                        Indented = options.WriteIndented,
                        MaxDepth = GetEffectiveMaxDepth(options),
    #if !DEBUG
                            SkipValidation = true
    #endif
                    };
            }

            public static JsonReaderOptions GetReaderOptions(JsonSerializerOptions? options)
            {
                return options is null
                    ? default
                    : new JsonReaderOptions
                    {
                        AllowTrailingCommas = options.AllowTrailingCommas,
                        CommentHandling = options.ReadCommentHandling,
                        MaxDepth = GetEffectiveMaxDepth(options),
                    };
            }

            public static JsonDocumentOptions GetDocumentOptions(JsonSerializerOptions? options)
            {
                return options is null
                    ? default
                    : new JsonDocumentOptions
                    {
                        MaxDepth = GetEffectiveMaxDepth(options),
                        AllowTrailingCommas = options.AllowTrailingCommas,
                        CommentHandling = options.ReadCommentHandling
                    };
            }

            public static JsonNodeOptions GetNodeOptions(JsonSerializerOptions? options)
            {
                return options is null
                    ? default
                    : new JsonNodeOptions
                    {
                        PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive
                    };
            }

            private static int GetEffectiveMaxDepth(JsonSerializerOptions options) => options.MaxDepth == 0 ? 64 : options.MaxDepth;
        }

        /// <summary>
        /// Maintains an index of equivalent JsonSerializerOptions instances with DefaultBufferSize = 1
        /// </summary>
        private static class JsonSerializerOptionsSmallBufferMapper
        {
            private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> s_smallBufferMap = new();
            private static readonly JsonSerializerOptions s_DefaultOptionsWithSmallBuffer = new JsonSerializerOptions { DefaultBufferSize = 1 };

            public static JsonSerializerOptions ResolveOptionsInstanceWithSmallBuffer(JsonSerializerOptions? options)
            {
                if (options == null || options == JsonSerializerOptions.Default)
                {
                    return s_DefaultOptionsWithSmallBuffer;
                }

                if (options.DefaultBufferSize == 1)
                {
                    return options;
                }

                if (s_smallBufferMap.TryGetValue(options, out JsonSerializerOptions resolvedValue))
                {
                    Assert.Equal(1, resolvedValue.DefaultBufferSize);
                    return resolvedValue;
                }

                JsonSerializerOptions smallBufferCopy = new JsonSerializerOptions(options)
                {
                    DefaultBufferSize = 1,
                };

                s_smallBufferMap.Add(options, smallBufferCopy);
                return smallBufferCopy;
            }
        }

        private sealed class Utf8BomInsertingStream : Stream
        {
            private const int Utf8BomLength = 3;
            private readonly static byte[] s_utf8Bom = Encoding.UTF8.GetPreamble();

            private readonly Stream _source;
            private byte[]? _prefixBytes;
            private int _prefixBytesOffset = 0;
            private int _prefixBytesCount = 0;

            public Utf8BomInsertingStream(Stream source)
            {
                Debug.Assert(source.CanRead);
                _source = source;
            }

            public override bool CanRead => _source.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_prefixBytes is null)
                {
                    // This is the first read operation; read the first 3 bytes
                    // from the source to determine if it already includes a BOM.
                    // Only insert a BOM if it's missing from the source stream.

                    _prefixBytes = new byte[2 * Utf8BomLength];
                    int bytesRead = ReadExactlyFromSource(_prefixBytes, Utf8BomLength, Utf8BomLength);

                    if (_prefixBytes.AsSpan(Utf8BomLength).SequenceEqual(s_utf8Bom))
                    {
                        _prefixBytesOffset = Utf8BomLength;
                        _prefixBytesCount = Utf8BomLength;
                    }
                    else
                    {
                        s_utf8Bom.CopyTo(_prefixBytes, 0);
                        _prefixBytesOffset = 0;
                        _prefixBytesCount = Utf8BomLength + bytesRead;
                    }
                }

                int prefixBytesToWrite = Math.Min(_prefixBytesCount, count);
                if (prefixBytesToWrite > 0)
                {
                    _prefixBytes.AsSpan(_prefixBytesOffset, prefixBytesToWrite).CopyTo(buffer.AsSpan(offset, count));
                    _prefixBytesOffset += prefixBytesToWrite;
                    _prefixBytesCount -= prefixBytesToWrite;
                    offset += prefixBytesToWrite;
                    count -= prefixBytesToWrite;
                }

                return prefixBytesToWrite + _source.Read(buffer, offset, count);
            }

            private int ReadExactlyFromSource(byte[] buffer, int offset, int count)
            {
                int totalRead = 0;

                while (totalRead < count)
                {
                    int read = _source.Read(buffer, offset + totalRead, count - totalRead);
                    if (read == 0)
                    {
                        break;
                    }

                    totalRead += read;
                }

                return totalRead;
            }

            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        // TODO: Deserialize to use PipeReader overloads once implemented
        private class PipelinesSerializerWrapper : JsonSerializerWrapper
        {
            public override JsonSerializerOptions DefaultOptions => JsonSerializerOptions.Default;
            public override bool SupportsNullValueOnDeserialize => true;

            public override async Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync<T>(new MemoryStream(Encoding.UTF8.GetBytes(json)), options);
            }
            public override async Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), type, options);
            }

            public override async Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonTypeInfo);
            }

            public override async Task<object> DeserializeWrapper(string value, JsonTypeInfo jsonTypeInfo)
            {
                return await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(value)), jsonTypeInfo);
            }

            public override async Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                return await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), type, context);
            }

            public override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                Pipe pipe = new Pipe();
                await JsonSerializer.SerializeAsync(pipe.Writer, value, inputType, options);
                ReadResult result = await pipe.Reader.ReadAsync();
                return Encoding.UTF8.GetString(result.Buffer.ToArray());
            }

            public override async Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                Pipe pipe = new Pipe();
                await JsonSerializer.SerializeAsync<T>(pipe.Writer, value, options);
                ReadResult result = await pipe.Reader.ReadAsync();
                return Encoding.UTF8.GetString(result.Buffer.ToArray());
            }

            public override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                Pipe pipe = new Pipe();
                await JsonSerializer.SerializeAsync(pipe.Writer, value, inputType, context);
                ReadResult result = await pipe.Reader.ReadAsync();
                return Encoding.UTF8.GetString(result.Buffer.ToArray());
            }

            public override async Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                Pipe pipe = new Pipe();
                await JsonSerializer.SerializeAsync(pipe.Writer, value, jsonTypeInfo);
                ReadResult result = await pipe.Reader.ReadAsync();
                return Encoding.UTF8.GetString(result.Buffer.ToArray());
            }

            public override async Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
            {
                Pipe pipe = new Pipe();
                await JsonSerializer.SerializeAsync(pipe.Writer, value, jsonTypeInfo);
                ReadResult result = await pipe.Reader.ReadAsync();
                return Encoding.UTF8.GetString(result.Buffer.ToArray());
            }
        }
    }
}
