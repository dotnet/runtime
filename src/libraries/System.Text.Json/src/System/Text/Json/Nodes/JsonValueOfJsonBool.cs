// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    internal sealed class JsonValueOfJsonBool : JsonValue
    {
        private readonly bool _value;

        private JsonValueKind ValueKind => _value ? JsonValueKind.True : JsonValueKind.False;

        internal JsonValueOfJsonBool(bool value, JsonNodeOptions? options)
            : base(options)
        {
            _value = value;
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null) => writer.WriteBooleanValue(_value);
        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonBool(_value, Options);
        private protected override JsonValueKind GetValueKindCore() => ValueKind;

        public override T GetValue<T>()
        {
            if (!TryGetValue(out T? value))
            {
                ThrowHelper.ThrowInvalidOperationException_NodeUnableToConvertElement(_value ? JsonValueKind.True : JsonValueKind.False, typeof(T));
            }

            return value;
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T? value)
            where T : default
        {
            if (typeof(T) == typeof(JsonElement))
            {
                value = (T)(object)JsonElement.Parse(_value ? JsonConstants.TrueValue : JsonConstants.FalseValue);
                return true;
            }

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                value = (T)(object)_value;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
