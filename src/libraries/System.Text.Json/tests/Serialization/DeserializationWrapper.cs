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
    public abstract class DeserializationWrapper
    {
        private static readonly JsonSerializerOptions _optionsWithSmallBuffer = new JsonSerializerOptions { DefaultBufferSize = 1 };

        public static DeserializationWrapper StringDeserializer => new StringDeserializerWrapper();
        public static DeserializationWrapper StreamDeserializer => new StreamDeserializerWrapper();

        protected internal abstract T Deserialize<T>(string json, JsonSerializerOptions options = null);

        protected internal abstract object Deserialize(string json, Type type, JsonSerializerOptions options = null);

        private class StringDeserializerWrapper : DeserializationWrapper
        {
            protected internal override T Deserialize<T>(string json, JsonSerializerOptions options = null)
            {
                return JsonSerializer.Deserialize<T>(json, options);
            }

            protected internal override object Deserialize(string json, Type type, JsonSerializerOptions options = null)
            {
                return JsonSerializer.Deserialize(json, type, options);
            }
        }

        private class StreamDeserializerWrapper : DeserializationWrapper
        {
            protected internal override T Deserialize<T>(string json, JsonSerializerOptions options = null)
            {
                if (options == null)
                {
                    options = _optionsWithSmallBuffer;
                }

                return Task.Run(async () =>
                {
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        return await JsonSerializer.DeserializeAsync<T>(stream, options);
                    }
                }).GetAwaiter().GetResult();
            }

            protected internal override object Deserialize(string json, Type type, JsonSerializerOptions options = null)
            {
                if (options == null)
                {
                    options = _optionsWithSmallBuffer;
                }

                return Task.Run(async () =>
                {
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        return await JsonSerializer.DeserializeAsync(stream, type, options);
                    }
                }).GetAwaiter().GetResult();
            }
        }
    }
}
