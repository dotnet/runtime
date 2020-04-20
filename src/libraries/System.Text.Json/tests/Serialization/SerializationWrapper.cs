// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
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

        protected internal abstract string Serialize(object value, Type inputType, JsonSerializerOptions options = null);

        protected internal abstract string Serialize<T>(T value, JsonSerializerOptions options = null);


        private class SpanSerializerWrapper : SerializationWrapper
        {
            protected internal override string Serialize(object value, Type inputType, JsonSerializerOptions options = null)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(value, inputType, options);
                return Encoding.UTF8.GetString(result);
            }

            protected internal override string Serialize<T>(T value, JsonSerializerOptions options = null)
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes<T>(value, options);
                return Encoding.UTF8.GetString(result);
            }
        }

        private class StringSerializerWrapper : SerializationWrapper
        {
            protected internal override string Serialize(object value, Type inputType, JsonSerializerOptions options = null)
            {
                return JsonSerializer.Serialize(value, inputType, options);
            }

            protected internal override string Serialize<T>(T value, JsonSerializerOptions options = null)
            {
                return JsonSerializer.Serialize(value, options);
            }
        }

        private class StreamSerializerWrapper : SerializationWrapper
        {
            protected internal override string Serialize(object value, Type inputType, JsonSerializerOptions options = null)
            {
                return Task.Run(async () =>
                {
                    using var stream = new MemoryStream();
                    await JsonSerializer.SerializeAsync(stream, value, inputType, options);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }).GetAwaiter().GetResult();
            }

            protected internal override string Serialize<T>(T value, JsonSerializerOptions options = null)
            {
                return Task.Run(async () =>
                {
                    using var stream = new MemoryStream();
                    await JsonSerializer.SerializeAsync<T>(stream, value, options);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }).GetAwaiter().GetResult();
            }
        }

        private class StreamSerializerWrapperWithSmallBuffer : StreamSerializerWrapper
        {
            protected internal override string Serialize(object value, Type inputType, JsonSerializerOptions options = null)
            {
                if (options == null)
                {
                    options = _optionsWithSmallBuffer;
                }

                return base.Serialize(value, inputType, options);
            }

            protected internal override string Serialize<T>(T value, JsonSerializerOptions options = null)
            {
                return base.Serialize<T>(value, options);
            }
        }

        private class WriterSerializerWrapper : SerializationWrapper
        {
            protected internal override string Serialize(object value, Type inputType, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize(writer, value, inputType, options);
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            protected internal override string Serialize<T>(T value, JsonSerializerOptions options = null)
            {
                using MemoryStream stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize<T>(writer, value, options);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
