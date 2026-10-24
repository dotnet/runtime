// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/compress_fragment.c and compress_fragment.h.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Scratch state of the one-pass (quality 0) fragment compressor (BrotliOnePassArena).</summary>
internal sealed class BrotliOnePassArena
{
    public readonly byte[] LitDepth = new byte[256];
    public readonly ushort[] LitBits = new ushort[256];

    // Command and distance prefix codes (each 64 symbols, stored back-to-back)
    // used for the next block. The command prefix code is over a smaller alphabet
    // with the following 64 symbols:
    //    0 - 15: insert length code 0, copy length code 0 - 15, same distance
    //   16 - 39: insert length code 0, copy length code 0 - 23
    //   40 - 63: insert length code 0 - 23, copy length code 0
    // Note that symbols 16 and 40 represent the same code in the full alphabet,
    // but we do not use either of them.
    public readonly byte[] CmdDepth = new byte[128];
    public readonly ushort[] CmdBits = new ushort[128];
    public readonly uint[] CmdHisto = new uint[128];

    // The compressed form of the command and distance prefix codes for the next block.
    public readonly byte[] CmdCode = new byte[512];
    public nuint CmdCodeNumbits;

    public readonly HuffmanTree[] Tree = new HuffmanTree[2 * BrotliConstants.NumLiteralSymbols + 1];
    public readonly uint[] Histogram = new uint[256];
    public readonly byte[] TmpDepth = new byte[BrotliConstants.NumCommandSymbols];
    public readonly ushort[] TmpBits = new ushort[64];

    /// <summary>The <c>small_table_</c> hash table of the encoder state (kept with the arena of the fast modes).</summary>
    public readonly int[] SmallTable = new int[1 << 10];
}

/// <summary>
/// Function for fast encoding of an input fragment, independently from the input
/// history. This function uses one-pass processing: when we find a backward
/// match, we immediately emit the corresponding command and literal codes to
/// the bit stream.
/// </summary>
/// <remarks>
/// Adapted from the CompressFragment() function in https://github.com/google/snappy/blob/master/snappy.cc
/// Pointers into the input fragment are represented as offsets relative to the start of the fragment (C <c>base_ip</c>).
/// </remarks>
internal static class CompressFragment
{
    /// <summary>BROTLI_MAX_BACKWARD_LIMIT(18).</summary>
    internal const int MaxDistance = (1 << 18) - BrotliConstants.WindowGap;

