// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class for abstracting System.IO.Pipelines JsonSerializer method families.
    /// </summary>
    public abstract partial class PipeJsonSerializerWrapper : StreamingJsonSerializerWrapper
    {
        public abstract Task SerializeWrapper(PipeWriter stream, object value, Type inputType, JsonSerializerOptions? options = null);
        public abstract Task SerializeWrapper<T>(PipeWriter stream, T value, JsonSerializerOptions? options = null);
        public abstract Task SerializeWrapper(PipeWriter stream, object value, Type inputType, JsonSerializerContext context);
        public abstract Task SerializeWrapper<T>(PipeWriter stream, T value, JsonTypeInfo<T> jsonTypeInfo);
        public abstract Task SerializeWrapper(PipeWriter stream, object value, JsonTypeInfo jsonTypeInfo);
        public abstract Task<object> DeserializeWrapper(PipeReader utf8Json, Type returnType, JsonSerializerOptions? options = null);
        public abstract Task<T> DeserializeWrapper<T>(PipeReader utf8Json, JsonSerializerOptions? options = null);
        public abstract Task<object> DeserializeWrapper(PipeReader utf8Json, Type returnType, JsonSerializerContext context);
        public abstract Task<T> DeserializeWrapper<T>(PipeReader utf8Json, JsonTypeInfo<T> jsonTypeInfo);
        public abstract Task<object> DeserializeWrapper(PipeReader utf8Json, JsonTypeInfo jsonTypeInfo);

        public override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
        {
            Pipe pipe = new Pipe();
            await SerializeWrapper(pipe.Writer, value, inputType, options);
            ReadResult result = await pipe.Reader.ReadAsync();
            return Encoding.UTF8.GetString(result.Buffer.ToArray());
        }

        public override async Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
        {
            Pipe pipe = new Pipe();
            await SerializeWrapper(pipe.Writer, value, options);
            ReadResult result = await pipe.Reader.ReadAsync();
            return Encoding.UTF8.GetString(result.Buffer.ToArray());
        }

        public override async Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
        {
            Pipe pipe = new Pipe();
            await SerializeWrapper(pipe.Writer, value, inputType, context);
            ReadResult result = await pipe.Reader.ReadAsync();
            return Encoding.UTF8.GetString(result.Buffer.ToArray());
        }

        public override async Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
        {
            Pipe pipe = new Pipe();
            await SerializeWrapper(pipe.Writer, value, jsonTypeInfo);
            ReadResult result = await pipe.Reader.ReadAsync();
            return Encoding.UTF8.GetString(result.Buffer.ToArray());
        }

        public override async Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
        {
            Pipe pipe = new Pipe();
            await SerializeWrapper(pipe.Writer, value, jsonTypeInfo);
            ReadResult result = await pipe.Reader.ReadAsync();
            return Encoding.UTF8.GetString(result.Buffer.ToArray());
        }

        public override async Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
        {
            Pipe pipe = new Pipe();
            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(json));
            pipe.Writer.Complete();
            return await DeserializeWrapper<T>(pipe.Reader, options);
        }

        public override async Task<object> DeserializeWrapper(string json, Type returnType, JsonSerializerOptions options = null)
        {
            Pipe pipe = new Pipe();
            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(json));
            pipe.Writer.Complete();
            return await DeserializeWrapper(pipe.Reader, returnType, options);
        }

        public override async Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
        {
            Pipe pipe = new Pipe();
            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(json));
            pipe.Writer.Complete();
            return await DeserializeWrapper(pipe.Reader, jsonTypeInfo);
        }

        public override async Task<object> DeserializeWrapper(string json, JsonTypeInfo jsonTypeInfo)
        {
            Pipe pipe = new Pipe();
            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(json));
            pipe.Writer.Complete();
            return await DeserializeWrapper(pipe.Reader, jsonTypeInfo);
        }

        public override async Task<object> DeserializeWrapper(string json, Type returnType, JsonSerializerContext context)
        {
            Pipe pipe = new Pipe();
            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(json));
            pipe.Writer.Complete();
            return await DeserializeWrapper(pipe.Reader, returnType, context);
        }
    }
}
