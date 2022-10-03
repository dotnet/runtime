// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# struct optional values: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-fsharpvalueoption-1.html
    // Serializes `ValueSome(value)` using the format of `value` and `ValueNone` values as `null`.
    internal sealed class FSharpValueOptionConverter<TValueOption, TElement> : JsonConverter<TValueOption>
        where TValueOption : struct, IEquatable<TValueOption>
    {
        // Reflect the converter strategy of the element type, since we use the identical contract for ValueSome(_) values.
        internal override ConverterStrategy ConverterStrategy => _converterStrategy;
        internal override Type? ElementType => typeof(TElement);
        // 'ValueNone' is encoded using 'default' at runtime and serialized as 'null' in JSON.
        public override bool HandleNull => true;

        private readonly JsonConverter<TElement> _elementConverter;
        private readonly FSharpCoreReflectionProxy.StructGetter<TValueOption, TElement> _optionValueGetter;
        private readonly Func<TElement?, TValueOption> _optionConstructor;
        private readonly ConverterStrategy _converterStrategy;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpValueOptionConverter(JsonConverter<TElement> elementConverter)
        {
            _elementConverter = elementConverter;
            _optionValueGetter = FSharpCoreReflectionProxy.Instance.CreateFSharpValueOptionValueGetter<TValueOption, TElement>();
            _optionConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpValueOptionSomeConstructor<TValueOption, TElement>();

            // Workaround for the base constructor depending on the (still unset) ConverterStrategy
            // to derive the CanUseDirectReadOrWrite and RequiresReadAhead values.
            _converterStrategy = elementConverter.ConverterStrategy;
            CanUseDirectReadOrWrite = elementConverter.CanUseDirectReadOrWrite;
            RequiresReadAhead = elementConverter.RequiresReadAhead;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TValueOption value)
        {
            // `null` values deserialize as `ValueNone`
            if (!state.IsContinuation && reader.TokenType == JsonTokenType.Null)
            {
                value = default;
                return true;
            }

            state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            if (_elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element))
            {
                value = _optionConstructor(element);
                return true;
            }

            value = default;
            return false;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, TValueOption value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (value.Equals(default))
            {
                // Write `ValueNone` values as null
                writer.WriteNullValue();
                return true;
            }

            TElement element = _optionValueGetter(ref value);

            state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            return _elementConverter.TryWrite(writer, element, options, ref state);
        }

        // Since this is a hybrid converter (ConverterStrategy depends on the element converter),
        // we need to override the value converter Write and Read methods too.

        public override void Write(Utf8JsonWriter writer, TValueOption value, JsonSerializerOptions options)
        {
            if (value.Equals(default))
            {
                // Write `ValueNone` values as null
                writer.WriteNullValue();
            }
            else
            {
                TElement element = _optionValueGetter(ref value);
                _elementConverter.Write(writer, element, options);
            }
        }

        public override TValueOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            TElement? element = _elementConverter.Read(ref reader, typeToConvert, options);
            return _optionConstructor(element);
        }
    }
}
