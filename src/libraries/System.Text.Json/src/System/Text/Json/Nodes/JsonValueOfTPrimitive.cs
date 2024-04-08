// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// A JsonValue encapsulating a primitive value using a built-in converter for the type.
    /// </summary>
    internal sealed class JsonValuePrimitive<TValue> : JsonValue<TValue>
    {
        // Default value used when calling into the converter.
        private static readonly JsonSerializerOptions s_defaultOptions = new();

        private readonly JsonConverter<TValue> _converter;

        public JsonValuePrimitive(TValue value, JsonConverter<TValue> converter, JsonNodeOptions? options = null) : base(value, options)
        {
            Debug.Assert(converter is { IsInternalConverter: true, ConverterStrategy: ConverterStrategy.Value });
            _converter = converter;
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

        internal override JsonNode DeepCloneCore()
        {
            // Primitive JsonValue's are generally speaking immutable so we don't need to do much here.
            // For the case of JsonElement clone the instance since it could be backed by pooled buffers.
            return Value is JsonElement element
                ? new JsonValuePrimitive<JsonElement>(element.Clone(), JsonMetadataServices.JsonElementConverter, Options)
                : new JsonValuePrimitive<TValue>(Value, _converter, Options);
        }
    }
}
