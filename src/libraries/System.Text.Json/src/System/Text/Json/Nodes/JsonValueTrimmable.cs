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
    }
}
