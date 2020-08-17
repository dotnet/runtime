// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        public static (int, int) CountNewLines(ReadOnlySpan<byte> data)
        {
            int lastLineFeedIndex = -1;
            int newLines = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == JsonConstants.LineFeed)
                {
                    lastLineFeedIndex = i;
                    newLines++;
                }
            }
            return (newLines, lastLineFeedIndex);
        }

        internal static JsonValueKind ToValueKind(this JsonTokenType tokenType)
        {
            switch (tokenType)
            {
                case JsonTokenType.None:
                    return JsonValueKind.Undefined;
                case JsonTokenType.StartArray:
                    return JsonValueKind.Array;
                case JsonTokenType.StartObject:
                    return JsonValueKind.Object;
                case JsonTokenType.String:
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    // This is the offset between the set of literals within JsonValueType and JsonTokenType
                    // Essentially: JsonTokenType.Null - JsonValueType.Null
                    return (JsonValueKind)((byte)tokenType - 4);
                default:
                    Debug.Fail($"No mapping for token type {tokenType}");
                    return JsonValueKind.Undefined;
            }
        }

        // Returns true if the TokenType is a primitive "value", i.e. String, Number, True, False, and Null
        // Otherwise, return false.
        public static bool IsTokenTypePrimitive(JsonTokenType tokenType) =>
            (tokenType - JsonTokenType.String) <= (JsonTokenType.Null - JsonTokenType.String);

        // A hex digit is valid if it is in the range: [0..9] | [A..F] | [a..f]
        // Otherwise, return false.
        public static bool IsHexDigit(byte nextByte) => HexConverter.IsHexChar(nextByte);

        public static bool TryGetEscapedDateTime(ReadOnlySpan<byte> source, out DateTime value)
        {
            int backslash = source.IndexOf(JsonConstants.BackSlash);
            Debug.Assert(backslash != -1);

            Debug.Assert(source.Length <= JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
            Span<byte> sourceUnescaped = stackalloc byte[source.Length];

            Unescape(source, sourceUnescaped, backslash, out int written);
            Debug.Assert(written > 0);

            sourceUnescaped = sourceUnescaped.Slice(0, written);
            Debug.Assert(!sourceUnescaped.IsEmpty);

            if (sourceUnescaped.Length <= JsonConstants.MaximumDateTimeOffsetParseLength
                && JsonHelpers.TryParseAsISO(sourceUnescaped, out DateTime tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetEscapedDateTimeOffset(ReadOnlySpan<byte> source, out DateTimeOffset value)
        {
            int backslash = source.IndexOf(JsonConstants.BackSlash);
            Debug.Assert(backslash != -1);

            Debug.Assert(source.Length <= JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
            Span<byte> sourceUnescaped = stackalloc byte[source.Length];

            Unescape(source, sourceUnescaped, backslash, out int written);
            Debug.Assert(written > 0);

            sourceUnescaped = sourceUnescaped.Slice(0, written);
            Debug.Assert(!sourceUnescaped.IsEmpty);

            if (sourceUnescaped.Length <= JsonConstants.MaximumDateTimeOffsetParseLength
                && JsonHelpers.TryParseAsISO(sourceUnescaped, out DateTimeOffset tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetEscapedGuid(ReadOnlySpan<byte> source, out Guid value)
        {
            Debug.Assert(source.Length <= JsonConstants.MaximumEscapedGuidLength);

            int idx = source.IndexOf(JsonConstants.BackSlash);
            Debug.Assert(idx != -1);

            Span<byte> utf8Unescaped = stackalloc byte[source.Length];

            Unescape(source, utf8Unescaped, idx, out int written);
            Debug.Assert(written > 0);

            utf8Unescaped = utf8Unescaped.Slice(0, written);
            Debug.Assert(!utf8Unescaped.IsEmpty);

            if (utf8Unescaped.Length == JsonConstants.MaximumFormatGuidLength
                && Utf8Parser.TryParse(utf8Unescaped, out Guid tmp, out _, 'D'))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        public static char GetFloatingPointStandardParseFormat(ReadOnlySpan<byte> span)
        {
            // Assume that 'e/E' is closer to the end.
            int startIndex = span.Length - 1;
            for (int i = startIndex; i >= 0; i--)
            {
                byte token = span[i];
                if (token == 'E' || token == 'e')
                {
                    return JsonConstants.ScientificNotationFormat;
                }
            }
            return default;
        }

        public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out float value)
        {
            if (span.Length == 3)
            {
                if (span.SequenceEqual(JsonConstants.NaNValue))
                {
                    value = float.NaN;
                    return true;
                }
            }
            else if (span.Length == 8)
            {
                if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
                {
                    value = float.PositiveInfinity;
                    return true;
                }
            }
            else if (span.Length == 9)
            {
                if (span.SequenceEqual(JsonConstants.NegativeInfinityValue))
                {
                    value = float.NegativeInfinity;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out double value)
        {
            if (span.Length == 3)
            {
                if (span.SequenceEqual(JsonConstants.NaNValue))
                {
                    value = double.NaN;
                    return true;
                }
            }
            else if (span.Length == 8)
            {
                if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
                {
                    value = double.PositiveInfinity;
                    return true;
                }
            }
            else if (span.Length == 9)
            {
                if (span.SequenceEqual(JsonConstants.NegativeInfinityValue))
                {
                    value = double.NegativeInfinity;
                    return true;
                }
            }

            value = 0;
            return false;
        }
    }
}
