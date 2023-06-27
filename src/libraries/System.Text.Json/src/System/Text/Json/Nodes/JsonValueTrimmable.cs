// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// Trim-safe since it either calls the converter directly or calls the JsonSerializer.Serialize(JsonTypeInfo{TValue}).
    /// </summary>
    internal sealed partial class JsonValueTrimmable<TValue> : JsonValue<TValue>
    {
        private readonly JsonTypeInfo<TValue>? _jsonTypeInfo;
        private readonly JsonConverter<TValue>? _converter;

        public JsonValueTrimmable(TValue value, JsonTypeInfo<TValue> jsonTypeInfo, JsonNodeOptions? options = null) : base(value, options)
        {
            _jsonTypeInfo = jsonTypeInfo;
        }

        public JsonValueTrimmable(TValue value, JsonConverter<TValue> converter, JsonNodeOptions? options = null) : base(value, options)
        {
            _converter = converter;
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            if (_converter != null)
            {
                options ??= s_defaultOptions;

                if (_converter.IsInternalConverterForNumberType)
                {
                    _converter.WriteNumberWithCustomHandling(writer, _value, options.NumberHandling);
                }
                else
                {
                    _converter.Write(writer, _value, options);
                }
            }
            else
            {
                Debug.Assert(_jsonTypeInfo != null);
                JsonSerializer.Serialize(writer, _value, _jsonTypeInfo);
            }
        }

        internal override JsonNode InternalDeepClone()
        {
            if (_converter is not null)
            {
                return _value is JsonElement element
                    ? new JsonValueTrimmable<JsonElement>(element.Clone(), JsonMetadataServices.JsonElementConverter, Options)
                    : new JsonValueTrimmable<TValue>(_value, _converter, Options);
            }
            else
            {
                Debug.Assert(_jsonTypeInfo != null);
                return JsonSerializer.SerializeToNode(_value, _jsonTypeInfo)!;
            }
        }

        internal override bool DeepEquals(JsonNode? node)
        {
            if (node is null)
            {
                return false;
            }

            if (_jsonTypeInfo is not null)
            {
                JsonNode? jsonNode = JsonSerializer.SerializeToNode(_value, _jsonTypeInfo);
                return DeepEquals(jsonNode, node);
            }
            else
            {
                if (node is JsonArray || node is JsonObject)
                {
                    return false;
                }

                if (node is JsonValueTrimmable<JsonElement> jsonElementNodeOther && _value is JsonElement jsonElementCurrent)
                {
                    if (jsonElementNodeOther._value.ValueKind != jsonElementCurrent.ValueKind)
                    {
                        return false;
                    }

                    switch (jsonElementCurrent.ValueKind)
                    {
                        case JsonValueKind.String:
                            return jsonElementCurrent.ValueEquals(jsonElementNodeOther._value.GetString());
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            return jsonElementCurrent.ValueKind == jsonElementNodeOther._value.ValueKind;
                        case JsonValueKind.Number:
                            return jsonElementCurrent.GetRawValue().Span.SequenceEqual(jsonElementNodeOther._value.GetRawValue().Span);
                        default:
                            Debug.Fail("Impossible case");
                            return false;
                    }
                }

                using var currentOutput = new PooledByteBufferWriter(JsonSerializerOptions.BufferSizeDefault);
                using (var writer = new Utf8JsonWriter(currentOutput, default))
                {
                    WriteTo(writer);
                }

                using var anotherOutput = new PooledByteBufferWriter(JsonSerializerOptions.BufferSizeDefault);
                using (var writer = new Utf8JsonWriter(anotherOutput, default))
                {
                    node.WriteTo(writer);
                }

                return currentOutput.WrittenMemory.Span.SequenceEqual(anotherOutput.WrittenMemory.Span);
            }
        }

        internal override JsonValueKind GetInternalValueKind()
        {
            if (_jsonTypeInfo is not null)
            {
                return JsonSerializer.SerializeToElement(_value, _jsonTypeInfo).ValueKind;
            }
            else
            {
                if (_value is JsonElement element)
                {
                    return element.ValueKind;
                }

                using var output = new PooledByteBufferWriter(JsonSerializerOptions.BufferSizeDefault);
                using (var writer = new Utf8JsonWriter(output, default))
                {
                    WriteTo(writer);
                }

                return JsonElement.ParseValue(output.WrittenMemory.Span, default).ValueKind;
            }
        }
    }
}
