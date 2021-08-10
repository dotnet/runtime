// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Provides a mechanism to invoke "fast-path" serialization logic via
    /// <see cref="JsonTypeInfo{T}.Serialize"/>. This type holds an optional
    /// reference to an actual <see cref="JsonConverter{T}"/> for the type
    /// <typeparamref name="T"/>, to provide a fallback when the fast path cannot be used.
    /// </summary>
    /// <typeparam name="T">The type to converter</typeparam>
    internal sealed class JsonMetadataServicesConverter<T> : JsonResumableConverter<T>
    {
        private readonly Func<JsonConverter<T>> _converterCreator;

        private readonly ConverterStrategy _converterStrategy;

        private JsonConverter<T>? _converter;

        // A backing converter for when fast-path logic cannot be used.
        private JsonConverter<T> Converter
        {
            get
            {
                _converter ??= _converterCreator();
                Debug.Assert(_converter != null);
                Debug.Assert(_converter.ConverterStrategy == _converterStrategy);
                return _converter;
            }
        }

        internal override ConverterStrategy ConverterStrategy => _converterStrategy;

        internal override Type? KeyType => Converter.KeyType;

        internal override Type? ElementType => Converter.ElementType;

        internal override bool ConstructorIsParameterized => Converter.ConstructorIsParameterized;

        public JsonMetadataServicesConverter(Func<JsonConverter<T>> converterCreator, ConverterStrategy converterStrategy)
        {
            _converterCreator = converterCreator ?? throw new ArgumentNullException(nameof(converterCreator));
            _converterStrategy = converterStrategy;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T? value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            if (_converterStrategy == ConverterStrategy.Object)
            {
                if (jsonTypeInfo.PropertyCache == null)
                {
                    jsonTypeInfo.InitializePropCache();
                }

                if (jsonTypeInfo.ParameterCache == null && jsonTypeInfo.IsObjectWithParameterizedCtor)
                {
                    jsonTypeInfo.InitializeParameterCache();
                }
            }

            return Converter.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            Debug.Assert(options == jsonTypeInfo.Options);

            if (!state.SupportContinuation &&
                jsonTypeInfo is JsonTypeInfo<T> info &&
                info.Serialize != null &&
                info.Options._context?.CanUseSerializationLogic == true)
            {
                info.Serialize(writer, value);
                return true;
            }

            if (_converterStrategy == ConverterStrategy.Object && jsonTypeInfo.PropertyCache == null)
            {
                jsonTypeInfo.InitializePropCache();
            }

            return Converter.OnTryWrite(writer, value, options, ref state);
        }
    }
}
