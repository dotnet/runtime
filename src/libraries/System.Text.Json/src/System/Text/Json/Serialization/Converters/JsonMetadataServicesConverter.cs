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
        // A backing converter for when fast-path logic cannot be used.
        internal JsonConverter<T> Converter { get; }

        internal override Type? KeyType => Converter.KeyType;
        internal override Type? ElementType => Converter.ElementType;
        public override bool HandleNull { get; }

        internal override bool ConstructorIsParameterized => Converter.ConstructorIsParameterized;
        internal override bool SupportsCreateObjectDelegate => Converter.SupportsCreateObjectDelegate;
        internal override bool CanHaveMetadata => Converter.CanHaveMetadata;

        internal override bool CanPopulate => Converter.CanPopulate;

        public JsonMetadataServicesConverter(JsonConverter<T> converter)
        {
            Converter = converter;
            ConverterStrategy = converter.ConverterStrategy;
            IsInternalConverter = converter.IsInternalConverter;
            IsInternalConverterForNumberType = converter.IsInternalConverterForNumberType;
            CanBePolymorphic = converter.CanBePolymorphic;

            // Ensure HandleNull values reflect the exact configuration of the source converter
            HandleNullOnRead = converter.HandleNullOnRead;
            HandleNullOnWrite = converter.HandleNullOnWrite;
            HandleNull = converter.HandleNullOnWrite;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T? value)
             => Converter.OnTryRead(ref reader, typeToConvert, options, ref state, out value);

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
            Debug.Assert(jsonTypeInfo is JsonTypeInfo<T> typeInfo && typeInfo.SerializeHandler != null);

            if (!state.SupportContinuation &&
                jsonTypeInfo.CanUseSerializeHandler &&
                !JsonHelpers.RequiresSpecialNumberHandlingOnWrite(state.Current.NumberHandling) &&
                !state.CurrentContainsMetadata) // Do not use the fast path if state needs to write metadata.
            {
                ((JsonTypeInfo<T>)jsonTypeInfo).SerializeHandler!(writer, value);
                return true;
            }

            return Converter.OnTryWrite(writer, value, options, ref state);
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
            => Converter.ConfigureJsonTypeInfo(jsonTypeInfo, options);
    }
}
