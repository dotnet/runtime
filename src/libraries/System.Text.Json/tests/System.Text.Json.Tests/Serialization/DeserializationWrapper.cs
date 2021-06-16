// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class for wrapping serialization calls which allows tests to run under different configurations.
    /// </summary>
    public abstract class DeserializationWrapper
    {
        private static readonly JsonSerializerOptions _optionsWithSmallBuffer = new JsonSerializerOptions { DefaultBufferSize = 1 };

        public static DeserializationWrapper StringDeserializer => new StringDeserializerWrapper();
        public static DeserializationWrapper StreamDeserializer => new StreamDeserializerWrapper();
        public static DeserializationWrapper SpanDeserializer => new SpanDeserializerWrapper();

        public static DeserializationWrapper ReaderDeserializer => new ReaderDeserializerWrapper();

        protected internal abstract Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null);

        protected internal abstract Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null);

        protected internal abstract Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo);

        protected internal abstract Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context);

        private class StringDeserializerWrapper : DeserializationWrapper
        {
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

        private class StreamDeserializerWrapper : DeserializationWrapper
        {
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

        private class SpanDeserializerWrapper : DeserializationWrapper
        {
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

        private class ReaderDeserializerWrapper : DeserializationWrapper
        {
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
