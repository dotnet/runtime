// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/hash_longest_match_quickly_inc.h.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Template parameters of <see cref="HashLongestMatchQuickly{TParams}"/>.</summary>
internal interface IHashLongestMatchQuicklyParams
{
    static abstract int BucketBits { get; }
    static abstract int BucketSweepBits { get; }
    static abstract int HashLen { get; }
    static abstract bool UseDictionary { get; }
}

// For BUCKET_SWEEP_BITS == 0, enabling the dictionary lookup makes compression
// a little faster (0.5% - 1%) and it compresses 0.15% better on small text and HTML inputs.

internal struct H2Params : IHashLongestMatchQuicklyParams
{
    public static int BucketBits => 16;
    public static int BucketSweepBits => 0;
    public static int HashLen => 5;
    public static bool UseDictionary => true;
}

internal struct H3Params : IHashLongestMatchQuicklyParams
{
    public static int BucketBits => 16;
    public static int BucketSweepBits => 1;
    public static int HashLen => 5;
    public static bool UseDictionary => false;
}

internal struct H4Params : IHashLongestMatchQuicklyParams
{
    public static int BucketBits => 17;
    public static int BucketSweepBits => 2;
    public static int HashLen => 5;
    public static bool UseDictionary => true;
}

internal struct H54Params : IHashLongestMatchQuicklyParams
{
    public static int BucketBits => 20;
    public static int BucketSweepBits => 2;
    public static int HashLen => 7;
    public static bool UseDictionary => false;
}

