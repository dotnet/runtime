// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
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
