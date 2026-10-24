// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/hash.h and c/enc/hash_base.h.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Basic common hash functions / constants (hash_base.h).</summary>
internal static class HashBase
{
    // kHashMul32 multiplier has these properties:
    // * The multiplier must be odd. Otherwise we may lose the highest bit.
    // * No long streaks of ones or zeros.
    // * There is no effort to ensure that it is a prime, the oddity is enough for this use.
    // * The number has been tuned heuristically against compression benchmarks.
    public const uint HashMul32 = 0x1E35A7BD;
    public const ulong HashMul64 = 0x1FE35A7BD3579BD3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Hash14(ReadOnlySpan<byte> data)
    {
        uint h = BinaryPrimitives.ReadUInt32LittleEndian(data) * HashMul32;
        // The higher bits contain more mixture from the multiplication, so we take our results from there.
        return h >> (32 - 14);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Hash15(ReadOnlySpan<byte> data)
    {
        uint h = BinaryPrimitives.ReadUInt32LittleEndian(data) * HashMul32;
        // The higher bits contain more mixture from the multiplication, so we take our results from there.
        return h >> (32 - 15);
    }
}

/// <summary>Result of the longest match search.</summary>
internal struct HasherSearchResult
{
    public nuint Len;
    public nuint Distance;
    public nuint Score;
    /// <summary>== len_code - len</summary>
    public int LenCodeDelta;
}

/// <summary>A backward match found by <see cref="HashToBinaryTree"/> / static dictionary search.</summary>
internal struct BackwardMatch
{
    public uint Distance;
    public uint LengthAndCode;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Init(nuint dist, nuint len)
    {
        Distance = (uint)dist;
        LengthAndCode = (uint)(len << 5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitDictionary(nuint dist, nuint len, nuint lenCode)
    {
        Distance = (uint)dist;
        LengthAndCode = (uint)((len << 5) | (len == lenCode ? 0 : lenCode));
    }

    public readonly nuint Length => LengthAndCode >> 5;

    public readonly nuint LengthCode
    {
        get
        {
            nuint code = LengthAndCode & 31;
            return code != 0 ? code : Length;
        }
    }
}

/// <summary>Lifecycle operations shared by all hasher implementations (the C <c>FN(...)</c> template functions).</summary>
internal interface IHasherBase
{
    static abstract nuint HashTypeLength { get; }
    static abstract nuint StoreLookahead { get; }

    /// <summary>Allocates the hasher tables (HashMemAllocInBytes) and initializes the hasher (Initialize).</summary>
    void Initialize(Hasher common, in EncoderParams parameters, bool oneShot, nuint inputSize);

    /// <summary>Releases the hasher tables.</summary>
    void Destroy();

    void Prepare(bool oneShot, nuint inputSize, ReadOnlySpan<byte> data);

    void StitchToPreviousBlock(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer, nuint ringbufferMask);
}

/// <summary>Hashers usable by <see cref="BackwardReferences.CreateBackwardReferences"/>.</summary>
internal interface IHasher : IHasherBase
{
    void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix);

    void StoreRange(ReadOnlySpan<byte> data, nuint mask, nuint ixStart, nuint ixEnd);

    void PrepareDistanceCache(Span<int> distanceCache);

    /// <summary>
    /// Finds a longest backward match of <c>data[curIx &amp; ringBufferMask]</c> up to the length of
    /// <paramref name="maxLength"/> and stores the position <paramref name="curIx"/> in the hash table.
    /// Does not look for matches longer than <paramref name="maxLength"/>.
    /// Does not look for matches further away than <paramref name="maxBackward"/>.
    /// Writes the best match into <paramref name="result"/>; its score is updated only if a better match is found.
    /// </summary>
    void FindLongestMatch(ReadOnlySpan<byte> data, nuint ringBufferMask, ReadOnlySpan<int> distanceCache, nuint curIx,
        nuint maxLength, nuint maxBackward, nuint dictionaryDistance, nuint maxDistance, ref HasherSearchResult result);
}

/// <summary>Shared helpers of the hashers (hash.h).</summary>
internal static class Hash
{
    public const nuint LiteralByteScore = 135;
    public const nuint DistanceBitPenalty = 30;

