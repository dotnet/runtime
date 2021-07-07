// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class for wrapping Stream-based JsonSerializer methods which allows tests to run under different configurations.
    /// </summary>
    public abstract class JsonSerializationWrapperForStream
    {
        public static JsonSerializationWrapperForStream AsyncStreamSerializer => new AsyncStreamSerializerWrapper();
        public static JsonSerializationWrapperForStream SyncStreamSerializer => new SyncStreamSerializerWrapper();

        protected internal abstract Task SerializeWrapper<T>(Stream stream, T value, JsonSerializerOptions options = null);
        protected internal abstract Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerOptions options = null);
        protected internal abstract Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo);
        protected internal abstract Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null);
        protected internal abstract Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null);
        protected internal abstract Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo);

        private class AsyncStreamSerializerWrapper : JsonSerializationWrapperForStream
        {
            protected internal override async Task SerializeWrapper<T>(Stream utf8Json, T value, JsonSerializerOptions options = null)
            {
                await JsonSerializer.SerializeAsync<T>(utf8Json, value, options);
            }

            protected internal override async Task SerializeWrapper(Stream utf8Json, object value, Type inputType, JsonSerializerOptions options = null)
            {
                await JsonSerializer.SerializeAsync(utf8Json, value, inputType, options);
            }

            protected internal override async Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);
            }

            protected internal override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync<T>(utf8Json, options);
            }

            protected internal override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
            {
                return await JsonSerializer.DeserializeAsync(utf8Json, returnType, options);
            }

            protected internal override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return await JsonSerializer.DeserializeAsync<T>(utf8Json, jsonTypeInfo);
            }
        }

        private class SyncStreamSerializerWrapper : JsonSerializationWrapperForStream
        {
            protected internal override Task SerializeWrapper<T>(Stream stream, T value, JsonSerializerOptions options = null)
            {
                JsonSerializer.Serialize<T>(stream, value, options);
                return Task.FromResult(false);
            }

            protected internal override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerOptions options = null)
            {
                JsonSerializer.Serialize(stream, value, inputType, options);
                return Task.FromResult(false);
            }

            protected internal override Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo)
            {
                JsonSerializer.Serialize(stream, value, jsonTypeInfo);
                return Task.FromResult(false);
            }

            protected internal override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null)
            {
                return await Task.FromResult(JsonSerializer.Deserialize<T>(utf8Json, options));
            }

            protected internal override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
            {
                return await Task.FromResult(JsonSerializer.Deserialize(utf8Json, returnType, options));
            }

            protected internal override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo)
            {
                return await Task.FromResult(JsonSerializer.Deserialize<T>(utf8Json, jsonTypeInfo));
            }
        }
    }
}
