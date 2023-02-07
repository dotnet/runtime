// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        public static (int, int) CountNewLines(ReadOnlySpan<byte> data)
        {
            int lastLineFeedIndex = data.LastIndexOf(JsonConstants.LineFeed);
            int newLines = 0;

            if (lastLineFeedIndex >= 0)
            {
                newLines = 1;
                data = data.Slice(0, lastLineFeedIndex);
#if NET8_OR_GREATER
                newLines += data.Count(JsonConstants.LineFeed);
#else
                int pos;
                while ((pos = data.IndexOf(JsonConstants.LineFeed)) >= 0)
                {
                    newLines++;
                    data = data.Slice(pos + 1);
                }
#endif
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

        // https://tools.ietf.org/html/rfc8259
        // Does the span contain '"', '\',  or any control characters (i.e. 0 to 31)
        // IndexOfAny(34, 92, < 32)
        // Borrowed and modified from SpanHelpers.Byte:
        // https://github.com/dotnet/corefx/blob/fc169cddedb6820aaabbdb8b7bece2a3df0fd1a5/src/Common/src/CoreLib/System/SpanHelpers.Byte.cs#L473-L604
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
        {
            return IndexOfOrLessThan(
                    ref MemoryMarshal.GetReference(span),
                    JsonConstants.Quote,
                    JsonConstants.BackSlash,
                    lessThan: 32,   // Space ' '
                    span.Length);
        }

        private static unsafe int IndexOfOrLessThan(ref byte searchSpace, byte value0, byte value1, byte lessThan, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uLessThan = lessThan; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)0; // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)length;

            if (!Vector128.IsHardwareAccelerated || length < Vector128<byte>.Count)
            {
                uint lookUp;
                while ((byte*)nLength >= (byte*)8)
                {
                    nLength -= 8;

                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 1);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found1;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 2);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found2;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 3);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found3;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 4);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found4;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 5);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found5;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 6);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found6;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 7);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found7;

                    index += 8;
                }

                if ((byte*)nLength >= (byte*)4)
                {
                    nLength -= 4;

                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 1);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found1;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 2);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found2;
                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 3);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found3;

                    index += 4;
                }

                while ((byte*)nLength > (byte*)0)
                {
                    nLength -= 1;

                    lookUp = Unsafe.AddByteOffset(ref searchSpace, index);
                    if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                        goto Found;

                    index += 1;
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
            {
                // Get comparison Vectors
                Vector256<byte> values0 = Vector256.Create(value0);
                Vector256<byte> values1 = Vector256.Create(value1);
                Vector256<byte> valuesLessThan = Vector256.Create(lessThan);

                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    var vData = Vector256.LoadUnsafe(ref currentSearchSpace);
                    var vMatches = Vector256.BitwiseOr(
                                    Vector256.BitwiseOr(
                                        Vector256.Equals(vData, values0),
                                        Vector256.Equals(vData, values1)),
                                    Vector256.LessThan(vData, valuesLessThan));

                    if (vMatches == Vector256<byte>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<byte>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, vMatches);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<byte>.Count != 0)
                {
                    var vData = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                    var vMatches = Vector256.BitwiseOr(
                                    Vector256.BitwiseOr(
                                        Vector256.Equals(vData, values0),
                                        Vector256.Equals(vData, values1)),
                                    Vector256.LessThan(vData, valuesLessThan));

                    if (vMatches != Vector256<byte>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, vMatches);
                    }
                }
            }
            else
            {
                // Get comparison Vectors
                Vector128<byte> values0 = Vector128.Create(value0);
                Vector128<byte> values1 = Vector128.Create(value1);
                Vector128<byte> valuesLessThan = Vector128.Create(lessThan);

                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    var vData = Vector128.LoadUnsafe(ref currentSearchSpace);
                    var vMatches = Vector128.BitwiseOr(
                                    Vector128.BitwiseOr(
                                        Vector128.Equals(vData, values0),
                                        Vector128.Equals(vData, values1)),
                                    Vector128.LessThan(vData, valuesLessThan));

                    if (vMatches == Vector128<byte>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<byte>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, vMatches);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector128<byte>.Count != 0)
                {
                    var vData = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                    var vMatches = Vector128.BitwiseOr(
                                    Vector128.BitwiseOr(
                                        Vector128.Equals(vData, values0),
                                        Vector128.Equals(vData, values1)),
                                    Vector128.LessThan(vData, valuesLessThan));

                    if (vMatches != Vector128<byte>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, vMatches);
                    }
                }
            }
            return -1;
        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return (int)(byte*)index;
        Found1:
            return (int)(byte*)(index + 1);
        Found2:
            return (int)(byte*)(index + 2);
        Found3:
            return (int)(byte*)(index + 3);
        Found4:
            return (int)(byte*)(index + 4);
        Found5:
            return (int)(byte*)(index + 5);
        Found6:
            return (int)(byte*)(index + 6);
        Found7:
            return (int)(byte*)(index + 7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex(ref byte searchSpace, ref byte current, Vector256<byte> equals)
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)(Unsafe.ByteOffset(ref searchSpace, ref current) / sizeof(byte));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex(ref byte searchSpace, ref byte current, Vector128<byte> equals)
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)(Unsafe.ByteOffset(ref searchSpace, ref current) / sizeof(byte));
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
