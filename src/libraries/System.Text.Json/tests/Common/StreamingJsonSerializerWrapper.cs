// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class for abstracting streaming JsonSerializer method families.
    /// </summary>
    public abstract partial class StreamingJsonSerializerWrapper : JsonSerializerWrapper
    {
        /// <summary>
        /// True if the serializer is streaming data synchronously.
        /// </summary>
        public virtual bool IsBlockingSerializer => false;

        public abstract Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerOptions? options = null);
        public abstract Task SerializeWrapper<T>(Stream stream, T value, JsonSerializerOptions? options = null);
        public abstract Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerContext context);
        public abstract Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo);
        public abstract Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions? options = null);
        public abstract Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions? options = null);
        public abstract Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerContext context);
        public abstract Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo);

        public override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
        {
            using var utf8Stream = new Utf8MemoryStream();
            await SerializeWrapper(utf8Stream, value, inputType, options);
            return utf8Stream.AsString();
        }

        public override async Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
        {
            using var utf8Stream = new Utf8MemoryStream();
            await SerializeWrapper(utf8Stream, value, options);
            return utf8Stream.AsString();
        }

        public override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
        {
            using var utf8Stream = new Utf8MemoryStream();
            await SerializeWrapper(utf8Stream, value, inputType, context);
            return utf8Stream.AsString();
        }

        public override async Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
        {
            using var utf8Stream = new Utf8MemoryStream();
            await SerializeWrapper(utf8Stream, value, jsonTypeInfo);
            return utf8Stream.AsString();
        }

        public override async Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
        {
            using var utf8Stream = new Utf8MemoryStream(json);
            return await DeserializeWrapper<T>(utf8Stream, options);
        }

        public override async Task<object> DeserializeWrapper(string json, Type returnType, JsonSerializerOptions options = null)
        {
            using var utf8Stream = new Utf8MemoryStream(json);
            return await DeserializeWrapper(utf8Stream, returnType, options);
        }

        public override async Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
        {
            using var utf8Stream = new Utf8MemoryStream(json);
            return await DeserializeWrapper(utf8Stream, jsonTypeInfo);
        }

        public override async Task<object> DeserializeWrapper(string json, Type returnType, JsonSerializerContext context)
        {
            using var utf8Stream = new Utf8MemoryStream(json);
            return await DeserializeWrapper(utf8Stream, returnType, context);
        }
    }
}
