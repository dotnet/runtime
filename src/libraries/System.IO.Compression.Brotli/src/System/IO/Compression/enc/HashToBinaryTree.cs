// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/hash_to_binary_tree_inc.h.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>
/// A (forgetful) hash table where each hash bucket contains a binary tree of sequences whose first 4 bytes share
/// the same hash code. Each sequence is MAX_TREE_COMP_LENGTH long and is identified by its starting position in
/// the input data. The binary tree is sorted by the lexicographic order of the sequences, and it is also a max-heap
/// with respect to the starting positions.
/// </summary>
internal struct HashToBinaryTree : IHasherBase
{
    private const int BucketBits = 17;
    private const int BucketSize = 1 << BucketBits;
    private const int MaxTreeSearchDepth = 64;
    private const int MaxTreeCompLength = 128;

    /// <summary>The window size minus 1.</summary>
    private nuint _windowMask;

    /// <summary>
    /// Hash table that maps the 4-byte hashes of the sequence to the last position where this hash was found,
    /// which is the root of the binary tree of sequences that share this hash bucket. uint32_t[BUCKET_SIZE]
    /// </summary>
    private uint[]? _buckets;

    /// <summary>
    /// A position used to mark a non-existent sequence, i.e. a tree is empty if its root is at invalid_pos_ and a
    /// node is a leaf if both its children are at invalid_pos_.
    /// </summary>
    private uint _invalidPos;

    /// <summary>
    /// The union of the binary trees of each hash bucket. The root of the tree corresponding to a hash is a sequence
    /// starting at buckets_[hash] and the left and right children of a sequence starting at pos are forest_[2 * pos]
    /// and forest_[2 * pos + 1]. uint32_t[2 * num_nodes]
    /// </summary>
    private uint[]? _forest;

    /// <summary>Scratch buffer for the static dictionary matches of <see cref="FindAllMatches"/>.</summary>
    private uint[]? _dictMatches;

    public static nuint HashTypeLength => 4;
    public static nuint StoreLookahead => MaxTreeCompLength;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashBytes(ReadOnlySpan<byte> data)
    {
        uint h = BinaryPrimitives.ReadUInt32LittleEndian(data) * HashBase.HashMul32;
        // The higher bits contain more mixture from the multiplication, so we take our results from there.
        return h >> (32 - BucketBits);
    }

    public void Initialize(Hasher common, in EncoderParams parameters, bool oneShot, nuint inputSize)
    {
        nuint numNodes = (nuint)1 << parameters.Lgwin;
        if (oneShot && inputSize < numNodes)
        {
            numNodes = inputSize;
        }
        _buckets = Hash.RentArray<uint>(BucketSize);
        _forest = Hash.RentArray<uint>(2 * numNodes);
        _dictMatches = new uint[StaticDict.MaxStaticDictionaryMatchLen + 1];

        _windowMask = (1u << parameters.Lgwin) - 1u;
        _invalidPos = (uint)(0 - _windowMask);
    }

    public void Destroy()
    {
        Hash.ReturnArray(ref _buckets);
        Hash.ReturnArray(ref _forest);
        _dictMatches = null;
    }

