// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    internal sealed class JsonValueOfJsonNumber : JsonValue<byte[]>
    {
        internal JsonValueOfJsonNumber(byte[] value, JsonNodeOptions? options)
            : base(value, options)
        {
        }

        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonNumber(Value, Options);
        private protected override JsonValueKind GetValueKindCore() => JsonValueKind.Number;

        public override T GetValue<T>()
        {
            if (!TryGetValue(out T? value))
            {
                ThrowHelper.ThrowInvalidOperationException_NodeUnableToConvertElement(JsonValueKind.Number, typeof(T));
            }

            return value;
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T? value)
            where T : default
        {
            if (typeof(T) == typeof(JsonElement))
            {
                value = (T)(object)JsonElement.Parse(Value);
                return true;
            }

            bool success;

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                success = Utf8Parser.TryParse(Value, out int result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
            {
                success = Utf8Parser.TryParse(Value, out long result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                success = Utf8Parser.TryParse(Value, out double result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
            {
                success = Utf8Parser.TryParse(Value, out short result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                success = Utf8Parser.TryParse(Value, out decimal result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
            {
                success = Utf8Parser.TryParse(Value, out byte result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
            {
                success = Utf8Parser.TryParse(Value, out float result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
            {
                success = Utf8Parser.TryParse(Value, out uint result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
            {
                success = Utf8Parser.TryParse(Value, out ushort result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
            {
                success = Utf8Parser.TryParse(Value, out ulong result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
            {
                success = Utf8Parser.TryParse(Value, out sbyte result, out int consumed) &&
                            consumed == Value.Length;

                value = (T)(object)result;
                return success;
            }

            value = default!;
            return false;
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteNumberValue(Value);
        }
    }
}
