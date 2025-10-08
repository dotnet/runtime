// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
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
                case JsonTokenType.String
                    when typeof(T) == typeof(JsonValuePrimitive<char>):
                    return JsonValue.Create(reader.GetChar(), options);
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
                case JsonTokenType.Number
                    when typeof(T) == typeof(JsonValuePrimitive<byte>):
                    return JsonValue.Create(reader.GetByte(), options);
                case JsonTokenType.Number
                    when typeof(T) == typeof(JsonValuePrimitive<decimal>):
                    return JsonValue.Create(reader.GetDecimal(), options);
                case JsonTokenType.Number
                    when typeof(T) == typeof(JsonValuePrimitive<double>):
                    return JsonValue.Create(reader.GetDouble(), options);
                case JsonTokenType.Number
                    when typeof(T) == typeof(JsonValuePrimitive<float>):
                    return JsonValue.Create(reader.GetSingle(), options);
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
}
