// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class for abstracting JsonSerializer method families behind an async/string serialization facade.
    /// </summary>
    public abstract partial class JsonSerializerWrapper
    {
        /// <summary>
        /// Either JsonSerializerOptions.Default for reflection or the JsonSerializerContext.Options for source gen.
        /// </summary>
        public abstract JsonSerializerOptions DefaultOptions { get; }

        /// <summary>
        /// Do the deserialize methods allow a value of 'null'.
        /// For example, deserializing JSON to a String supports null by returning a 'null' String reference from a literal value of "null".
        /// </summary>
        public virtual bool SupportsNullValueOnDeserialize => false;

        public abstract Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null);

        public abstract Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null);

        public abstract Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context);

        public abstract Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo);

        public abstract Task<string> SerializeWrapper(object value, JsonTypeInfo jsonTypeInfo);

        public abstract Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null);

        public abstract Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null);

        public abstract Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo);

        public abstract Task<object> DeserializeWrapper(string value, JsonTypeInfo jsonTypeInfo);

        public abstract Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context);


        public JsonTypeInfo GetTypeInfo(Type type, bool mutable = false)
        {
            JsonSerializerOptions defaultOptions = DefaultOptions;
            // return a fresh mutable instance or the cached readonly metadata
            return mutable ? defaultOptions.TypeInfoResolver.GetTypeInfo(type, defaultOptions) : defaultOptions.GetTypeInfo(type);
        }

        public JsonTypeInfo<T> GetTypeInfo<T>(bool mutable = false)
            => (JsonTypeInfo<T>)GetTypeInfo(typeof(T), mutable);

        public JsonSerializerOptions GetDefaultOptionsWithMetadataModifier(Action<JsonTypeInfo> modifier)
        {
            JsonSerializerOptions defaultOptions = DefaultOptions;
            return new JsonSerializerOptions(defaultOptions)
            {
                TypeInfoResolver = defaultOptions.TypeInfoResolver.WithModifier(modifier)
            };
        }

        public JsonSerializerOptions CreateOptions(
            Action<JsonSerializerOptions> configure = null,
            bool includeFields = false,
            List<JsonConverter> customConverters = null,
            Action<JsonTypeInfo> modifier = null)
        {
            IJsonTypeInfoResolver resolver = DefaultOptions.TypeInfoResolver;
            resolver = modifier != null ? resolver.WithModifier(modifier) : resolver;

            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = resolver,
                IncludeFields = includeFields,
            };

            if (customConverters != null)
            {
                foreach (JsonConverter converter in customConverters)
                {
                    options.Converters.Add(converter);
                }
            }

            configure?.Invoke(options);

            options.MakeReadOnly();

            return options;
        }
    }

    public static class JsonTypeInfoResolverExtensions
    {
        public static IJsonTypeInfoResolver WithModifier(this IJsonTypeInfoResolver resolver, Action<JsonTypeInfo> modifier)
            => new JsonTypeInfoResolverWithModifier(resolver, modifier);

        private class JsonTypeInfoResolverWithModifier : IJsonTypeInfoResolver
        {
            private readonly IJsonTypeInfoResolver _source;
            private readonly Action<JsonTypeInfo> _modifier;

            public JsonTypeInfoResolverWithModifier(IJsonTypeInfoResolver source, Action<JsonTypeInfo> modifier)
            {
                _source = source;
                _modifier = modifier;
            }

            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo? typeInfo = _source.GetTypeInfo(type, options);

                if (typeInfo != null)
                {
                    _modifier(typeInfo);
                }

                return typeInfo;
            }
        }
    }
}
