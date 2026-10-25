// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/hash_longest_match_simd_inc.h
// and c/enc/matching_tag_mask.h.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.IO.Compression;

/// <summary>matching_tag_mask.h</summary>
internal static class MatchingTagMask
{
    /// <summary>
    /// Returns a bit mask of the tags in <paramref name="tagBucket"/> (16 * <paramref name="chunkCount"/> bytes) that
    /// are equal to <paramref name="tag"/>, in circular search order starting at <paramref name="head"/>.
    /// Each tag occupies 1 &lt;&lt; GetMaskShift(chunkCount) bits, with at most one bit set per tag.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetMatchingTagMask(nuint chunkCount, byte tag, ReadOnlySpan<byte> tagBucket, nuint head)
    {
        Vector128<byte> comparisonMask = Vector128.Create(tag);
        if (AdvSimd.Arm64.IsSupported)
        {
            return GetMatchingTagMaskArm64(chunkCount, tagBucket, comparisonMask, (int)head);
        }

        int count = (int)head;
        if (chunkCount == 1)
        {
            ushort value = (ushort)MatchMask(tagBucket, 0, comparisonMask);
            count &= 0x0F;
            return ushort.RotateRight(value, count);
        }
        if (chunkCount == 2)
        {
            uint value = MatchMask(tagBucket, 0, comparisonMask) | (MatchMask(tagBucket, 16, comparisonMask) << 16);
            return uint.RotateRight(value, count);
        }

        ulong matches = 0;
        for (int i = 0; (nuint)i < chunkCount && i < 4; i++)
        {
            matches |= (ulong)MatchMask(tagBucket, 16 * i, comparisonMask) << (16 * i);
        }
        return ulong.RotateRight(matches, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaskShift(nuint chunkCount) =>
        AdvSimd.Arm64.IsSupported ? (chunkCount == 1 ? 2 : chunkCount == 2 ? 1 : 0) : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetMatchingTagMaskArm64(nuint chunkCount, ReadOnlySpan<byte> tagBucket, Vector128<byte> comparisonMask, int head)
    {
        Vector128<byte> a = Vector128.Equals(Vector128.Create(tagBucket.Slice(0, 16)), comparisonMask);
        if (chunkCount == 1)
        {
            // SHRN preserves four equal bits per tag; keep one for candidate iteration.
            ulong matches = AdvSimd.ShiftRightLogicalNarrowingLower(a.AsUInt16(), 4).AsUInt64().ToScalar();
            return ulong.RotateRight(matches, head << 2) & 0x8888888888888888UL;
        }

        Vector128<byte> b = Vector128.Equals(Vector128.Create(tagBucket.Slice(16, 16)), comparisonMask);
        if (chunkCount == 2)
        {
            // Interleave pairs of tags into two-bit groups before narrowing to a single 64-bit mask.
            Vector64<byte> even = AdvSimd.ShiftRightLogicalNarrowingLower(AdvSimd.Arm64.UnzipEven(a.AsUInt16(), b.AsUInt16()), 6);
            Vector64<byte> odd = AdvSimd.ShiftRightLogicalNarrowingLower(AdvSimd.Arm64.UnzipOdd(a.AsUInt16(), b.AsUInt16()), 6);
            ulong matches = AdvSimd.ShiftLeftAndInsert(even, odd, 4).AsUInt64().ToScalar();
            return ulong.RotateRight(matches, head << 1) & 0xAAAAAAAAAAAAAAAAUL;
        }

        Vector128<byte> c = Vector128.Equals(Vector128.Create(tagBucket.Slice(32, 16)), comparisonMask);
        Vector128<byte> d = Vector128.Equals(Vector128.Create(tagBucket.Slice(48, 16)), comparisonMask);
        Vector128<byte> weights = Vector128.Create((byte)1, 2, 4, 8, 16, 32, 64, 128, 1, 2, 4, 8, 16, 32, 64, 128);
        Vector128<byte> ab = AdvSimd.Arm64.AddPairwise(a & weights, b & weights);
        Vector128<byte> cd = AdvSimd.Arm64.AddPairwise(c & weights, d & weights);
        Vector128<byte> combined = AdvSimd.Arm64.AddPairwise(ab, cd);
        ulong compact = AdvSimd.Arm64.AddPairwise(combined, combined).AsUInt64().ToScalar();
        return ulong.RotateRight(compact, head);
    }

    /// <summary>Returns the 16-bit mask of the bytes of the 16-byte chunk at <paramref name="offset"/> that are equal to the tag.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MatchMask(ReadOnlySpan<byte> tagBucket, int offset, Vector128<byte> comparisonMask) =>
        Vector128.Equals(Vector128.Create(tagBucket.Slice(offset, Vector128<byte>.Count)), comparisonMask).ExtractMostSignificantBits();
}

/// <summary>
/// A (forgetful) hash table to the data seen by the compressor, to help create backward references to previous data.
/// This is a hash map of fixed size (bucket_size_) to a ring buffer of fixed size (block_size_).
/// The ring buffer contains the last block_size_ index positions of the given hash key in the compressed data.
/// The SIMD variant keeps an 8-bit tag of the hash per entry to quickly find candidates.
/// </summary>
internal struct HashLongestMatchSimd : IHasher
{
    private const int TagHashBits = 8;
    private const uint TagHashMask = (1 << TagHashBits) - 1;

    /// <summary>Number of hash buckets.</summary>
    private nuint _bucketSize;
    /// <summary>Only block_size_ newest backward references are kept, and the older are forgotten.</summary>
    private nuint _blockSize;
    /// <summary>Left-shift for computing hash bucket index from hash value.</summary>
    private int _hashShift;
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

    public static nuint HashTypeLength => 4;
    public static nuint StoreLookahead => 4;

    /// <summary>HashBytes is the function that chooses the bucket to place the address in.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytes(ReadOnlySpan<byte> data, nuint index, int shift)
    {
        uint h = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice((int)index, sizeof(uint))) * HashBase.HashMul32;
        // The higher bits contain more mixture from the multiplication, so we take our results from there.
        return h >> shift;
    }

    public void Initialize(Hasher common, in EncoderParams parameters, bool oneShot, nuint inputSize)
    {
        _common = common;
        _hashShift = 32 - common.Params.BucketBits - TagHashBits;
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
                uint hash = HashBytes(data, i, _hashShift);
                uint key = hash >> TagHashBits;
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
        Store(data, mask, ix, _num!, _tags!, _buckets!, _hashShift, _blockMask, _blockBits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix, ushort[] num, byte[] tags, uint[] buckets,
        int hashShift, uint blockMask, int blockBits)
    {
        nuint hash = HashBytes(data, ix & mask, hashShift);
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
        int hashShift = _hashShift;
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
                Store(data, mask, i, num, tags, buckets, hashShift, blockMask, blockBits);
            }
            return;
        }

        // (ixStart + k) & mask == startMasked + k for all positions of the range: walk the bytes with a sliding span,
        // so that each position needs a single load without further bounds checks.
        ReadOnlySpan<byte> d = data.Slice((int)startMasked, (int)count + (sizeof(uint) - 1));
        uint ix = (uint)ixStart;
        for (; d.Length >= sizeof(uint); d = d.Slice(1), ix++)
        {
            nuint hash = (BinaryPrimitives.ReadUInt32LittleEndian(d) * HashBase.HashMul32) >> hashShift;
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
        uint hash = HashBytes(data, curIxMasked, _hashShift);
        uint key = hash >> TagHashBits;
        int bucketOffset = (int)((nuint)key << _blockBits);
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
