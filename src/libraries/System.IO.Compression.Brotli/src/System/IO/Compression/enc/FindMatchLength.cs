// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/find_match_length.h.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

internal static class FindMatchLength
{
    /// <summary>
    /// Returns the length of the common prefix of <paramref name="s1"/> and <paramref name="s2"/>, but at most <paramref name="limit"/>.
    /// Both spans must contain at least <paramref name="limit"/> bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint FindMatchLengthWithLimit(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2, nuint limit)
    {
        int lim = (int)limit;
        s1 = s1.Slice(0, lim);
        s2 = s2.Slice(0, lim);
        if (lim >= 16)
        {
            // Most matches are short: check the first 16 bytes with scalar loads, then switch to the vectorized helper.
            ulong x = BinaryPrimitives.ReadUInt64LittleEndian(s2) ^ BinaryPrimitives.ReadUInt64LittleEndian(s1);
            if (x != 0)
            {
                return (nuint)(ulong.TrailingZeroCount(x) >> 3);
            }
            x = BinaryPrimitives.ReadUInt64LittleEndian(s2.Slice(8)) ^ BinaryPrimitives.ReadUInt64LittleEndian(s1.Slice(8));
            if (x != 0)
            {
                return (nuint)(8 + (ulong.TrailingZeroCount(x) >> 3));
            }
            return (nuint)(16 + s1.Slice(16).CommonPrefixLength(s2.Slice(16)));
        }

        int matched = 0;
        if (lim >= 8)
        {
            ulong x = BinaryPrimitives.ReadUInt64LittleEndian(s2) ^ BinaryPrimitives.ReadUInt64LittleEndian(s1);
            if (x != 0)
            {
                return (nuint)(ulong.TrailingZeroCount(x) >> 3);
            }
            matched = 8;
        }
        while ((uint)matched < (uint)s1.Length && (uint)matched < (uint)s2.Length && s1[matched] == s2[matched])
        {
            matched++;
        }
        return (nuint)matched;
    }
}