/// <summary>
/// A (forgetful) hash table to the data seen by the compressor, to help create backward references to previous data.
/// This is a hash map of fixed size (BUCKET_SIZE).
/// </summary>
internal struct HashLongestMatchQuickly<TParams> : IHasher
    where TParams : struct, IHashLongestMatchQuicklyParams
{
    /// <summary>Shortcut to the common hasher state.</summary>
    private Hasher _common;

    /// <summary>uint32_t[BUCKET_SIZE]</summary>
    private uint[]? _buckets;

    private static int BucketSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 1 << TParams.BucketBits;
    }

    private static uint BucketMask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)BucketSize - 1;
    }

    private static int BucketSweep
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 1 << TParams.BucketSweepBits;
    }

    private static uint BucketSweepMask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)(BucketSweep - 1) << 3;
    }

    public static nuint HashTypeLength => 8;
    public static nuint StoreLookahead => 8;

    /// <summary>
    /// HashBytes is the function that chooses the bucket to place the address in.
    /// The HashLongestMatch and HashLongestMatchQuickly classes have separate, different implementations of hashing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytes(ReadOnlySpan<byte> data, nuint index) =>
        HashValue(BinaryPrimitives.ReadUInt64LittleEndian(data.Slice((int)index, sizeof(ulong))));

    /// <summary>Hashes the (little-endian) 8 bytes loaded at the position; only the low HASH_LEN bytes are used.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashValue(ulong value)
    {
        // (value << (64 - 8 * HASH_LEN)) * kHashMul64, with the shift folded into the multiplier.
        ulong h = value * (HashBase.HashMul64 << (64 - 8 * TParams.HashLen));
        // The higher bits contain more mixture from the multiplication, so we take our results from there.
        return (uint)(h >> (64 - TParams.BucketBits));
    }

    public void Initialize(Hasher common, in EncoderParams parameters, bool oneShot, nuint inputSize)
    {
        _common = common;
        _buckets = Hash.RentArray<uint>((nuint)BucketSize);
    }

    public void Destroy()
    {
        Hash.ReturnArray(ref _buckets);
    }

    public void Prepare(bool oneShot, nuint inputSize, ReadOnlySpan<byte> data)
    {
        Span<uint> buckets = _buckets.AsSpan(0, BucketSize);
        // Partial preparation is 100 times slower (per socket).
        nuint partialPrepareThreshold = (nuint)(BucketSize >> 5);
        if (oneShot && inputSize <= partialPrepareThreshold)
        {
            for (nuint i = 0; i < inputSize; ++i)
            {
                uint key = HashBytes(data, i);
                if (BucketSweep == 1)
                {
                    buckets[(int)key] = 0;
                }
                else
                {
                    for (uint j = 0; j < (uint)BucketSweep; ++j)
                    {
                        buckets[(int)((key + (j << 3)) & BucketMask)] = 0;
                    }
                }
            }
        }
        else
        {
            // It is not strictly necessary to fill this buffer here, but not filling will make the results of
            // the compression stochastic (but correct). This is because random data would cause the system to
            // find accidentally good backward references here and there.
            buckets.Clear();
        }
    }

    /// <summary>
    /// Look at 5 bytes at &amp;data[ix &amp; mask].
    /// Compute a hash from these, and store the value somewhere within [ix .. ix+3].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix) => Store(data, mask, ix, _buckets!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix, uint[] buckets)
    {
        uint key = HashBytes(data, ix & mask);
        if (BucketSweep == 1)
        {
            buckets[(int)key] = (uint)ix;
        }
        else
        {
            // Wiggle the value with the bucket sweep range.
            uint off = (uint)ix & BucketSweepMask;
            buckets[(int)((key + off) & BucketMask)] = (uint)ix;
        }
    }

    public void StoreRange(ReadOnlySpan<byte> data, nuint mask, nuint ixStart, nuint ixEnd)
    {
        if (ixStart >= ixEnd)
        {
            return;
        }

        nuint startMasked = ixStart & mask;
        nuint count = ixEnd - ixStart;
        if (startMasked + count - 1 > mask)
        {
            // The range wraps around the end of the ring buffer.
            uint[] buckets = _buckets!;
            for (nuint i = ixStart; i < ixEnd; ++i)
            {
                Store(data, mask, i, buckets);
            }
            return;
        }

        // (ixStart + k) & mask == startMasked + k for all positions of the range: walk the bytes with a sliding span,
        // so that each position needs a single 8-byte load without further bounds checks.
        Span<uint> table = _buckets.AsSpan(0, BucketSize);
        ReadOnlySpan<byte> d = data.Slice((int)startMasked, (int)count + (sizeof(ulong) - 1));
        uint ix = (uint)ixStart;
        for (; d.Length >= sizeof(ulong); d = d.Slice(1), ix++)
        {
            uint key = HashValue(BinaryPrimitives.ReadUInt64LittleEndian(d));
            if (BucketSweep == 1)
            {
                table[(int)(key & BucketMask)] = ix;
            }
            else
            {
                // Wiggle the value with the bucket sweep range.
                table[(int)((key + (ix & BucketSweepMask)) & BucketMask)] = ix;
            }
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FindLongestMatch(ReadOnlySpan<byte> data, nuint ringBufferMask, ReadOnlySpan<int> distanceCache, nuint curIx,
        nuint maxLength, nuint maxBackward, nuint dictionaryDistance, nuint maxDistance, ref HasherSearchResult result)
    {
        Span<uint> buckets = _buckets.AsSpan(0, BucketSize);
        nuint bestLenIn = result.Len;
        nuint curIxMasked = curIx & ringBufferMask;
        ReadOnlySpan<byte> cur = data.Slice((int)curIxMasked);
        int compareChar = cur[(int)bestLenIn];
        nuint key = HashBytes(data, curIxMasked);
        nuint keyOut = 0;
        nuint minScore = result.Score;
        nuint bestScore = result.Score;
        nuint bestLen = bestLenIn;
        nuint cachedBackward = (nuint)distanceCache[0];
        nuint prevIx = curIx - cachedBackward;

        result.LenCodeDelta = 0;
        if (prevIx < curIx)
        {
            prevIx &= (uint)ringBufferMask;
            if (compareChar == data[(int)(prevIx + bestLen)])
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(data.Slice((int)prevIx), cur, maxLength);
                if (len >= 4)
                {
                    nuint score = Hash.BackwardReferenceScoreUsingLastDistance(len);
                    if (bestScore < score)
                    {
                        result.Len = len;
                        result.Distance = cachedBackward;
                        result.Score = score;
                        if (BucketSweep == 1)
                        {
                            buckets[(int)(key & BucketMask)] = (uint)curIx;
                            return;
                        }
                        else
                        {
                            bestLen = len;
                            bestScore = score;
                            compareChar = cur[(int)len];
                        }
                    }
                }
            }
        }

        if (BucketSweep == 1)
        {
            // Only one to look for, don't bother to prepare for a loop.
            prevIx = buckets[(int)(key & BucketMask)];
            buckets[(int)(key & BucketMask)] = (uint)curIx;
            nuint backward = curIx - prevIx;
            prevIx &= (uint)ringBufferMask;
            if (compareChar != data[(int)(prevIx + bestLenIn)])
            {
                return;
            }
            if (backward == 0 || backward > maxBackward)
            {
                return;
            }
            nuint len = FindMatchLength.FindMatchLengthWithLimit(data.Slice((int)prevIx), cur, maxLength);
            if (len >= 4)
            {
                nuint score = Hash.BackwardReferenceScore(len, backward);
                if (bestScore < score)
                {
                    result.Len = len;
                    result.Distance = backward;
                    result.Score = score;
                    return;
                }
            }
        }
        else
        {
            keyOut = (key + (curIx & BucketSweepMask)) & BucketMask;
            for (nuint i = 0; i < (nuint)BucketSweep; ++i)
            {
                prevIx = buckets[(int)((key + (i << 3)) & BucketMask)];
                nuint backward = curIx - prevIx;
                prevIx &= (uint)ringBufferMask;
                if (compareChar != data[(int)(prevIx + bestLen)])
                {
                    continue;
                }
                if (backward == 0 || backward > maxBackward)
                {
                    continue;
                }
                nuint len = FindMatchLength.FindMatchLengthWithLimit(data.Slice((int)prevIx), cur, maxLength);
                if (len >= 4)
                {
                    nuint score = Hash.BackwardReferenceScore(len, backward);
                    if (bestScore < score)
                    {
                        bestLen = len;
                        result.Len = len;
                        compareChar = cur[(int)len];
                        bestScore = score;
                        result.Score = score;
                        result.Distance = backward;
                    }
                }
            }
        }

        if (TParams.UseDictionary && minScore == result.Score)
        {
            Hash.SearchInStaticDictionary(_common, cur, maxLength, dictionaryDistance, maxDistance, ref result, shallow: true);
        }

        if (BucketSweep != 1)
        {
            buckets[(int)(keyOut & BucketMask)] = (uint)curIx;
        }
    }
}
