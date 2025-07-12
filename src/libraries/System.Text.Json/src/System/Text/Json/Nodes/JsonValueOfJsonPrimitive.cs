// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
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

            if (_valueKind is not JsonValueKind.String)
            {
                writer.WriteRawValue(_value);
                return;
            }

            byte[]? rented = null;

            try
            {
                int quotedValueLength = _value.Length + 2;
                Span<byte> buffer = quotedValueLength <= JsonConstants.StackallocByteThreshold
                    ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                    : (rented = ArrayPool<byte>.Shared.Rent(quotedValueLength));
                buffer = buffer.Slice(0, quotedValueLength);

                buffer[0] = buffer[buffer.Length - 1] = JsonConstants.Quote;
                _value.CopyTo(buffer.Slice(1, _value.Length));

                writer.WriteRawValue(buffer);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
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

        public override bool TryGetValue<T>([NotNullWhen(true)] out T? value) where T : default
        {
            bool success;

            if (typeof(T) == typeof(JsonElement))
            {
                success = TryGetJsonElement(this, out JsonElement element);
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

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                    {
                        value = (T)(object)(_valueKind is JsonValueKind.True ? true : false);
                        return true;
                    }
                    break;
            }

            value = default!;
            return false;

            static bool TryGetJsonElement(JsonValue @this, out JsonElement element)
            {
                Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(
                    options: default,
                    JsonSerializerOptions.BufferSizeDefault,
                    out PooledByteBufferWriter output);

                try
                {
                    @this.WriteTo(writer);
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
    }
}