    /// <summary>
    /// Score must be positive after applying maximal penalty.
    /// BROTLI_DISTANCE_BIT_PENALTY * 8 * sizeof(size_t); the 64-bit value is used on all platforms.
    /// </summary>
    public const nuint ScoreBase = DistanceBitPenalty * 8 * 8;

    /// <summary>MAX_NUM_MATCHES == 64 + MAX_TREE_SEARCH_DEPTH</summary>
    public const int MaxNumMatchesH10 = 128;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrepareDistanceCache(Span<int> distanceCache, int numDistances)
    {
        if (numDistances > 4)
        {
            int lastDistance = distanceCache[0];
            distanceCache[4] = lastDistance - 1;
            distanceCache[5] = lastDistance + 1;
            distanceCache[6] = lastDistance - 2;
            distanceCache[7] = lastDistance + 2;
            distanceCache[8] = lastDistance - 3;
            distanceCache[9] = lastDistance + 3;
            if (numDistances > 10)
            {
                int nextLastDistance = distanceCache[1];
                distanceCache[10] = nextLastDistance - 1;
                distanceCache[11] = nextLastDistance + 1;
                distanceCache[12] = nextLastDistance - 2;
                distanceCache[13] = nextLastDistance + 2;
                distanceCache[14] = nextLastDistance - 3;
                distanceCache[15] = nextLastDistance + 3;
            }
        }
    }

    /// <summary>
    /// Usually, we always choose the longest backward reference. This function allows for the exception of that rule.
    /// If we choose a backward reference that is further away, it will usually be coded with more bits.
    /// We approximate this by assuming log2(distance). If the distance can be expressed in terms of the
    /// last four distances, we use some heuristic constants to estimate the bits cost. For the first up to
    /// four literals we use the bit cost of the literals from the literal cost model, after that we
    /// use the average bit cost of the cost model.
    /// This function is used to sometimes discard a longer backward reference when it is not much longer
    /// and the bit cost for encoding it is more than the saved literals.
    /// <paramref name="backwardReferenceOffset"/> MUST be positive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint BackwardReferenceScore(nuint copyLength, nuint backwardReferenceOffset) =>
        ScoreBase + LiteralByteScore * copyLength - DistanceBitPenalty * FastLog.Log2FloorNonZero(backwardReferenceOffset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint BackwardReferenceScoreUsingLastDistance(nuint copyLength) =>
        LiteralByteScore * copyLength + ScoreBase + 15;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint BackwardReferencePenaltyUsingLastDistance(nuint distanceShortCode) =>
        (nuint)39 + ((0x1CA10u >> (int)(distanceShortCode & 0xE)) & 0xE);

    /// <summary>
    /// Tests the dictionary word of length <paramref name="len"/> and index <paramref name="wordIdx"/> against
    /// <paramref name="data"/> (the input data starting at the current position).
    /// </summary>
    private static bool TestStaticDictionaryItem(nuint len, nuint wordIdx, ReadOnlySpan<byte> data, nuint maxLength,
        nuint maxBackward, nuint maxDistance, ref HasherSearchResult result)
    {
        nuint offset = BrotliDictionary.OffsetsByLength[(int)len] + len * wordIdx;
        if (len > maxLength)
        {
            return false;
        }

        nuint matchlen = FindMatchLength.FindMatchLengthWithLimit(data, BrotliDictionary.Data.Slice((int)offset), len);
        if (matchlen + EncoderDictionary.CutoffTransformsCount <= len || matchlen == 0)
        {
            return false;
        }

        nuint backward;
        {
            nuint cut = len - matchlen;
            nuint transformId = (cut << 2) + (nuint)((EncoderDictionary.CutoffTransforms >> (int)(cut * 6)) & 0x3F);
            backward = maxBackward + 1 + wordIdx + (transformId << BrotliDictionary.SizeBitsByLength[(int)len]);
        }
        if (backward > maxDistance)
        {
            return false;
        }

        nuint score = BackwardReferenceScore(matchlen, backward);
        if (score < result.Score)
        {
            return false;
        }

        result.Len = matchlen;
        result.LenCodeDelta = (int)len - (int)matchlen;
        result.Distance = backward;
        result.Score = score;
        return true;
    }

    /// <summary>
    /// Searches the static dictionary for <paramref name="data"/> (the input data starting at the current position);
    /// <paramref name="common"/> holds the dictionary lookup statistics used to shut the search down.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SearchInStaticDictionary(Hasher common, ReadOnlySpan<byte> data, nuint maxLength,
        nuint maxBackward, nuint maxDistance, ref HasherSearchResult result, bool shallow)
    {
        // The dictionary search is shut down (and the call is cheap) when it rarely finds matches.
        if (common.DictNumMatches < (common.DictNumLookups >> 7))
        {
            return;
        }

        SearchInStaticDictionaryCore(common, data, maxLength, maxBackward, maxDistance, ref result, shallow);
    }

