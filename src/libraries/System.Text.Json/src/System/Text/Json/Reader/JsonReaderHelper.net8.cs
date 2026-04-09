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
            // Fast path for " in the first 16 bytes
            if (Vector128.IsHardwareAccelerated && span.Length >= 16)
            {
                ref byte ptr = ref MemoryMarshal.GetReference(span);
                Vector128<byte> matches = Vector128.Equals(Vector128.LoadUnsafe(ref ptr), Vector128.Create((byte)'"'));
                if (AdvSimd.IsSupported)
                {
                    // TODO: use Vector128.IndexOf for both AdvSimd and Sse2 once
                    ulong mask = AdvSimd.ShiftRightLogicalNarrowingLower(matches.AsUInt16(), 4).AsUInt64().ToScalar();
                    if (mask != 0)
                    {
                        return BitOperations.TrailingZeroCount(mask) >> 2;
                    }
                }
                else
                {
                    uint mask = matches.ExtractMostSignificantBits();
                    if (mask != 0)
                    {
                        return BitOperations.TrailingZeroCount(mask);
                    }
                }
            }
            return span.IndexOfAny(s_controlQuoteBackslash);
        }
    }
}
