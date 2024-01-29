// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// A JsonValue encapsulating arbitrary types using custom JsonTypeInfo metadata.
    /// </summary>
    internal sealed class JsonValueCustomized<TValue> : JsonValue<TValue>
    {
        private readonly JsonTypeInfo<TValue> _jsonTypeInfo;

        public JsonValueCustomized(TValue value, JsonTypeInfo<TValue> jsonTypeInfo, JsonNodeOptions? options = null) : base(value, options)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);
            _jsonTypeInfo = jsonTypeInfo;
        }

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

        internal override JsonNode DeepCloneCore()
        {
            return JsonSerializer.SerializeToNode(Value, _jsonTypeInfo)!;
        }
    }
}
