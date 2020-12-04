// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
        public static DeserializationWrapper SpanDeserializer => new SpanDesearializationWrapper();

        protected internal abstract Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null);

        protected internal abstract Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null);

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
        }

        private class StreamDeserializerWrapper : DeserializationWrapper
        {
            protected internal override async Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                if (options == null)
                {
                    options = _optionsWithSmallBuffer;
                }

                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync<T>(stream, options);
                }
            }

            protected internal override async Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                if (options == null)
                {
                    options = _optionsWithSmallBuffer;
                }

                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return await JsonSerializer.DeserializeAsync(stream, type, options);
                }
            }
        }

        private class SpanDesearializationWrapper : DeserializationWrapper
        {
            protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json.AsSpan(), options));
            }

            protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            {
                return Task.FromResult(JsonSerializer.Deserialize(json.AsSpan(), type, options));
            }
        }
    }
}