    private static void SearchInStaticDictionaryCore(Hasher common, ReadOnlySpan<byte> data, nuint maxLength,
        nuint maxBackward, nuint maxDistance, ref HasherSearchResult result, bool shallow)
    {
        ReadOnlySpan<byte> lengths = EncoderDictionary.HashTableLengths;
        ReadOnlySpan<ushort> words = EncoderDictionary.HashTableWords;
        int key = (int)HashBase.Hash14(data) << 1;
        int end = key + (shallow ? 1 : 2);
        for (; key < end; ++key)
        {
            common.DictNumLookups++;
            byte len = lengths[key];
            if (len != 0)
            {
                bool itemMatches = TestStaticDictionaryItem(len, words[key], data, maxLength, maxBackward, maxDistance, ref result);
                if (itemMatches)
                {
                    common.DictNumMatches++;
                }
            }
        }
    }

    /// <summary>Rents a (not cleared) array of at least <paramref name="length"/> elements from the shared pool.</summary>
    internal static T[] RentArray<T>(nuint length)
    {
        Debug.Assert(length <= int.MaxValue);
        return ArrayPool<T>.Shared.Rent((int)length);
    }

    /// <summary>Returns an array rented by <see cref="RentArray{T}"/> to the shared pool and clears the reference.</summary>
    internal static void ReturnArray<T>(ref T[]? array)
    {
        T[]? a = array;
        array = null;
        if (a is not null)
        {
            ReturnArray(a);
        }
    }

    /// <summary>Returns an array rented by <see cref="RentArray{T}"/> to the shared pool.</summary>
    internal static void ReturnArray<T>(T[] array)
    {
        if (array.Length > 0)
        {
            ArrayPool<T>.Shared.Return(array);
        }
    }
}

/// <summary>
/// The C <c>Hasher</c>: the common hasher state (<c>HasherCommon</c>) plus the per-type hasher state
/// (the C union of all hasher types). The constructor is <c>HasherInit</c>.
/// </summary>
internal sealed class Hasher
{
    /// <summary>False before the first invocation of Setup (where the tables are allocated).</summary>
    public bool IsSetup;

    public nuint DictNumLookups;
    public nuint DictNumMatches;

    public HasherParams Params;

    /// <summary>
    /// False if hasher needs to be "prepared" before use (before the first invocation of Setup or after Reset).
    /// "preparation" is hasher data initialization (using input ringbuffer).
    /// </summary>
    public bool IsPrepared;

    internal HashLongestMatchQuickly<H2Params> H2;
    internal HashLongestMatchQuickly<H3Params> H3;
    internal HashLongestMatchQuickly<H4Params> H4;
    internal HashLongestMatch H5;
    internal HashLongestMatch64 H6;
    internal HashToBinaryTree H10;
    internal HashForgetfulChain<H40Params> H40;
    internal HashForgetfulChain<H41Params> H41;
    internal HashForgetfulChain<H42Params> H42;
    internal HashLongestMatchQuickly<H54Params> H54;
    internal HashLongestMatchSimd H58;
    internal HashLongestMatch64Simd H68;

    /// <summary>
    /// Scratch data of the Zopfli backward references search, allocated on first use and reused by the following
    /// meta-blocks of the encoder instance.
    /// </summary>
    internal BackwardReferencesHq.ZopfliCostModel? ZopfliCostModel;

