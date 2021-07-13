// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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

        private class SpanSerializerWrapper : JsonSerializerWrapperForString
        {
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
            protected internal override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, value, inputType, options);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync<T>(stream, value, options);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, value, inputType, context);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override async Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync<T>(stream, options ?? _optionsWithSmallBuffer);
                }
            }

            protected internal override async Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync(stream, type, options ?? _optionsWithSmallBuffer);
                }
            }

            protected internal override async Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo);
                }
            }

            protected internal override async Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync(stream, type, context);
                }
            }
        }

        private class AsyncStreamSerializerWrapperWithSmallBuffer : AsyncStreamSerializerWrapper
        {
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
            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using var stream = new MemoryStream();
                JsonSerializer.Serialize(stream, value, inputType, options);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                using var stream = new MemoryStream();
                JsonSerializer.Serialize<T>(stream, value, options);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                using var stream = new MemoryStream();
                JsonSerializer.Serialize(stream, value, inputType, context);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                using var stream = new MemoryStream();
                JsonSerializer.Serialize(stream, value, jsonTypeInfo);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(stream, options ?? _optionsWithSmallBuffer));
                }
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return Task.FromResult(JsonSerializer.Deserialize(stream, type, options ?? _optionsWithSmallBuffer));
                }
            }

            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(stream, jsonTypeInfo));
                }
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return Task.FromResult(JsonSerializer.Deserialize(stream, type, context));
                }
            }
        }

        private class ReaderWriterSerializerWrapper : JsonSerializerWrapperForString
        {
            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize(writer, value, inputType, options);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize<T>(writer, value, options);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            {
                using MemoryStream stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize(writer, value, inputType, context);
                return Task.FromResult(Encoding.UTF8.GetString(stream.ToArray()));
            }

            protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                using MemoryStream stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize(writer, value, jsonTypeInfo);
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
    }
}
