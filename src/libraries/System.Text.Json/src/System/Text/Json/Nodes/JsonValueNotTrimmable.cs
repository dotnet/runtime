// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// Not trim-safe since it calls JsonSerializer.Serialize(JsonSerializerOptions).
    /// </summary>
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed partial class JsonValueNotTrimmable<TValue> : JsonValue<TValue>
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        public JsonValueNotTrimmable(TValue value, JsonNodeOptions? options = null) : base(value, options) { }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            JsonSerializer.Serialize(writer, _value, options);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        internal override JsonNode InternalDeepClone() => JsonSerializer.SerializeToNode(_value)!;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        internal override bool DeepEquals(JsonNode? node)
        {
            JsonNode? jsonNode = JsonSerializer.SerializeToNode(_value);
            return DeepEquals(jsonNode, node);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        internal override JsonValueKind GetInternalValueKind()
        {
            JsonNode? jsonNode = JsonSerializer.SerializeToNode(_value);
            return jsonNode is null ? JsonValueKind.Null : jsonNode.GetValueKind();
        }
    }
}
