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

        public bool IsSourceGeneratedSerializer => DefaultOptions.TypeInfoResolver is JsonSerializerContext;

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


        public virtual JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions? options = null, bool mutable = false)
        {
            options ??= DefaultOptions;
            options.MakeReadOnly(populateMissingResolver: true);
            return mutable ? options.TypeInfoResolver.GetTypeInfo(type, options) : options.GetTypeInfo(type);
        }

        public JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerOptions? options = null,bool mutable = false)
            => (JsonTypeInfo<T>)GetTypeInfo(typeof(T), options, mutable);

        public JsonSerializerOptions GetDefaultOptionsWithMetadataModifier(Action<JsonTypeInfo> modifier)
        {
            JsonSerializerOptions defaultOptions = DefaultOptions;
            return new JsonSerializerOptions(defaultOptions)
            {
                TypeInfoResolver = defaultOptions.TypeInfoResolver.WithAddedModifier(modifier)
            };
        }

        public JsonSerializerOptions CreateOptions(
            Action<JsonSerializerOptions> configure = null,
            bool? includeFields = false,
            List<JsonConverter>? customConverters = null,
            Action<JsonTypeInfo>? modifier = null,
            bool makeReadOnly = true)
        {
            var options = new JsonSerializerOptions(DefaultOptions);

            if (includeFields != null)
            {
                options.IncludeFields = includeFields.Value;
            }

            if (modifier != null && options.TypeInfoResolver != null)
            {
                options.TypeInfoResolver = DefaultOptions.TypeInfoResolver.WithAddedModifier(modifier);
            }

            if (customConverters != null)
            {
                foreach (JsonConverter converter in customConverters)
                {
                    options.Converters.Add(converter);
                }
            }

            configure?.Invoke(options);

            if (makeReadOnly)
            {
                options.MakeReadOnly();
            }

            return options;
        }
    }
}
