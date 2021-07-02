// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# optional values: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-option-1.html
    // Serializes `Some(value)` using the format of `value` and `None` values as `null`.
    internal sealed class FSharpOptionConverter<TOption, TElement> : JsonResumableConverter<TOption>
        where TOption : class
    {
        // While technically not implementing IEnumerable, F# optionals are effectively generic collections of at most one element.
        internal override ConverterStrategy ConverterStrategy => ConverterStrategy.Enumerable;
        internal override Type? ElementType => typeof(TElement);

        private readonly Func<TOption, TElement> _optionValueGetter;
        private readonly Func<TElement, TOption> _optionConstructor;

        public FSharpOptionConverter(JsonConverter<TElement> elementConverter)
        {
            _optionValueGetter = FSharpCoreReflectionProxy.Instance.CreateFSharpOptionValueGetter<TOption, TElement>();
            _optionConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpOptionConstructor<TOption, TElement>();
            // If the element converter is value, this converter will also be writing values
            // Set a flag to signal this fact to the covnerter infrastracture.
            CanWriteJsonValues = elementConverter.ConverterStrategy == ConverterStrategy.Value;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TOption? value)
        {
            state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            var elementConverter = (JsonConverter<TElement>)state.Current.JsonPropertyInfo.ConverterBase;

            if (elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element))
            {
                // null element values are deserialized as 'None'.
                value = element is null ? null : _optionConstructor(element);
                return true;
            }

            value = null;
            return false;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, TOption value, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(value is not null); // 'None' values are encoded as null: handled by the base converter.
            state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            var elementConverter = (JsonConverter<TElement>)state.Current.DeclaredJsonPropertyInfo.ConverterBase;

            TElement element = _optionValueGetter(value);
            return elementConverter.TryWrite(writer, element, options, ref state);
        }
    }
}
