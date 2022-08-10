// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter wrapper which casts SourceType into TargetType
    /// </summary>
    internal sealed class CastingConverter<T, TSource> : JsonConverter<T>
    {
        private readonly JsonConverter<TSource> _sourceConverter;

        internal override Type? KeyType => _sourceConverter.KeyType;
        internal override Type? ElementType => _sourceConverter.ElementType;

        public override bool HandleNull => _sourceConverter.HandleNull;
        internal override ConverterStrategy ConverterStrategy => _sourceConverter.ConverterStrategy;
        internal override bool SupportsCreateObjectDelegate => _sourceConverter.SupportsCreateObjectDelegate;

        internal CastingConverter(JsonConverter<TSource> sourceConverter) : base(initialize: false)
        {
            Debug.Assert(typeof(T).IsInSubtypeRelationshipWith(typeof(TSource)));
            Debug.Assert(sourceConverter.SourceConverterForCastingConverter is null, "casting converters should not be layered.");

            _sourceConverter = sourceConverter;
            Initialize();

            IsInternalConverter = sourceConverter.IsInternalConverter;
            IsInternalConverterForNumberType = sourceConverter.IsInternalConverterForNumberType;
            RequiresReadAhead = sourceConverter.RequiresReadAhead;
            CanUseDirectReadOrWrite = sourceConverter.CanUseDirectReadOrWrite;
            CanBePolymorphic = sourceConverter.CanBePolymorphic;
        }

        internal override JsonConverter? SourceConverterForCastingConverter => _sourceConverter;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => CastOnRead(_sourceConverter.Read(ref reader, typeToConvert, options));

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => _sourceConverter.Write(writer, CastOnWrite(value), options);

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T? value)
        {
            bool result = _sourceConverter.OnTryRead(ref reader, typeToConvert, options, ref state, out TSource? sourceValue);
            value = CastOnRead(sourceValue);
            return result;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
            => _sourceConverter.OnTryWrite(writer, CastOnWrite(value), options, ref state);

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => CastOnRead(_sourceConverter.ReadAsPropertyName(ref reader, typeToConvert, options));

        internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => CastOnRead(_sourceConverter.ReadAsPropertyNameCore(ref reader, typeToConvert, options));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => _sourceConverter.WriteAsPropertyName(writer, CastOnWrite(value), options);

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
            => _sourceConverter.WriteAsPropertyNameCore(writer, CastOnWrite(value), options, isWritingExtensionDataProperty);

        internal override T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => CastOnRead(_sourceConverter.ReadNumberWithCustomHandling(ref reader, handling, options));

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
            => _sourceConverter.WriteNumberWithCustomHandling(writer, CastOnWrite(value), handling);

        private static T CastOnRead(TSource? source)
        {
            if (default(T) is null && default(TSource) is null && source is null)
            {
                return default!;
            }

            if (source is T t)
            {
                return t;
            }

            HandleFailure(source);
            return default!;

            static void HandleFailure(TSource? source)
            {
                if (source is null)
                {
                    ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(typeof(T));
                }
                else
                {
                    ThrowHelper.ThrowInvalidCastException_DeserializeUnableToAssignValue(typeof(TSource), typeof(T));
                }
            }
        }

        private static TSource CastOnWrite(T source)
        {
            if (default(TSource) is not null && default(T) is null && source is null)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(TSource));
            }

            return (TSource)(object?)source!;
        }
    }
}
