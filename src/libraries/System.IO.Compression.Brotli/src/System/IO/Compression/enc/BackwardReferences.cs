// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/backward_references.h,
// backward_references.c and backward_references_inc.h.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Function to find backward reference copies.</summary>
internal static class BackwardReferences
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ComputeDistanceCode(nuint distance, nuint maxDistance, ReadOnlySpan<int> distCache)
    {
        if (distance <= maxDistance)
        {
            nuint distancePlus3 = distance + 3;
            nuint offset0 = distancePlus3 - (nuint)distCache[0];
            nuint offset1 = distancePlus3 - (nuint)distCache[1];
            if (distance == (nuint)distCache[0])
            {
                return 0;
            }
            else if (distance == (nuint)distCache[1])
            {
                return 1;
            }
            else if (offset0 < 7)
            {
                return (nuint)((0x9750468 >> (int)(4 * offset0)) & 0xF);
            }
            else if (offset1 < 7)
            {
                return (nuint)((0xFDB1ACE >> (int)(4 * offset1)) & 0xF);
            }
            else if (distance == (nuint)distCache[2])
            {
                return 2;
            }
            else if (distance == (nuint)distCache[3])
            {
                return 3;
            }
        }
        return distance + BrotliConstants.NumDistanceShortCodes - 1;
    }

    /// <summary>
    /// Finds the backward references (commands) for the <paramref name="numBytes"/> bytes at <paramref name="position"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="ringbuffer"/> is the whole ring buffer (including the tail and the slack bytes after it).
    /// <paramref name="literalContextLut"/> is unused (only used by contextual dictionaries, which are not supported).
    /// <paramref name="commands"/> is the next output commands to write to; <paramref name="numCommands"/> is initially
    /// the total amount of commands output by previous calls and is incremented by the amount written by this call.
    /// </remarks>
    public static void CreateBackwardReferences(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer, nuint ringbufferMask,
        ReadOnlySpan<byte> literalContextLut, in EncoderParams parameters, Hasher hasher, Span<int> distCache,
        ref nuint lastInsertLen, Span<Command> commands, ref nuint numCommands, ref nuint numLiterals)
    {
        // The literal context is only used by contextual dictionaries, which are not supported.
        _ = literalContextLut;
        switch (parameters.Hasher.Type)
        {
            case 2:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H2, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 3:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H3, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 4:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H4, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 5:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H5, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 6:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H6, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 40:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H40, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 41:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H41, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 42:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H42, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 54:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H54, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 58:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H58, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            case 68:
                CreateBackwardReferences(numBytes, position, ringbuffer, ringbufferMask, parameters, ref hasher.H68, distCache,
                    ref lastInsertLen, commands, ref numCommands, ref numLiterals);
                return;
            default:
                Debug.Fail($"Unexpected hasher type {parameters.Hasher.Type}");
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateBackwardReferences<THasher>(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer,
        nuint ringbufferMask, in EncoderParams parameters, ref THasher hasher, Span<int> distCache,
        ref nuint lastInsertLen, Span<Command> commands, ref nuint numCommands, ref nuint numLiterals)
        where THasher : struct, IHasher
    {
        // Set maximum distance, see section 9.1. of the spec.
        nuint maxBackwardLimit = BrotliConstants.MaxBackwardLimit(parameters.Lgwin);
        nuint positionOffset = parameters.StreamOffset;
        nuint maxDistanceParam = parameters.Dist.MaxDistance;
        bool extensiveReferenceSearch = parameters.Quality >= Quality.MinQualityForExtensiveReferenceSearch;

        int commandCount = 0;
        nuint insertLength = lastInsertLen;
        nuint posEnd = position + numBytes;
        nuint storeEnd = numBytes >= THasher.StoreLookahead ?
            position + numBytes - THasher.StoreLookahead + 1 : position;

        // For speed up heuristics for random data.
        nuint randomHeuristicsWindowSize = Quality.LiteralSpreeLengthForSparseSearch(parameters);
        nuint applyRandomHeuristics = position + randomHeuristicsWindowSize;
        // Compound dictionaries are not supported, so their total size (the gap) is always 0.
        const nuint Gap = 0;

        // Minimum score to accept a backward reference.
        const nuint MinScore = Hash.ScoreBase + 100;

        hasher.PrepareDistanceCache(distCache);

        while (position + THasher.HashTypeLength < posEnd)
        {
            nuint maxLength = posEnd - position;
            nuint maxDistance = Math.Min(position, maxBackwardLimit);
            nuint dictionaryStart = Math.Min(position + positionOffset, maxBackwardLimit);
            HasherSearchResult sr;
            sr.Len = 0;
            sr.LenCodeDelta = 0;
            sr.Distance = 0;
            sr.Score = MinScore;
            hasher.FindLongestMatch(ringbuffer, ringbufferMask, distCache, position, maxLength,
                maxDistance, dictionaryStart + Gap, maxDistanceParam, ref sr);
            if (sr.Score > MinScore)
            {
                // Found a match. Let's look for something even better ahead.
                int delayedBackwardReferencesInRow = 0;
                --maxLength;
                for (; ; --maxLength)
                {
                    const nuint CostDiffLazy = 175;
                    HasherSearchResult sr2;
                    sr2.Len = !extensiveReferenceSearch ? Math.Min(sr.Len - 1, maxLength) : 0;
                    sr2.LenCodeDelta = 0;
                    sr2.Distance = 0;
                    sr2.Score = MinScore;
                    maxDistance = Math.Min(position + 1, maxBackwardLimit);
                    dictionaryStart = Math.Min(position + 1 + positionOffset, maxBackwardLimit);
                    hasher.FindLongestMatch(ringbuffer, ringbufferMask, distCache, position + 1, maxLength,
                        maxDistance, dictionaryStart + Gap, maxDistanceParam, ref sr2);
                    if (sr2.Score >= sr.Score + CostDiffLazy)
                    {
                        // Ok, let's just write one byte for now and start a match from the next byte.
                        ++position;
                        ++insertLength;
                        sr = sr2;
                        if (++delayedBackwardReferencesInRow < 4 &&
                            position + THasher.HashTypeLength < posEnd)
                        {
                            continue;
                        }
                    }
                    break;
                }
                applyRandomHeuristics = position + 2 * sr.Len + randomHeuristicsWindowSize;
                dictionaryStart = Math.Min(position + positionOffset, maxBackwardLimit);
                {
                    // The first 16 codes are special short-codes, and the minimum offset is 1.
                    nuint distanceCode = ComputeDistanceCode(sr.Distance, dictionaryStart + Gap, distCache);
                    if ((sr.Distance <= (dictionaryStart + Gap)) && distanceCode > 0)
                    {
                        distCache[3] = distCache[2];
                        distCache[2] = distCache[1];
                        distCache[1] = distCache[0];
                        distCache[0] = (int)sr.Distance;
                        hasher.PrepareDistanceCache(distCache);
                    }
                    commands[commandCount++].Init(parameters.Dist, insertLength, sr.Len, sr.LenCodeDelta, distanceCode);
                }
                numLiterals += insertLength;
                insertLength = 0;
                // Put the hash keys into the table, if there are enough bytes left.
                // Depending on the hasher implementation, it can push all positions
                // in the given range or only a subset of them.
                // Avoid hash poisoning with RLE data.
                {
                    nuint rangeStart = position + 2;
                    nuint rangeEnd = Math.Min(position + sr.Len, storeEnd);
                    if (sr.Distance < (sr.Len >> 2))
                    {
                        rangeStart = Math.Min(rangeEnd, Math.Max(rangeStart, position + sr.Len - (sr.Distance << 2)));
                    }
                    hasher.StoreRange(ringbuffer, ringbufferMask, rangeStart, rangeEnd);
                }
                position += sr.Len;
            }
            else
            {
                ++insertLength;
                ++position;
                // If we have not seen matches for a long time, we can skip some
                // match lookups. Unsuccessful match lookups are very very expensive
                // and this kind of a heuristic speeds up compression quite a lot.
                if (position > applyRandomHeuristics)
                {
                    // Going through uncompressible data, jump.
                    if (position > applyRandomHeuristics + 4 * randomHeuristicsWindowSize)
                    {
                        // It is quite a long time since we saw a copy, so we assume
                        // that this data is not compressible, and store hashes less
                        // often. Hashes of non compressible data are less likely to
                        // turn out to be useful in the future, too, so we store less of
                        // them to not to flood out the hash table of good compressible data.
                        nuint margin = Math.Max(THasher.StoreLookahead - 1, 4);
                        nuint posJump = Math.Min(position + 16, posEnd - margin);
                        for (; position < posJump; position += 4)
                        {
                            hasher.Store(ringbuffer, ringbufferMask, position);
                            insertLength += 4;
                        }
                    }
                    else
                    {
                        nuint margin = Math.Max(THasher.StoreLookahead - 1, 2);
                        nuint posJump = Math.Min(position + 8, posEnd - margin);
                        for (; position < posJump; position += 2)
                        {
                            hasher.Store(ringbuffer, ringbufferMask, position);
                            insertLength += 2;
                        }
                    }
                }
            }
        }
        insertLength += posEnd - position;
        lastInsertLen = insertLength;
        numCommands += (nuint)commandCount;
    }
}
