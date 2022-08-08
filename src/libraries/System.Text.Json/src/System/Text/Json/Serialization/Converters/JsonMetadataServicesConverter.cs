// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Provides a mechanism to invoke "fast-path" serialization logic via
    /// <see cref="JsonTypeInfo{T}.SerializeHandler"/>. This type holds an optional
    /// reference to an actual <see cref="JsonConverter{T}"/> for the type
    /// <typeparamref name="T"/>, to provide a fallback when the fast path cannot be used.
    /// </summary>
    /// <typeparam name="T">The type to converter</typeparam>
    internal sealed class JsonMetadataServicesConverter<T> : JsonResumableConverter<T>
    {
        private readonly Func<JsonConverter<T>>? _converterCreator;

        private readonly ConverterStrategy _converterStrategy;

        private JsonConverter<T>? _converter;

        // A backing converter for when fast-path logic cannot be used.
        internal JsonConverter<T> Converter
        {
            get
            {
                _converter ??= _converterCreator!();
                Debug.Assert(_converter != null);
                Debug.Assert(_converter.ConverterStrategy == _converterStrategy);
                return _converter;
            }
        }

        internal override ConverterStrategy ConverterStrategy => _converterStrategy;

        internal override Type? KeyType => Converter.KeyType;

        internal override Type? ElementType => Converter.ElementType;

        internal override bool ConstructorIsParameterized => Converter.ConstructorIsParameterized;
        internal override bool SupportsCreateObjectDelegate => Converter.SupportsCreateObjectDelegate;
        internal override bool CanHaveMetadata => Converter.CanHaveMetadata;

        public JsonMetadataServicesConverter(Func<JsonConverter<T>> converterCreator, ConverterStrategy converterStrategy)
        {
            if (converterCreator is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(converterCreator));
            }

            _converterCreator = converterCreator;
            _converterStrategy = converterStrategy;
        }

        public JsonMetadataServicesConverter(JsonConverter<T> converter)
        {
            _converter = converter;
            _converterStrategy = converter.ConverterStrategy;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T? value)
             => Converter.OnTryRead(ref reader, typeToConvert, options, ref state, out value);

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            Debug.Assert(options == jsonTypeInfo.Options);

            if (!state.SupportContinuation &&
                jsonTypeInfo.CanUseSerializeHandler &&
                !state.CurrentContainsMetadata) // Do not use the fast path if state needs to write metadata.
            {
                Debug.Assert(jsonTypeInfo is JsonTypeInfo<T> typeInfo && typeInfo.SerializeHandler != null);
                Debug.Assert(options.SerializerContext?.CanUseSerializationLogic == true);
                ((JsonTypeInfo<T>)jsonTypeInfo).SerializeHandler!(writer, value);
                return true;
            }

            jsonTypeInfo.ValidateCanBeUsedForMetadataSerialization();
            return Converter.OnTryWrite(writer, value, options, ref state);
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
            => Converter.ConfigureJsonTypeInfo(jsonTypeInfo, options);
    }
}