    public void Prepare(bool oneShot, nuint inputSize, ReadOnlySpan<byte> data)
    {
        _buckets.AsSpan(0, BucketSize).Fill(_invalidPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly nuint LeftChildIndex(nuint pos) => 2 * (pos & _windowMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly nuint RightChildIndex(nuint pos) => 2 * (pos & _windowMask) + 1;

    /// <summary>
    /// Stores the hash of the next 4 bytes and in a single tree-traversal, the hash bucket's binary tree is searched
    /// for matches and is re-rooted at the current position.
    /// If less than MAX_TREE_COMP_LENGTH data is available, the hash bucket of the current position is searched for
    /// matches, but the state of the hash table is not changed, since we can not know the final sorting order of the
    /// current (incomplete) sequence.
    /// This function must be called with increasing cur_ix positions.
    /// Found matches are written to <paramref name="matches"/> starting at <paramref name="matchCount"/>; if it is
    /// empty, no matches are collected (and <paramref name="bestLen"/> is not used).
    /// </summary>
    /// <returns>The new number of matches in <paramref name="matches"/>.</returns>
    private readonly int StoreAndFindMatches(ReadOnlySpan<byte> data, nuint curIx, nuint ringBufferMask, nuint maxLength,
        nuint maxBackward, ref nuint bestLen, Span<BackwardMatch> matches, int matchCount)
    {
        nuint curIxMasked = curIx & ringBufferMask;
        nuint maxCompLen = Math.Min(maxLength, (nuint)MaxTreeCompLength);
        bool shouldRerootTree = maxLength >= MaxTreeCompLength;
        uint key = HashBytes(data.Slice((int)curIxMasked));
        uint[] buckets = _buckets!;
        uint[] forest = _forest!;
        uint invalidPos = _invalidPos;
        bool collectMatches = !matches.IsEmpty;
        nuint prevIx = buckets[key];
        // The forest index of the rightmost node of the left subtree of the new root, updated as we traverse and
        // re-root the tree of the hash bucket.
        nuint nodeLeft = LeftChildIndex(curIx);
        // The forest index of the leftmost node of the right subtree of the new root, updated as we traverse and
        // re-root the tree of the hash bucket.
        nuint nodeRight = RightChildIndex(curIx);
        // The match length of the rightmost node of the left subtree of the new root, updated as we traverse and
        // re-root the tree of the hash bucket.
        nuint bestLenLeft = 0;
        // The match length of the leftmost node of the right subtree of the new root, updated as we traverse and
        // re-root the tree of the hash bucket.
        nuint bestLenRight = 0;
        if (shouldRerootTree)
        {
            buckets[key] = (uint)curIx;
        }
        for (nuint depthRemaining = MaxTreeSearchDepth; ; --depthRemaining)
        {
            nuint backward = curIx - prevIx;
            nuint prevIxMasked = prevIx & ringBufferMask;
            if (backward == 0 || backward > maxBackward || depthRemaining == 0)
            {
                if (shouldRerootTree)
                {
                    forest[(int)nodeLeft] = invalidPos;
                    forest[(int)nodeRight] = invalidPos;
                }
                break;
            }
            {
                nuint curLen = Math.Min(bestLenLeft, bestLenRight);
                Debug.Assert(curLen <= MaxTreeCompLength);
                nuint len = curLen + FindMatchLength.FindMatchLengthWithLimit(
                    data.Slice((int)(curIxMasked + curLen)), data.Slice((int)(prevIxMasked + curLen)), maxLength - curLen);
                if (collectMatches && len > bestLen)
                {
                    bestLen = len;
                    matches[matchCount++].Init(backward, len);
                }
                if (len >= maxCompLen)
                {
                    if (shouldRerootTree)
                    {
                        forest[(int)nodeLeft] = forest[(int)LeftChildIndex(prevIx)];
                        forest[(int)nodeRight] = forest[(int)RightChildIndex(prevIx)];
                    }
                    break;
                }
                if (data[(int)(curIxMasked + len)] > data[(int)(prevIxMasked + len)])
                {
                    bestLenLeft = len;
                    if (shouldRerootTree)
                    {
                        forest[(int)nodeLeft] = (uint)prevIx;
                    }
                    nodeLeft = RightChildIndex(prevIx);
                    prevIx = forest[(int)nodeLeft];
                }
                else
                {
                    bestLenRight = len;
                    if (shouldRerootTree)
                    {
                        forest[(int)nodeRight] = (uint)prevIx;
                    }
                    nodeRight = LeftChildIndex(prevIx);
                    prevIx = forest[(int)nodeRight];
                }
            }
        }
        return matchCount;
    }

    /// <summary>
    /// Finds all backward matches of &amp;data[cur_ix &amp; ring_buffer_mask] up to the length of max_length and
    /// stores the position cur_ix in the hash table.
    /// Stores the found matches in matches[0] to matches[n - 1] and returns n. The matches will be sorted by strictly
    /// increasing length and (non-strictly) increasing distance.
    /// </summary>
    public readonly nuint FindAllMatches(ReadOnlySpan<byte> data, nuint ringBufferMask, nuint curIx, nuint maxLength,
        nuint maxBackward, nuint dictionaryDistance, in EncoderParams parameters, Span<BackwardMatch> matches)
    {
        nuint curIxMasked = curIx & ringBufferMask;
        nuint bestLen = 1;
        nuint shortMatchMaxBackward = parameters.Quality != Quality.HqZopflificationQuality ? 16u : 64u;
        nuint stop = curIx - shortMatchMaxBackward;
        Span<uint> dictMatches = _dictMatches;
        int matchCount = 0;
        if (curIx < shortMatchMaxBackward)
        {
            stop = 0;
        }
        for (nuint i = curIx - 1; i > stop && bestLen <= 2; --i)
        {
            nuint prevIx = i;
            nuint backward = curIx - prevIx;
            if (backward > maxBackward)
            {
                break;
            }
            prevIx &= ringBufferMask;
            if (data[(int)curIxMasked] != data[(int)prevIx] ||
                data[(int)curIxMasked + 1] != data[(int)prevIx + 1])
            {
                continue;
            }
            {
                nuint len = FindMatchLength.FindMatchLengthWithLimit(data.Slice((int)prevIx), data.Slice((int)curIxMasked), maxLength);
                if (len > bestLen)
                {
                    bestLen = len;
                    matches[matchCount++].Init(backward, len);
                }
            }
        }
        if (bestLen < maxLength)
        {
            matchCount = StoreAndFindMatches(data, curIx, ringBufferMask, maxLength, maxBackward, ref bestLen, matches, matchCount);
        }
        dictMatches.Fill(StaticDict.InvalidMatch);
        {
            nuint minlen = Math.Max(4, bestLen + 1);
            if (StaticDict.FindAllStaticDictionaryMatches(data.Slice((int)curIxMasked), minlen, maxLength, dictMatches))
            {
                nuint maxlen = Math.Min(StaticDict.MaxStaticDictionaryMatchLen, maxLength);
                for (nuint l = minlen; l <= maxlen; ++l)
                {
                    uint dictId = dictMatches[(int)l];
                    if (dictId < StaticDict.InvalidMatch)
                    {
                        nuint distance = dictionaryDistance + (dictId >> 5) + 1;
                        if (distance <= parameters.Dist.MaxDistance)
                        {
                            matches[matchCount++].InitDictionary(distance, l, dictId & 31);
                        }
                    }
                }
            }
        }
        return (nuint)matchCount;
    }

    /// <summary>
    /// Stores the hash of the next 4 bytes and re-roots the binary tree at the current sequence, without returning
    /// any matches.
    /// REQUIRES: ix + MAX_TREE_COMP_LENGTH &lt;= end-of-current-block
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Store(ReadOnlySpan<byte> data, nuint mask, nuint ix)
    {
        // Maximum distance is window size - 16, see section 9.1. of the spec.
        nuint maxBackward = _windowMask - BrotliConstants.WindowGap + 1;
        nuint unusedBestLen = 0;
        StoreAndFindMatches(data, ix, mask, MaxTreeCompLength, maxBackward, ref unusedBestLen, default, 0);
    }

    public readonly void StoreRange(ReadOnlySpan<byte> data, nuint mask, nuint ixStart, nuint ixEnd)
    {
        nuint i = ixStart;
        nuint j = ixStart;
        if (ixStart + 63 <= ixEnd)
        {
            i = ixEnd - 63;
        }
        if (ixStart + 512 <= i)
        {
            for (; j < i; j += 8)
            {
                Store(data, mask, j);
            }
        }
        for (; i < ixEnd; ++i)
        {
            Store(data, mask, i);
        }
    }

    public void StitchToPreviousBlock(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer, nuint ringbufferMask)
    {
        if (numBytes >= HashTypeLength - 1 && position >= MaxTreeCompLength)
        {
            // Store the last `MAX_TREE_COMP_LENGTH - 1` positions in the hasher.
            // These could not be calculated before, since they require knowledge
            // of both the previous and the current block.
            nuint iStart = position - MaxTreeCompLength + 1;
            nuint iEnd = Math.Min(position, iStart + numBytes);
            for (nuint i = iStart; i < iEnd; ++i)
            {
                // Maximum distance is window size - 16, see section 9.1. of the spec.
                // Furthermore, we have to make sure that we don't look further back
                // from the start of the next block than the window size, otherwise we
                // could access already overwritten areas of the ring-buffer.
                nuint maxBackward = _windowMask - Math.Max(BrotliConstants.WindowGap - 1, position - i);
                // We know that i + MAX_TREE_COMP_LENGTH <= position + num_bytes, i.e. the
                // end of the current block and that we have at least
                // MAX_TREE_COMP_LENGTH tail in the ring-buffer.
                nuint unusedBestLen = 0;
                StoreAndFindMatches(ringbuffer, i, ringbufferMask, MaxTreeCompLength, maxBackward, ref unusedBestLen, default, 0);
            }
        }
    }
}
