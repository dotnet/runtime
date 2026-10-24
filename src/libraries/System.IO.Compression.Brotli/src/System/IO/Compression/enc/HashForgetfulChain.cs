// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/hash_forgetful_chain_inc.h.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Template parameters of <see cref="HashForgetfulChain{TParams}"/>.</summary>
internal interface IHashForgetfulChainParams
{
    static abstract int BucketBits { get; }
    static abstract int NumBanks { get; }
    static abstract int BankBits { get; }
    static abstract int NumLastDistancesToCheck { get; }
}

internal struct H40Params : IHashForgetfulChainParams
{
    public static int BucketBits => 15;
    public static int NumBanks => 1;
    public static int BankBits => 16;
    public static int NumLastDistancesToCheck => 4;
}

internal struct H41Params : IHashForgetfulChainParams
{
    public static int BucketBits => 15;
    public static int NumBanks => 1;
    public static int BankBits => 16;
    public static int NumLastDistancesToCheck => 10;
}

internal struct H42Params : IHashForgetfulChainParams
{
    public static int BucketBits => 15;
    public static int NumBanks => 512;
    public static int BankBits => 9;
    public static int NumLastDistancesToCheck => 16;
}

/// <summary>A node of a <see cref="HashForgetfulChain{TParams}"/> chain.</summary>
internal struct ForgetfulChainSlot
{
    public ushort Delta;
    public ushort Next;
}

