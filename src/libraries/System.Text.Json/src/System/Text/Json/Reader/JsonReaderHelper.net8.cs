// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009"u8 +
            "\u000A\u000B\u000C\u000D\u000E\u000F\u0010\u0011\u0012\u0013"u8 +
            "\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D"u8 +
            "\u001E\u001F\"\\"u8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
        {
            // Per requirements: focus on first 32 bytes
            if (!AdvSimd.IsSupported || span.Length < 32)
            {
                return span.IndexOfAny(s_controlQuoteBackslash);
            }

            Vector128<byte> ControlBound = Vector128.Create((byte)32);
            Vector128<byte> VecQuote = Vector128.Create((byte)'"');
            Vector128<byte> VecBackslash = Vector128.Create((byte)'\\');

            ref byte ptr = ref MemoryMarshal.GetReference(span);

            // --- BLOCK 1 (Likely Hit) ---
            Vector128<byte> v1 = Vector128.LoadUnsafe(ref ptr);

            // Combine all checks into one mask
            Vector128<byte> m1 = Vector128.LessThan(v1, ControlBound) |
                                 Vector128.Equals(v1, VecQuote) |
                                 Vector128.Equals(v1, VecBackslash);

            // Narrow to 64-bit scalar: Each 4 bits in 'mask1' represents 1 byte in 'v1'
            ulong mask1 = AdvSimd.ShiftRightLogicalNarrowingLower(m1.AsUInt16(), 4).AsUInt64().ToScalar();

            // Single scalar branch (CBNZ on ARM64)
            if (mask1 != 0)
            {
                // TrailingZeroCount / 4 gives the byte index (0-15)
                return BitOperations.TrailingZeroCount(mask1) >> 2;
            }

            // --- BLOCK 2 ---
            Vector128<byte> v2 = Vector128.LoadUnsafe(ref ptr, 16);
            Vector128<byte> m2 = Vector128.LessThan(v2, ControlBound) |
                                 Vector128.Equals(v2, VecQuote) |
                                 Vector128.Equals(v2, VecBackslash);

            ulong mask2 = AdvSimd.ShiftRightLogicalNarrowingLower(m2.AsUInt16(), 4).AsUInt64().ToScalar();

            if (mask2 != 0)
            {
                return 16 + (BitOperations.TrailingZeroCount(mask2) >> 2);
            }

            return span.Slice(32).IndexOfAny(s_controlQuoteBackslash);
        }
    }
}
