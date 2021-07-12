// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# struct optional values: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-fsharpvalueoption-1.html
    internal sealed class FSharpValueOptionConverter<TValueOption, TElement> : JsonResumableConverter<TValueOption>
        where TValueOption : struct, IEquatable<TValueOption>
    {
        // While technically not implementing IEnumerable, F# optionals are effectively generic collections of at most one element.
        internal override ConverterStrategy ConverterStrategy => ConverterStrategy.Enumerable;
        internal override Type? ElementType => typeof(TElement);

        public override bool HandleNull => true;

        private readonly FSharpCoreReflectionProxy.StructGetter<TValueOption, TElement> _optionValueGetter;
        private readonly Func<TElement?, TValueOption> _optionConstructor;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpValueOptionConverter(JsonConverter<TElement> elementConverter)
        {
            _optionValueGetter = FSharpCoreReflectionProxy.Instance.CreateFSharpValueOptionValueGetter<TValueOption, TElement>();
            _optionConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpValueOptionSomeConstructor<TValueOption, TElement>();
            // If the element converter is value, this converter will also be writing values
            // Set a flag to signal this fact to the converter infrastructure.
            CanWriteJsonValues = elementConverter.ConverterStrategy == ConverterStrategy.Value;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TValueOption value)
        {
            state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            var elementConverter = (JsonConverter<TElement>)state.Current.JsonPropertyInfo.ConverterBase;

            // `null` values deserialize as `ValueNone`
            if (!state.IsContinuation && reader.TokenType == JsonTokenType.Null)
            {
                value = default;
                return true;
            }

            if (elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element))
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

            state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            var elementConverter = (JsonConverter<TElement>)state.Current.DeclaredJsonPropertyInfo.ConverterBase;

            TElement element = _optionValueGetter(ref value);
            return elementConverter.TryWrite(writer, element, options, ref state);
        }
    }
}
