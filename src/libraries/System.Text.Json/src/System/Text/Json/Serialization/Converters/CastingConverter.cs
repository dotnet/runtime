// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter wrapper which casts SourceType into TargetType
    /// </summary>
    internal sealed class CastingConverter<T> : JsonConverter<T>
    {
        private readonly JsonConverter _sourceConverter;
        internal override Type? KeyType => _sourceConverter.KeyType;
        internal override Type? ElementType => _sourceConverter.ElementType;

        public override bool HandleNull { get; }
        internal override bool SupportsCreateObjectDelegate => _sourceConverter.SupportsCreateObjectDelegate;

        internal CastingConverter(JsonConverter sourceConverter, bool handleNull, bool handleNullOnRead, bool handleNullOnWrite)
        {
            Debug.Assert(typeof(T).IsInSubtypeRelationshipWith(sourceConverter.TypeToConvert));
            Debug.Assert(sourceConverter.SourceConverterForCastingConverter is null, "casting converters should not be layered.");

            _sourceConverter = sourceConverter;
            IsInternalConverter = sourceConverter.IsInternalConverter;
            IsInternalConverterForNumberType = sourceConverter.IsInternalConverterForNumberType;
            ConverterStrategy = sourceConverter.ConverterStrategy;
            CanBePolymorphic = sourceConverter.CanBePolymorphic;

            // Ensure HandleNull values reflect the exact configuration of the source converter
            HandleNullOnRead = handleNullOnRead;
            HandleNullOnWrite = handleNullOnWrite;
            HandleNull = handleNull;
        }

        internal override JsonConverter? SourceConverterForCastingConverter => _sourceConverter;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsObject(ref reader, typeToConvert, options));

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => _sourceConverter.WriteAsObject(writer, value, options);

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T? value)
        {
            bool result = _sourceConverter.OnTryReadAsObject(ref reader, typeToConvert, options, ref state, out object? sourceValue);
            value = JsonSerializer.UnboxOnRead<T>(sourceValue);
            return result;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
            => _sourceConverter.OnTryWriteAsObject(writer, value, options, ref state);

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsPropertyNameAsObject(ref reader, typeToConvert, options))!;

        internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsPropertyNameCoreAsObject(ref reader, typeToConvert, options))!;

        public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
            => _sourceConverter.WriteAsPropertyNameAsObject(writer, value, options);

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
            => _sourceConverter.WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);

        internal override T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadNumberWithCustomHandlingAsObject(ref reader, handling, options))!;

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T? value, JsonNumberHandling handling)
            => _sourceConverter.WriteNumberWithCustomHandlingAsObject(writer, value, handling);
    }
}
