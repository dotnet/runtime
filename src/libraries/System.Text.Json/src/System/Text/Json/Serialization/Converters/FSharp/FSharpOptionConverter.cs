// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# optional values: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-option-1.html
    // Serializes `Some(value)` using the format of `value` and `None` values as `null`.
    internal sealed class FSharpOptionConverter<TOption, TElement> : JsonConverter<TOption>
        where TOption : class
    {
        // Reflect the converter strategy of the element type, since we use the identical contract for Some(_) values.
        internal override ConverterStrategy ConverterStrategy => _converterStrategy;
        internal override Type? ElementType => typeof(TElement);
        // 'None' is encoded using 'null' at runtime and serialized as 'null' in JSON.
        public override bool HandleNull => true;

        private readonly JsonConverter<TElement> _elementConverter;
        private readonly Func<TOption, TElement> _optionValueGetter;
        private readonly Func<TElement?, TOption> _optionConstructor;
        private readonly ConverterStrategy _converterStrategy;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpOptionConverter(JsonConverter<TElement> elementConverter)
        {
            _elementConverter = elementConverter;
            _optionValueGetter = FSharpCoreReflectionProxy.Instance.CreateFSharpOptionValueGetter<TOption, TElement>();
            _optionConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpOptionSomeConstructor<TOption, TElement>();

            // temporary workaround for JsonConverter base constructor needing to access
            // ConverterStrategy when calculating `CanUseDirectReadOrWrite`.
            // TODO move `CanUseDirectReadOrWrite` from JsonConverter to JsonTypeInfo.
            _converterStrategy = _elementConverter.ConverterStrategy;
            CanUseDirectReadOrWrite = _converterStrategy == ConverterStrategy.Value;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TOption? value)
        {
            // `null` values deserialize as `None`
            if (!state.IsContinuation && reader.TokenType == JsonTokenType.Null)
            {
                value = null;
                return true;
            }

            state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            if (_elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element))
            {
                value = _optionConstructor(element);
                return true;
            }

            value = null;
            return false;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, TOption value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (value is null)
            {
                // Write `None` values as null
                writer.WriteNullValue();
                return true;
            }

            TElement element = _optionValueGetter(value);
            state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            return _elementConverter.TryWrite(writer, element, options, ref state);
        }

        // Since this is a hybrid converter (ConverterStrategy depends on the element converter),
        // we need to override the value converter Write and Read methods too.

        public override void Write(Utf8JsonWriter writer, TOption value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                TElement element = _optionValueGetter(value);
                _elementConverter.Write(writer, element, options);
            }
        }

        public override TOption? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            TElement? element = _elementConverter.Read(ref reader, typeToConvert, options);
            return _optionConstructor(element);
        }
    }
}
