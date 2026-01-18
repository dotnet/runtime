// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        // Characters that require the bracketed property name syntax in JSON Path.
        private static readonly SearchValues<char> s_specialCharacters = SearchValues.Create("$. '/\"[]()\t\n\r\f\b\\\u0085\u2028\u2029");

        // Characters that need to be escaped in the single-quoted bracket notation.
        private static readonly SearchValues<char> s_charactersToEscape = SearchValues.Create("'\\");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsSpecialCharacters(this ReadOnlySpan<char> text) =>
            text.ContainsAny(s_specialCharacters);

        /// <summary>
        /// Appends a property name escaped for use in JSON Path single-quoted bracket notation.
        /// Escapes single quotes as \' and backslashes as \\.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendEscapedPropertyName(this ref ValueStringBuilder builder, string propertyName)
        {
            AppendEscapedPropertyName(ref builder, propertyName.AsSpan());
        }

        /// <summary>
        /// Appends a property name escaped for use in JSON Path single-quoted bracket notation.
        /// Escapes single quotes as \' and backslashes as \\.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendEscapedPropertyName(this ref ValueStringBuilder builder, ReadOnlySpan<char> span)
        {
            int length = span.Length;
            if (length == 0)
                return;

            // Find first special character
            int i = span.IndexOfAny(s_charactersToEscape);
            // Fast path: no special characters
            if (i < 0)
            {
                builder.Append(span);
                return;
            }

            // Pre-allocate enough space (worst-case: every character needs escaping)
            builder.EnsureCapacity(builder.Length + length + 16); // Small buffer for typical cases

            // Process in chunks using spans
            int start = 0;

            do
            {
                // Append safe segment before special character
                if (i > start)
                {
                    builder.Append(span.Slice(start, i - start));
                }

                // Handle the special character
                char c = span[i];
                if (c == '\'' || c == '\\')  // Direct comparison is fastest
                {
                    builder.Append('\\');
                }
                builder.Append(c);

                // Move past this character
                start = i + 1;
                // Find next special character
                if (start < length)
                {
                    ReadOnlySpan<char> remaining = span.Slice(start);
                    int next = remaining.IndexOfAny(s_charactersToEscape);
                    i = next >= 0 ? start + next : -1;
                }
                else
                {
                    i = -1;
                }
            } while (i >= 0);

            // Append any remaining safe characters
            if (start < length)
            {
                builder.Append(span.Slice(start));
            }
        }

        /// <summary>
        /// Appends a property name escaped for use in JSON Path single-quoted bracket notation.
        /// Escapes single quotes as \' and backslashes as \\.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendEscapedPropertyName(this StringBuilder builder, string propertyName)
        {
            AppendEscapedPropertyName(builder, propertyName.AsSpan());
        }

        /// <summary>
        /// Appends a property name escaped for use in JSON Path single-quoted bracket notation.
        /// Escapes single quotes as \' and backslashes as \\.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendEscapedPropertyName(this StringBuilder builder, ReadOnlySpan<char> span)
        {
            int length = span.Length;
            if (length == 0)
                return;

            // Find first special character
            int i = span.IndexOfAny(s_charactersToEscape);
            // Fast path: no special characters
            if (i < 0)
            {
                builder.Append(span);
                return;
            }

            // Calculate required capacity
            // Worst-case scenario: every character needs escaping (doubles the length)
            int estimatedCapacity = builder.Length + length * 2;
            if (builder.Capacity < estimatedCapacity)
            {
                builder.EnsureCapacity(estimatedCapacity);
            }

            // Process in chunks using spans
            int start = 0;

            do
            {
                // Append safe segment before special character
                if (i > start)
                {
                    builder.Append(span.Slice(start, i - start));
                }

                // Handle the special character
                char c = span[i];
                if (c == '\'' || c == '\\')  // Direct comparison is fastest
                {
                    builder.Append('\\');
                }
                builder.Append(c);

                // Move past this character
                start = i + 1;
                
                // Find next special character
                if (start < length)
                {
                    ReadOnlySpan<char> remaining = span.Slice(start);
                    int next = remaining.IndexOfAny(s_charactersToEscape);
                    i = next >= 0 ? start + next : -1;
                }
                else
                {
                    i = -1;
                }
            } while (i >= 0);

            // Append any remaining safe characters
            if (start < length)
            {
                builder.Append(span.Slice(start));
            }
        }
       [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int) CountNewLines(ReadOnlySpan<byte> data)
        {
            int lastLineFeedIndex = data.LastIndexOf(JsonConstants.LineFeed);
            if (lastLineFeedIndex < 0)
            {
                return (0, -1);
            }
            
            // Use SIMD-optimized Count method (.NET 10.0.2)
            int newLines = 1 + data.Slice(0, lastLineFeedIndex).Count(JsonConstants.LineFeed);
            
            return (newLines, lastLineFeedIndex);
        }

 [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static JsonValueKind ToValueKind(this JsonTokenType tokenType)
        {
            return tokenType switch
            {
                    JsonTokenType.None => JsonValueKind.Undefined,
                    JsonTokenType.StartArray => JsonValueKind.Array,
                    JsonTokenType.StartObject => JsonValueKind.Object,
                    JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or
                    JsonTokenType.False or JsonTokenType.Null => (JsonValueKind)((byte)tokenType - 4),
                    // This is the offset between the set of literals within JsonValueType and JsonTokenType
                    // Essentially: JsonTokenType.Null - JsonValueType.Null
                     _ => GetUndefinedWithDebug(tokenType)
                    };
        }
[MethodImpl(MethodImplOptions.NoInlining)]
private static JsonValueKind GetUndefinedWithDebug(JsonTokenType tokenType)
{
    Debug.Fail($"No mapping for token type {tokenType}");
    return JsonValueKind.Undefined;
}

        // Returns true if the TokenType is a primitive "value", i.e. String, Number, True, False, and Null
        // Otherwise, return false.
        public static bool IsTokenTypePrimitive(JsonTokenType tokenType) =>
            (tokenType - JsonTokenType.String) <= (JsonTokenType.Null - JsonTokenType.String);

        // A hex digit is valid if it is in the range: [0..9] | [A..F] | [a..f]
        // Otherwise, return false.
        public static bool IsHexDigit(byte nextByte) => HexConverter.IsHexChar(nextByte);

        public static bool TryGetValue(ReadOnlySpan<byte> segment, bool isEscaped, out DateTime value)
        {
            if (!JsonHelpers.IsValidDateTimeOffsetParseLength(segment.Length))
            {
                value = default;
                return false;
            }

            // Segment needs to be unescaped
            if (isEscaped)
            {
                return TryGetEscapedDateTime(segment, out value);
            }

            Debug.Assert(segment.IndexOf(JsonConstants.BackSlash) == -1);

            if (JsonHelpers.TryParseAsISO(segment, out DateTime tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetEscapedDateTime(ReadOnlySpan<byte> source, out DateTime value)
        {
            Debug.Assert(source.Length <= JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
            Span<byte> sourceUnescaped = stackalloc byte[JsonConstants.MaximumEscapedDateTimeOffsetParseLength];

            Unescape(source, sourceUnescaped, out int written);
            Debug.Assert(written > 0);

            sourceUnescaped = sourceUnescaped.Slice(0, written);
            Debug.Assert(!sourceUnescaped.IsEmpty);

            if (JsonHelpers.IsValidUnescapedDateTimeOffsetParseLength(sourceUnescaped.Length)
                && JsonHelpers.TryParseAsISO(sourceUnescaped, out DateTime tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetValue(ReadOnlySpan<byte> segment, bool isEscaped, out DateTimeOffset value)
        {
            if (!JsonHelpers.IsValidDateTimeOffsetParseLength(segment.Length))
            {
                value = default;
                return false;
            }

            // Segment needs to be unescaped
            if (isEscaped)
            {
                return TryGetEscapedDateTimeOffset(segment, out value);
            }

            Debug.Assert(segment.IndexOf(JsonConstants.BackSlash) == -1);

            if (JsonHelpers.TryParseAsISO(segment, out DateTimeOffset tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetEscapedDateTimeOffset(ReadOnlySpan<byte> source, out DateTimeOffset value)
        {
            Debug.Assert(source.Length <= JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
            Span<byte> sourceUnescaped = stackalloc byte[JsonConstants.MaximumEscapedDateTimeOffsetParseLength];

            Unescape(source, sourceUnescaped, out int written);
            Debug.Assert(written > 0);

            sourceUnescaped = sourceUnescaped.Slice(0, written);
            Debug.Assert(!sourceUnescaped.IsEmpty);

            if (JsonHelpers.IsValidUnescapedDateTimeOffsetParseLength(sourceUnescaped.Length)
                && JsonHelpers.TryParseAsISO(sourceUnescaped, out DateTimeOffset tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetValue(ReadOnlySpan<byte> segment, bool isEscaped, out Guid value)
        {
            if (segment.Length > JsonConstants.MaximumEscapedGuidLength)
            {
                value = default;
                return false;
            }

            // Segment needs to be unescaped
            if (isEscaped)
            {
                return TryGetEscapedGuid(segment, out value);
            }

            Debug.Assert(segment.IndexOf(JsonConstants.BackSlash) == -1);

            if (segment.Length == JsonConstants.MaximumFormatGuidLength
                && Utf8Parser.TryParse(segment, out Guid tmp, out _, 'D'))
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

            Span<byte> utf8Unescaped = stackalloc byte[JsonConstants.MaximumEscapedGuidLength];
            Unescape(source, utf8Unescaped, out int written);
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

#if NET
        public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out Half value)
        {
            if (span.Length == 3)
            {
                if (span.SequenceEqual(JsonConstants.NaNValue))
                {
                    value = Half.NaN;
                    return true;
                }
            }
            else if (span.Length == 8)
            {
                if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
                {
                    value = Half.PositiveInfinity;
                    return true;
                }
            }
            else if (span.Length == 9)
            {
                if (span.SequenceEqual(JsonConstants.NegativeInfinityValue))
                {
                    value = Half.NegativeInfinity;
                    return true;
                }
            }

            value = default;
            return false;
        }
#endif

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