/// <summary>
/// A (forgetful) hash table to the data seen by the compressor, to help create backward references to previous data.
/// Hashes are stored in chains which are bucketed to groups. Group of chains share a storage "bank".
/// When more than "bank size" chain nodes are added, oldest nodes are replaced; this way several chains may share a tail.
/// </summary>
internal struct HashForgetfulChain<TParams> : IHasher
    where TParams : struct, IHashForgetfulChainParams
{
    private const int TinyHashSize = 65536;

    /// <summary>Up to 1KiB.</summary>
    private ushort[]? _freeSlotIdx;
    private nuint _maxHops;

    /// <summary>Shortcut to the common hasher state.</summary>
    private Hasher _common;

    /// <summary>uint32_t addr[BUCKET_SIZE]</summary>
    private uint[]? _addr;
    /// <summary>uint16_t head[BUCKET_SIZE]</summary>
    private ushort[]? _head;
    /// <summary>Truncated hash used for quick rejection of "distance cache" candidates. uint8_t tiny_hash[65536]</summary>
    private byte[]? _tinyHash;
    /// <summary>Bank banks[NUM_BANKS], each bank holding BANK_SIZE slots.</summary>
    private ForgetfulChainSlot[]? _banks;

    private static int BankSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 1 << TParams.BankBits;
    }

    /// <summary>Number of hash buckets.</summary>
    private static int BucketSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 1 << TParams.BucketBits;
    }

    public static nuint HashTypeLength => 4;
    public static nuint StoreLookahead => 4;

    /// <summary>HashBytes is the function that chooses the bucket to place the address in.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint HashBytes(ReadOnlySpan<byte> data, nuint index)
    {
        uint h = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice((int)index, sizeof(uint))) * HashBase.HashMul32;
        // The higher bits contain more mixture from the multiplication, so we take our results from there.
        return h >> (32 - TParams.BucketBits);
    }

    public void Initialize(Hasher common, in EncoderParams parameters, bool oneShot, nuint inputSize)
    {
        _common = common;
        _maxHops = (nuint)(parameters.Quality > 6 ? 7u : 8u) << (parameters.Quality - 4);
        _freeSlotIdx = new ushort[TParams.NumBanks];
        _addr = Hash.RentArray<uint>((nuint)BucketSize);
        _head = Hash.RentArray<ushort>((nuint)BucketSize);
        _tinyHash = Hash.RentArray<byte>(TinyHashSize);
        _banks = Hash.RentArray<ForgetfulChainSlot>((nuint)(TParams.NumBanks * BankSize));
    }

    public void Destroy()
    {
        _freeSlotIdx = null;
        Hash.ReturnArray(ref _addr);
        Hash.ReturnArray(ref _head);
        Hash.ReturnArray(ref _tinyHash);
        Hash.ReturnArray(ref _banks);
    }

    public void Prepare(bool oneShot, nuint inputSize, ReadOnlySpan<byte> data)
    {
        Span<uint> addr = _addr.AsSpan(0, BucketSize);
        Span<ushort> head = _head.AsSpan(0, BucketSize);
        // Partial preparation is 100 times slower (per socket).
        nuint partialPrepareThreshold = (nuint)(BucketSize >> 6);
        if (oneShot && inputSize <= partialPrepareThreshold)
        {
            for (nuint i = 0; i < inputSize; ++i)
            {
                int bucket = (int)HashBytes(data, i);
                // See InitEmpty comment.
                addr[bucket] = 0xCCCCCCCC;
                head[bucket] = 0xCCCC;
            }
        }
        else
        {
            // Fill |addr| array with 0xCCCCCCCC value. Because of wrapping, position processed by hasher never
            // reaches 3GB + 64M; this makes all new chains to be terminated after the first node.
            addr.Fill(0xCCCCCCCC);
            head.Clear();
        }
        _tinyHash.AsSpan(0, TinyHashSize).Clear();
        _freeSlotIdx.AsSpan().Clear();
    }

    /// <summary>
    /// Look at 4 bytes at &amp;data[ix &amp; mask]. Compute a hash from these, and prepend node to corresponding chain;
    /// also update tiny_hash for current position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix) =>
        Store(data, mask, ix, _freeSlotIdx!, _addr!, _head!, _tinyHash!, _banks!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix, ushort[] freeSlotIdx, uint[] addr, ushort[] head,
        byte[] tinyHash, ForgetfulChainSlot[] banks)
    {
        nuint key = HashBytes(data, ix & mask);
        nuint bank = key & (nuint)(TParams.NumBanks - 1);
        nuint idx = (nuint)(freeSlotIdx[(int)bank]++ & (BankSize - 1));
        nuint delta = ix - addr[(int)key];
        tinyHash[(ushort)ix] = (byte)key;
        if (delta > 0xFFFF)
        {
            delta = 0xFFFF;
        }
        ref ForgetfulChainSlot slot = ref banks[(int)((bank << TParams.BankBits) + idx)];
        slot.Delta = (ushort)delta;
        slot.Next = head[(int)key];
        addr[(int)key] = (uint)ix;
        head[(int)key] = (ushort)idx;
    }

    public void StoreRange(ReadOnlySpan<byte> data, nuint mask, nuint ixStart, nuint ixEnd)
    {
        ushort[] freeSlotIdx = _freeSlotIdx!;
        uint[] addr = _addr!;
        ushort[] head = _head!;
        byte[] tinyHash = _tinyHash!;
        ForgetfulChainSlot[] banks = _banks!;
        for (nuint i = ixStart; i < ixEnd; ++i)
        {
            Store(data, mask, i, freeSlotIdx, addr, head, tinyHash, banks);
        }
    }

    public void StitchToPreviousBlock(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer, nuint ringbufferMask)
    {
        if (numBytes >= HashTypeLength - 1 && position >= 3)
        {
            // Prepare the hashes for three last bytes of the last write.
            // These could not be calculated before, since they require knowledge
            // of both the previous and the current block.
            Store(ringbuffer, ringbufferMask, position - 3);
            Store(ringbuffer, ringbufferMask, position - 2);
            Store(ringbuffer, ringbufferMask, position - 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareDistanceCache(Span<int> distanceCache)
    {
        Hash.PrepareDistanceCache(distanceCache, TParams.NumLastDistancesToCheck);
    }

    /// <summary>
    /// REQUIRES: PrepareDistanceCache must be invoked for current distance cache values; if this method is invoked
    /// repeatedly with the same distance cache values, it is enough to invoke PrepareDistanceCache once.
    /// </summary>
    public void FindLongestMatch(ReadOnlySpan<byte> data, nuint ringBufferMask, ReadOnlySpan<int> distanceCache, nuint curIx,
        nuint maxLength, nuint maxBackward, nuint dictionaryDistance, nuint maxDistance, ref HasherSearchResult result)
    {
        uint[] addr = _addr!;
        ushort[] head = _head!;
        byte[] tinyHashes = _tinyHash!;
        nuint curIxMasked = curIx & ringBufferMask;
        ReadOnlySpan<byte> cur = data.Slice((int)curIxMasked);
        // Don't accept a short copy from far away.
        nuint minScore = result.Score;
        nuint bestScore = result.Score;
        nuint bestLen = result.Len;
        nuint key = HashBytes(data, curIxMasked);
        byte tinyHash = (byte)key;
        result.Len = 0;
        result.LenCodeDelta = 0;

        // Try last distance first.
        for (int i = 0; i < TParams.NumLastDistancesToCheck; ++i)
        {
            nuint backward = (nuint)distanceCache[i];
            nuint prevIx = curIx - backward;
            // For distance code 0 we want to consider 2-byte matches.
            if (i > 0 && tinyHashes[(ushort)prevIx] != tinyHash)
            {
                continue;
            }
            if (prevIx >= curIx || backward > maxBackward)
            {
                continue;
            }
            prevIx &= ringBufferMask;
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(data.Slice((int)prevIx), cur, maxLength);
                if (len >= 2)
                {
                    nuint score = Hash.BackwardReferenceScoreUsingLastDistance(len);
                    if (bestScore < score)
                    {
                        if (i != 0)
                        {
                            score -= Hash.BackwardReferencePenaltyUsingLastDistance((nuint)i);
                        }
                        if (bestScore < score)
                        {
                            bestScore = score;
                            bestLen = len;
                            result.Len = bestLen;
                            result.Distance = backward;
                            result.Score = bestScore;
                        }
                    }
                }
            }
        }

        // We require matches of len >4, so increase best_len to 3, so we can compare 4 bytes all the time.
        if (bestLen < 3)
        {
            bestLen = 3;
        }

        {
            nuint bank = key & (nuint)(TParams.NumBanks - 1);
            Span<ForgetfulChainSlot> slots = _banks.AsSpan((int)(bank << TParams.BankBits), BankSize);
            nuint backward = 0;
            nuint hops = _maxHops;
            nuint delta = curIx - addr[(int)key];
            nuint slot = head[(int)key];
            // The reference implementation skips the candidate if "cur_ix_masked + best_len > ring_buffer_mask"
            // in every iteration (without any other effect), but it only changes when best_len changes, so it is
            // checked up-front and after each improvement. The 4 bytes ending at best_len + 1 are cached for the same reason.
            if (curIxMasked + bestLen <= ringBufferMask)
            {
                uint curTail4 = BinaryPrimitives.ReadUInt32LittleEndian(cur.Slice((int)bestLen - 3));
                nuint prevLimit = ringBufferMask - bestLen;
                // tailData[prevIx] == data[prevIx + bestLen - 3]
                ReadOnlySpan<byte> tailData = data.Slice((int)bestLen - 3);
                while (hops-- != 0)
                {
                    nuint last = slot;
                    backward += delta;
                    if (backward > maxBackward)
                    {
                        break;
                    }
                    nuint prevIx = (curIx - backward) & ringBufferMask;
                    ForgetfulChainSlot lastSlot = slots[(int)last];
                    slot = lastSlot.Next;
                    delta = lastSlot.Delta;
                    if (prevIx > prevLimit ||
                        // Compare 4 bytes ending at best_len + 1.
                        curTail4 != BinaryPrimitives.ReadUInt32LittleEndian(tailData.Slice((int)prevIx, 4)))
                    {
                        continue;
                    }
                    nuint len = FindMatchLength.FindMatchLengthWithLimit(data.Slice((int)prevIx), cur, maxLength);
                    if (len >= 4)
                    {
                        // Comparing for >= 3 does not change the semantics, but just saves for a few unnecessary
                        // binary logarithms in backward reference score, since we are not interested in such short matches.
                        nuint score = Hash.BackwardReferenceScore(len, backward);
                        if (bestScore < score)
                        {
                            bestScore = score;
                            bestLen = len;
                            result.Len = bestLen;
                            result.Distance = backward;
                            result.Score = bestScore;
                            if (curIxMasked + bestLen > ringBufferMask)
                            {
                                break;
                            }
                            curTail4 = BinaryPrimitives.ReadUInt32LittleEndian(cur.Slice((int)bestLen - 3));
                            prevLimit = ringBufferMask - bestLen;
                            tailData = data.Slice((int)bestLen - 3);
                        }
                    }
                }
            }
            Store(data, ringBufferMask, curIx);
        }

        if (result.Score == minScore)
        {
            Hash.SearchInStaticDictionary(_common, cur, maxLength, dictionaryDistance, maxDistance, ref result, shallow: false);
        }
    }
}
