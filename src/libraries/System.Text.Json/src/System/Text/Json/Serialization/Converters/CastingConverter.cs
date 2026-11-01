// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Reflection;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter wrapper which casts SourceType into TargetType
    /// </summary>
    internal sealed class CastingConverter<T> : JsonConverter<T>
    {
        private readonly JsonConverter _sourceConverter;

        // Whether property name serialization can be delegated to the source converter.
        //
        // Delegation is unsafe only for converters declared for object: their inherited property name
        // implementations fall back to ObjectConverter, which is the one converter that re-dispatches
        // on the runtime type. That lookup resolves back to this casting converter, which delegates
        // again, recursing until the stack overflows (see dotnet/runtime#118381). For every other
        // declared type the inherited implementation either has no fallback at all or a fallback that
        // serializes the type directly, so delegating cannot resolve back to this instance.
        private readonly bool _delegatePropertyNameSerialization;

        internal override Type? KeyType => _sourceConverter.KeyType;
        internal override Type? ElementType => _sourceConverter.ElementType;
        internal override JsonConverter? NullableElementConverter => _sourceConverter.NullableElementConverter;

        public override bool HandleNull { get; }
        internal override bool SupportsCreateObjectDelegate => _sourceConverter.SupportsCreateObjectDelegate;

        internal CastingConverter(JsonConverter sourceConverter)
        {
            Debug.Assert(typeof(T).IsInSubtypeRelationshipWith(sourceConverter.Type!));
            Debug.Assert(sourceConverter.SourceConverterForCastingConverter is null, "casting converters should not be layered.");

            _sourceConverter = sourceConverter;
            IsInternalConverter = sourceConverter.IsInternalConverter;
            IsInternalConverterForNumberType = sourceConverter.IsInternalConverterForNumberType;
            ConverterStrategy = sourceConverter.ConverterStrategy;
            CanBePolymorphic = sourceConverter.CanBePolymorphic;

            // Ensure HandleNull values reflect the exact configuration of the source converter
            HandleNullOnRead = sourceConverter.HandleNullOnRead;
            HandleNullOnWrite = sourceConverter.HandleNullOnWrite;
            HandleNull = sourceConverter.HandleNullOnWrite;

            _delegatePropertyNameSerialization = sourceConverter.Type != typeof(object);
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
        {
            if (_delegatePropertyNameSerialization)
            {
                return JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsPropertyNameAsObject(ref reader, typeToConvert, options))!;
            }
            else
            {
                // Delegating to a converter declared for object would recurse back into this instance.
                // Use our base class logic instead, which resolves the fallback for type T.
                return base.ReadAsPropertyName(ref reader, typeToConvert, options);
            }
        }

        internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (_delegatePropertyNameSerialization)
            {
                return JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsPropertyNameCoreAsObject(ref reader, typeToConvert, options))!;
            }
            else
            {
                // Delegating to a converter declared for object would recurse back into this instance.
                // Use our base class logic instead, which resolves the fallback for type T.
                return base.ReadAsPropertyNameCore(ref reader, typeToConvert, options);
            }
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
        {
            if (_delegatePropertyNameSerialization)
            {
                _sourceConverter.WriteAsPropertyNameAsObject(writer, value, options);
            }
            else
            {
                // Delegating to a converter declared for object would recurse back into this instance.
                // Use our base class logic instead, which resolves the fallback for type T.
                base.WriteAsPropertyName(writer, value, options);
            }
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            if (_delegatePropertyNameSerialization)
            {
                _sourceConverter.WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);
            }
            else
            {
                // Delegating to a converter declared for object would recurse back into this instance.
                // Use our base class logic instead, which resolves the fallback for type T.
                base.WriteAsPropertyNameCore(writer, value!, options, isWritingExtensionDataProperty);
            }
        }

        internal override T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadNumberWithCustomHandlingAsObject(ref reader, handling, options))!;

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T? value, JsonNumberHandling handling)
            => _sourceConverter.WriteNumberWithCustomHandlingAsObject(writer, value, handling);

        internal override JsonSchema? GetSchema(JsonNumberHandling numberHandling)
            => _sourceConverter.GetSchema(numberHandling);

        internal override JsonValueType GetSupportedJsonValueTypes(JsonNumberHandling numberHandling)
            => _sourceConverter.GetSupportedJsonValueTypes(numberHandling);
    }
}
