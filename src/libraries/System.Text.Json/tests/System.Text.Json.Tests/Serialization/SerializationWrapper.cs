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
    public abstract class SerializationWrapper
    {
        private static readonly JsonSerializerOptions _optionsWithSmallBuffer = new JsonSerializerOptions { DefaultBufferSize = 1 };

        public static SerializationWrapper SpanSerializer => new SpanSerializerWrapper();
        public static SerializationWrapper StringSerializer => new StringSerializerWrapper();
        public static SerializationWrapper StreamSerializer => new StreamSerializerWrapper();
        public static SerializationWrapper StreamSerializerWithSmallBuffer => new StreamSerializerWrapperWithSmallBuffer();
        public static SerializationWrapper WriterSerializer => new WriterSerializerWrapper();

        protected internal abstract Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null);

        protected internal abstract Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null);

        protected internal abstract Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context);

        protected internal abstract Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo);


        private class SpanSerializerWrapper : SerializationWrapper
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
        }

        private class StringSerializerWrapper : SerializationWrapper
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
        }

        private class StreamSerializerWrapper : SerializationWrapper
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
        }

        private class StreamSerializerWrapperWithSmallBuffer : StreamSerializerWrapper
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

        private class WriterSerializerWrapper : SerializationWrapper
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
        }
    }
}
