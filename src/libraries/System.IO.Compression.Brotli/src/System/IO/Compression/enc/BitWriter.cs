// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/write_bits.h.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

internal static class BitWriter
{
    /// <summary>
    /// Writes <paramref name="nBits"/> (at most 56) bits of <paramref name="bits"/> at bit position <paramref name="pos"/>.
    /// The bits beyond the current position in <paramref name="array"/> must be zero, and the array must have at least
    /// 8 bytes available starting at byte <c>pos / 8</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBits(nuint nBits, ulong bits, ref nuint pos, Span<byte> array)
    {
        Debug.Assert((bits >> (int)nBits) == 0);
        Debug.Assert(nBits <= 56);
        // This can write up to 56 bits at a time, 7 bits are lost by being perhaps already in array[p] and at least
        // 1 bit is needed to initialize the bit-stream ahead (i.e. if 7 bits are in array[p] and we write 57 bits,
        // then the next write will access a byte that was never initialized).
        Span<byte> p = array.Slice((int)(pos >> 3), 8);
        ulong v = p[0];
        v |= bits << (int)(pos & 7);
        BinaryPrimitives.WriteUInt64LittleEndian(p, v);
        pos += nBits;
    }

    public static void WriteBitsPrepareStorage(nuint pos, Span<byte> array)
    {
        Debug.Assert((pos & 7) == 0);
        array[(int)(pos >> 3)] = 0;
    }
}
