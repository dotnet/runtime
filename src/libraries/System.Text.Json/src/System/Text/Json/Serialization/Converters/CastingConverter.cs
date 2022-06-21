// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter wrapper which casts SourceType into TargetType
    /// </summary>
    internal sealed class CastingConverter<TTarget, TSource> : JsonConverter<TTarget>
    {
        private JsonConverter<TSource> _sourceConverter;

        internal override Type? KeyType => _sourceConverter.KeyType;
        internal override Type? ElementType => _sourceConverter.ElementType;

        public override bool HandleNull => _sourceConverter.HandleNull;
        internal override ConverterStrategy ConverterStrategy => _sourceConverter.ConverterStrategy;

        internal CastingConverter(JsonConverter<TSource> sourceConverter) : base(initialize: false)
        {
            _sourceConverter = sourceConverter;
            Initialize();

            IsInternalConverter = sourceConverter.IsInternalConverter;
            IsInternalConverterForNumberType = sourceConverter.IsInternalConverterForNumberType;
            RequiresReadAhead = sourceConverter.RequiresReadAhead;
            CanUseDirectReadOrWrite = sourceConverter.CanUseDirectReadOrWrite;
        }

        public override TTarget? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Cast<TSource, TTarget>(_sourceConverter.Read(ref reader, typeToConvert, options));

        public override void Write(Utf8JsonWriter writer, TTarget value, JsonSerializerOptions options)
            => _sourceConverter.Write(writer, Cast<TTarget, TSource>(value), options);

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TTarget? value)
        {
            bool result = _sourceConverter.OnTryRead(ref reader, typeToConvert, options, ref state, out TSource? sourceValue);
            value = Cast<TSource, TTarget>(sourceValue);
            return result;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, TTarget value, JsonSerializerOptions options, ref WriteStack state)
            => _sourceConverter.OnTryWrite(writer, Cast<TTarget, TSource>(value), options, ref state);

        public override TTarget ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Cast<TSource, TTarget>(_sourceConverter.ReadAsPropertyName(ref reader, typeToConvert, options));

        internal override TTarget ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Cast<TSource, TTarget>(_sourceConverter.ReadAsPropertyNameCore(ref reader, typeToConvert, options));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, TTarget value, JsonSerializerOptions options)
            => _sourceConverter.WriteAsPropertyName(writer, Cast<TTarget, TSource>(value), options);

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, TTarget value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
            => _sourceConverter.WriteAsPropertyNameCore(writer, Cast<TTarget, TSource>(value), options, isWritingExtensionDataProperty);

        internal override TTarget ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => Cast<TSource, TTarget>(_sourceConverter.ReadNumberWithCustomHandling(ref reader, handling, options));

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, TTarget value, JsonNumberHandling handling)
            => _sourceConverter.WriteNumberWithCustomHandling(writer, Cast<TTarget, TSource>(value), handling);

        private static TCastTarget Cast<TCastSource, TCastTarget>(TCastSource? source) => (TCastTarget)(object?)source!;
    }
}
