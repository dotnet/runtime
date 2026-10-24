// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/command.h, command.c and prefix.h.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>An insert-and-copy command produced by the backward reference search.</summary>
internal struct Command
{
    public uint InsertLen;
    /// <summary>Stores copy_len in low 25 bits and copy_code - copy_len in high 7 bit.</summary>
    public uint CopyLen;
    /// <summary>Stores distance extra bits.</summary>
    public uint DistExtra;
    public ushort CmdPrefix;
    /// <summary>Stores distance code in low 10 bits and number of extra bits in high 6 bits.</summary>
    public ushort DistPrefix;

    public static ReadOnlySpan<uint> InsBase =>
    [
        0, 1, 2, 3, 4, 5, 6, 8, 10, 14, 18, 26,
        34, 50, 66, 98, 130, 194, 322, 578, 1090, 2114, 6210, 22594,
    ];

    public static ReadOnlySpan<uint> InsExtra =>
    [
        0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10, 12, 14, 24,
    ];

    public static ReadOnlySpan<uint> CopyBase =>
    [
        2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 18,
        22, 30, 38, 54, 70, 102, 134, 198, 326, 582, 1094, 2118,
    ];

    public static ReadOnlySpan<uint> CopyExtra =>
    [
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10, 24,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetInsertLengthCode(nuint insertlen)
    {
        if (insertlen < 6)
        {
            return (ushort)insertlen;
        }
        else if (insertlen < 130)
        {
            uint nbits = FastLog.Log2FloorNonZero(insertlen - 2) - 1u;
            return (ushort)((nbits << 1) + (uint)((insertlen - 2) >> (int)nbits) + 2);
        }
        else if (insertlen < 2114)
        {
            return (ushort)(FastLog.Log2FloorNonZero(insertlen - 66) + 10);
        }
        else if (insertlen < 6210)
        {
            return 21;
        }
        else if (insertlen < 22594)
        {
            return 22;
        }
        else
        {
            return 23;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetCopyLengthCode(nuint copylen)
    {
        if (copylen < 10)
        {
            return (ushort)(copylen - 2);
        }
        else if (copylen < 134)
        {
            uint nbits = FastLog.Log2FloorNonZero(copylen - 6) - 1u;
            return (ushort)((nbits << 1) + (uint)((copylen - 6) >> (int)nbits) + 4);
        }
        else if (copylen < 2118)
        {
            return (ushort)(FastLog.Log2FloorNonZero(copylen - 70) + 12);
        }
        else
        {
            return 23;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort CombineLengthCodes(ushort inscode, ushort copycode, bool useLastDistance)
    {
        ushort bits64 = (ushort)((copycode & 0x7u) | ((inscode & 0x7u) << 3));
        if (useLastDistance && inscode < 8u && copycode < 16u)
        {
            return (copycode < 8u) ? bits64 : (ushort)(bits64 | 64u);
        }
        else
        {
            // Specification: 5 Encoding of ... (last table)
            // offset = 2 * index, where index is in range [0..8]
            uint offset = 2u * ((uint)(copycode >> 3) + 3u * (uint)(inscode >> 3));
            // All values in specification are K * 64,
            // where   K = [2, 3, 6, 4, 5, 8, 7, 9, 10],
            //     i + 1 = [1, 2, 3, 4, 5, 6, 7, 8,  9],
            // K - i - 1 = [1, 1, 3, 0, 0, 2, 0, 1,  2] = D.
            // All values in D require only 2 bits to encode.
            // Magic constant is shifted 6 bits left, to avoid final multiplication.
            offset = (offset << 5) + 0x40u + ((0x520D40u >> (int)offset) & 0xC0u);
            return (ushort)(offset | bits64);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetLengthCode(nuint insertlen, nuint copylen, bool useLastDistance, out ushort code)
    {
        ushort inscode = GetInsertLengthCode(insertlen);
        ushort copycode = GetCopyLengthCode(copylen);
        code = CombineLengthCodes(inscode, copycode, useLastDistance);
    }

    public static uint GetInsertBase(ushort inscode) => InsBase[inscode];
    public static uint GetInsertExtra(ushort inscode) => InsExtra[inscode];
    public static uint GetCopyBase(ushort copycode) => CopyBase[copycode];
    public static uint GetCopyExtra(ushort copycode) => CopyExtra[copycode];

    /// <summary>Port of PrefixEncodeCopyDistance (c/enc/prefix.h).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrefixEncodeCopyDistance(nuint distanceCode, nuint numDirectCodes, nuint postfixBits, out ushort code, out uint extraBits)
    {
        if (distanceCode < BrotliConstants.NumDistanceShortCodes + numDirectCodes)
        {
            code = (ushort)distanceCode;
            extraBits = 0;
        }
        else
        {
            nuint dist = ((nuint)1 << (int)(postfixBits + 2u)) + (distanceCode - BrotliConstants.NumDistanceShortCodes - numDirectCodes);
            nuint bucket = FastLog.Log2FloorNonZero(dist) - 1;
            nuint postfixMask = (1u << (int)postfixBits) - 1;
            nuint postfix = dist & postfixMask;
            nuint prefix = (dist >> (int)bucket) & 1;
            nuint offset = (2 + prefix) << (int)bucket;
            nuint nbits = bucket - postfixBits;
            code = (ushort)((nbits << 10) |
                (BrotliConstants.NumDistanceShortCodes + numDirectCodes + ((2 * (nbits - 1) + prefix) << (int)postfixBits) + postfix));
            extraBits = (uint)((dist - offset) >> (int)postfixBits);
        }
    }

    /// <summary>Port of InitCommand.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Init(in DistanceParams dist, nuint insertlen, nuint copylen, int copylenCodeDelta, nuint distanceCode)
    {
        // Don't rely on signed int representation, use honest casts.
        uint delta = (byte)(sbyte)copylenCodeDelta;
        InsertLen = (uint)insertlen;
        CopyLen = (uint)(copylen | ((nuint)delta << 25));
        // The distance prefix and extra bits are stored in this Command as if
        // npostfix and ndirect were 0, they are only recomputed later after the
        // clustering if needed.
        PrefixEncodeCopyDistance(distanceCode, dist.NumDirectDistanceCodes, dist.DistancePostfixBits, out DistPrefix, out DistExtra);
        GetLengthCode(insertlen, (nuint)((int)copylen + copylenCodeDelta), (DistPrefix & 0x3FF) == 0, out CmdPrefix);
    }

    /// <summary>Port of InitInsertCommand.</summary>
    public void InitInsert(nuint insertlen)
    {
        InsertLen = (uint)insertlen;
        CopyLen = 4 << 25;
        DistExtra = 0;
        DistPrefix = BrotliConstants.NumDistanceShortCodes;
        GetLengthCode(insertlen, 4, false, out CmdPrefix);
    }

    /// <summary>Port of CommandRestoreDistanceCode.</summary>
    public readonly uint RestoreDistanceCode(in DistanceParams dist)
    {
        if ((DistPrefix & 0x3FFu) < BrotliConstants.NumDistanceShortCodes + dist.NumDirectDistanceCodes)
        {
            return DistPrefix & 0x3FFu;
        }
        else
        {
            uint dcode = DistPrefix & 0x3FFu;
            uint nbits = (uint)DistPrefix >> 10;
            uint extra = DistExtra;
            uint postfixMask = (1U << (int)dist.DistancePostfixBits) - 1U;
            uint hcode = (dcode - dist.NumDirectDistanceCodes - BrotliConstants.NumDistanceShortCodes) >> (int)dist.DistancePostfixBits;
            uint lcode = (dcode - dist.NumDirectDistanceCodes - BrotliConstants.NumDistanceShortCodes) & postfixMask;
            uint offset = ((2U + (hcode & 1U)) << (int)nbits) - 4U;
            return ((offset + extra) << (int)dist.DistancePostfixBits) + lcode + dist.NumDirectDistanceCodes + BrotliConstants.NumDistanceShortCodes;
        }
    }

    /// <summary>Port of CommandDistanceContext.</summary>
    public readonly uint DistanceContext()
    {
        uint r = (uint)CmdPrefix >> 6;
        uint c = CmdPrefix & 7u;
        if ((r == 0 || r == 2 || r == 4 || r == 7) && (c <= 2))
        {
            return c;
        }
        return 3;
    }

    /// <summary>Port of CommandCopyLen.</summary>
    public readonly uint CopyLength => CopyLen & 0x1FFFFFF;

    /// <summary>Port of CommandCopyLenCode.</summary>
    public readonly uint CopyLenCode()
    {
        uint modifier = CopyLen >> 25;
        int delta = (sbyte)(byte)(modifier | ((modifier & 0x40) << 1));
        return (uint)((int)(CopyLen & 0x1FFFFFF) + delta);
    }
}
