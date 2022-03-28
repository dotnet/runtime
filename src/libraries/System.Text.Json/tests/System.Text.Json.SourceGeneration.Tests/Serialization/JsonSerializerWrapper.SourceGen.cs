// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;

namespace System.Text.Json.SourceGeneration.Tests
{
    internal sealed class StringSerializerWrapper : JsonSerializerWrapper
    {
        private readonly JsonSerializerContext _defaultContext;
        private readonly Func<JsonSerializerOptions, JsonSerializerContext> _customContextCreator;

        public StringSerializerWrapper(JsonSerializerContext defaultContext!!, Func<JsonSerializerOptions, JsonSerializerContext> customContextCreator!!)
        {
            _defaultContext = defaultContext;
            _customContextCreator = customContextCreator;
        }

        public override Task<string> SerializeWrapper(object value, Type type, JsonSerializerOptions? options = null)
        {
            JsonSerializerContext context = GetJsonSerializerContext(options);
            return Task.FromResult(JsonSerializer.Serialize(value, type, context));
        }

        public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions? options = null)
        {
            Type runtimeType = GetRuntimeType(value);

            if (runtimeType != typeof(T))
            {
                return SerializeWrapper(value, runtimeType, options);
            }

            JsonSerializerContext context = GetJsonSerializerContext(options);
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return Task.FromResult(JsonSerializer.Serialize(value, typeInfo));
        }

        public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            => throw new NotImplementedException();

        public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            => throw new NotImplementedException();

        public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions? options = null)
        {
            JsonSerializerContext context = GetJsonSerializerContext(options);
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, typeInfo));
        }

        public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions? options = null)
        {
            JsonSerializerContext context = GetJsonSerializerContext(options);
            return Task.FromResult(JsonSerializer.Deserialize(json, type, context));
        }

        public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            => throw new NotImplementedException();

        public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            => throw new NotImplementedException();

        private JsonSerializerContext GetJsonSerializerContext(JsonSerializerOptions? options)
             => options is null ? _defaultContext : _customContextCreator(new JsonSerializerOptions(options));

        private Type GetRuntimeType<TValue>(in TValue value)
        {
            if (typeof(TValue) == typeof(object) && value != null)
            {
                return value.GetType();
            }

            return typeof(TValue);
        }
    }

    internal sealed class AsyncStreamSerializerWrapper : StreamingJsonSerializerWrapper
    {
        private readonly JsonSerializerContext _defaultContext;
        private readonly Func<JsonSerializerOptions, JsonSerializerContext> _customContextCreator;

        public override bool IsAsyncSerializer => true;

        public AsyncStreamSerializerWrapper(JsonSerializerContext defaultContext!!, Func<JsonSerializerOptions, JsonSerializerContext> customContextCreator!!)
        {
            _defaultContext = defaultContext;
            _customContextCreator = customContextCreator;
        }

        public override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions? options = null)
        {
            JsonSerializerContext context = GetJsonSerializerContext(options);
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return await JsonSerializer.DeserializeAsync<T>(utf8Json, typeInfo);
        }

        public override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
        {
            JsonSerializerContext context = GetJsonSerializerContext(options);
            return await JsonSerializer.DeserializeAsync(utf8Json, returnType, context);
        }

        public override Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo) => throw new NotImplementedException();

        public override async Task SerializeWrapper<T>(Stream stream, T value, JsonSerializerOptions options = null)
        {
            Type runtimeType = GetRuntimeType(value);
            if (runtimeType != typeof(T))
            {
                await JsonSerializer.SerializeAsync(stream, value, runtimeType, options);
                return;
            }

            JsonSerializerContext context = GetJsonSerializerContext(options);
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            await JsonSerializer.SerializeAsync<T>(stream, value, typeInfo);
        }

        public override async Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerOptions options = null)
        {
            JsonSerializerContext context = GetJsonSerializerContext(options);
            await JsonSerializer.SerializeAsync(stream, value, inputType, context);
        }

        public override Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo) => throw new NotImplementedException();
        public override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerContext context) => throw new NotImplementedException();
        public override Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerContext context) => throw new NotImplementedException();

        private JsonSerializerContext GetJsonSerializerContext(JsonSerializerOptions? options)
            => options is null ? _defaultContext : _customContextCreator(new JsonSerializerOptions(options));

        private Type GetRuntimeType<TValue>(in TValue value)
        {
            if (typeof(TValue) == typeof(object) && value != null)
            {
                return value.GetType();
            }

            return typeof(TValue);
        }
    }
}
