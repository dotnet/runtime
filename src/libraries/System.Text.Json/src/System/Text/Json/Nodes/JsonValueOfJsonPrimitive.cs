// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;

namespace System.Text.Json.Nodes
{
    internal static class JsonValueOfJsonPrimitive
    {
        internal static JsonValue CreatePrimitiveValue(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.False:
                case JsonTokenType.True:
                    return new JsonValueOfJsonBool(reader.GetBoolean(), options);
                case JsonTokenType.String:
                    byte[] buffer = new byte[reader.ValueLength];
                    ReadOnlyMemory<byte> utf8String = buffer.AsMemory(0, reader.CopyString(buffer));
                    return new JsonValueOfJsonString(utf8String, options);
                case JsonTokenType.Number:
                    byte[] numberValue = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray();
                    return new JsonValueOfJsonNumber(numberValue, options);
                default:
                    Debug.Fail("Only primitives allowed.");
                    ThrowHelper.ThrowJsonException();
                    return null!; // Unreachable, but required for compilation.
            }
        }
    }

    internal sealed class JsonValueOfJsonString : JsonValue
    {
        private readonly ReadOnlyMemory<byte> _value;

        internal JsonValueOfJsonString(ReadOnlyMemory<byte> utf8String, JsonNodeOptions? options)
            : base(options)
        {
            _value = utf8String;
        }

        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonString(_value, Options);
        private protected override JsonValueKind GetValueKindCore() => JsonValueKind.String;

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteStringValue(_value.Span);
        }

        public override T GetValue<T>()
        {
            if (!TryGetValue(out T? value))
            {
                ThrowHelper.ThrowInvalidOperationException_NodeUnableToConvertElement(JsonValueKind.String, typeof(T));
            }

            return value;
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T? value)
            where T : default
        {
            if (typeof(T) == typeof(JsonElement))
            {
                value = (T)(object)JsonWriterHelper.WriteString(_value.Span, static serialized => JsonElement.Parse(serialized));
                return true;
            }

            if (typeof(T) == typeof(string))
            {
                string? result = JsonReaderHelper.TranscodeHelper(_value.Span);

                Debug.Assert(result != null);
                value = (T)(object)result;
                return true;
            }

            bool success;

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                success = JsonReaderHelper.TryGetValue(_value.Span, isEscaped: false, out DateTime result);
                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
            {
                success = JsonReaderHelper.TryGetValue(_value.Span, isEscaped: false, out DateTimeOffset result);
                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
            {
                success = JsonReaderHelper.TryGetValue(_value.Span, isEscaped: false, out Guid result);
                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(char) || typeof(T) == typeof(char?))
            {
                string? result = JsonReaderHelper.TranscodeHelper(_value.Span);

                Debug.Assert(result != null);
                if (result.Length == 1)
                {
                    value = (T)(object)result[0];
                    return true;
                }
            }

            value = default!;
            return false;
        }
    }

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
