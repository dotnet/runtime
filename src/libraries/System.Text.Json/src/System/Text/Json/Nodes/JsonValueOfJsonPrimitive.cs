// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Extensions;

namespace System.Text.Json.Nodes
{
    internal static class JsonValueOfJsonPrimitive
    {
        internal static JsonValue CreatePrimitiveValue<T>(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.False:
                case JsonTokenType.True:
                    return JsonValue.Create(reader.GetBoolean(), options);
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<DateTime>):
                    return JsonValue.Create(reader.GetDateTime(), options);
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<DateTimeOffset>):
                    return JsonValue.Create(reader.GetDateTimeOffset(), options);
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<Guid>):
                    return JsonValue.Create(reader.GetGuid(), options);
#if NET
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<DateOnly>):
                    return JsonValue.Create(reader.GetDateOnly(), options);
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<TimeOnly>):
                    return JsonValue.Create(reader.GetTimeOnly(), options);
#endif
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<TimeSpan>):
                    return JsonValue.Create(reader.GetTimeSpan(), options);
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<Uri>):
                    return JsonValue.Create(reader.GetUri(), options);
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<Version>):
                    return JsonValue.Create(reader.GetVersion(), options);
                case JsonTokenType.String:
                    byte[] buffer = new byte[reader.ValueLength];
                    ReadOnlyMemory<byte> utf8String = buffer.AsMemory(0, reader.CopyString(buffer));
                    return new JsonValueOfJsonString(utf8String, options);
#if NET
                case JsonTokenType.Number
                    when typeof(T) == typeof(JsonValuePrimitive<Half>):
                    return JsonValue.Create(reader.GetHalf(), options);
                case JsonTokenType.Number
                    when typeof(T) == typeof(JsonValuePrimitive<Int128>):
                    return JsonValue.Create(reader.GetInt128(), options);
                case JsonTokenType.Number
                    when typeof(T) == typeof(JsonValuePrimitive<UInt128>):
                    return JsonValue.Create(reader.GetUInt128(), options);
#endif
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
}
