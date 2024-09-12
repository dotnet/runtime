// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// A JsonValue that encapsulates arbitrary .NET type configurations.
    /// Paradoxically, instances of this type can be of any JsonValueKind
    /// (including objects and arrays) and introspecting these values is
    /// generally slower compared to the other JsonValue implementations.
    /// </summary>
    internal sealed class JsonValueCustomized<TValue> : JsonValue<TValue>
    {
        private readonly JsonTypeInfo<TValue> _jsonTypeInfo;
        private JsonValueKind? _valueKind;

        public JsonValueCustomized(TValue value, JsonTypeInfo<TValue> jsonTypeInfo, JsonNodeOptions? options = null): base(value, options)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);
            _jsonTypeInfo = jsonTypeInfo;
        }

        private protected override JsonValueKind GetValueKindCore() => _valueKind ??= ComputeValueKind();
        internal override JsonNode DeepCloneCore() => JsonSerializer.SerializeToNode(Value, _jsonTypeInfo)!;

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            JsonTypeInfo<TValue> jsonTypeInfo = _jsonTypeInfo;

            if (options != null && options != jsonTypeInfo.Options)
            {
                options.MakeReadOnly();
                jsonTypeInfo = (JsonTypeInfo<TValue>)options.GetTypeInfoInternal(typeof(TValue));
            }

            jsonTypeInfo.Serialize(writer, Value);
        }

        /// <summary>
        /// Computes the JsonValueKind of the value by serializing it and reading the resultant JSON.
        /// </summary>
        private JsonValueKind ComputeValueKind()
        {
            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(options: default, JsonSerializerOptions.BufferSizeDefault, out PooledByteBufferWriter output);
            try
            {
                WriteTo(writer);
                writer.Flush();
                Utf8JsonReader reader = new(output.WrittenMemory.Span);
                bool success = reader.Read();
                Debug.Assert(success);
                return JsonReaderHelper.ToValueKind(reader.TokenType);
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
            }
        }
    }
}