    /// <summary>DestroyHasher: releases the hasher tables. The hasher may be set up again afterwards.</summary>
    public void Destroy()
    {
        if (IsSetup)
        {
            switch (Params.Type)
            {
                case 2: H2.Destroy(); break;
                case 3: H3.Destroy(); break;
                case 4: H4.Destroy(); break;
                case 5: H5.Destroy(); break;
                case 6: H6.Destroy(); break;
                case 10: H10.Destroy(); break;
                case 40: H40.Destroy(); break;
                case 41: H41.Destroy(); break;
                case 42: H42.Destroy(); break;
                case 54: H54.Destroy(); break;
                case 58: H58.Destroy(); break;
                case 68: H68.Destroy(); break;
            }
        }
        IsSetup = false;
        ZopfliCostModel = null;
    }

    /// <summary>HasherReset</summary>
    public void Reset()
    {
        IsPrepared = false;
    }

    private void Setup(ref EncoderParams parameters, ReadOnlySpan<byte> data, nuint position, nuint inputSize, bool isLast)
    {
        bool oneShot = position == 0 && isLast;
        if (!IsSetup)
        {
            Quality.ChooseHasher(parameters, ref parameters.Hasher);
            Params = parameters.Hasher;
            DictNumLookups = 0;
            DictNumMatches = 0;
            switch (Params.Type)
            {
                case 2: H2.Initialize(this, parameters, oneShot, inputSize); break;
                case 3: H3.Initialize(this, parameters, oneShot, inputSize); break;
                case 4: H4.Initialize(this, parameters, oneShot, inputSize); break;
                case 5: H5.Initialize(this, parameters, oneShot, inputSize); break;
                case 6: H6.Initialize(this, parameters, oneShot, inputSize); break;
                case 10: H10.Initialize(this, parameters, oneShot, inputSize); break;
                case 40: H40.Initialize(this, parameters, oneShot, inputSize); break;
                case 41: H41.Initialize(this, parameters, oneShot, inputSize); break;
                case 42: H42.Initialize(this, parameters, oneShot, inputSize); break;
                case 54: H54.Initialize(this, parameters, oneShot, inputSize); break;
                case 58: H58.Initialize(this, parameters, oneShot, inputSize); break;
                case 68: H68.Initialize(this, parameters, oneShot, inputSize); break;
            }
            Reset();
            IsSetup = true;
        }

        if (!IsPrepared)
        {
            switch (Params.Type)
            {
                case 2: H2.Prepare(oneShot, inputSize, data); break;
                case 3: H3.Prepare(oneShot, inputSize, data); break;
                case 4: H4.Prepare(oneShot, inputSize, data); break;
                case 5: H5.Prepare(oneShot, inputSize, data); break;
                case 6: H6.Prepare(oneShot, inputSize, data); break;
                case 10: H10.Prepare(oneShot, inputSize, data); break;
                case 40: H40.Prepare(oneShot, inputSize, data); break;
                case 41: H41.Prepare(oneShot, inputSize, data); break;
                case 42: H42.Prepare(oneShot, inputSize, data); break;
                case 54: H54.Prepare(oneShot, inputSize, data); break;
                case 58: H58.Prepare(oneShot, inputSize, data); break;
                case 68: H68.Prepare(oneShot, inputSize, data); break;
            }
            IsPrepared = true;
        }
    }

    /// <summary>
    /// Sets up (allocates and prepares) the hasher if needed and stores the positions of the last bytes
    /// of the previous block that could not be hashed before.
    /// <paramref name="data"/> is the whole ring buffer (including the tail and the slack bytes after it).
    /// </summary>
    public static void InitOrStitchToPreviousBlock(Hasher hasher, ReadOnlySpan<byte> data, nuint mask,
        ref EncoderParams parameters, nuint position, nuint inputSize, bool isLast)
    {
        hasher.Setup(ref parameters, data, position, inputSize, isLast);
        switch (hasher.Params.Type)
        {
            case 2: hasher.H2.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 3: hasher.H3.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 4: hasher.H4.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 5: hasher.H5.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 6: hasher.H6.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 10: hasher.H10.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 40: hasher.H40.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 41: hasher.H41.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 42: hasher.H42.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 54: hasher.H54.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 58: hasher.H58.StitchToPreviousBlock(inputSize, position, data, mask); break;
            case 68: hasher.H68.StitchToPreviousBlock(inputSize, position, data, mask); break;
        }
    }
}
