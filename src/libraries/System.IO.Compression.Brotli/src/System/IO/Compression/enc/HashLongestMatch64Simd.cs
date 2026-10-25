// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/hash_longest_match64_simd_inc.h.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>
/// A (forgetful) hash table to the data seen by the compressor, to help create backward references to previous data.
/// This is a hash map of fixed size (bucket_size_) to a ring buffer of fixed size (block_size_).
/// The ring buffer contains the last block_size_ index positions of the given hash key in the compressed data.
/// The SIMD variant of <see cref="HashLongestMatch64"/> keeps an 8-bit tag of the hash per entry to quickly find candidates.
/// </summary>
internal struct HashLongestMatch64Simd : IHasher
{
    private const int TagHashBits = 8;
    private const uint TagHashMask = (1 << TagHashBits) - 1;

    /// <summary>Number of hash buckets.</summary>
    private nuint _bucketSize;
    /// <summary>Only block_size_ newest backward references are kept, and the older are forgotten.</summary>
    private nuint _blockSize;
    /// <summary>Hash multiplier tuned to match length.</summary>
    private ulong _hashMul;
    /// <summary>Mask for accessing entries in a block (in a ring-buffer manner).</summary>
    private uint _blockMask;
    private int _blockBits;
    private int _numLastDistancesToCheck;

    /// <summary>Shortcut to the common hasher state.</summary>
    private Hasher _common;

    /// <summary>Number of entries in a particular bucket (counting down from 65535). uint16_t[bucket_size]</summary>
    private ushort[]? _num;
    /// <summary>uint8_t[bucket_size * block_size]</summary>
    private byte[]? _tags;
    /// <summary>Buckets containing block_size_ of backward references. uint32_t[bucket_size * block_size]</summary>
    private uint[]? _buckets;

    public static nuint HashTypeLength => 8;
    public static nuint StoreLookahead => 8;

