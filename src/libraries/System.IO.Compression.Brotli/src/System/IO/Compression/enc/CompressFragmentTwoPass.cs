// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/compress_fragment_two_pass.c and compress_fragment_two_pass.h.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Scratch state of the two-pass (quality 1) fragment compressor (BrotliTwoPassArena).</summary>
internal sealed class BrotliTwoPassArena
{
    public readonly uint[] LitHisto = new uint[256];
    public readonly byte[] LitDepth = new byte[256];
    public readonly ushort[] LitBits = new ushort[256];

    public readonly uint[] CmdHisto = new uint[128];
    public readonly byte[] CmdDepth = new byte[128];
    public readonly ushort[] CmdBits = new ushort[128];

    // BuildAndStoreCommandPrefixCode
    public readonly HuffmanTree[] TmpTree = new HuffmanTree[2 * BrotliConstants.NumLiteralSymbols + 1];
    public readonly byte[] TmpDepth = new byte[BrotliConstants.NumCommandSymbols];
    public readonly ushort[] TmpBits = new ushort[64];

    /// <summary>The <c>small_table_</c> hash table of the encoder state (kept with the arena of the fast modes).</summary>
    public readonly int[] SmallTable = new int[1 << 10];
}

/// <summary>
/// Function for fast encoding of an input fragment, independently from the input
/// history. This function uses two-pass processing: in the first pass we save
/// the found backward matches and literal bytes into a buffer, and in the
/// second pass we emit them into the bit stream using prefix codes built based
/// on the actual command and literal byte histograms.
/// </summary>
/// <remarks>
/// Pointers into the input fragment are represented as offsets relative to the start of the fragment (C <c>base_ip</c>).
/// </remarks>
internal static class CompressFragmentTwoPass
{
    /// <summary>kCompressFragmentTwoPassBlockSize.</summary>
    public const int BlockSize = 1 << 17;

    // Acceptable loss for uncompressible speedup is 2%
    private const double MinRatio = 0.98;
    private const int SampleRate = 43;

    /// <summary>The minimal match length, a compile-time constant (4 or 6) of the C template.</summary>
    private interface IMinMatch
    {
        static abstract int Value { get; }
    }

    private struct MinMatch4 : IMinMatch
    {
        public static int Value => 4;
    }

