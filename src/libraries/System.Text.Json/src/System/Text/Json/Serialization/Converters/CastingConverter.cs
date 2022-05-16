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
    internal sealed class CastingConverter<TargetType, SourceType> : JsonConverter<TargetType>
    {
        private JsonConverter<SourceType> _sourceConverter;

        internal override Type? KeyType => _sourceConverter.KeyType;
        internal override Type? ElementType => _sourceConverter.ElementType;

        public override bool HandleNull => _sourceConverter.HandleNull;
        internal override ConverterStrategy ConverterStrategy => _sourceConverter.ConverterStrategy;

        internal CastingConverter(JsonConverter<SourceType> sourceConverter) : base(initialize: false)
        {
            _sourceConverter = sourceConverter;
            Initialize();

            IsInternalConverter = sourceConverter.IsInternalConverter;
            IsInternalConverterForNumberType = sourceConverter.IsInternalConverterForNumberType;
            RequiresReadAhead = sourceConverter.RequiresReadAhead;
            CanUseDirectReadOrWrite = sourceConverter.CanUseDirectReadOrWrite;
        }

        public override TargetType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ConvertToTarget(_sourceConverter.Read(ref reader, typeToConvert, options));

        public override void Write(Utf8JsonWriter writer, TargetType value, JsonSerializerOptions options)
            => _sourceConverter.Write(writer, ConvertToSource(value), options);

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TargetType? value)
        {
            bool ret = _sourceConverter.OnTryRead(ref reader, typeToConvert, options, ref state, out SourceType? sourceValue);
            value = ConvertToTarget(sourceValue);
            return ret;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, TargetType value, JsonSerializerOptions options, ref WriteStack state)
            => _sourceConverter.OnTryWrite(writer, ConvertToSource(value), options, ref state);

        public override TargetType ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ConvertToTarget(_sourceConverter.ReadAsPropertyName(ref reader, typeToConvert, options));

        internal override TargetType ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ConvertToTarget(_sourceConverter.ReadAsPropertyNameCore(ref reader, typeToConvert, options));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, TargetType value, JsonSerializerOptions options)
            => _sourceConverter.WriteAsPropertyName(writer, ConvertToSource(value), options);

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, TargetType value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
            => _sourceConverter.WriteAsPropertyNameCore(writer, ConvertToSource(value), options, isWritingExtensionDataProperty);

        internal override TargetType ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => ConvertToTarget(_sourceConverter.ReadNumberWithCustomHandling(ref reader, handling, options));

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, TargetType value, JsonNumberHandling handling)
            => _sourceConverter.WriteNumberWithCustomHandling(writer, ConvertToSource(value), handling);

        private static SourceType ConvertToSource(TargetType? targetValue)
            => (SourceType)(object?)targetValue!;

        private static TargetType ConvertToTarget(SourceType? sourceValue)
            => (TargetType)(object?)sourceValue!;
    }
}