    /// <summary>HashBytes is the function that chooses the bucket to place the address in.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint HashBytes(ReadOnlySpan<byte> data, nuint index, ulong hashMul)
    {
        ulong h = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice((int)index, sizeof(ulong))) * hashMul;
        // The higher bits contain more mixture from the multiplication, so we take our results from there.
        return (nuint)(h >> (64 - 15 - TagHashBits));
    }

    public void Initialize(Hasher common, in EncoderParams parameters, bool oneShot, nuint inputSize)
    {
        _common = common;
        _hashMul = HashBase.HashMul64 << (64 - 5 * 8);
        Debug.Assert(common.Params.BucketBits == 15);
        _bucketSize = (nuint)1 << common.Params.BucketBits;
        _blockSize = (nuint)1 << common.Params.BlockBits;
        _blockMask = (uint)(_blockSize - 1);
        _blockBits = common.Params.BlockBits;
        _numLastDistancesToCheck = common.Params.NumLastDistancesToCheck;
        _num = Hash.RentArray<ushort>(_bucketSize);
        _tags = Hash.RentArray<byte>(_bucketSize * _blockSize);
        _buckets = Hash.RentArray<uint>(_bucketSize * _blockSize);
    }

    public void Destroy()
    {
        Hash.ReturnArray(ref _num);
        Hash.ReturnArray(ref _tags);
        Hash.ReturnArray(ref _buckets);
    }

    public void Prepare(bool oneShot, nuint inputSize, ReadOnlySpan<byte> data)
    {
        Span<ushort> num = _num.AsSpan(0, (int)_bucketSize);
        // Partial preparation is 100 times slower (per socket).
        nuint partialPrepareThreshold = _bucketSize >> 6;
        if (oneShot && inputSize <= partialPrepareThreshold)
        {
            for (nuint i = 0; i < inputSize; ++i)
            {
                nuint hash = HashBytes(data, i, _hashMul);
                nuint key = hash >> TagHashBits;
                num[(int)key] = 65535;
            }
        }
        else
        {
            num.Fill(65535);
        }
    }

    /// <summary>
    /// Look at 4 bytes at &amp;data[ix &amp; mask].
    /// Compute a hash from these, and store the value of ix at that position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix) =>
        Store(data, mask, ix, _num!, _tags!, _buckets!, _hashMul, _blockMask, _blockBits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix, ushort[] num, byte[] tags, uint[] buckets,
        ulong hashMul, uint blockMask, int blockBits)
    {
        nuint hash = HashBytes(data, ix & mask, hashMul);
        nuint key = hash >> TagHashBits;
        byte tag = (byte)(hash & TagHashMask);
        nuint minorIx = num[(int)key] & blockMask;
        nuint offset = minorIx + (key << blockBits);
        --num[(int)key];
        buckets[(int)offset] = (uint)ix;
        tags[(int)offset] = tag;
    }

    public void StoreRange(ReadOnlySpan<byte> data, nuint mask, nuint ixStart, nuint ixEnd)
    {
        ushort[] num = _num!;
        byte[] tags = _tags!;
        uint[] buckets = _buckets!;
        ulong hashMul = _hashMul;
        uint blockMask = _blockMask;
        int blockBits = _blockBits;
        if (ixStart >= ixEnd)
        {
            return;
        }

        nuint startMasked = ixStart & mask;
        nuint count = ixEnd - ixStart;
        if (startMasked + count - 1 > mask)
        {
            // The range wraps around the end of the ring buffer.
            for (nuint i = ixStart; i < ixEnd; ++i)
            {
                Store(data, mask, i, num, tags, buckets, hashMul, blockMask, blockBits);
            }
            return;
        }

        // (ixStart + k) & mask == startMasked + k for all positions of the range: walk the bytes with a sliding span,
        // so that each position needs a single load without further bounds checks.
        ReadOnlySpan<byte> d = data.Slice((int)startMasked, (int)count + (sizeof(ulong) - 1));
        uint ix = (uint)ixStart;
        for (; d.Length >= sizeof(ulong); d = d.Slice(1), ix++)
        {
            nuint hash = (nuint)((BinaryPrimitives.ReadUInt64LittleEndian(d) * hashMul) >> (64 - 15 - TagHashBits));
            nuint key = hash >> TagHashBits;
            nuint minorIx = num[(int)key] & blockMask;
            nuint offset = minorIx + (key << blockBits);
            --num[(int)key];
            buckets[(int)offset] = ix;
            tags[(int)offset] = (byte)(hash & TagHashMask);
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
        Hash.PrepareDistanceCache(distanceCache, _numLastDistancesToCheck);
    }

    /// <summary>
    /// REQUIRES: PrepareDistanceCache must be invoked for current distance cache values; if this method is invoked
    /// repeatedly with the same distance cache values, it is enough to invoke PrepareDistanceCache once.
    /// </summary>
    public void FindLongestMatch(ReadOnlySpan<byte> data, nuint ringBufferMask, ReadOnlySpan<int> distanceCache, nuint curIx,
        nuint maxLength, nuint maxBackward, nuint dictionaryDistance, nuint maxDistance, ref HasherSearchResult result)
    {
        ushort[] num = _num!;
        nuint curIxMasked = curIx & ringBufferMask;
        ReadOnlySpan<byte> cur = data.Slice((int)curIxMasked);
        // Don't accept a short copy from far away.
        nuint minScore = result.Score;
        nuint bestScore = result.Score;
        nuint bestLen = result.Len;
        // Precalculate the hash key and prefetch the bucket.
        nuint hash = HashBytes(data, curIxMasked, _hashMul);
        nuint key = hash >> TagHashBits;
        int bucketOffset = (int)(key << _blockBits);
        nuint blockSize = _blockSize;
        Span<uint> bucket = _buckets.AsSpan(bucketOffset, (int)blockSize);
        Span<byte> tagBucket = _tags.AsSpan(bucketOffset, (int)blockSize);
        uint blockMask = _blockMask;
        result.Len = 0;
        result.LenCodeDelta = 0;

        // Try last distance first.
        int numLastDistancesToCheck = _numLastDistancesToCheck;
        for (int i = 0; i < numLastDistancesToCheck; ++i)
        {
            nuint backward = (nuint)distanceCache[i];
            nuint prevIx = curIx - backward;
            if (prevIx >= curIx)
            {
                continue;
            }
            if (backward > maxBackward)
            {
                continue;
            }
            prevIx &= ringBufferMask;

            if (curIxMasked + bestLen > ringBufferMask)
            {
                break;
            }
            if (prevIx + bestLen > ringBufferMask ||
                cur[(int)bestLen] != data[(int)(prevIx + bestLen)])
            {
                continue;
            }
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(data.Slice((int)prevIx), cur, maxLength);
                if (len >= 3 || (len == 2 && i < 2))
                {
                    // Comparing for >= 2 does not change the semantics, but just saves for a few unnecessary
                    // binary logarithms in backward reference score, since we are not interested in such short matches.
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
        // We require matches of len >4, so increase best_len to 3, so we can compare 4 bytes all the time.
        if (bestLen < 3)
        {
            bestLen = 3;
        }

        {
            byte tag = (byte)(hash & TagHashMask);
            uint first4 = BinaryPrimitives.ReadUInt32LittleEndian(cur);
            nuint maxLengthM4 = maxLength - 4;
            ushort numKey = num[(int)key];
            nuint head = (nuint)(numKey + 1) & blockMask;
            ulong matches = MatchingTagMask.GetMatchingTagMask(blockSize / 16, tag, tagBucket, head);
            int maskShift = MatchingTagMask.GetMaskShift(blockSize / 16);
            // Mask off any matches from uninitialized tags.
            ushort n = (ushort)(65535 - numKey);
            ulong blockHasUnusedSlots = blockSize > n ? 1UL : 0UL;
            ulong mask = (blockHasUnusedSlots << ((n << maskShift) & (64 - 1))) - 1;
            matches &= mask;
            // The reference implementation checks "cur_ix_masked + best_len > ring_buffer_mask" (and then stops
            // the search) in every iteration, but it only changes when best_len changes, so it is checked up-front
            // and after each improvement. The 4 bytes ending at best_len + 1 are cached for the same reason.
            if (curIxMasked + bestLen <= ringBufferMask)
            {
                uint curTail4 = BinaryPrimitives.ReadUInt32LittleEndian(cur.Slice((int)bestLen - 3));
                nuint prevLimit = ringBufferMask - bestLen;
                // tailData[prevIx] == data[prevIx + bestLen - 3]
                ReadOnlySpan<byte> tailData = data.Slice((int)bestLen - 3);
                for (; matches > 0; matches &= matches - 1)
                {
                    nuint rbIndex = (head + (nuint)(ulong.TrailingZeroCount(matches) >> maskShift)) & blockMask;
                    nuint prevIx = bucket[(int)rbIndex];
                    nuint backward = curIx - prevIx;
                    if (backward > maxBackward)
                    {
                        break;
                    }
                    prevIx &= ringBufferMask;
                    if (prevIx > prevLimit ||
                        // Compare 4 bytes ending at best_len + 1.
                        curTail4 != BinaryPrimitives.ReadUInt32LittleEndian(tailData.Slice((int)prevIx, 4)))
                    {
                        continue;
                    }
                    ReadOnlySpan<byte> prev = data.Slice((int)prevIx);
                    if (first4 != BinaryPrimitives.ReadUInt32LittleEndian(prev))
                    {
                        continue;
                    }
                    nuint len = FindMatchLength.FindMatchLengthWithLimit(prev.Slice(4), cur.Slice(4), maxLengthM4) + 4;
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
            bucket[(int)(numKey & blockMask)] = (uint)curIx;
            tagBucket[(int)(numKey & blockMask)] = tag;
            num[(int)key] = (ushort)(numKey - 1);
        }

        if (minScore == result.Score)
        {
            Hash.SearchInStaticDictionary(_common, cur, maxLength, dictionaryDistance, maxDistance, ref result, shallow: false);
        }
    }
}
