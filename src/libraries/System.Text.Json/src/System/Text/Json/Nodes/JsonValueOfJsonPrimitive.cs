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
                    byte[] quotedStringValue;

                    if (reader.ValueIsEscaped)
                    {
                        ReadOnlySpan<byte> unescapedValue = reader.GetUnescapedSpan();

                        quotedStringValue = new byte[checked(2 + unescapedValue.Length)];
                        unescapedValue.CopyTo(quotedStringValue.AsSpan().Slice(1, unescapedValue.Length));
                    }
                    else if (reader.HasValueSequence)
                    {
                        // Throw if the long to int conversion fails. This can be accommodated in the future if needed by
                        // adding another derived class that has a backing ReadOnlySequence<byte> instead of a byte[].
                        int bufferLength = checked((int)(2 + reader.ValueSequence.Length));
                        quotedStringValue = new byte[bufferLength];
                        reader.ValueSequence.CopyTo(quotedStringValue.AsSpan().Slice(1, bufferLength - 2));
                    }
                    else
                    {
                        quotedStringValue = new byte[2 + reader.ValueSpan.Length];
                        reader.ValueSpan.CopyTo(quotedStringValue.AsSpan(1, reader.ValueSpan.Length));
                    }

                    quotedStringValue[0] = quotedStringValue[quotedStringValue.Length - 1] = JsonConstants.Quote;
                    return new JsonValueOfJsonString(quotedStringValue, options);
                case JsonTokenType.Number:
                    byte[] numberValue = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray();
                    return new JsonValueOfJsonNumber(new JsonValueOfJsonNumber.JsonNumber(numberValue), options);
                default:
                    Debug.Fail("Only primitives allowed.");
                    ThrowHelper.ThrowJsonException();
                    return null!; // Unreachable, but required for compilation.
            }
        }

        private sealed class JsonValueOfJsonString : JsonValue
        {
            private readonly byte[] _value;

            internal JsonValueOfJsonString(byte[] value, JsonNodeOptions? options)
                : base(options)
            {
                _value = value;
            }

            private ReadOnlySpan<byte> ValueWithoutQuotes => new ReadOnlySpan<byte>(_value, 1, _value.Length - 2);

            internal override JsonNode DeepCloneCore() => new JsonValueOfJsonString(_value, Options);
            private protected override JsonValueKind GetValueKindCore() => JsonValueKind.String;

            public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
            {
                ArgumentNullException.ThrowIfNull(writer);

                writer.WriteStringValue(ValueWithoutQuotes);
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
                    int firstByteToEscape = JsonWriterHelper.NeedsEscaping(ValueWithoutQuotes, JavaScriptEncoder.Default);

                    if (firstByteToEscape != -1)
                    {
                        value = (T)(object)EscapeAndConvert(ValueWithoutQuotes, firstByteToEscape);
                        return true;
                    }

                    value = (T)(object)JsonElement.Parse(_value);
                    return true;
                }

                if (typeof(T) == typeof(string))
                {
                    string? result = JsonReaderHelper.TranscodeHelper(ValueWithoutQuotes);

                    Debug.Assert(result != null);
                    value = (T)(object)result;
                    return true;
                }

                bool success;

                if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                {
                    success = JsonReaderHelper.TryGetValue(ValueWithoutQuotes, isEscaped: false, out DateTime result);
                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
                {
                    success = JsonReaderHelper.TryGetValue(ValueWithoutQuotes, isEscaped: false, out DateTimeOffset result);
                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
                {
                    success = JsonReaderHelper.TryGetValue(ValueWithoutQuotes, isEscaped: false, out Guid result);
                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(char) || typeof(T) == typeof(char?))
                {
                    string? result = JsonReaderHelper.TranscodeHelper(ValueWithoutQuotes);

                    Debug.Assert(result != null);
                    if (result.Length == 1)
                    {
                        value = (T)(object)result[0];
                        return true;
                    }
                }

                value = default!;
                return false;

                static JsonElement EscapeAndConvert(ReadOnlySpan<byte> value, int idx)
                {
                    Debug.Assert(idx != -1);
                    Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);

                    int length = checked(2 + JsonWriterHelper.GetMaxEscapedLength(value.Length, idx));
                    byte[]? rented = null;

                    try
                    {
                        scoped Span<byte> escapedValue;

                        if (length > JsonConstants.StackallocByteThreshold)
                        {
                            rented = ArrayPool<byte>.Shared.Rent(length);
                            escapedValue = rented;
                        }
                        else
                        {
                            escapedValue = stackalloc byte[JsonConstants.StackallocByteThreshold];
                        }

                        escapedValue[0] = JsonConstants.Quote;
                        JsonWriterHelper.EscapeString(value, escapedValue.Slice(1), idx, JavaScriptEncoder.Default, out int written);
                        escapedValue[1 + written] = JsonConstants.Quote;

                        return JsonElement.Parse(escapedValue.Slice(0, written + 2));
                    }
                    finally
                    {
                        if (rented != null)
                        {
                            ArrayPool<byte>.Shared.Return(rented);
                        }
                    }
                }
            }
        }

        private sealed class JsonValueOfJsonBool : JsonValue
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

        private sealed class JsonValueOfJsonNumber : JsonValue
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

            public override bool TryGetValue<T>([NotNullWhen(true)] out T? value)
                where T : default
            {
                if (typeof(T) == typeof(JsonElement))
                {
                    value = (T)(object)JsonElement.Parse(_value.Bytes);
                    return true;
                }

                bool success;

                if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out int result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out long result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out double result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out short result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out decimal result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out byte result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out float result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out uint result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out ushort result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out ulong result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
                {
                    success = Utf8Parser.TryParse(_value.Bytes, out sbyte result, out int consumed) &&
                              consumed == _value.Bytes.Length;

                    value = (T)(object)result;
                    return success;
                }

                value = default!;
                return false;
            }

            public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
            {
                ArgumentNullException.ThrowIfNull(writer);

                writer.WriteNumberValue(_value.Bytes);
            }

            // This can be optimized to also store the decimal point position and the exponent so that
            // conversion to different numeric types can be done without parsing the string again.
            // Utf8Parser uses an internal ref struct, Number.NumberBuffer, which is really the
            // same functionality that we would want here.
            internal readonly struct JsonNumber
            {
                internal byte[] Bytes { get; }

                internal JsonNumber(byte[] bytes)
                {
                    Bytes = bytes;
                }
            }
        }
    }
}
