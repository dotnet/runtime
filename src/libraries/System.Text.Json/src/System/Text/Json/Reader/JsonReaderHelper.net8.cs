// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        /// <summary>'"', '\',  or any control characters (i.e. 0 to 31).</summary>
        /// <remarks>https://tools.ietf.org/html/rfc8259</remarks>
        private static readonly SearchValues<byte> s_controlQuoteBackslash = SearchValues.Create(
            // Any Control, < 32 (' ')
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A\u000B\u000C\u000D\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F"u8 +
            // Quote
            "\""u8 +
            // Backslash
            "\\"u8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
        {
            // For most inputs, we have a large span and we typically find a match within the first 16 bytes
            // Usually, it's a quote in a property name.
            if (!Vector128.IsHardwareAccelerated || !BitConverter.IsLittleEndian || span.Length < Vector128<byte>.Count)
                return IndexOfQuoteOrAnyControlOrBackSlash_Fallback(span);

#pragma warning disable SYSLIB5003 // SVE is experimental
            if (Sve.IsSupported)
            {
                // SVE: Use predicated comparisons + brkb + cntp to find the index.
                // The result flows through predicate registers directly to a scalar cntp —
                // no SIMD-to-GP transfer (UMOV) is needed at all.
                // On V2: cmpeq(4cy) + cmplo(4cy) + orr(1cy) + brkb(2cy) + cntp(2cy)
                ref byte searchSpace = ref MemoryMarshal.GetReference(span);
                unsafe
                {
                    fixed (byte* ptr = &searchSpace)
                    {
                        Vector<byte> mask16 = Sve.CreateTrueMaskByte(SveMaskPattern.VectorCount16);
                        Vector<byte> data = Sve.LoadVector(mask16, ptr);

                        Vector<byte> combined = Sve.CompareEqual(data, new Vector<byte>((byte)'"'))
                                              | Sve.CompareEqual(data, new Vector<byte>((byte)'\\'))
                                              | Sve.CompareLessThan(data, new Vector<byte>((byte)0x20));

                        if (Sve.TestAnyTrue(mask16, combined))
                        {
                            // brkb: sets predicate bits BEFORE the first match
                            // cntp: counts those bits = index of first match
                            Vector<byte> beforeFirst = Sve.CreateBreakBeforeMask(mask16, combined);
                            return (int)Sve.GetActiveElementCount(mask16, beforeFirst);
                        }
                    }
                }
            }
#pragma warning restore SYSLIB5003 // SVE is experimental

            Vector128<byte> vec = Vector128.Create(span);

            // Any control character (i.e. 0 to 31) or '"' or '\'
            Vector128<byte> cmp = Vector128.LessThan(vec, Vector128.Create(JsonConstants.Space)) |
                                  Vector128.Equals(vec, Vector128.Create(JsonConstants.Quote)) |
                                  Vector128.Equals(vec, Vector128.Create(JsonConstants.BackSlash));

            // TODO: this really should be just Vector128.IndexOfWhereAllBitsSet
            // but that is not currently optimized in JIT for ARM64, so we do it manually here.
            if (AdvSimd.IsSupported)
            {
                ulong mask = AdvSimd.ShiftRightLogicalNarrowingLower(cmp.AsUInt16(), 4).AsUInt64().ToScalar();
                if (mask != 0)
                    return BitOperations.TrailingZeroCount(mask) >> 2;
            }
            else
            {
                uint mask = cmp.ExtractMostSignificantBits();
                if (mask != 0)
                    return BitOperations.TrailingZeroCount(mask);
            }

            int fallbackIndex = IndexOfQuoteOrAnyControlOrBackSlash_Fallback(span.Slice(Vector128<byte>.Count));
            return fallbackIndex >= 0 ? fallbackIndex + Vector128<byte>.Count : fallbackIndex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfQuoteOrAnyControlOrBackSlash_Fallback(ReadOnlySpan<byte> span) =>
            span.IndexOfAny(s_controlQuoteBackslash);
    }
}
