// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;

namespace System.Text.Json.SourceGeneration.Tests
{
    internal sealed class StringSerializerWrapper : JsonSerializerWrapperForString
    {
        private readonly JsonSerializerContext _defaultContext;
        private readonly Func<JsonSerializerOptions, JsonSerializerContext> _customContextCreator;

        protected internal override bool SupportsNullValueOnDeserialize => false;

        public StringSerializerWrapper(JsonSerializerContext defaultContext!!, Func<JsonSerializerOptions, JsonSerializerContext> customContextCreator!!)
        {
            _defaultContext = defaultContext;
            _customContextCreator = customContextCreator;
        }

        protected internal override Task<string> SerializeWrapper(object value, Type type, JsonSerializerOptions? options = null)
        {
            if (options != null)
            {
                return Task.FromResult(Serialize(value, type, options));
            }

            return Task.FromResult(JsonSerializer.Serialize(value, type, _defaultContext));
        }

        private string Serialize(object value, Type type, JsonSerializerOptions options)
        {
            JsonSerializerContext context = _customContextCreator(new JsonSerializerOptions(options));
            return JsonSerializer.Serialize(value, type, context);
        }

        protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions? options = null)
        {
            Type runtimeType = GetRuntimeType(value);

            if (runtimeType != typeof(T))
            {
                return SerializeWrapper(value, runtimeType, options);
            }

            if (options != null)
            {
                return Task.FromResult(Serialize(value, options));
            }

            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)_defaultContext.GetTypeInfo(typeof(T));
            return Task.FromResult(JsonSerializer.Serialize(value, typeInfo));
        }

        private string Serialize<T>(T value, JsonSerializerOptions options)
        {
            JsonSerializerContext context = _customContextCreator(new JsonSerializerOptions(options));
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return JsonSerializer.Serialize(value, typeInfo);
        }

        private static Type GetRuntimeType<TValue>(in TValue value)
        {
            if (typeof(TValue) == typeof(object) && value != null)
            {
                return value.GetType();
            }

            return typeof(TValue);
        }

        protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            => throw new NotImplementedException();

        protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            => throw new NotImplementedException();

        protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions? options = null)
        {
            if (options != null)
            {
                return Task.FromResult(Deserialize<T>(json, options));
            }

            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)_defaultContext.GetTypeInfo(typeof(T));
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, typeInfo));
        }

        private T Deserialize<T>(string json, JsonSerializerOptions options)
        {
            JsonSerializerContext context = _customContextCreator(new JsonSerializerOptions(options));
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return JsonSerializer.Deserialize<T>(json, typeInfo);
        }

        protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions? options = null)
        {
            if (options != null)
            {
                return Task.FromResult(Deserialize(json, type, options));
            }

            return Task.FromResult(JsonSerializer.Deserialize(json, type, _defaultContext));
        }

        private object Deserialize(string json, Type type, JsonSerializerOptions options)
        {
            JsonSerializerContext context = _customContextCreator(new JsonSerializerOptions(options));
            return JsonSerializer.Deserialize(json, type, context);
        }

        protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            => throw new NotImplementedException();

        protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            => throw new NotImplementedException();
    }

    internal sealed class StreamSerializerWrapper : JsonSerializerWrapperForStream
    {
        private readonly JsonSerializerContext _defaultContext;
        private readonly Func<JsonSerializerOptions, JsonSerializerContext> _customContextCreator;

        public StreamSerializerWrapper(JsonSerializerContext defaultContext!!, Func<JsonSerializerOptions, JsonSerializerContext> customContextCreator!!)
        {
            _defaultContext = defaultContext;
            _customContextCreator = customContextCreator;
        }

        protected internal override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions? options = null)
        {
            if (options != null)
            {
                return await Deserialize<T>(utf8Json, options);
            }

            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)_defaultContext.GetTypeInfo(typeof(T));
            return await JsonSerializer.DeserializeAsync<T>(utf8Json, typeInfo);
        }

        private async Task<T> Deserialize<T>(Stream utf8Json, JsonSerializerOptions options)
        {
            JsonSerializerContext context = _customContextCreator(new JsonSerializerOptions(options));
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return await JsonSerializer.DeserializeAsync<T>(utf8Json, typeInfo);
        }

        protected internal override Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null) => throw new NotImplementedException();
        protected internal override Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo) => throw new NotImplementedException();

        protected internal override async Task SerializeWrapper<T>(Stream stream, T value, JsonSerializerOptions options = null)
        {
            JsonSerializerContext context = options != null
                ? _customContextCreator(new JsonSerializerOptions(options))
                : _defaultContext;

            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            await JsonSerializer.SerializeAsync<T>(stream, value, typeInfo);
        }

        protected internal override async Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerOptions options = null)
        {
            JsonSerializerContext context = options != null
                ? _customContextCreator(new JsonSerializerOptions(options))
                : _defaultContext;

            await JsonSerializer.SerializeAsync(stream, value, inputType, context);
        }
        protected internal override Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo) => throw new NotImplementedException();
    }
}
