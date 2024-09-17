// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// A JsonValue encapsulating a primitive value using a built-in converter for the type.
    /// </summary>
    internal sealed class JsonValuePrimitive<TValue> : JsonValue<TValue>
    {
        private readonly JsonConverter<TValue> _converter;
        private readonly JsonValueKind _valueKind;

        public JsonValuePrimitive(TValue value, JsonConverter<TValue> converter, JsonNodeOptions? options) : base(value, options)
        {
            Debug.Assert(TypeIsSupportedPrimitive, $"The type {typeof(TValue)} is not a supported primitive.");
            Debug.Assert(converter is { IsInternalConverter: true, ConverterStrategy: ConverterStrategy.Value });

            _converter = converter;
            _valueKind = DetermineValueKind(value);
        }

        private protected override JsonValueKind GetValueKindCore() => _valueKind;
        internal override JsonNode DeepCloneCore() => new JsonValuePrimitive<TValue>(Value, _converter, Options);

        internal override bool DeepEqualsCore(JsonNode otherNode)
        {
            if (otherNode is JsonValue otherValue && otherValue.TryGetValue(out TValue? v))
            {
                // Because TValue is equatable and otherNode returns a matching
                // type we can short circuit the comparison in this case.
                return EqualityComparer<TValue>.Default.Equals(Value, v);
            }

            return base.DeepEqualsCore(otherNode);
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            JsonConverter<TValue> converter = _converter;
            options ??= s_defaultOptions;

            if (converter.IsInternalConverterForNumberType)
            {
                converter.WriteNumberWithCustomHandling(writer, Value, options.NumberHandling);
            }
            else
            {
                converter.Write(writer, Value, options);
            }
        }
    }
}
