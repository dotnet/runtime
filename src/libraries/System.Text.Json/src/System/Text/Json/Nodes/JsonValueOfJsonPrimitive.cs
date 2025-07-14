// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    internal static class JsonValueOfJsonPrimitiveHelpers
    {
        internal static JsonValue CreatePrimitiveValue(ref Utf8JsonReader reader, JsonNodeOptions? options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.False:
                case JsonTokenType.True:
                    return new JsonValueOfJsonBool(reader.GetBoolean(), options);
                case JsonTokenType.String:
                    byte[] stringValue = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray();
                    return new JsonValueOfJsonString(stringValue, reader.ValueIsEscaped, options);
                case JsonTokenType.Number:
                    return new JsonValueOfJsonNumber(ref reader, options);
                default:
                    Debug.Fail("Only primitives allowed.");
                    ThrowHelper.ThrowJsonException();
                    return null!; // Unreachable, but required for compilation.
            }
        }

        internal static bool TryGetJsonElement(JsonValue value, out JsonElement element)
        {
            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(
                options: default,
                JsonSerializerOptions.BufferSizeDefault,
                out PooledByteBufferWriter output);

            try
            {
                value.WriteTo(writer);
                writer.Flush();
                Utf8JsonReader reader = new(output.WrittenSpan);

                bool success = JsonElement.TryParseValue(ref reader, out JsonElement? parsed);
                element = parsed.GetValueOrDefault();
                return success;
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
            }
        }
    }

    internal sealed class JsonValueOfJsonString : JsonValue
    {
        private readonly byte[] _value;
        private readonly bool _isEscaped;

        internal JsonValueOfJsonString(byte[] value, bool isEscaped, JsonNodeOptions? options)
            : base(options)
        {
            _value = value;
            _isEscaped = isEscaped;
        }

        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonString(_value, _isEscaped, Options);
        private protected override JsonValueKind GetValueKindCore() => JsonValueKind.String;

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(writer);

            if (_isEscaped)
            {
                writer.WriteStringValue(JsonReaderHelper.GetUnescapedString(_value));
            }
            else
            {
                writer.WriteStringValue(_value);
            }
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
            bool success;

            if (typeof(T) == typeof(JsonElement))
            {
                success = JsonValueOfJsonPrimitiveHelpers.TryGetJsonElement(this, out JsonElement element);
                value = (T)(object)element;
                return success;
            }

            if (typeof(T) == typeof(string))
            {
                string? result = _isEscaped
                    ? JsonReaderHelper.GetUnescapedString(_value)
                    : JsonReaderHelper.TranscodeHelper(_value);

                Debug.Assert(result != null);
                value = (T)(object)result;
                return true;
            }

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                success = JsonReaderHelper.TryGetValue(_value, _isEscaped, out DateTime result);
                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
            {
                success = JsonReaderHelper.TryGetValue(_value, _isEscaped, out DateTimeOffset result);
                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
            {
                success = JsonReaderHelper.TryGetValue(_value, _isEscaped, out Guid result);
                value = (T)(object)result;
                return success;
            }

            if (typeof(T) == typeof(char) || typeof(T) == typeof(char?))
            {
                string? result = _isEscaped
                    ? JsonReaderHelper.GetUnescapedString(_value)
                    : JsonReaderHelper.TranscodeHelper(_value);

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

        public override T GetValue<T>()
        {
            if (!TryGetValue(out T? value))
            {
                ThrowHelper.ThrowInvalidOperationException_NodeUnableToConvertElement(ValueKind, typeof(T));
            }

            return value;
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T? value)
            where T : default
        {
            bool success;

            if (typeof(T) == typeof(JsonElement))
            {
                success = JsonValueOfJsonPrimitiveHelpers.TryGetJsonElement(this, out JsonElement element);
                value = (T)(object)element;
                return success;
            }

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                value = (T)(object)_value;
                return true;
            }

            value = default!;
            return false;
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null) => writer.WriteBooleanValue(_value);
        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonBool(_value, Options);
        private protected override JsonValueKind GetValueKindCore() => ValueKind;
    }

    internal sealed class JsonValueOfJsonNumber : JsonValue
    {
        private readonly JsonNumber _value;

        internal JsonValueOfJsonNumber(JsonNumber number, JsonNodeOptions? options)
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

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteNumberValue(_value);
        }

        // This can be optimized to also store the decimal point position and the exponent
        internal struct JsonNumber
        {
            byte[] _bytes;
        }
    }

    internal sealed class JsonValueOfJsonPrimitive : JsonValue
    {
        private readonly byte[] _value;
        private readonly JsonValueKind _valueKind;
        private readonly bool _isEscaped;

        public JsonValueOfJsonPrimitive(ref Utf8JsonReader reader, JsonNodeOptions? options)
            : base(options)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False);

            _isEscaped = reader.ValueIsEscaped;
            _valueKind = reader.TokenType.ToValueKind();
            _value = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray();
        }

        private JsonValueOfJsonPrimitive(byte[] value, JsonValueKind valueKind, bool isEscaped, JsonNodeOptions? options)
            : base(options)
        {
            _value = value;
            _valueKind = valueKind;
            _isEscaped = isEscaped;
        }

        internal override JsonNode DeepCloneCore() => new JsonValueOfJsonPrimitive(_value, _valueKind, _isEscaped, Options);
        private protected override JsonValueKind GetValueKindCore() => _valueKind;

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(writer);

            switch (_valueKind)
            {
                case JsonValueKind.String:
                    if (_isEscaped)
                    {
                        writer.WriteStringValue(JsonReaderHelper.GetUnescapedString(_value));
                    }
                    else
                    {
                        writer.WriteStringValue(_value);
                    }
                    break;

                case JsonValueKind.Number:
                    writer.WriteNumberValue(_value);
                    break;

                default:
                    ThrowHelper.ThrowJsonException();
                    break;
            }
        }

        public override T GetValue<T>()
        {
            if (!TryGetValue(out T? value))
            {
                ThrowHelper.ThrowInvalidOperationException_NodeUnableToConvertElement(_valueKind, typeof(T));
            }

            return value;
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T? value)
            where T : default
        {
            bool success;

            if (typeof(T) == typeof(JsonElement))
            {
                success = JsonValueOfJsonPrimitiveHelpers.TryGetJsonElement(this, out JsonElement element);
                value = (T)(object)element;
                return success;
            }

            switch (_valueKind)
            {
                case JsonValueKind.Number:
                    if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                    {
                        success = Utf8Parser.TryParse(_value, out int result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
                    {
                        success = Utf8Parser.TryParse(_value, out long result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
                    {
                        success = Utf8Parser.TryParse(_value, out double result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
                    {
                        success = Utf8Parser.TryParse(_value, out short result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                    {
                        success = Utf8Parser.TryParse(_value, out decimal result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
                    {
                        success = Utf8Parser.TryParse(_value, out byte result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
                    {
                        success = Utf8Parser.TryParse(_value, out float result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
                    {
                        success = Utf8Parser.TryParse(_value, out uint result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
                    {
                        success = Utf8Parser.TryParse(_value, out ushort result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
                    {
                        success = Utf8Parser.TryParse(_value, out ulong result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
                    {
                        success = Utf8Parser.TryParse(_value, out sbyte result, out int bytesConsumed) &&
                                  _value.Length == bytesConsumed;
                        value = (T)(object)result;
                        return success;
                    }
                    break;

                case JsonValueKind.String:
                    if (typeof(T) == typeof(string))
                    {
                        string? result = _isEscaped
                            ? JsonReaderHelper.GetUnescapedString(_value)
                            : JsonReaderHelper.TranscodeHelper(_value);

                        Debug.Assert(result != null);
                        value = (T)(object)result;
                        return true;
                    }

                    if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                    {
                        success = JsonReaderHelper.TryGetValue(_value, _isEscaped, out DateTime result);
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
                    {
                        success = JsonReaderHelper.TryGetValue(_value, _isEscaped, out DateTimeOffset result);
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
                    {
                        success = JsonReaderHelper.TryGetValue(_value, _isEscaped, out Guid result);
                        value = (T)(object)result;
                        return success;
                    }

                    if (typeof(T) == typeof(char) || typeof(T) == typeof(char?))
                    {
                        string? result = _isEscaped
                            ? JsonReaderHelper.GetUnescapedString(_value)
                            : JsonReaderHelper.TranscodeHelper(_value);

                        Debug.Assert(result != null);
                        if (result.Length == 1)
                        {
                            value = (T)(object)result[0];
                            return true;
                        }
                    }
                    break;
            }

            value = default!;
            return false;
        }
    }
}
