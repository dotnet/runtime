// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// Not trim-safe since it calls JsonSerializer.Serialize(JsonSerializerOptions).
    /// </summary>
    internal sealed partial class JsonValueNotTrimmable<TValue> : JsonValue<TValue>
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        public JsonValueNotTrimmable(TValue value, JsonNodeOptions? options = null) : base(value, options) { }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked with RequiresUnreferencedCode.")]
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            JsonSerializer.Serialize(writer, _value, options);
        }
    }
}