    private struct MinMatch6 : IMinMatch
    {
        public static int Value => 6;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hash<TMinMatch>(ReadOnlySpan<byte> data, int p, int shift)
        where TMinMatch : struct, IMinMatch
    {
        ulong h = (BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(p, 8)) << ((8 - TMinMatch.Value) * 8)) * HashBase.HashMul32;
        return (uint)(h >> shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytesAtOffset<TMinMatch>(ulong v, int offset, int shift)
        where TMinMatch : struct, IMinMatch
    {
        Debug.Assert(offset <= 8 - TMinMatch.Value);
        ulong h = ((v >> (8 * offset)) << ((8 - TMinMatch.Value) * 8)) * HashBase.HashMul32;
        return (uint)(h >> shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Load64(ReadOnlySpan<byte> data, int p) => BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(p, 8));

    /// <summary>Hash of the 8 bytes loaded at some position (same as <see cref="Hash{TMinMatch}"/>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytes<TMinMatch>(ulong bytes, int shift)
        where TMinMatch : struct, IMinMatch
    {
        ulong h = (bytes << ((8 - TMinMatch.Value) * 8)) * HashBase.HashMul32;
        return (uint)(h >> shift);
    }

    /// <summary>
    /// IsMatch(p1, p2, min_match): compares the first <c>min_match</c> bytes. The arguments are little-endian loads
    /// of (at least) <c>min_match</c> bytes at p1 and p2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatch<TMinMatch>(ulong p1Bytes, ulong p2Bytes)
        where TMinMatch : struct, IMinMatch
    {
        ulong mask = TMinMatch.Value == 4 ? 0xFFFF_FFFFUL : 0xFFFF_FFFF_FFFFUL;
        return ((p1Bytes ^ p2Bytes) & mask) == 0;
    }

    /// <summary>
    /// Builds a command and distance prefix code (each 64 symbols) into "depth" and
    /// "bits" based on "histogram" and stores it into the bit stream.
    /// </summary>
    private static void BuildAndStoreCommandPrefixCode(BrotliTwoPassArena s, ref nuint storageIx, Span<byte> storage)
    {
        Span<byte> cmdDepth = s.CmdDepth;
        Span<ushort> cmdBits = s.CmdBits;
        Span<byte> tmpDepth = s.TmpDepth;
        Span<ushort> tmpBits = s.TmpBits;
        ReadOnlySpan<uint> cmdHisto = s.CmdHisto;

        // Tree size for building a tree over 64 symbols is 2 * 64 + 1.
        tmpDepth.Clear();
        EntropyEncode.CreateHuffmanTree(cmdHisto, 64, 15, s.TmpTree, cmdDepth);
        EntropyEncode.CreateHuffmanTree(cmdHisto.Slice(64), 64, 14, s.TmpTree, cmdDepth.Slice(64));
        // We have to jump through a few hoops here in order to compute
        // the command bits because the symbols are in a different order than in
        // the full alphabet. This looks complicated, but having the symbols
        // in this order in the command bits saves a few branches in the Emit*
        // functions.
        cmdDepth.Slice(24, 24).CopyTo(tmpDepth);
        cmdDepth.Slice(0, 8).CopyTo(tmpDepth.Slice(24));
        cmdDepth.Slice(48, 8).CopyTo(tmpDepth.Slice(32));
        cmdDepth.Slice(8, 8).CopyTo(tmpDepth.Slice(40));
        cmdDepth.Slice(56, 8).CopyTo(tmpDepth.Slice(48));
        cmdDepth.Slice(16, 8).CopyTo(tmpDepth.Slice(56));
        EntropyEncode.ConvertBitDepthsToSymbols(tmpDepth, 64, tmpBits);
        tmpBits.Slice(24, 8).CopyTo(cmdBits);
        tmpBits.Slice(40, 8).CopyTo(cmdBits.Slice(8));
        tmpBits.Slice(56, 8).CopyTo(cmdBits.Slice(16));
        tmpBits.Slice(0, 24).CopyTo(cmdBits.Slice(24));
        tmpBits.Slice(32, 8).CopyTo(cmdBits.Slice(48));
        tmpBits.Slice(48, 8).CopyTo(cmdBits.Slice(56));
        EntropyEncode.ConvertBitDepthsToSymbols(cmdDepth.Slice(64), 64, cmdBits.Slice(64));
        {
            // Create the bit length array for the full command alphabet.
            tmpDepth.Slice(0, 64).Clear(); // only 64 first values were used
            cmdDepth.Slice(24, 8).CopyTo(tmpDepth);
            cmdDepth.Slice(32, 8).CopyTo(tmpDepth.Slice(64));
            cmdDepth.Slice(40, 8).CopyTo(tmpDepth.Slice(128));
            cmdDepth.Slice(48, 8).CopyTo(tmpDepth.Slice(192));
            cmdDepth.Slice(56, 8).CopyTo(tmpDepth.Slice(384));
            for (int i = 0; i < 8; ++i)
            {
                tmpDepth[128 + 8 * i] = cmdDepth[i];
                tmpDepth[256 + 8 * i] = cmdDepth[8 + i];
                tmpDepth[448 + 8 * i] = cmdDepth[16 + i];
            }
            BrotliBitStream.StoreHuffmanTree(tmpDepth, BrotliConstants.NumCommandSymbols, s.TmpTree, ref storageIx, storage);
        }
        BrotliBitStream.StoreHuffmanTree(cmdDepth.Slice(64), 64, s.TmpTree, ref storageIx, storage);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitInsertLen(uint insertlen, Span<uint> commands, ref int commandsIx)
    {
        if (insertlen < 6)
        {
            commands[commandsIx] = insertlen;
        }
        else if (insertlen < 130)
        {
            uint tail = insertlen - 2;
            uint nbits = FastLog.Log2FloorNonZero(tail) - 1u;
            uint prefix = tail >> (int)nbits;
            uint inscode = (nbits << 1) + prefix + 2;
            uint extra = tail - (prefix << (int)nbits);
            commands[commandsIx] = inscode | (extra << 8);
        }
        else if (insertlen < 2114)
        {
            uint tail = insertlen - 66;
            uint nbits = FastLog.Log2FloorNonZero(tail);
            uint code = nbits + 10;
            uint extra = tail - (1u << (int)nbits);
            commands[commandsIx] = code | (extra << 8);
        }
        else if (insertlen < 6210)
        {
            uint extra = insertlen - 2114;
            commands[commandsIx] = 21 | (extra << 8);
        }
        else if (insertlen < 22594)
        {
            uint extra = insertlen - 6210;
            commands[commandsIx] = 22 | (extra << 8);
        }
        else
        {
            uint extra = insertlen - 22594;
            commands[commandsIx] = 23 | (extra << 8);
        }
        ++commandsIx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitCopyLen(nuint copylen, Span<uint> commands, ref int commandsIx)
    {
        if (copylen < 10)
        {
            commands[commandsIx] = (uint)(copylen + 38);
        }
        else if (copylen < 134)
        {
            nuint tail = copylen - 6;
            nuint nbits = FastLog.Log2FloorNonZero(tail) - 1;
            nuint prefix = tail >> (int)nbits;
            nuint code = (nbits << 1) + prefix + 44;
            nuint extra = tail - (prefix << (int)nbits);
            commands[commandsIx] = (uint)(code | (extra << 8));
        }
        else if (copylen < 2118)
        {
            nuint tail = copylen - 70;
            nuint nbits = FastLog.Log2FloorNonZero(tail);
            nuint code = nbits + 52;
            nuint extra = tail - ((nuint)1 << (int)nbits);
            commands[commandsIx] = (uint)(code | (extra << 8));
        }
        else
        {
            nuint extra = copylen - 2118;
            commands[commandsIx] = (uint)(63 | (extra << 8));
        }
        ++commandsIx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitCopyLenLastDistance(nuint copylen, Span<uint> commands, ref int commandsIx)
    {
        if (copylen < 12)
        {
            commands[commandsIx] = (uint)(copylen + 20);
            ++commandsIx;
        }
        else if (copylen < 72)
        {
            nuint tail = copylen - 8;
            nuint nbits = FastLog.Log2FloorNonZero(tail) - 1;
            nuint prefix = tail >> (int)nbits;
            nuint code = (nbits << 1) + prefix + 28;
            nuint extra = tail - (prefix << (int)nbits);
            commands[commandsIx] = (uint)(code | (extra << 8));
            ++commandsIx;
        }
        else if (copylen < 136)
        {
            nuint tail = copylen - 8;
            nuint code = (tail >> 5) + 54;
            nuint extra = tail & 31;
            commands[commandsIx] = (uint)(code | (extra << 8));
            ++commandsIx;
            commands[commandsIx] = 64;
            ++commandsIx;
        }
        else if (copylen < 2120)
        {
            nuint tail = copylen - 72;
            nuint nbits = FastLog.Log2FloorNonZero(tail);
            nuint code = nbits + 52;
            nuint extra = tail - ((nuint)1 << (int)nbits);
            commands[commandsIx] = (uint)(code | (extra << 8));
            ++commandsIx;
            commands[commandsIx] = 64;
            ++commandsIx;
        }
        else
        {
            nuint extra = copylen - 2120;
            commands[commandsIx] = (uint)(63 | (extra << 8));
            ++commandsIx;
            commands[commandsIx] = 64;
            ++commandsIx;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitDistance(uint distance, Span<uint> commands, ref int commandsIx)
    {
        uint d = distance + 3;
        uint nbits = FastLog.Log2FloorNonZero(d) - 1;
        uint prefix = (d >> (int)nbits) & 1;
        uint offset = (2 + prefix) << (int)nbits;
        uint distcode = 2 * (nbits - 1) + prefix + 80;
        uint extra = d - offset;
        commands[commandsIx] = distcode | (extra << 8);
        ++commandsIx;
    }

    /// <summary>
    /// Updates the hash table with the hashes of some positions within the last copy and returns the next candidate.
    /// <paramref name="lastOffsetFor4"/> selects the C variant: the first copy of this code in CreateCommands
    /// hashes "ip - 1" using offset 0 (sic) for the 4-byte match length; the second one uses offset 2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int UpdateTableAfterCopy<TMinMatch>(ReadOnlySpan<byte> data, int ip, Span<int> table, int shift, int lastOffsetFor4,
        out ulong ipBytes)
        where TMinMatch : struct, IMinMatch
    {
        ulong inputBytes;
        uint curHash;
        uint prevHash;
        if (TMinMatch.Value == 4)
        {
            inputBytes = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(ip - 3, 8));
            ipBytes = inputBytes >> 24;
            curHash = HashBytesAtOffset<TMinMatch>(inputBytes, 3, shift);
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, 0, shift);
            table[(int)prevHash] = ip - 3;
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, 1, shift);
            table[(int)prevHash] = ip - 2;
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, lastOffsetFor4, shift);
            table[(int)prevHash] = ip - 1;
        }
        else
        {
            inputBytes = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(ip - 5, 8));
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, 0, shift);
            table[(int)prevHash] = ip - 5;
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, 1, shift);
            table[(int)prevHash] = ip - 4;
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, 2, shift);
            table[(int)prevHash] = ip - 3;
            inputBytes = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(ip - 2, 8));
            ipBytes = inputBytes >> 16;
            curHash = HashBytesAtOffset<TMinMatch>(inputBytes, 2, shift);
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, 0, shift);
            table[(int)prevHash] = ip - 2;
            prevHash = HashBytesAtOffset<TMinMatch>(inputBytes, 1, shift);
            table[(int)prevHash] = ip - 1;
        }

        int candidate = table[(int)curHash];
        table[(int)curHash] = ip;
        return candidate;
    }

    /// <summary>
    /// Step 1 of CreateCommands: scans forward in the input looking for a match of at least <c>min_match</c> bytes.
    /// </summary>
    /// <returns>The match candidate; -1 if we got close to exhausting the input (C "goto emit_remainder").</returns>
    /// <remarks>
    /// <paramref name="ip"/>: In: position to start the search at; out: position of the match.<br/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindNextMatch<TMinMatch>(ReadOnlySpan<byte> data, Span<int> table, int shift, int lastDistance, int ipLimit,
        ref int ip)
        where TMinMatch : struct, IMinMatch
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
        // last match; dividing it by 32 (ie. right-shifting by five) gives the
        // number of bytes to move ahead for each iteration.
        uint skip = 32;

        int nextIp = ip;
        int curIp;
        int candidate;
        ulong curBytes;
        // "nextBytes" caches the 8 bytes at "nextIp" (used both for hashing and for matching).
        ulong nextBytes = Load64(data, nextIp);
        uint nextHash = HashBytes<TMinMatch>(nextBytes, shift);
        while (true)
        {
            do
            {
                uint hash = nextHash;
                uint bytesBetweenHashLookups = skip++ >> 5;
                curIp = nextIp;
                curBytes = nextBytes;
                Debug.Assert(hash == Hash<TMinMatch>(data, curIp, shift));
                nextIp = curIp + (int)bytesBetweenHashLookups;
                if (nextIp > ipLimit)
                {
                    ip = curIp;
                    return -1;
                }
                nextBytes = Load64(data, nextIp);
                nextHash = HashBytes<TMinMatch>(nextBytes, shift);
                candidate = curIp - lastDistance;
                if (IsMatch<TMinMatch>(curBytes, Load64(data, candidate)))
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
            while (!IsMatch<TMinMatch>(curBytes, Load64(data, candidate)));

            // Check copy distance. If candidate is not feasible, continue search.
            // Checking is done outside of hot loop to reduce overhead.
            if (curIp - candidate <= CompressFragment.MaxDistance)
            {
                ip = curIp;
                return candidate;
            }
        }
    }

    /// <summary>FindMatchLengthWithLimit(data + s1, data + s2, limit).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint FindMatchLengthAt(ReadOnlySpan<byte> data, int s1, int s2, nuint limit) =>
        FindMatchLength.FindMatchLengthWithLimit(data.Slice(s1), data.Slice(s2), limit);

    /// <param name="data">The whole input fragment; offset 0 is the C <c>base_ip</c>.</param>
    /// <param name="input">Offset of the current block within <paramref name="data"/>.</param>
    /// <param name="blockSize">Size of the current block.</param>
    /// <param name="inputSize">Remaining input size starting at <paramref name="input"/>.</param>
    /// <param name="table">Hash table.</param>
    /// <param name="literals">Literal buffer.</param>
    /// <param name="literalsIx">Number of literals written to <paramref name="literals"/>.</param>
    /// <param name="commands">Command buffer.</param>
    /// <param name="commandsIx">Number of commands written to <paramref name="commands"/>.</param>
    private static void CreateCommands<TMinMatch, TTable>(ReadOnlySpan<byte> data, int input, nuint blockSize, nuint inputSize,
        Span<int> table, Span<byte> literals, ref int literalsIx, Span<uint> commands, ref int commandsIx)
        where TMinMatch : struct, IMinMatch
        where TTable : struct, IHashTableBits
    {
        // "ip" is the input pointer.
        int ip = input;
        int shift = 64 - TTable.Value;
        table = table.Slice(0, 1 << TTable.Value);
        int ipEnd = input + (int)blockSize;
        // "nextEmit" is a pointer to the first byte that is not covered by a
        // previous copy. Bytes between "nextEmit" and the start of the next copy or
        // the end of the input will be emitted as literal bytes.
        int nextEmit = input;

        int lastDistance = -1;
        const nuint InputMarginBytes = BrotliConstants.WindowGap;
        int minMatch = TMinMatch.Value;
        int cmdIx = commandsIx;
        int litIx = literalsIx;

        if (blockSize >= InputMarginBytes)
        {
            // For the last block, we need to keep a 16 bytes margin so that we can be
            // sure that all distances are at most window size - 16.
            // For all other blocks, we only need to keep a margin of 5 bytes so that
            // we don't go over the block size with a copy.
            nuint lenLimit = Math.Min(blockSize - (nuint)minMatch, inputSize - InputMarginBytes);
            int ipLimit = input + (int)lenLimit;

            ++ip;
            while (true)
            {
                // Step 1: Scan forward in the input looking for a 6-byte-long match.
                // If we get close to exhausting the input then goto emit_remainder.
                Debug.Assert(nextEmit < ip);
                int candidate = FindNextMatch<TMinMatch>(data, table, shift, lastDistance, ipLimit, ref ip);
                if (candidate < 0)
                {
                    break;
                }

                // The (at least min_match valid) bytes at "ip" after the table update.
                ulong ipBytes;

                // Step 2: Emit the found match together with the literal bytes from
                // "nextEmit", and then see if we can find a next match immediately
                // afterwards. Repeat until we find no match for the input
                // without emitting some literal bytes.
                {
                    // We have a 6-byte match at ip, and we need to emit bytes in
                    // [nextEmit, ip).
                    int @base = ip;
                    nuint matched = (nuint)minMatch + FindMatchLengthAt(data, candidate + minMatch, ip + minMatch, (nuint)(ipEnd - ip) - (nuint)minMatch);
                    int distance = @base - candidate; // > 0
                    int insert = @base - nextEmit;
                    ip += (int)matched;
                    EmitInsertLen((uint)insert, commands, ref cmdIx);
                    data.Slice(nextEmit, insert).CopyTo(literals.Slice(litIx));
                    litIx += insert;
                    if (distance == lastDistance)
                    {
                        commands[cmdIx] = 64;
                        ++cmdIx;
                    }
                    else
                    {
                        EmitDistance((uint)distance, commands, ref cmdIx);
                        lastDistance = distance;
                    }
                    EmitCopyLenLastDistance(matched, commands, ref cmdIx);

                    nextEmit = ip;
                    if (ip >= ipLimit)
                    {
                        break;
                    }
                    // We could immediately start working at ip now, but to improve
                    // compression we first update "table" with the hashes of some
                    // positions within the last copy.
                    candidate = UpdateTableAfterCopy<TMinMatch>(data, ip, table, shift, 0, out ipBytes);
                }

                bool reachedLimit = false;
                while (ip - candidate <= CompressFragment.MaxDistance &&
                    IsMatch<TMinMatch>(ipBytes, Load64(data, candidate)))
                {
                    // We have a 6-byte match at ip, and no need to emit any
                    // literal bytes prior to ip.
                    int @base = ip;
                    nuint matched = (nuint)minMatch + FindMatchLengthAt(data, candidate + minMatch, ip + minMatch, (nuint)(ipEnd - ip) - (nuint)minMatch);
                    ip += (int)matched;
                    lastDistance = @base - candidate; // > 0
                    EmitCopyLen(matched, commands, ref cmdIx);
                    EmitDistance((uint)lastDistance, commands, ref cmdIx);

                    nextEmit = ip;
                    if (ip >= ipLimit)
                    {
                        reachedLimit = true;
                        break;
                    }
                    // We could immediately start working at ip now, but to improve
                    // compression we first update "table" with the hashes of some
                    // positions within the last copy.
                    candidate = UpdateTableAfterCopy<TMinMatch>(data, ip, table, shift, 2, out ipBytes);
                }
                if (reachedLimit)
                {
                    break;
                }

                ++ip;
            }
        }

        // emit_remainder:
        Debug.Assert(nextEmit <= ipEnd);
        // Emit the remaining bytes as literals.
        if (nextEmit < ipEnd)
        {
            uint insert = (uint)(ipEnd - nextEmit);
            EmitInsertLen(insert, commands, ref cmdIx);
            data.Slice(nextEmit, (int)insert).CopyTo(literals.Slice(litIx));
            litIx += (int)insert;
        }
        commandsIx = cmdIx;
        literalsIx = litIx;
    }

    private static ReadOnlySpan<uint> NumExtraBits =>
    [
        0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5,
        6, 7, 8, 9, 10, 12, 14, 24, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 2, 2, 3, 3, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10, 24,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8,
        9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16,
        17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22, 23, 23, 24, 24,
    ];

    private static ReadOnlySpan<uint> InsertOffset =>
    [
        0, 1, 2, 3, 4, 5, 6, 8, 10, 14, 18, 26,
        34, 50, 66, 98, 130, 194, 322, 578, 1090, 2114, 6210, 22594,
    ];

    private static void StoreCommands(BrotliTwoPassArena s, ReadOnlySpan<byte> literals, nuint numLiterals,
        ReadOnlySpan<uint> commands, nuint numCommands, ref nuint storageIx, Span<byte> storage)
    {
        Span<uint> litHisto = s.LitHisto;
        Span<byte> litDepth = s.LitDepth;
        Span<ushort> litBits = s.LitBits;
        Span<uint> cmdHisto = s.CmdHisto;
        Span<byte> cmdDepth = s.CmdDepth;
        Span<ushort> cmdBits = s.CmdBits;
        ReadOnlySpan<uint> numExtraBits = NumExtraBits;
        ReadOnlySpan<uint> insertOffset = InsertOffset;

        litHisto.Clear();
        cmdDepth.Clear();
        cmdBits.Clear();
        cmdHisto.Clear();
        literals = literals.Slice(0, (int)numLiterals);
        commands = commands.Slice(0, (int)numCommands);
        for (int i = 0; i < literals.Length; ++i)
        {
            ++litHisto[literals[i]];
        }
        BrotliBitStream.BuildAndStoreHuffmanTreeFast(s.TmpTree, litHisto, numLiterals, /* maxBits = */ 8,
            litDepth, litBits, ref storageIx, storage);

        for (int i = 0; i < commands.Length; ++i)
        {
            uint code = commands[i] & 0xFF;
            Debug.Assert(code < 128);
            ++cmdHisto[(int)code];
        }
        cmdHisto[1] += 1;
        cmdHisto[2] += 1;
        cmdHisto[64] += 1;
        cmdHisto[84] += 1;
        BuildAndStoreCommandPrefixCode(s, ref storageIx, storage);

        int literalIx = 0;
        nuint pos = storageIx;
        for (int i = 0; i < commands.Length; ++i)
        {
            uint cmd = commands[i];
            int code = (int)(cmd & 0xFF);
            uint extra = cmd >> 8;
            Debug.Assert(code < 128);
            CompressFragment.WriteBits(cmdDepth[code], cmdBits[code], numExtraBits[code], extra, ref pos, storage);
            if (code < 24)
            {
                uint insert = insertOffset[code] + extra;
                pos = CompressFragment.EmitLiterals(literals.Slice(literalIx, (int)insert), litDepth, litBits, pos, storage);
                literalIx += (int)insert;
            }
        }
        storageIx = pos;
    }

    private static bool ShouldCompress(BrotliTwoPassArena s, ReadOnlySpan<byte> input, nuint inputSize, nuint numLiterals)
    {
        double corpusSize = (double)inputSize;
        if ((double)numLiterals < MinRatio * corpusSize)
        {
            return true;
        }
        else
        {
            double maxTotalBitCost = corpusSize * 8 * MinRatio / SampleRate;
            Span<uint> litHisto = s.LitHisto;
            litHisto.Clear();
            for (nuint i = 0; i < inputSize; i += SampleRate)
            {
                ++litHisto[input[(int)i]];
            }
            return BitCost.BitsEntropy(litHisto, 256) < maxTotalBitCost;
        }
    }

    private static void EmitUncompressedMetaBlock(ReadOnlySpan<byte> input, nuint inputSize, ref nuint storageIx, Span<byte> storage)
    {
        CompressFragment.StoreMetaBlockHeader(inputSize, true, ref storageIx, storage);
        storageIx = (storageIx + 7u) & ~(nuint)7u;
        input.Slice(0, (int)inputSize).CopyTo(storage.Slice((int)(storageIx >> 3)));
        storageIx += inputSize << 3;
        storage[(int)(storageIx >> 3)] = 0;
    }

    private static void CompressFragmentTwoPassImpl<TMinMatch, TTable>(BrotliTwoPassArena s, ReadOnlySpan<byte> data, nuint inputSize,
        Span<uint> commandBuf, Span<byte> literalBuf, Span<int> table, ref nuint storageIx, Span<byte> storage)
        where TMinMatch : struct, IMinMatch
        where TTable : struct, IHashTableBits
    {
        // Offset 0 of "data" is the start of the first block (C "base_ip"), used for position and distance computations.
        int input = 0;

        while (inputSize > 0)
        {
            nuint blockSize = Math.Min(inputSize, (nuint)BlockSize);
            int commands = 0;
            int literals = 0;
            nuint numLiterals;
            CreateCommands<TMinMatch, TTable>(data, input, blockSize, inputSize, table, literalBuf, ref literals, commandBuf, ref commands);
            numLiterals = (nuint)literals;
            if (ShouldCompress(s, data.Slice(input), blockSize, numLiterals))
            {
                nuint numCommands = (nuint)commands;
                CompressFragment.StoreMetaBlockHeader(blockSize, false, ref storageIx, storage);
                // No block splits, no contexts.
                BitWriter.WriteBits(13, 0, ref storageIx, storage);
                StoreCommands(s, literalBuf, numLiterals, commandBuf, numCommands, ref storageIx, storage);
            }
            else
            {
                // Since we did not find many backward references and the entropy of
                // the data is close to 8 bits, we can simply emit an uncompressed block.
                // This makes compression speed of uncompressible data about 3x faster.
                EmitUncompressedMetaBlock(data.Slice(input), blockSize, ref storageIx, storage);
            }
            input += (int)blockSize;
            inputSize -= blockSize;
        }
    }

    /// <summary>
    /// Port of BrotliCompressFragmentTwoPass.
    /// Compresses "input" string to the "*storage" buffer as one or more complete
    /// meta-blocks, and updates the "*storage_ix" bit position.
    /// If "is_last" is 1, emits an additional empty last meta-block.
    /// REQUIRES: "input_size" is greater than zero, or "is_last" is 1.
    /// REQUIRES: "input_size" is less or equal to maximal metablock size (1 &lt;&lt; 24).
    /// REQUIRES: "command_buf" and "literal_buf" point to at least
    ///            kCompressFragmentTwoPassBlockSize long arrays.
    /// REQUIRES: All elements in "table[0..table_size-1]" are initialized to zero.
    /// REQUIRES: "table_size" is a power of two
    /// OUTPUT: maximal copy distance &lt;= |input_size|
    /// OUTPUT: maximal copy distance &lt;= BROTLI_MAX_BACKWARD_LIMIT(18)
    /// </summary>
    internal static void Compress(BrotliTwoPassArena s, ReadOnlySpan<byte> input, nuint inputSize, bool isLast,
        Span<uint> commandBuf, Span<byte> literalBuf, Span<int> table, nuint tableSize, ref nuint storageIx, Span<byte> storage)
    {
        nuint initialStorageIx = storageIx;
        nuint tableBits = FastLog.Log2FloorNonZero(tableSize);
        input = input.Slice(0, (int)inputSize);
        switch (tableBits)
        {
            case 8:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable8>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 9:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable9>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 10:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable10>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 11:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable11>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 12:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable12>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 13:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable13>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 14:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable14>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 15:
                CompressFragmentTwoPassImpl<MinMatch4, HashTable15>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 16:
                CompressFragmentTwoPassImpl<MinMatch6, HashTable16>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            case 17:
                CompressFragmentTwoPassImpl<MinMatch6, HashTable17>(s, input, inputSize, commandBuf, literalBuf, table, ref storageIx, storage);
                break;
            default:
                Debug.Fail("Unexpected table size.");
                break;
        }

        // If output is larger than single uncompressed block, rewrite it.
        if (storageIx - initialStorageIx > 31 + (inputSize << 3))
        {
            CompressFragment.RewindBitPosition(initialStorageIx, ref storageIx, storage);
            EmitUncompressedMetaBlock(input, inputSize, ref storageIx, storage);
        }

        if (isLast)
        {
            BitWriter.WriteBits(1, 1, ref storageIx, storage); // islast
            BitWriter.WriteBits(1, 1, ref storageIx, storage); // isempty
            storageIx = (storageIx + 7u) & ~(nuint)7u;
        }
    }
}
