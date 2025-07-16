// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    internal static class JsonValueOfJsonPrimitive
    {
        internal static JsonValue CreatePrimitiveValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.False:
                case JsonTokenType.True:
                    return new JsonValueOfJsonBool(reader.GetBoolean(), options);
                case JsonTokenType.String:
                    byte[] quotedStringValue;

                    if (reader.HasValueSequence)
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
                    return new JsonValueOfJsonString(quotedStringValue, reader.ValueIsEscaped, options);
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
            private readonly bool _isEscaped;
            private readonly JsonSerializerOptions _serializerOptions;

            internal JsonValueOfJsonString(byte[] value, bool isEscaped, JsonSerializerOptions options)
                : base(options.GetNodeOptions())
            {
                _value = value;
                _isEscaped = isEscaped;
                _serializerOptions = options;
            }

            private ReadOnlySpan<byte> ValueWithoutQuotes => _value.AsSpan(1, _value.Length - 2);

            internal override JsonNode DeepCloneCore() => new JsonValueOfJsonString(_value, _isEscaped, _serializerOptions);
            private protected override JsonValueKind GetValueKindCore() => JsonValueKind.String;

            public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
            {
                ArgumentNullException.ThrowIfNull(writer);

                if (_isEscaped)
                {
                    writer.WriteStringValue(JsonReaderHelper.GetUnescapedString(ValueWithoutQuotes));
                }
                else
                {
                    writer.WriteStringValue(ValueWithoutQuotes);
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
                if (!_serializerOptions.TryGetTypeInfo(typeof(T), out JsonTypeInfo? ti))
                {
                    value = default!;
                    return false;
                }

                JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)ti;

                try
                {
                    // JsonValue should not represent a null value, so we treat it the same as a deserialization failure.
                    value = JsonSerializer.Deserialize(new ReadOnlySpan<byte>(_value), typeInfo)!;
                    return value != null;
                }
                catch (JsonException)
                {
                    value = default!;
                    return false;
                }
            }
        }

        private sealed class JsonValueOfJsonBool : JsonValue
        {
            private readonly bool _value;
            private readonly JsonSerializerOptions _serializerOptions;

            private JsonValueKind ValueKind => _value ? JsonValueKind.True : JsonValueKind.False;

            internal JsonValueOfJsonBool(bool value, JsonSerializerOptions options)
                : base(options.GetNodeOptions())
            {
                _value = value;
                _serializerOptions = options;
            }

            public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null) => writer.WriteBooleanValue(_value);
            internal override JsonNode DeepCloneCore() => new JsonValueOfJsonBool(_value, _serializerOptions);
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
                if (!_serializerOptions.TryGetTypeInfo(typeof(T), out JsonTypeInfo? ti))
                {
                    value = default!;
                    return false;
                }

                JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)ti;

                try
                {
                    // JsonValue should not represent a null value, so we treat it the same as a deserialization failure.
                    value = JsonSerializer.Deserialize(_value ? JsonConstants.TrueValue : JsonConstants.FalseValue, typeInfo)!;
                    return value != null;
                }
                catch (JsonException)
                {
                    value = default!;
                    return false;
                }
            }
        }

        private sealed class JsonValueOfJsonNumber : JsonValue
        {
            private readonly JsonNumber _value;
            private readonly JsonSerializerOptions _serializerOptions;

            internal JsonValueOfJsonNumber(JsonNumber number, JsonSerializerOptions options)
                : base(options.GetNodeOptions())
            {
                _value = number;
                _serializerOptions = options;
            }

            internal override JsonNode DeepCloneCore() => new JsonValueOfJsonNumber(_value, _serializerOptions);
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
                if (!_serializerOptions.TryGetTypeInfo(typeof(T), out JsonTypeInfo? ti))
                {
                    value = default!;
                    return false;
                }

                JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)ti;

                try
                {
                    // JsonValue should not represent a null value, so we treat it the same as a deserialization failure.
                    value = JsonSerializer.Deserialize(new ReadOnlySpan<byte>(_value.Bytes), typeInfo)!;
                    return value != null;
                }
                catch (JsonException)
                {
                    value = default!;
                    return false;
                }
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
