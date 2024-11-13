// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// Defines a primitive JSON value that is wrapping a <see cref="JsonElement"/>.
    /// </summary>
    internal sealed class JsonValueOfElement : JsonValue<JsonElement>
    {
        public JsonValueOfElement(JsonElement value, JsonNodeOptions? options) : base(value, options)
        {
            Debug.Assert(value.ValueKind is JsonValueKind.False or JsonValueKind.True or JsonValueKind.Number or JsonValueKind.String);
        }

        internal override JsonElement? UnderlyingElement => Value;
        internal override JsonNode DeepCloneCore() => new JsonValueOfElement(Value.Clone(), Options);
        private protected override JsonValueKind GetValueKindCore() => Value.ValueKind;

        internal override bool DeepEqualsCore(JsonNode otherNode)
        {
            if (otherNode.UnderlyingElement is JsonElement otherElement)
            {
                return JsonElement.DeepEquals(Value, otherElement);
            }

            if (otherNode is JsonValue)
            {
                // Dispatch to the other value in case it knows
                // how to convert JsonElement to its own type.
                return otherNode.DeepEqualsCore(this);
            }

            return base.DeepEqualsCore(otherNode);
        }

        public override TypeToConvert GetValue<TypeToConvert>()
        {
            if (!TryGetValue(out TypeToConvert? value))
            {
                ThrowHelper.ThrowInvalidOperationException_NodeUnableToConvertElement(Value.ValueKind, typeof(TypeToConvert));
            }

            return value;
        }

        public override bool TryGetValue<TypeToConvert>([NotNullWhen(true)] out TypeToConvert value)
        {
            bool success;

            if (Value is TypeToConvert element)
            {
                value = element;
                return true;
            }

            switch (Value.ValueKind)
            {
                case JsonValueKind.Number:
                    if (typeof(TypeToConvert) == typeof(int) || typeof(TypeToConvert) == typeof(int?))
                    {
                        success = Value.TryGetInt32(out int result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(long) || typeof(TypeToConvert) == typeof(long?))
                    {
                        success = Value.TryGetInt64(out long result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(double) || typeof(TypeToConvert) == typeof(double?))
                    {
                        success = Value.TryGetDouble(out double result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(short) || typeof(TypeToConvert) == typeof(short?))
                    {
                        success = Value.TryGetInt16(out short result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(decimal) || typeof(TypeToConvert) == typeof(decimal?))
                    {
                        success = Value.TryGetDecimal(out decimal result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(byte) || typeof(TypeToConvert) == typeof(byte?))
                    {
                        success = Value.TryGetByte(out byte result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(float) || typeof(TypeToConvert) == typeof(float?))
                    {
                        success = Value.TryGetSingle(out float result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(uint) || typeof(TypeToConvert) == typeof(uint?))
                    {
                        success = Value.TryGetUInt32(out uint result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(ushort) || typeof(TypeToConvert) == typeof(ushort?))
                    {
                        success = Value.TryGetUInt16(out ushort result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(ulong) || typeof(TypeToConvert) == typeof(ulong?))
                    {
                        success = Value.TryGetUInt64(out ulong result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(sbyte) || typeof(TypeToConvert) == typeof(sbyte?))
                    {
                        success = Value.TryGetSByte(out sbyte result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }
                    break;

                case JsonValueKind.String:
                    if (typeof(TypeToConvert) == typeof(string))
                    {
                        string? result = Value.GetString();
                        Debug.Assert(result != null);
                        value = (TypeToConvert)(object)result;
                        return true;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTime) || typeof(TypeToConvert) == typeof(DateTime?))
                    {
                        success = Value.TryGetDateTime(out DateTime result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTimeOffset) || typeof(TypeToConvert) == typeof(DateTimeOffset?))
                    {
                        success = Value.TryGetDateTimeOffset(out DateTimeOffset result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(Guid) || typeof(TypeToConvert) == typeof(Guid?))
                    {
                        success = Value.TryGetGuid(out Guid result);
                        value = (TypeToConvert)(object)result;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(char) || typeof(TypeToConvert) == typeof(char?))
                    {
                        string? result = Value.GetString();
                        Debug.Assert(result != null);
                        if (result.Length == 1)
                        {
                            value = (TypeToConvert)(object)result[0];
                            return true;
                        }
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (typeof(TypeToConvert) == typeof(bool) || typeof(TypeToConvert) == typeof(bool?))
                    {
                        value = (TypeToConvert)(object)Value.GetBoolean();
                        return true;
                    }
                    break;
            }

            value = default!;
            return false;
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            Value.WriteTo(writer);
        }
    }
}
