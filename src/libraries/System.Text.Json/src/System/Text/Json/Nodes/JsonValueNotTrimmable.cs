// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// Not trim-safe since it calls JsonSerializer.Serialize(JsonSerializerOptions).
    /// </summary>
    internal sealed partial class JsonValueNotTrimmable<TValue> : JsonValue<TValue>
    {
        public JsonValueNotTrimmable(TValue value, JsonNodeOptions? options = null) : base(value, options)
        {
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            JsonSerializer.Serialize(writer, _value, options);
        }
    }
}
