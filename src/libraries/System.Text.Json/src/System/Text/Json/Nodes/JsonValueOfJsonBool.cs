// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace System.Text.Json.Nodes
{
    internal sealed class JsonValueOfJsonBool : JsonValuePrimitive<bool>
    {
        private JsonValueKind ValueKind => Value ? JsonValueKind.True : JsonValueKind.False;

        internal JsonValueOfJsonBool(bool value, JsonConverter<bool> converter, JsonNodeOptions? options)
            : base(value, converter, options)
        {
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null) => writer.WriteBooleanValue(Value);
        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonBool(Value, _converter, Options);
        private protected override JsonValueKind GetValueKindCore() => ValueKind;

        public override T GetValue<T>()
        {
            if (!TryGetValue(out T? value))
            {
                ThrowHelper.ThrowInvalidOperationException_NodeUnableToConvertElement(Value ? JsonValueKind.True : JsonValueKind.False, typeof(T));
            }

            return value;
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T value)
            where T : default
        {
            if (typeof(T) == typeof(JsonElement))
            {
                value = (T)(object)JsonElement.Parse(Value ? JsonConstants.TrueValue : JsonConstants.FalseValue);
                return true;
            }

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                value = (T)(object)Value;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
