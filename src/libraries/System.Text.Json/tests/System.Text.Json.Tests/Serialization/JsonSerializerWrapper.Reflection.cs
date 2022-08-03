// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class JsonSerializerWrapper
    {
        public static JsonSerializerWrapper SpanSerializer { get; } = new SpanSerializerWrapper();
        public static JsonSerializerWrapper StringSerializer { get; } = new StringSerializerWrapper();
        public static StreamingJsonSerializerWrapper AsyncStreamSerializer { get; } = new AsyncStreamSerializerWrapper();
        public static StreamingJsonSerializerWrapper AsyncStreamSerializerWithSmallBuffer { get; } = new AsyncStreamSerializerWrapper(forceSmallBufferInOptions: true);
        public static StreamingJsonSerializerWrapper SyncStreamSerializer { get; } = new SyncStreamSerializerWrapper();
        public static StreamingJsonSerializerWrapper SyncStreamSerializerWithSmallBuffer { get; } = new SyncStreamSerializerWrapper(forceSmallBufferInOptions: true);
        public static JsonSerializerWrapper ReaderWriterSerializer { get; } = new ReaderWriterSerializerWrapper();
        public static JsonSerializerWrapper DocumentSerializer { get; } = new DocumentSerializerWrapper();
        public static JsonSerializerWrapper ElementSerializer { get; } = new ElementSerializerWrapper();
        public static JsonSerializerWrapper NodeSerializer { get; } = new NodeSerializerWrapper();

        private class SpanSerializerWrapper : JsonSerializerWrapper
        {
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
        }

        private class StringSerializerWrapper : JsonSerializerWrapper
        {
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
        }

        private class AsyncStreamSerializerWrapper : StreamingJsonSerializerWrapper
        {
            private readonly bool _forceSmallBufferInOptions;

            public override bool IsAsyncSerializer => true;

            public AsyncStreamSerializerWrapper(bool forceSmallBufferInOptions = false)
            {
                _forceSmallBufferInOptions = forceSmallBufferInOptions;
            }

            private JsonSerializerOptions? ResolveOptionsInstance(JsonSerializerOptions? options)
                => _forceSmallBufferInOptions ? JsonSerializerOptionsSmallBufferMapper.ResolveOptionsInstanceWithSmallBuffer(options) : options;

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

            public override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerContext context)
            {
                return JsonSerializer.SerializeAsync(stream, value, inputType, context);
            }

            public override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync<T>(utf8Json, ResolveOptionsInstance(options));
            }

            public override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync(utf8Json, returnType, ResolveOptionsInstance(options));
            }

            public override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return await JsonSerializer.DeserializeAsync<T>(utf8Json, jsonTypeInfo);
            }

            public override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerContext context)
            {
                return await JsonSerializer.DeserializeAsync(utf8Json, returnType, context);
            }
        }

        private class SyncStreamSerializerWrapper : StreamingJsonSerializerWrapper
        {
            private readonly bool _forceSmallBufferInOptions;

            public SyncStreamSerializerWrapper(bool forceSmallBufferInOptions = false)
            {
                _forceSmallBufferInOptions = forceSmallBufferInOptions;
            }

            private JsonSerializerOptions? ResolveOptionsInstance(JsonSerializerOptions? options)
                => _forceSmallBufferInOptions ? JsonSerializerOptionsSmallBufferMapper.ResolveOptionsInstanceWithSmallBuffer(options) : options;

            public override bool IsAsyncSerializer => false;

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

            public override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerContext context)
            {
                JsonSerializer.Serialize(stream, value, inputType, context);
                return Task.CompletedTask;
            }

            public override Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null)
            {
                T result = JsonSerializer.Deserialize<T>(utf8Json, ResolveOptionsInstance(options));
                return Task.FromResult(result);
            }

            public override Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
            {
                object result = JsonSerializer.Deserialize(utf8Json, returnType, ResolveOptionsInstance(options));
                return Task.FromResult(result);
            }

            public override Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo)
            {
                T result = JsonSerializer.Deserialize<T>(utf8Json, jsonTypeInfo);
                return Task.FromResult(result);
            }

            public override Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerContext context)
            {
                object result = JsonSerializer.Deserialize(utf8Json, returnType, context);
                return Task.FromResult(result);
            }
        }

        private class ReaderWriterSerializerWrapper : JsonSerializerWrapper
        {
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

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json), OptionsHelpers.GetReaderOptions(context?.Options));
                return Task.FromResult(JsonSerializer.Deserialize(ref reader, type, context));
            }
        }

        private class DocumentSerializerWrapper : JsonSerializerWrapper
        {
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

            public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                using JsonDocument document = JsonDocument.Parse(json, OptionsHelpers.GetDocumentOptions(context?.Options));
                return Task.FromResult(document.RootElement.Deserialize(type, context));
            }
        }

        private class NodeSerializerWrapper : JsonSerializerWrapper
        {
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
    }
}
