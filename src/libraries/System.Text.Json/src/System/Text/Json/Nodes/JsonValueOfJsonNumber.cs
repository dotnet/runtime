// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    internal sealed class JsonValueOfJsonNumber : JsonValue
    {
        // This can be optimized to store the decimal point position and the exponent so that
        // conversion to different numeric types can be done without parsing the string again.
        // Utf8Parser uses an internal ref struct, Number.NumberBuffer, which is really the
        // same functionality that we would want here.
        private readonly byte[] _value;

        internal JsonValueOfJsonNumber(byte[] number, JsonNodeOptions? options)
            : base(options)
        {
            _value = number;
        }

        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonNumber(_value, Options);
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
                value = (T)(object)JsonElement.Parse(_value);
                return true;
            }

            bool success;

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                success = Utf8Parser.TryParse(_value, out int result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
            {
                success = Utf8Parser.TryParse(_value, out long result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                success = Utf8Parser.TryParse(_value, out double result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
            {
                success = Utf8Parser.TryParse(_value, out short result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                success = Utf8Parser.TryParse(_value, out decimal result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
            {
                success = Utf8Parser.TryParse(_value, out byte result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
            {
                success = Utf8Parser.TryParse(_value, out float result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
            {
                success = Utf8Parser.TryParse(_value, out uint result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
            {
                success = Utf8Parser.TryParse(_value, out ushort result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
            {
                success = Utf8Parser.TryParse(_value, out ulong result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
            {
                success = Utf8Parser.TryParse(_value, out sbyte result, out int consumed) &&
                            consumed == _value.Length;

                value = (T)(object)result;
                return success;
            }

            value = default!;
            return false;
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteNumberValue(_value);
        }
    }
}
