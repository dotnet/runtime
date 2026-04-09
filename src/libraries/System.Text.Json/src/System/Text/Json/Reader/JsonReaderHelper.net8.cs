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
            if (!Vector128.IsHardwareAccelerated || span.Length < 16)
                return IndexOfQuoteOrAnyControlOrBackSlash_Fallback(span);

            Vector128<byte> vec = Vector128.Create(span);

            // Any control character (i.e. 0 to 31) or '"' or '\'
            Vector128<byte> cmp = Vector128.LessThan(vec, Vector128.Create((byte)32)) |
                                  Vector128.Equals(vec, Vector128.Create((byte)'"')) |
                                  Vector128.Equals(vec, Vector128.Create((byte)'\\'));

            // TODO: this really should be just Vector128.IndexOfWhereAllBitsSet
            // but that is not currently optimized in JIT for ARM64, so we do it manually here.
            if (AdvSimd.IsSupported)
            {
                if (cmp != Vector128<byte>.Zero)
                {
                    ulong mask = AdvSimd.ShiftRightLogicalNarrowingLower(cmp.AsUInt16(), 4).AsUInt64().ToScalar();
                    return BitOperations.TrailingZeroCount(mask) >> 2;
                }
            }
            else
            {
                uint mask = cmp.ExtractMostSignificantBits();
                if (mask != 0)
                    return BitOperations.TrailingZeroCount(mask);
            }

            int fallbackIndex = IndexOfQuoteOrAnyControlOrBackSlash_Fallback(span.Slice(16));
            return fallbackIndex >= 0 ? fallbackIndex + 16 : fallbackIndex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfQuoteOrAnyControlOrBackSlash_Fallback(ReadOnlySpan<byte> span) =>
            span.IndexOfAny(s_controlQuoteBackslash);
    }
}