    // Acceptable loss for uncompressible speedup is 2%
    private const nuint MinRatio = 980;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hash(ReadOnlySpan<byte> data, int p, int shift)
    {
        ulong h = (BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(p, 8)) << 24) * HashBase.HashMul32;
        return (uint)(h >> shift);
    }

    /// <summary>FindMatchLengthWithLimit(data + s1, data + s2, limit); kept out of line to keep the main loop small.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint FindMatchLengthAt(ReadOnlySpan<byte> data, int s1, int s2, nuint limit) =>
        FindMatchLength.FindMatchLengthWithLimit(data.Slice(s1), data.Slice(s2), limit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytesAtOffset(ulong v, int offset, int shift)
    {
        Debug.Assert(offset >= 0);
        Debug.Assert(offset <= 3);
        ulong h = ((v >> (8 * offset)) << 24) * HashBase.HashMul32;
        return (uint)(h >> shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Load64(ReadOnlySpan<byte> data, int p) => BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(p, 8));

    /// <summary>Hash of the 8 bytes loaded at some position (same as <see cref="Hash"/>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytes(ulong bytes, int shift)
    {
        ulong h = (bytes << 24) * HashBase.HashMul32;
        return (uint)(h >> shift);
    }

    /// <summary>
    /// IsMatch(p1, p2): compares the first 5 bytes. The arguments are little-endian loads of (at least) 5 bytes at
    /// p1 and p2 (the C code compares 4 + 1 bytes; the result is the same).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatch(ulong p1Bytes, ulong p2Bytes) => ((p1Bytes ^ p2Bytes) & 0xFF_FFFF_FFFF) == 0;

    /// <summary>
    /// Builds a literal prefix code into "depths" and "bits" based on the statistics
    /// of the "input" string and stores it into the bit stream.
    /// Note that the prefix code here is built from the pre-LZ77 input, therefore
    /// we can only approximate the statistics of the actual literal stream.
    /// Moreover, for long inputs we build a histogram from a sample of the input
    /// and thus have to assign a non-zero depth for each literal.
    /// Returns estimated compression ratio millibytes/char for encoding given input
    /// with generated code.
    /// </summary>
    /// <remarks>The C "depths" / "bits" arguments are always <c>s-&gt;lit_depth</c> / <c>s-&gt;lit_bits</c>.</remarks>
    private static nuint BuildAndStoreLiteralPrefixCode(BrotliOnePassArena s, ReadOnlySpan<byte> input, nuint inputSize,
        ref nuint storageIx, Span<byte> storage)
    {
        Span<byte> depths = s.LitDepth;
        Span<ushort> bits = s.LitBits;
        Span<uint> histogram = s.Histogram;
        nuint histogramTotal;
        histogram.Clear();

        if (inputSize < (1 << 15))
        {
            ReadOnlySpan<byte> inp = input.Slice(0, (int)inputSize);
            for (int i = 0; i < inp.Length; ++i)
            {
                ++histogram[inp[i]];
            }
            histogramTotal = inputSize;
            for (int i = 0; i < 256; ++i)
            {
                // We weigh the first 11 samples with weight 3 to account for the
                // balancing effect of the LZ77 phase on the histogram.
                uint adjust = 2 * Math.Min(histogram[i], 11u);
                histogram[i] += adjust;
                histogramTotal += adjust;
            }
        }
        else
        {
            const nuint SampleRate = 29;
            for (nuint i = 0; i < inputSize; i += SampleRate)
            {
                ++histogram[input[(int)i]];
            }
            histogramTotal = (inputSize + SampleRate - 1) / SampleRate;
            for (int i = 0; i < 256; ++i)
            {
                // We add 1 to each population count to avoid 0 bit depths (since this is
                // only a sample and we don't know if the symbol appears or not), and we
                // weigh the first 11 samples with weight 3 to account for the balancing
                // effect of the LZ77 phase on the histogram (more frequent symbols are
                // more likely to be in backward references instead as literals).
                uint adjust = 1 + 2 * Math.Min(histogram[i], 11u);
                histogram[i] += adjust;
                histogramTotal += adjust;
            }
        }
        BrotliBitStream.BuildAndStoreHuffmanTreeFast(s.Tree, histogram, histogramTotal, /* maxBits = */ 8,
            depths, bits, ref storageIx, storage);
        {
            nuint literalRatio = 0;
            for (int i = 0; i < 256; ++i)
            {
                if (histogram[i] != 0)
                {
                    literalRatio += (nuint)(histogram[i] * depths[i]);
                }
            }
            // Estimated encoding ratio, millibytes per symbol.
            return (literalRatio * 125) / histogramTotal;
        }
    }

    /// <summary>
    /// Builds a command and distance prefix code (each 64 symbols) into "depth" and
    /// "bits" based on "histogram" and stores it into the bit stream.
    /// </summary>
    private static void BuildAndStoreCommandPrefixCode(BrotliOnePassArena s, ref nuint storageIx, Span<byte> storage)
    {
        ReadOnlySpan<uint> histogram = s.CmdHisto;
        Span<byte> depth = s.CmdDepth;
        Span<ushort> bits = s.CmdBits;
        Span<byte> tmpDepth = s.TmpDepth;
        Span<ushort> tmpBits = s.TmpBits;
        tmpDepth.Clear();

        EntropyEncode.CreateHuffmanTree(histogram, 64, 15, s.Tree, depth);
        EntropyEncode.CreateHuffmanTree(histogram.Slice(64), 64, 14, s.Tree, depth.Slice(64));
        // We have to jump through a few hoops here in order to compute
        // the command bits because the symbols are in a different order than in
        // the full alphabet. This looks complicated, but having the symbols
        // in this order in the command bits saves a few branches in the Emit*
        // functions.
        depth.Slice(0, 24).CopyTo(tmpDepth);
        depth.Slice(40, 8).CopyTo(tmpDepth.Slice(24));
        depth.Slice(24, 8).CopyTo(tmpDepth.Slice(32));
        depth.Slice(48, 8).CopyTo(tmpDepth.Slice(40));
        depth.Slice(32, 8).CopyTo(tmpDepth.Slice(48));
        depth.Slice(56, 8).CopyTo(tmpDepth.Slice(56));
        EntropyEncode.ConvertBitDepthsToSymbols(tmpDepth, 64, tmpBits);
        tmpBits.Slice(0, 24).CopyTo(bits);
        tmpBits.Slice(32, 8).CopyTo(bits.Slice(24));
        tmpBits.Slice(48, 8).CopyTo(bits.Slice(32));
        tmpBits.Slice(24, 8).CopyTo(bits.Slice(40));
        tmpBits.Slice(40, 8).CopyTo(bits.Slice(48));
        tmpBits.Slice(56, 8).CopyTo(bits.Slice(56));
        EntropyEncode.ConvertBitDepthsToSymbols(depth.Slice(64), 64, bits.Slice(64));
        {
            // Create the bit length array for the full command alphabet.
            tmpDepth.Slice(0, 64).Clear(); // only 64 first values were used
            depth.Slice(0, 8).CopyTo(tmpDepth);
            depth.Slice(8, 8).CopyTo(tmpDepth.Slice(64));
            depth.Slice(16, 8).CopyTo(tmpDepth.Slice(128));
            depth.Slice(24, 8).CopyTo(tmpDepth.Slice(192));
            depth.Slice(32, 8).CopyTo(tmpDepth.Slice(384));
            for (int i = 0; i < 8; ++i)
            {
                tmpDepth[128 + 8 * i] = depth[40 + i];
                tmpDepth[256 + 8 * i] = depth[48 + i];
                tmpDepth[448 + 8 * i] = depth[56 + i];
            }
            BrotliBitStream.StoreHuffmanTree(tmpDepth, BrotliConstants.NumCommandSymbols, s.Tree, ref storageIx, storage);
        }
        BrotliBitStream.StoreHuffmanTree(depth.Slice(64), 64, s.Tree, ref storageIx, storage);
    }

    /// <summary>
    /// Equivalent to <c>BrotliWriteBits(n1, bits1); BrotliWriteBits(n2, bits2);</c> (<paramref name="n1"/> + <paramref name="n2"/> &lt;= 56),
    /// done with a single store.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteBits(nuint n1, ulong bits1, nuint n2, ulong bits2, ref nuint pos, Span<byte> storage) =>
        BitWriter.WriteBits(n1 + n2, bits1 | (bits2 << (int)n1), ref pos, storage);

    /// <summary>Equivalent to three consecutive BrotliWriteBits calls (at most 56 bits in total), done with a single store.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteBits(nuint n1, ulong bits1, nuint n2, ulong bits2, nuint n3, ulong bits3, ref nuint pos, Span<byte> storage) =>
        BitWriter.WriteBits(n1 + n2 + n3, bits1 | (bits2 << (int)n1) | (bits3 << (int)(n1 + n2)), ref pos, storage);

    // The Emit* functions below take the bit position by value and return the updated one, so that the caller
    // can keep it in a register.

    /// <summary>REQUIRES: insertlen &lt; 6210</summary>
    private static nuint EmitInsertLen(nuint insertlen, BrotliOnePassArena s, nuint storageIx, Span<byte> storage)
    {
        byte[] depth = s.CmdDepth;
        ushort[] bits = s.CmdBits;
        uint[] histo = s.CmdHisto;
        if (insertlen < 6)
        {
            int code = (int)insertlen + 40;
            BitWriter.WriteBits(depth[code], bits[code], ref storageIx, storage);
            ++histo[code];
        }
        else if (insertlen < 130)
        {
            nuint tail = insertlen - 2;
            uint nbits = FastLog.Log2FloorNonZero(tail) - 1u;
            nuint prefix = tail >> (int)nbits;
            int inscode = (int)((nbits << 1) + prefix + 42);
            WriteBits(depth[inscode], bits[inscode], nbits, tail - (prefix << (int)nbits), ref storageIx, storage);
            ++histo[inscode];
        }
        else if (insertlen < 2114)
        {
            nuint tail = insertlen - 66;
            uint nbits = FastLog.Log2FloorNonZero(tail);
            int code = (int)nbits + 50;
            WriteBits(depth[code], bits[code], nbits, tail - ((nuint)1 << (int)nbits), ref storageIx, storage);
            ++histo[code];
        }
        else
        {
            WriteBits(depth[61], bits[61], 12, insertlen - 2114, ref storageIx, storage);
            ++histo[61];
        }
        return storageIx;
    }

    private static nuint EmitLongInsertLen(nuint insertlen, BrotliOnePassArena s, nuint storageIx, Span<byte> storage)
    {
        byte[] depth = s.CmdDepth;
        ushort[] bits = s.CmdBits;
        uint[] histo = s.CmdHisto;
        if (insertlen < 22594)
        {
            WriteBits(depth[62], bits[62], 14, insertlen - 6210, ref storageIx, storage);
            ++histo[62];
        }
        else
        {
            WriteBits(depth[63], bits[63], 24, insertlen - 22594, ref storageIx, storage);
            ++histo[63];
        }
        return storageIx;
    }

    private static nuint EmitCopyLen(nuint copylen, BrotliOnePassArena s, nuint storageIx, Span<byte> storage)
    {
        byte[] depth = s.CmdDepth;
        ushort[] bits = s.CmdBits;
        uint[] histo = s.CmdHisto;
        if (copylen < 10)
        {
            int code = (int)copylen + 14;
            BitWriter.WriteBits(depth[code], bits[code], ref storageIx, storage);
            ++histo[code];
        }
        else if (copylen < 134)
        {
            nuint tail = copylen - 6;
            uint nbits = FastLog.Log2FloorNonZero(tail) - 1u;
            nuint prefix = tail >> (int)nbits;
            int code = (int)((nbits << 1) + prefix + 20);
            WriteBits(depth[code], bits[code], nbits, tail - (prefix << (int)nbits), ref storageIx, storage);
            ++histo[code];
        }
        else if (copylen < 2118)
        {
            nuint tail = copylen - 70;
            uint nbits = FastLog.Log2FloorNonZero(tail);
            int code = (int)nbits + 28;
            WriteBits(depth[code], bits[code], nbits, tail - ((nuint)1 << (int)nbits), ref storageIx, storage);
            ++histo[code];
        }
        else
        {
            WriteBits(depth[39], bits[39], 24, copylen - 2118, ref storageIx, storage);
            ++histo[39];
        }
        return storageIx;
    }

    private static nuint EmitCopyLenLastDistance(nuint copylen, BrotliOnePassArena s, nuint storageIx, Span<byte> storage)
    {
        byte[] depth = s.CmdDepth;
        ushort[] bits = s.CmdBits;
        uint[] histo = s.CmdHisto;
        if (copylen < 12)
        {
            int code = (int)copylen - 4;
            BitWriter.WriteBits(depth[code], bits[code], ref storageIx, storage);
            ++histo[code];
        }
        else if (copylen < 72)
        {
            nuint tail = copylen - 8;
            uint nbits = FastLog.Log2FloorNonZero(tail) - 1;
            nuint prefix = tail >> (int)nbits;
            int code = (int)((nbits << 1) + prefix + 4);
            WriteBits(depth[code], bits[code], nbits, tail - (prefix << (int)nbits), ref storageIx, storage);
            ++histo[code];
        }
        else if (copylen < 136)
        {
            nuint tail = copylen - 8;
            int code = (int)(tail >> 5) + 30;
            WriteBits(depth[code], bits[code], 5, tail & 31, depth[64], bits[64], ref storageIx, storage);
            ++histo[code];
            ++histo[64];
        }
        else if (copylen < 2120)
        {
            nuint tail = copylen - 72;
            uint nbits = FastLog.Log2FloorNonZero(tail);
            int code = (int)nbits + 28;
            WriteBits(depth[code], bits[code], nbits, tail - ((nuint)1 << (int)nbits), depth[64], bits[64], ref storageIx, storage);
            ++histo[code];
            ++histo[64];
        }
        else
        {
            WriteBits(depth[39], bits[39], 24, copylen - 2120, depth[64], bits[64], ref storageIx, storage);
            ++histo[39];
            ++histo[64];
        }
        return storageIx;
    }

    private static nuint EmitDistance(nuint distance, BrotliOnePassArena s, nuint storageIx, Span<byte> storage)
    {
        byte[] depth = s.CmdDepth;
        ushort[] bits = s.CmdBits;
        uint[] histo = s.CmdHisto;
        nuint d = distance + 3;
        uint nbits = FastLog.Log2FloorNonZero(d) - 1u;
        nuint prefix = (d >> (int)nbits) & 1;
        nuint offset = (2 + prefix) << (int)nbits;
        int distcode = (int)(2 * (nbits - 1) + prefix + 80);
        WriteBits(depth[distcode], bits[distcode], nbits, d - offset, ref storageIx, storage);
        ++histo[distcode];
        return storageIx;
    }

    private static nuint EmitLiterals(ReadOnlySpan<byte> input, BrotliOnePassArena s, nuint storageIx, Span<byte> storage) =>
        EmitLiterals(input, s.LitDepth, s.LitBits, storageIx, storage);

    /// <summary>
    /// Writes the prefix codes of <paramref name="input"/> (equivalent to one BrotliWriteBits call per literal).
    /// The bits are collected in a 64-bit accumulator that is stored when it is (almost) full.
    /// REQUIRES: all depths are at most 15.
    /// </summary>
    internal static nuint EmitLiterals(ReadOnlySpan<byte> input, ReadOnlySpan<byte> depth, ReadOnlySpan<ushort> bits,
        nuint storageIx, Span<byte> storage)
    {
        if (input.IsEmpty)
        {
            return storageIx;
        }
        // Same as BrotliWriteBits: the byte at the current position holds the pending (storageIx & 7) bits.
        int bytePos = (int)(storageIx >> 3);
        ulong acc = storage[bytePos];
        int accBits = (int)(storageIx & 7);
        for (int j = 0; j < input.Length; j++)
        {
            byte lit = input[j];
            Debug.Assert(depth[lit] <= 15);
            acc |= (ulong)bits[lit] << accBits;
            accBits += depth[lit];
            if (accBits >= 48)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(storage.Slice(bytePos, 8), acc);
                int fullBytes = accBits >> 3;
                bytePos += fullBytes;
                acc >>= fullBytes << 3;
                accBits &= 7;
            }
        }
        BinaryPrimitives.WriteUInt64LittleEndian(storage.Slice(bytePos, 8), acc);
        return ((nuint)bytePos << 3) + (nuint)accBits;
    }

    /// <summary>REQUIRES: len &lt;= 1 &lt;&lt; 24.</summary>
    internal static void StoreMetaBlockHeader(nuint len, bool isUncompressed, ref nuint storageIx, Span<byte> storage)
    {
        nuint nibbles = 6;
        // ISLAST
        BitWriter.WriteBits(1, 0, ref storageIx, storage);
        if (len <= (1U << 16))
        {
            nibbles = 4;
        }
        else if (len <= (1U << 20))
        {
            nibbles = 5;
        }
        BitWriter.WriteBits(2, nibbles - 4, ref storageIx, storage);
        BitWriter.WriteBits(nibbles * 4, len - 1, ref storageIx, storage);
        // ISUNCOMPRESSED
        BitWriter.WriteBits(1, isUncompressed ? 1UL : 0UL, ref storageIx, storage);
    }

    private static void UpdateBits(nuint nBits, uint bits, nuint pos, Span<byte> array)
    {
        while (nBits > 0)
        {
            nuint bytePos = pos >> 3;
            nuint nUnchangedBits = pos & 7;
            nuint nChangedBits = Math.Min(nBits, 8 - nUnchangedBits);
            nuint totalBits = nUnchangedBits + nChangedBits;
            uint mask = (~((1u << (int)totalBits) - 1u)) | ((1u << (int)nUnchangedBits) - 1u);
            uint unchangedBits = array[(int)bytePos] & mask;
            uint changedBits = bits & ((1u << (int)nChangedBits) - 1u);
            array[(int)bytePos] = (byte)((changedBits << (int)nUnchangedBits) | unchangedBits);
            nBits -= nChangedBits;
            bits >>= (int)nChangedBits;
            pos += nChangedBits;
        }
    }

    internal static void RewindBitPosition(nuint newStorageIx, ref nuint storageIx, Span<byte> storage)
    {
        nuint bitpos = newStorageIx & 7;
        nuint mask = (1u << (int)bitpos) - 1;
        storage[(int)(newStorageIx >> 3)] &= (byte)mask;
        storageIx = newStorageIx;
    }

    private static bool ShouldMergeBlock(BrotliOnePassArena s, ReadOnlySpan<byte> data, nuint len, ReadOnlySpan<byte> depths)
    {
        Span<uint> histo = s.Histogram;
        const nuint SampleRate = 43;
        histo.Clear();
        for (nuint i = 0; i < len; i += SampleRate)
        {
            ++histo[data[(int)i]];
        }
        {
            nuint total = (len + SampleRate - 1) / SampleRate;
            double r = (FastLog.FastLog2(total) + 0.5) * (double)total + 200;
            for (int i = 0; i < 256; ++i)
            {
                r -= (double)histo[i] * (depths[i] + FastLog.FastLog2(histo[i]));
            }
            return r >= 0.0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldUseUncompressedMode(int metablockStart, int nextEmit, nuint insertlen, nuint literalRatio)
    {
        nuint compressed = (nuint)(nextEmit - metablockStart);
        if (compressed * 50 > insertlen)
        {
            return false;
        }
        else
        {
            return literalRatio > MinRatio;
        }
    }

    /// <summary>Emits <c>data[begin..end)</c> as an uncompressed meta-block, overwriting everything after <paramref name="storageIxStart"/>.</summary>
    private static void EmitUncompressedMetaBlock(ReadOnlySpan<byte> data, int begin, int end, nuint storageIxStart,
        ref nuint storageIx, Span<byte> storage)
    {
        nuint len = (nuint)(end - begin);
        RewindBitPosition(storageIxStart, ref storageIx, storage);
        StoreMetaBlockHeader(len, true, ref storageIx, storage);
        storageIx = (storageIx + 7u) & ~(nuint)7u;
        data.Slice(begin, (int)len).CopyTo(storage.Slice((int)(storageIx >> 3)));
        storageIx += len << 3;
        storage[(int)(storageIx >> 3)] = 0;
    }

    /// <summary>
    /// Updates "table" with the hashes of some positions within the last copy and returns the next candidate
    /// (the previous table entry for <paramref name="ip"/>).
    /// </summary>
    /// <remarks><paramref name="ipBytes"/> receives the (5 valid) bytes at <paramref name="ip"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int UpdateTableAfterCopy(ReadOnlySpan<byte> data, int ip, Span<int> table, int shift, out ulong ipBytes)
    {
        ulong inputBytes = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(ip - 3, 8));
        ipBytes = inputBytes >> 24;
        uint prevHash = HashBytesAtOffset(inputBytes, 0, shift);
        uint curHash = HashBytesAtOffset(inputBytes, 3, shift);
        table[(int)prevHash] = ip - 3;
        prevHash = HashBytesAtOffset(inputBytes, 1, shift);
        table[(int)prevHash] = ip - 2;
        prevHash = HashBytesAtOffset(inputBytes, 2, shift);
        table[(int)prevHash] = ip - 1;

        int candidate = table[(int)curHash];
        table[(int)curHash] = ip;
        return candidate;
    }

    private static ReadOnlySpan<uint> CmdHistoSeed =>
    [
        0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 0, 0, 0, 0,
    ];

    /// <summary>
    /// Step 1 of the "emit_commands" loop: scans forward in the input looking for a 5-byte-long match.
    /// </summary>
    /// <returns>The match candidate; -1 if we got close to exhausting the input (C "goto emit_remainder").</returns>
    /// <remarks>
    /// <paramref name="ip"/>: In: position to start the search at; out: position of the match.<br/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindNextMatch(ReadOnlySpan<byte> data, Span<int> table, int shift, int lastDistance, int ipLimit,
        ref int ip)
    {
        // Heuristic match skipping: If 32 bytes are scanned with no matches
        // found, start looking only at every other byte. If 32 more bytes are
        // scanned, look at every third byte, etc.. When a match is found,
        // immediately go back to looking at every byte. This is a small loss
        // (~5% performance, ~0.1% density) for compressible data due to more
        // bookkeeping, but for non-compressible data (such as JPEG) it's a huge
        // win since the compressor quickly "realizes" the data is incompressible
        // and doesn't bother looking for matches everywhere.
        //
        // The "skip" variable keeps track of how many bytes there are since the
        // last match; dividing it by 32 (i.e. right-shifting by five) gives the
        // number of bytes to move ahead for each iteration.
        uint skip = 32;

        int nextIp = ip;
        int curIp;
        int candidate;
        ulong curBytes;
        // "nextBytes" caches the 8 bytes at "nextIp" (used both for hashing and for matching).
        ulong nextBytes = Load64(data, nextIp);
        uint nextHash = HashBytes(nextBytes, shift);
        while (true)
        {
            do
            {
                uint hash = nextHash;
                uint bytesBetweenHashLookups = skip++ >> 5;
                Debug.Assert(hash == Hash(data, nextIp, shift));
                curIp = nextIp;
                curBytes = nextBytes;
                nextIp = curIp + (int)bytesBetweenHashLookups;
                if (nextIp > ipLimit)
                {
                    ip = curIp;
                    return -1;
                }
                nextBytes = Load64(data, nextIp);
                nextHash = HashBytes(nextBytes, shift);
                candidate = curIp - lastDistance;
                if (IsMatch(curBytes, Load64(data, candidate)))
                {
                    if (candidate < curIp)
                    {
                        table[(int)hash] = curIp;
                        break;
                    }
                }
                candidate = table[(int)hash];
                Debug.Assert(candidate >= 0);
                Debug.Assert(candidate < curIp);

                table[(int)hash] = curIp;
            }
            while (!IsMatch(curBytes, Load64(data, candidate)));

            // Check copy distance. If candidate is not feasible, continue search.
            // Checking is done outside of hot loop to reduce overhead.
            if (curIp - candidate <= MaxDistance)
            {
                ip = curIp;
                return candidate;
            }
        }
    }

    /// <summary>
    /// The "emit_commands" loop of BrotliCompressFragmentFastImpl: finds matches in the current block and emits
    /// the commands and literals. Kept separate from the meta-block bookkeeping to keep the hot loop small.
    /// </summary>
    /// <param name="s">Arena.</param>
    /// <param name="data">The whole input fragment; offset 0 is the C <c>base_ip</c>.</param>
    /// <param name="input">Offset of the current block.</param>
    /// <param name="inputSize">Remaining input size starting at <paramref name="input"/>.</param>
    /// <param name="blockSize">Size of the current block.</param>
    /// <param name="nextEmit">The first byte that is not covered by a previous copy.</param>
    /// <param name="metablockStart">Start of the current meta-block.</param>
    /// <param name="mlenStorageIx">Bit position of the MLEN field of the current meta-block header.</param>
    /// <param name="literalRatio">Estimated literal compression ratio of the current meta-block.</param>
    /// <param name="table">Hash table.</param>
    /// <param name="shift">64 - log2(table size).</param>
    /// <param name="storageIx">Bit position in <paramref name="storage"/>.</param>
    /// <param name="storage">Output.</param>
    /// <returns>
    /// -1 when the block was processed completely (C "goto emit_remainder"); otherwise the meta-block was emitted
    /// uncompressed up to the returned offset (C "goto next_block").
    /// </returns>
    private static int EmitCommands(BrotliOnePassArena s, ReadOnlySpan<byte> data, int input, nuint inputSize, nuint blockSize,
        ref int nextEmit, int metablockStart, nuint mlenStorageIx, nuint literalRatio, Span<int> table, int shift,
        ref nuint storageIx, Span<byte> storage)
    {
        const nuint InputMarginBytes = BrotliConstants.WindowGap;
        const nuint MinMatchLen = 5;

        // Initialize the command and distance histograms. We will gather
        // statistics of command and distance codes during the processing
        // of this block and use it to update the command and distance
        // prefix codes for the next block.
        CmdHistoSeed.CopyTo(s.CmdHisto);

        // "ip" is the input pointer.
        int ip = input;
        int lastDistance = -1;
        int ipEnd = input + (int)blockSize;

        if (blockSize < InputMarginBytes)
        {
            return -1;
        }

        // For the last block, we need to keep a 16 bytes margin so that we can be
        // sure that all distances are at most window size - 16.
        // For all other blocks, we only need to keep a margin of 5 bytes so that
        // we don't go over the block size with a copy.
        nuint lenLimit = Math.Min(blockSize - MinMatchLen, inputSize - InputMarginBytes);
        int ipLimit = input + (int)lenLimit;

        // The bit position is kept in a local and written back on exit.
        nuint pos = storageIx;

        ++ip;
        while (true)
        {
            Debug.Assert(nextEmit < ip);
            int candidate = FindNextMatch(data, table, shift, lastDistance, ipLimit, ref ip);
            if (candidate < 0)
            {
                storageIx = pos;
                return -1;
            }

            // The (at least 5 valid) bytes at "ip" after the table update.
            ulong ipBytes;

            // Step 2: Emit the found match together with the literal bytes from
            // "nextEmit" to the bit stream, and then see if we can find a next match
            // immediately afterwards. Repeat until we find no match for the input
            // without emitting some literal bytes.
            {
                // We have a 5-byte match at ip, and we need to emit bytes in
                // [nextEmit, ip).
                int @base = ip;
                nuint matched = 5 + FindMatchLengthAt(data, candidate + 5, ip + 5, (nuint)(ipEnd - ip) - 5);
                int distance = @base - candidate; // > 0
                nuint insert = (nuint)(@base - nextEmit);
                ip += (int)matched;
                if (insert < 6210)
                {
                    pos = EmitInsertLen(insert, s, pos, storage);
                }
                else if (ShouldUseUncompressedMode(metablockStart, nextEmit, insert, literalRatio))
                {
                    storageIx = pos;
                    EmitUncompressedMetaBlock(data, metablockStart, @base, mlenStorageIx - 3, ref storageIx, storage);
                    return @base;
                }
                else
                {
                    pos = EmitLongInsertLen(insert, s, pos, storage);
                }
                pos = EmitLiterals(data.Slice(nextEmit, (int)insert), s, pos, storage);
                if (distance == lastDistance)
                {
                    BitWriter.WriteBits(s.CmdDepth[64], s.CmdBits[64], ref pos, storage);
                    ++s.CmdHisto[64];
                }
                else
                {
                    pos = EmitDistance((nuint)distance, s, pos, storage);
                    lastDistance = distance;
                }
                pos = EmitCopyLenLastDistance(matched, s, pos, storage);

                nextEmit = ip;
                if (ip >= ipLimit)
                {
                    storageIx = pos;
                    return -1;
                }
                // We could immediately start working at ip now, but to improve
                // compression we first update "table" with the hashes of some positions
                // within the last copy.
                candidate = UpdateTableAfterCopy(data, ip, table, shift, out ipBytes);
            }

            while (IsMatch(ipBytes, Load64(data, candidate)))
            {
                // We have a 5-byte match at ip, and no need to emit any literal bytes
                // prior to ip.
                int @base = ip;
                nuint matched = 5 + FindMatchLengthAt(data, candidate + 5, ip + 5, (nuint)(ipEnd - ip) - 5);
                if (ip - candidate > MaxDistance)
                {
                    break;
                }
                ip += (int)matched;
                lastDistance = @base - candidate; // > 0
                pos = EmitCopyLen(matched, s, pos, storage);
                pos = EmitDistance((nuint)lastDistance, s, pos, storage);

                nextEmit = ip;
                if (ip >= ipLimit)
                {
                    storageIx = pos;
                    return -1;
                }
                // We could immediately start working at ip now, but to improve
                // compression we first update "table" with the hashes of some positions
                // within the last copy.
                candidate = UpdateTableAfterCopy(data, ip, table, shift, out ipBytes);
            }

            ++ip;
        }
    }

    /// <param name="s">Arena.</param>
    /// <param name="data">The whole input fragment; offset 0 is the C <c>base_ip</c>.</param>
    /// <param name="inputSize">Size of the fragment.</param>
    /// <param name="isLast">Whether this is the last fragment.</param>
    /// <param name="table">Hash table (all zeros).</param>
    /// <param name="tableBits">Log2 of the hash table size.</param>
    /// <param name="storageIx">Bit position in <paramref name="storage"/>.</param>
    /// <param name="storage">Output.</param>
    private static void CompressFragmentFastImpl(BrotliOnePassArena s, ReadOnlySpan<byte> data, nuint inputSize, bool isLast,
        Span<int> table, nuint tableBits, ref nuint storageIx, Span<byte> storage)
    {
        int ipEnd;

        // "input" is the offset of the current block start.
        int input = 0;

        // "nextEmit" is a pointer to the first byte that is not covered by a
        // previous copy. Bytes between "nextEmit" and the start of the next copy or
        // the end of the input will be emitted as literal bytes.
        int nextEmit = input;
        // The start of the first block (C "base_ip") is offset 0.

        const nuint FirstBlockSize = 3 << 15;
        const nuint MergeBlockSize = 1 << 16;

        int metablockStart = input;
        nuint blockSize = Math.Min(inputSize, FirstBlockSize);
        nuint totalBlockSize = blockSize;
        // Save the bit position of the MLEN field of the meta-block header, so that
        // we can update it later if we decide to extend this meta-block.
        nuint mlenStorageIx = storageIx + 3;

        nuint literalRatio;

        int shift = 64 - (int)tableBits;

        StoreMetaBlockHeader(blockSize, false, ref storageIx, storage);
        // No block splits, no contexts.
        BitWriter.WriteBits(13, 0, ref storageIx, storage);

        literalRatio = BuildAndStoreLiteralPrefixCode(s, data.Slice(input), blockSize, ref storageIx, storage);

        {
            // Store the pre-compressed command and distance prefix codes.
            ReadOnlySpan<byte> cmdCode = s.CmdCode;
            nuint cmdCodeNumbits = s.CmdCodeNumbits;
            for (nuint i = 0; i + 7 < cmdCodeNumbits; i += 8)
            {
                BitWriter.WriteBits(8, cmdCode[(int)(i >> 3)], ref storageIx, storage);
            }
            BitWriter.WriteBits(cmdCodeNumbits & 7, cmdCode[(int)(cmdCodeNumbits >> 3)], ref storageIx, storage);
        }

    emit_commands:
        ipEnd = input + (int)blockSize;
        {
            int uncompressedEnd = EmitCommands(s, data, input, inputSize, blockSize, ref nextEmit, metablockStart,
                mlenStorageIx, literalRatio, table, shift, ref storageIx, storage);
            if (uncompressedEnd >= 0)
            {
                inputSize -= (nuint)(uncompressedEnd - input);
                input = uncompressedEnd;
                nextEmit = input;
                goto next_block;
            }
        }

        // emit_remainder:
        Debug.Assert(nextEmit <= ipEnd);
        input += (int)blockSize;
        inputSize -= blockSize;
        blockSize = Math.Min(inputSize, MergeBlockSize);

        // Decide if we want to continue this meta-block instead of emitting the
        // last insert-only command.
        if (inputSize > 0 &&
            totalBlockSize + blockSize <= (1 << 20) &&
            ShouldMergeBlock(s, data.Slice(input), blockSize, s.LitDepth))
        {
            Debug.Assert(totalBlockSize > (1 << 16));
            // Update the size of the current meta-block and continue emitting commands.
            // We can do this because the current size and the new size both have 5
            // nibbles.
            totalBlockSize += blockSize;
            UpdateBits(20, (uint)(totalBlockSize - 1), mlenStorageIx, storage);
            goto emit_commands;
        }

        // Emit the remaining bytes as literals.
        if (nextEmit < ipEnd)
        {
            nuint insert = (nuint)(ipEnd - nextEmit);
            if (insert < 6210)
            {
                storageIx = EmitInsertLen(insert, s, storageIx, storage);
                storageIx = EmitLiterals(data.Slice(nextEmit, (int)insert), s, storageIx, storage);
            }
            else if (ShouldUseUncompressedMode(metablockStart, nextEmit, insert, literalRatio))
            {
                EmitUncompressedMetaBlock(data, metablockStart, ipEnd, mlenStorageIx - 3, ref storageIx, storage);
            }
            else
            {
                storageIx = EmitLongInsertLen(insert, s, storageIx, storage);
                storageIx = EmitLiterals(data.Slice(nextEmit, (int)insert), s, storageIx, storage);
            }
        }
        nextEmit = ipEnd;

    next_block:
        // If we have more data, write a new meta-block header and prefix codes and
        // then continue emitting commands.
        if (inputSize > 0)
        {
            metablockStart = input;
            blockSize = Math.Min(inputSize, FirstBlockSize);
            totalBlockSize = blockSize;
            // Save the bit position of the MLEN field of the meta-block header, so that
            // we can update it later if we decide to extend this meta-block.
            mlenStorageIx = storageIx + 3;
            StoreMetaBlockHeader(blockSize, false, ref storageIx, storage);
            // No block splits, no contexts.
            BitWriter.WriteBits(13, 0, ref storageIx, storage);
            literalRatio = BuildAndStoreLiteralPrefixCode(s, data.Slice(input), blockSize, ref storageIx, storage);
            BuildAndStoreCommandPrefixCode(s, ref storageIx, storage);
            goto emit_commands;
        }

        if (!isLast)
        {
            // If this is not the last block, update the command and distance prefix
            // codes for the next block and store the compressed forms.
            s.CmdCode[0] = 0;
            s.CmdCodeNumbits = 0;
            BuildAndStoreCommandPrefixCode(s, ref s.CmdCodeNumbits, s.CmdCode);
        }
    }

    /// <summary>
    /// Compresses "input" string to the "*storage" buffer as one or more complete
    /// meta-blocks, and updates the "*storage_ix" bit position.
    /// If "is_last" is 1, emits an additional empty last meta-block.
    /// "cmd_depth" and "cmd_bits" contain the command and distance prefix codes
    /// (see comment in encode.h) used for the encoding of this input fragment.
    /// If "is_last" is 0, they are updated to reflect the statistics
    /// of this input fragment, to be used for the encoding of the next fragment.
    /// "*cmd_code_numbits" is the number of bits of the compressed representation
    /// of the command and distance prefix codes, and "cmd_code" is an array of
    /// at least "(*cmd_code_numbits + 7) >> 3" size that contains the compressed
    /// command and distance prefix codes. If "is_last" is 0, these are also
    /// updated to represent the updated "cmd_depth" and "cmd_bits".
    /// REQUIRES: "input_size" is greater than zero, or "is_last" is 1.
    /// REQUIRES: "input_size" is less or equal to maximal metablock size (1 &lt;&lt; 24).
    /// REQUIRES: All elements in "table[0..table_size-1]" are initialized to zero.
    /// REQUIRES: "table_size" is an odd (9, 11, 13, 15) power of two
    /// OUTPUT: maximal copy distance &lt;= |input_size|
    /// OUTPUT: maximal copy distance &lt;= BROTLI_MAX_BACKWARD_LIMIT(18)
    /// </summary>
    internal static void CompressFragmentFast(BrotliOnePassArena s, ReadOnlySpan<byte> input, nuint inputSize, bool isLast,
        Span<int> table, nuint tableSize, ref nuint storageIx, Span<byte> storage)
    {
        nuint initialStorageIx = storageIx;
        nuint tableBits = FastLog.Log2FloorNonZero(tableSize);

        if (inputSize == 0)
        {
            Debug.Assert(isLast);
            BitWriter.WriteBits(1, 1, ref storageIx, storage); // islast
            BitWriter.WriteBits(1, 1, ref storageIx, storage); // isempty
            storageIx = (storageIx + 7u) & ~(nuint)7u;
            return;
        }

        input = input.Slice(0, (int)inputSize);
        switch (tableBits)
        {
            case 9:
            case 11:
            case 13:
            case 15:
                CompressFragmentFastImpl(s, input, inputSize, isLast, table, tableBits, ref storageIx, storage);
                break;
            default:
                Debug.Fail("Unexpected table size.");
                break;
        }

        // If output is larger than single uncompressed block, rewrite it.
        if (storageIx - initialStorageIx > 31 + (inputSize << 3))
        {
            EmitUncompressedMetaBlock(input, 0, (int)inputSize, initialStorageIx, ref storageIx, storage);
        }

        if (isLast)
        {
            BitWriter.WriteBits(1, 1, ref storageIx, storage); // islast
            BitWriter.WriteBits(1, 1, ref storageIx, storage); // isempty
            storageIx = (storageIx + 7u) & ~(nuint)7u;
        }
    }
}
