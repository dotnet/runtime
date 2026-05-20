// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for union types. Fully stateless — reads/writes using the classifier,
    /// deconstructor, and constructor delegates configured on <see cref="JsonTypeInfo{T}"/>.
    /// All configuration is performed by the resolver, not the converter.
    /// </summary>
    internal sealed class JsonUnionConverter<TUnion> : JsonResumableConverter<TUnion>
    {
        public JsonUnionConverter()
        {
            // The union converter dispatches across all JSON value shapes (the actual case
            // type determines which shape is valid). The non-Value converter machinery
            // verifies post-Read positioning per token kind; flagging SupportsMultipleTokenTypes
            // lets value-token cases (string/number/bool) satisfy the verification.
            SupportsMultipleTokenTypes = true;
        }

        // Override the framework's default null short-circuit so that JSON null
        // is delivered to Read where it is dispatched to the case constructor
        // accepting null (or rejected when no case opts in).
        public override bool HandleNull => true;

        private protected override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Union;

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out TUnion? value)
        {
            JsonTypeInfo<TUnion> typeInfo = (JsonTypeInfo<TUnion>)state.Current.JsonTypeInfo;

            Func<Type, object?, TUnion>? constructor = typeInfo.UnionConstructor;
            if (constructor is null)
            {
                ThrowHelper.ThrowJsonException_UnionCannotCreateValue(typeToConvert);
            }

            if (reader.TokenType is JsonTokenType.Null)
            {
                Type? nullableCaseType = typeInfo.UnionNullableCaseType;
                if (nullableCaseType is null)
                {
                    ThrowHelper.ThrowJsonException_UnionDoesNotAcceptNull(typeToConvert);
                }

                value = constructor(nullableCaseType, null);
                return true;
            }

            JsonTypeInfo caseTypeInfo;
            Type caseType;

            if (state.IsContinuation)
            {
                caseTypeInfo = state.Current.JsonPropertyInfo!.JsonTypeInfo;
                caseType = caseTypeInfo.Type;
            }
            else
            {
                if (!TryResolveCaseType(ref reader, typeToConvert, typeInfo, out caseType))
                {
                    value = default;
                    return false;
                }

                caseTypeInfo = options.GetTypeInfoInternal(caseType);
                state.Current.JsonPropertyInfo = caseTypeInfo.PropertyInfoForTypeInfo;
            }

            JsonConverter caseConverter = caseTypeInfo.Converter;
            if (!caseConverter.TryReadAsObject(ref reader, caseType, options, ref state, out object? caseValue))
            {
                value = default;
                return false;
            }

            value = constructor(caseType, caseValue);
            return true;
        }

        private static bool TryResolveCaseType(ref Utf8JsonReader reader, Type typeToConvert, JsonTypeInfo<TUnion> typeInfo, out Type caseType)
        {
            caseType = null!;
            JsonTypeClassifier? classifier = typeInfo.TypeClassifier;
            if (classifier is not null)
            {
                // Ensure the entire value has been pre-buffered.
                Utf8JsonReader readerCopy = reader;
                if (!readerCopy.TrySkipPartial())
                {
                    return false;
                }

                // Pass a copy of the reader to the classifier.
                readerCopy = reader;
                caseType = classifier(ref readerCopy)!;

                if (caseType is null)
                {
                    ThrowHelper.ThrowJsonException_UnionTypeClassifierReturnedNull(typeToConvert, reader.TokenType);
                }
            }
            else
            {
                // Default path: JSON value shape matching. Diagnostics recorded at configure time
                // surface here so that ambiguous / custom-converter cases fail with a precise
                // message ONLY when a deserialization is actually attempted (config never throws).
                JsonTokenType tokenType = reader.TokenType;
                JsonValueType valueType = GetJsonValueType(tokenType);

                if ((typeInfo.UnionAmbiguousValueTypes & valueType) != 0)
                {
                    ThrowHelper.ThrowJsonException_UnionAmbiguousJsonValueType(typeToConvert, valueType);
                }

                if (typeInfo.UnionHasCustomConverterCase)
                {
                    ThrowHelper.ThrowJsonException_UnionCaseWithCustomConverterRequiresClassifier(typeToConvert);
                }

                Type? resolvedCaseType = null;
                typeInfo.UnionValueTypeMap?.TryGetValue(valueType, out resolvedCaseType);
                caseType = resolvedCaseType!;
            }

            if (caseType is null)
            {
                ThrowHelper.ThrowJsonException_UnionJsonTokenTypeNotSupported(typeToConvert, reader.TokenType);
            }

            return true;
        }

        private static JsonValueType GetJsonValueType(JsonTokenType tokenType) =>
            tokenType switch
            {
                JsonTokenType.StartObject => JsonValueType.Object,
                JsonTokenType.StartArray => JsonValueType.Array,
                JsonTokenType.String => JsonValueType.String,
                JsonTokenType.Number => JsonValueType.Number,
                JsonTokenType.True or JsonTokenType.False => JsonValueType.Boolean,
                JsonTokenType.Null => JsonValueType.Null,
                _ => JsonValueType.None,
            };

        internal override bool OnTryWrite(Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return true;
            }

            // Union converters delegate inline to the case converter without emitting
            // structural JSON tokens (StartObject/StartArray), so the writer's depth
            // does not increase across union-to-union recursion. Guard against cyclic
            // union references using the WriteStack frame depth instead.
            if (state.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
            }

            JsonTypeInfo<TUnion> typeInfo = (JsonTypeInfo<TUnion>)state.Current.JsonTypeInfo;

            Func<TUnion, (Type?, object?)>? deconstructor = typeInfo.UnionDeconstructor;
            if (deconstructor is null)
            {
                ThrowHelper.ThrowJsonException_UnionCannotReadValue(typeof(TUnion));
            }

            (Type? caseType, object? caseValue) = deconstructor(value);

            if (caseType is null)
            {
                // A null case type signals the canonical "null union" state. Both members
                // of the deconstructor tuple convey that signal: caseType doubles as the
                // discriminator, and a non-null caseValue here is ignored.
                writer.WriteNullValue();
                return true;
            }

            JsonTypeInfo caseTypeInfo = options.GetTypeInfoInternal(caseType);
            state.Current.JsonPropertyInfo = caseTypeInfo.PropertyInfoForTypeInfo;
            return caseTypeInfo.Converter.TryWriteAsObject(writer, caseValue, options, ref state);
        }
    }
}
