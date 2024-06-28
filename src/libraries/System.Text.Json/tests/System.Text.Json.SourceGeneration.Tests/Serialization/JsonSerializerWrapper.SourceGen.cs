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

        public StringSerializerWrapper(JsonSerializerContext defaultContext)
        {
            _defaultContext = defaultContext ?? throw new ArgumentNullException(nameof(defaultContext));
        }

        public override JsonSerializerOptions DefaultOptions => _defaultContext.Options;

        public override Task<string> SerializeWrapper(object value, Type type, JsonSerializerOptions? options = null)
            => Task.FromResult(JsonSerializer.Serialize(value, type, GetOptions(options)));

        public override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions? options = null)
            => Task.FromResult(JsonSerializer.Serialize(value, GetOptions(options)));

        public override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context)
            => Task.FromResult(JsonSerializer.Serialize(value, inputType, context));

        public override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
            => Task.FromResult(JsonSerializer.Serialize(value, jsonTypeInfo));

        public override Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo)
            => Task.FromResult(JsonSerializer.Serialize(value, jsonTypeInfo));

        public override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions? options = null)
            => Task.FromResult(JsonSerializer.Deserialize<T>(json, GetOptions(options)));

        public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions? options = null)
            => Task.FromResult(JsonSerializer.Deserialize(json, type, GetOptions(options)));

        public override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
            => Task.FromResult(JsonSerializer.Deserialize(json, jsonTypeInfo));

        public override Task<object> DeserializeWrapper(string json, JsonTypeInfo jsonTypeInfo)
            => Task.FromResult(JsonSerializer.Deserialize(json, jsonTypeInfo));

        public override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context)
            => Task.FromResult(JsonSerializer.Deserialize(json, type, context));

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions? options = null, bool mutable = false) => base.GetTypeInfo(type, GetOptions(options), mutable);

        private JsonSerializerOptions GetOptions(JsonSerializerOptions? options = null)
        {
            if (options is null)
            {
                return _defaultContext.Options;
            }

            if (options.TypeInfoResolver is null or DefaultJsonTypeInfoResolver { Modifiers.Count: 0 })
            {
                return new JsonSerializerOptions(options) { TypeInfoResolver = _defaultContext };
            }

            return options;
        }
    }

    internal sealed class AsyncStreamSerializerWrapper : StreamingJsonSerializerWrapper
    {
        private readonly JsonSerializerContext _defaultContext;

        public override JsonSerializerOptions DefaultOptions => _defaultContext.Options;
        public override bool IsAsyncSerializer => true;

        public AsyncStreamSerializerWrapper(JsonSerializerContext defaultContext)
        {
            _defaultContext = defaultContext ?? throw new ArgumentNullException(nameof(defaultContext));
        }

        public override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions? options = null)
        {
            return await JsonSerializer.DeserializeAsync<T>(utf8Json, GetOptions(options));
        }

        public override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null)
        {
            return await JsonSerializer.DeserializeAsync(utf8Json, returnType, GetOptions(options));
        }

        public override async Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo)
        {
            return await JsonSerializer.DeserializeAsync<T>(utf8Json, jsonTypeInfo);
        }

        public override async Task<object> DeserializeWrapper(Stream utf8Json, JsonTypeInfo jsonTypeInfo)
        {
            return await JsonSerializer.DeserializeAsync(utf8Json, jsonTypeInfo);
        }

        public override async Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerContext context)
        {
            return await JsonSerializer.DeserializeAsync(utf8Json, returnType, context);
        }

        public override Task SerializeWrapper<T>(Stream stream, T value, JsonSerializerOptions options = null)
            => JsonSerializer.SerializeAsync<T>(stream, value, GetOptions(options));

        public override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerOptions options = null)
            => JsonSerializer.SerializeAsync(stream, value, inputType, GetOptions(options));

        public override Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo)
            => JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);

        public override Task SerializeWrapper(Stream stream, object value, JsonTypeInfo jsonTypeInfo)
            => JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);

        public override Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerContext context)
            => JsonSerializer.SerializeAsync(stream, value, inputType, context);

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions? options = null, bool mutable = false) => base.GetTypeInfo(type, GetOptions(options), mutable);

        private JsonSerializerOptions GetOptions(JsonSerializerOptions? options = null)
        {
            if (options is null)
            {
                return _defaultContext.Options;
            }

            if (options.TypeInfoResolver is null or DefaultJsonTypeInfoResolver { Modifiers.Count: 0 })
            {
                return new JsonSerializerOptions(options) { TypeInfoResolver = _defaultContext };
            }

            return options;
        }
    }
}
