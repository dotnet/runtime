// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;

namespace System.Text.Json.SourceGeneration.Tests
{
    internal sealed class JsonSerializerWrapperForString_SourceGen : JsonSerializerWrapperForString
    {
        private readonly JsonSerializerContext _defaultContext;
        private readonly Func<JsonSerializerOptions, JsonSerializerContext> _customContextCreator;

        public JsonSerializerWrapperForString_SourceGen(JsonSerializerContext defaultContext, Func<JsonSerializerOptions, JsonSerializerContext> customContextCreator)
        {
            _defaultContext = defaultContext ?? throw new ArgumentNullException(nameof(defaultContext));
            _customContextCreator = customContextCreator ?? throw new ArgumentNullException(nameof(defaultContext));
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
}
