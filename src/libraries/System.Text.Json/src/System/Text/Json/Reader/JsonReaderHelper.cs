// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

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

        private static int IndexOfOrLessThanNonVector(ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                byte value = span[i];
                if (value == JsonConstants.Quote || value == JsonConstants.BackSlash || JsonConstants.Space > value)
                    return i;
            }

            return -1;
        }

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

        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(Vector<byte> match)
        {
            var vector64 = Vector.AsVectorUInt64(match);
            ulong candidate = 0;
            int i = 0;
            // Pattern unrolled by jit https://github.com/dotnet/coreclr/pull/8001
            for (; i < Vector<ulong>.Count; i++)
            {
                candidate = vector64[i];
                if (candidate != 0)
                {
                    break;
                }
            }

            // Single LEA instruction with jitted const (using function result)
            return i * 8 + LocateFirstFoundByte(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(ulong match)
        {
            // Flag least significant power of two bit
            var powerOfTwoFlag = match ^ (match - 1);
            // Shift all powers of two into the high byte and extract
            return (int)((powerOfTwoFlag * XorPowerOfTwoToHighByte) >> 57);
        }

        private const ulong XorPowerOfTwoToHighByte = (0x07ul |
                                               0x06ul << 8 |
                                               0x05ul << 16 |
                                               0x04ul << 24 |
                                               0x03ul << 32 |
                                               0x02ul << 40 |
                                               0x01ul << 48) + 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte AddByteOffset(ref byte start, nuint offset)
            => ref Unsafe.AddByteOffset(ref start, (IntPtr)(nint)offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<byte> LoadVector(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref start, (IntPtr)(nint)offset));
    }
}
