// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/backward_references_hq.h
// and backward_references_hq.c.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>A node of the Zopfli shortest path search.</summary>
internal struct ZopfliNode
{
    /// <summary>Best length to get up to this byte (not including this byte itself); highest 7 bit is used to reconstruct the length code.</summary>
    public uint Length;
    /// <summary>Distance associated with the length.</summary>
    public uint Distance;
    /// <summary>Number of literal inserts before this copy; highest 5 bits contain distance short code + 1 (or zero if no short code).</summary>
    public uint DcodeInsertLength;

    /// <summary>
    /// The C union of <c>float cost</c>, <c>uint32_t next</c> and <c>uint32_t shortcut</c>, stored as raw bits.
    /// During forward pass |cost| it used to store the goal function. When node is processed its |cost| is
    /// invalidated in favor of |shortcut|. On path back-tracing pass |next| is assigned the offset to next node on the path.
    /// </summary>
    private uint _u;

    /// <summary>Smallest cost to get to this byte from the beginning, as found so far.</summary>
    public float Cost
    {
        readonly get => BitConverter.UInt32BitsToSingle(_u);
        set => _u = BitConverter.SingleToUInt32Bits(value);
    }

    /// <summary>
    /// Offset to the next node on the path. Equals to command_length() of the next node on the path.
    /// For last node equals to <see cref="uint.MaxValue"/>.
    /// </summary>
    public uint Next
    {
        readonly get => _u;
        set => _u = value;
    }

    /// <summary>Node position that provides next distance for distance cache.</summary>
    public uint Shortcut
    {
        readonly get => _u;
        set => _u = value;
    }

    public readonly uint CopyLength => Length & 0x1FFFFFF;

    public readonly uint LengthCode
    {
        get
        {
            uint modifier = Length >> 25;
            return CopyLength + 9u - modifier;
        }
    }

    public readonly uint CopyDistance => Distance;

    public readonly uint DistanceCode
    {
        get
        {
            uint shortCode = DcodeInsertLength >> 27;
            return shortCode == 0 ?
                CopyDistance + BrotliConstants.NumDistanceShortCodes - 1 :
                shortCode - 1;
        }
    }

    public readonly uint CommandLength => CopyLength + (DcodeInsertLength & 0x7FFFFFF);
}

/// <summary>High quality (Zopfli) backward references search, used for qualities 10 and 11.</summary>
internal static class BackwardReferencesHq
{
    /// <summary>BrotliCalculateDistanceCodeLimit(BROTLI_MAX_ALLOWED_DISTANCE, 3, 120).</summary>
    private const int MaxEffectiveDistanceAlphabetSize = 544;

    /// <summary>~= 2 ^ 127</summary>
    private const float Infinity = 1.7e38f;

    private static ReadOnlySpan<byte> DistanceCacheIndex =>
    [
        0, 1, 2, 3, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1,
    ];

    private static ReadOnlySpan<int> DistanceCacheOffset =>
    [
        0, 0, 0, 0, -1, 1, -2, 2, -3, 3, -1, 1, -2, 2, -3, 3,
    ];

    /// <summary>Histogram based cost model for zopflification.</summary>
    internal sealed class ZopfliCostModel
    {
        /// <summary>The insert and copy length symbols.</summary>
        public readonly float[] CostCmd = new float[BrotliConstants.NumCommandSymbols];
        public float[] CostDist = [];
        public uint DistanceHistogramSize;
        /// <summary>Cumulative costs of literals per position in the stream.</summary>
        public float[] LiteralCosts = [];
        public float MinCostCmd;
        public nuint NumBytes;

        // Temporary data (a union in C).
        public readonly nuint[] LiteralHistograms = new nuint[3 * 256];
        public readonly uint[] HistogramLiteral = new uint[BrotliConstants.NumLiteralSymbols];
        public readonly uint[] HistogramCmd = new uint[BrotliConstants.NumCommandSymbols];
        public readonly uint[] HistogramDist = new uint[MaxEffectiveDistanceAlphabetSize];
        public readonly float[] CostLiteral = new float[BrotliConstants.NumLiteralSymbols];

        /// <summary>Scratch buffer for the matches of one position (BrotliZopfliComputeShortestPath).</summary>
        public readonly BackwardMatch[] Matches = new BackwardMatch[2 * (Hash.MaxNumMatchesH10 + 64)];

        /// <summary>InitZopfliCostModel</summary>
        public void Init(in DistanceParams dist, nuint numBytes)
        {
            NumBytes = numBytes;
            LiteralCosts = Hash.RentArray<float>(numBytes + 2);
            CostDist = Hash.RentArray<float>(dist.AlphabetSizeLimit);
            DistanceHistogramSize = dist.AlphabetSizeLimit;
        }

        /// <summary>CleanupZopfliCostModel</summary>
        public void Cleanup()
        {
            Hash.ReturnArray(LiteralCosts);
            Hash.ReturnArray(CostDist);
            LiteralCosts = [];
            CostDist = [];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetLiteralCosts(nuint from, nuint to) => LiteralCosts[(int)to] - LiteralCosts[(int)from];
    }

    private struct PosData
    {
        public nuint Pos;
        public DistanceCache4 DistanceCache;
        public float Costdiff;
        public float Cost;
    }

    [InlineArray(4)]
    private struct DistanceCache4
    {
        private int _element0;
    }

    [InlineArray(8)]
    private struct PosDataArray8
    {
        private PosData _element0;
    }

    /// <summary>Maintains the smallest 8 cost difference together with their positions.</summary>
    private struct StartPosQueue
    {
        public PosDataArray8 Q;
        public nuint Idx;

        public readonly nuint Size => Math.Min(Idx, 8);

        public void Push(in PosData posdata)
        {
            nuint offset = ~(Idx++) & 7;
            nuint len = Size;
            Span<PosData> q = Q;
            q[(int)offset] = posdata;
            // Restore the sorted order. In the list of |len| items at most |len - 1|
            // adjacent element comparisons / swaps are required.
            for (nuint i = 1; i < len; ++i)
            {
                int a = (int)(offset & 7);
                int b = (int)((offset + 1) & 7);
                if (q[a].Costdiff > q[b].Costdiff)
                {
                    (q[a], q[b]) = (q[b], q[a]);
                }
                ++offset;
            }
        }

        [UnscopedRef]
        public ref PosData At(nuint k) => ref Q[(int)((k - Idx) & 7)];
    }

    /// <summary>BrotliInitZopfliNodes</summary>
    internal static void InitZopfliNodes(Span<ZopfliNode> array)
    {
        ZopfliNode stub = default;
        stub.Length = 1;
        stub.Distance = 0;
        stub.DcodeInsertLength = 0;
        stub.Cost = Infinity;
        array.Fill(stub);
    }

    private static void SetCost(ReadOnlySpan<uint> histogram, nuint histogramSize, bool literalHistogram, Span<float> cost)
    {
        nuint sum = 0;
        histogram = histogram.Slice(0, (int)histogramSize);
        cost = cost.Slice(0, (int)histogramSize);
        for (int i = 0; i < histogram.Length; i++)
        {
            sum += histogram[i];
        }
        float log2sum = (float)FastLog.FastLog2(sum);
        nuint missingSymbolSum = sum;
        if (!literalHistogram)
        {
            for (int i = 0; i < histogram.Length; i++)
            {
                if (histogram[i] == 0)
                {
                    missingSymbolSum++;
                }
            }
        }
        float missingSymbolCost = (float)FastLog.FastLog2(missingSymbolSum) + 2;
        for (int i = 0; i < histogram.Length; i++)
        {
            if (histogram[i] == 0)
            {
                cost[i] = missingSymbolCost;
                continue;
            }

            // Shannon bits for this symbol.
            cost[i] = log2sum - (float)FastLog.FastLog2(histogram[i]);

            // Cannot be coded with less than 1 bit
            if (cost[i] < 1)
            {
                cost[i] = 1;
            }
        }
    }

    private static void ZopfliCostModelSetFromCommands(ZopfliCostModel self, nuint position, ReadOnlySpan<byte> ringbuffer,
        nuint ringbufferMask, ReadOnlySpan<Command> commands, nuint numCommands, nuint lastInsertLen)
    {
        nuint pos = position - lastInsertLen;
        float minCostCmd = Infinity;
        float[] costCmd = self.CostCmd;
        uint[] histogramLiteral = self.HistogramLiteral;
        uint[] histogramCmd = self.HistogramCmd;
        uint[] histogramDist = self.HistogramDist;

        Array.Clear(histogramLiteral);
        Array.Clear(histogramCmd);
        Array.Clear(histogramDist);

        for (int i = 0; (nuint)i < numCommands; i++)
        {
            nuint inslength = commands[i].InsertLen;
            nuint copylength = commands[i].CopyLength;
            nuint distcode = commands[i].DistPrefix & 0x3FFu;
            nuint cmdcode = commands[i].CmdPrefix;

            histogramCmd[(int)cmdcode]++;
            if (cmdcode >= 128)
            {
                histogramDist[(int)distcode]++;
            }

            for (nuint j = 0; j < inslength; j++)
            {
                histogramLiteral[ringbuffer[(int)((pos + j) & ringbufferMask)]]++;
            }

            pos += inslength + copylength;
        }

        SetCost(histogramLiteral, BrotliConstants.NumLiteralSymbols, true, self.CostLiteral);
        SetCost(histogramCmd, BrotliConstants.NumCommandSymbols, false, costCmd);
        SetCost(histogramDist, self.DistanceHistogramSize, false, self.CostDist);

        for (int i = 0; i < BrotliConstants.NumCommandSymbols; ++i)
        {
            minCostCmd = Math.Min(minCostCmd, costCmd[i]);
        }
        self.MinCostCmd = minCostCmd;

        {
            float[] literalCosts = self.LiteralCosts;
            float[] costLiteral = self.CostLiteral;
            float literalCarry = 0.0f;
            nuint numBytes = self.NumBytes;
            literalCosts[0] = 0.0f;
            for (nuint i = 0; i < numBytes; ++i)
            {
                literalCarry += costLiteral[ringbuffer[(int)((position + i) & ringbufferMask)]];
                literalCosts[(int)i + 1] = literalCosts[(int)i] + literalCarry;
                literalCarry -= literalCosts[(int)i + 1] - literalCosts[(int)i];
            }
        }
    }

    private static void ZopfliCostModelSetFromLiteralCosts(ZopfliCostModel self, nuint position, ReadOnlySpan<byte> ringbuffer,
        nuint ringbufferMask)
    {
        float[] literalCosts = self.LiteralCosts;
        float literalCarry = 0.0f;
        float[] costDist = self.CostDist;
        float[] costCmd = self.CostCmd;
        nuint numBytes = self.NumBytes;
        LiteralCost.EstimateBitCostsForLiterals(position, numBytes, ringbufferMask, ringbuffer, self.LiteralHistograms,
            literalCosts.AsSpan(1));
        literalCosts[0] = 0.0f;
        for (nuint i = 0; i < numBytes; ++i)
        {
            literalCarry += literalCosts[(int)i + 1];
            literalCosts[(int)i + 1] = literalCosts[(int)i] + literalCarry;
            literalCarry -= literalCosts[(int)i + 1] - literalCosts[(int)i];
        }
        for (int i = 0; i < BrotliConstants.NumCommandSymbols; ++i)
        {
            costCmd[i] = (float)FastLog.FastLog2((nuint)(11 + (uint)i));
        }
        for (uint i = 0; i < self.DistanceHistogramSize; ++i)
        {
            costDist[i] = (float)FastLog.FastLog2((nuint)(20 + i));
        }
        self.MinCostCmd = (float)FastLog.FastLog2(11);
    }

    /// <summary>
    /// REQUIRES: len &gt;= 2, start_pos &lt;= pos
    /// REQUIRES: cost &lt; kInfinity, nodes[start_pos].cost &lt; kInfinity
    /// Maintains the "ZopfliNode array invariant".
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateZopfliNode(Span<ZopfliNode> nodes, nuint pos, nuint startPos, nuint len, nuint lenCode,
        nuint dist, nuint shortCode, float cost)
    {
        ref ZopfliNode next = ref nodes[(int)(pos + len)];
        next.Length = (uint)(len | ((len + 9u - lenCode) << 25));
        next.Distance = (uint)dist;
        next.DcodeInsertLength = (uint)((shortCode << 27) | (pos - startPos));
        next.Cost = cost;
    }

    /// <summary>Returns the minimum possible copy length that can improve the cost of any future position.</summary>
    private static nuint ComputeMinimumCopyLength(float startCost, ReadOnlySpan<ZopfliNode> nodes, nuint numBytes, nuint pos)
    {
        // Compute the minimum possible cost of reaching any future position.
        float minCost = startCost;
        nuint len = 2;
        nuint nextLenBucket = 4;
        nuint nextLenOffset = 10;
        while (pos + len <= numBytes && nodes[(int)(pos + len)].Cost <= minCost)
        {
            // We already reached (pos + len) with no more cost than the minimum
            // possible cost of reaching anything from this pos, so there is no point in
            // looking for lengths <= len.
            ++len;
            if (len == nextLenOffset)
            {
                // We reached the next copy length code bucket, so we add one more
                // extra bit to the minimum cost.
                minCost += 1.0f;
                nextLenOffset += nextLenBucket;
                nextLenBucket *= 2;
            }
        }
        return len;
    }

    /// <summary>
    /// REQUIRES: nodes[pos].cost &lt; kInfinity
    /// REQUIRES: nodes[0..pos] satisfies that "ZopfliNode array invariant".
    /// </summary>
    private static uint ComputeDistanceShortcut(nuint blockStart, nuint pos, nuint maxBackwardLimit, nuint gap,
        ReadOnlySpan<ZopfliNode> nodes)
    {
        nuint cLen = nodes[(int)pos].CopyLength;
        nuint iLen = nodes[(int)pos].DcodeInsertLength & 0x7FFFFFF;
        nuint dist = nodes[(int)pos].CopyDistance;
        // Since |block_start + pos| is the end position of the command, the copy part
        // starts from |block_start + pos - c_len|. Distances that are greater than
        // this or greater than |max_backward_limit| + |gap| are static dictionary
        // references, and do not update the last distances.
        // Also distance code 0 (last distance) does not update the last distances.
        if (pos == 0)
        {
            return 0;
        }
        else if (dist + cLen <= blockStart + pos + gap &&
                 dist <= maxBackwardLimit + gap &&
                 nodes[(int)pos].DistanceCode > 0)
        {
            return (uint)pos;
        }
        else
        {
            return nodes[(int)(pos - cLen - iLen)].Shortcut;
        }
    }

    /// <summary>
    /// Fills in dist_cache[0..3] with the last four distances (as defined by Section 4. of the Spec) that would be
    /// used at (block_start + pos) if we used the shortest path of commands from block_start, computed from
    /// nodes[0..pos]. The last four distances at block_start are in starting_dist_cache[0..3].
    /// REQUIRES: nodes[pos].cost &lt; kInfinity
    /// REQUIRES: nodes[0..pos] satisfies that "ZopfliNode array invariant".
    /// </summary>
    private static void ComputeDistanceCache(nuint pos, ReadOnlySpan<int> startingDistCache, ReadOnlySpan<ZopfliNode> nodes,
        Span<int> distCache)
    {
        int idx = 0;
        nuint p = nodes[(int)pos].Shortcut;
        while (idx < 4 && p > 0)
        {
            nuint iLen = nodes[(int)p].DcodeInsertLength & 0x7FFFFFF;
            nuint cLen = nodes[(int)p].CopyLength;
            nuint dist = nodes[(int)p].CopyDistance;
            distCache[idx++] = (int)dist;
            // Because of prerequisite, p >= c_len + i_len >= 2.
            p = nodes[(int)(p - cLen - iLen)].Shortcut;
        }
        for (int k = 0; idx < 4; ++idx)
        {
            distCache[idx] = startingDistCache[k++];
        }
    }

    /// <summary>Maintains "ZopfliNode array invariant" and pushes node to the queue, if it is eligible.</summary>
    private static void EvaluateNode(nuint blockStart, nuint pos, nuint maxBackwardLimit, nuint gap,
        ReadOnlySpan<int> startingDistCache, ZopfliCostModel model, ref StartPosQueue queue, Span<ZopfliNode> nodes)
    {
        // Save cost, because ComputeDistanceCache invalidates it.
        float nodeCost = nodes[(int)pos].Cost;
        nodes[(int)pos].Shortcut = ComputeDistanceShortcut(blockStart, pos, maxBackwardLimit, gap, nodes);
        if (nodeCost <= model.GetLiteralCosts(0, pos))
        {
            PosData posdata = default;
            posdata.Pos = pos;
            posdata.Cost = nodeCost;
            posdata.Costdiff = nodeCost - model.GetLiteralCosts(0, pos);
            ComputeDistanceCache(pos, startingDistCache, nodes, posdata.DistanceCache);
            queue.Push(posdata);
        }
    }

    /// <summary>Returns longest copy length.</summary>
    private static nuint UpdateNodes(nuint numBytes, nuint blockStart, nuint pos, ReadOnlySpan<byte> ringbuffer,
        nuint ringbufferMask, in EncoderParams parameters, nuint maxBackwardLimit, ReadOnlySpan<int> startingDistCache,
        nuint numMatches, ReadOnlySpan<BackwardMatch> matches, ZopfliCostModel model, ref StartPosQueue queue,
        Span<ZopfliNode> nodes)
    {
        nuint streamOffset = parameters.StreamOffset;
        nuint curIx = blockStart + pos;
        nuint curIxMasked = curIx & ringbufferMask;
        nuint maxDistance = Math.Min(curIx, maxBackwardLimit);
        nuint dictionaryStart = Math.Min(curIx + streamOffset, maxBackwardLimit);
        nuint maxLen = numBytes - pos;
        nuint maxZopfliLen = Quality.MaxZopfliLen(parameters);
        nuint maxIters = Quality.MaxZopfliCandidates(parameters);
        nuint minLen;
        nuint result = 0;
        // Compound dictionaries are not supported, so their total size (the gap) is always 0.
        const nuint Gap = 0;
        ReadOnlySpan<byte> cur = ringbuffer.Slice((int)curIxMasked);
        ReadOnlySpan<float> costCmd = model.CostCmd;
        ReadOnlySpan<float> costDist = model.CostDist;

        Debug.Assert(curIxMasked + maxLen <= ringbufferMask);

        EvaluateNode(blockStart + streamOffset, pos, maxBackwardLimit, Gap, startingDistCache, model, ref queue, nodes);

        {
            ref PosData posdata = ref queue.At(0);
            float minCost = posdata.Cost + model.MinCostCmd + model.GetLiteralCosts(posdata.Pos, pos);
            minLen = ComputeMinimumCopyLength(minCost, nodes, numBytes, pos);
        }

        // Go over the command starting positions in order of increasing cost difference.
        for (nuint k = 0; k < maxIters && k < queue.Size; ++k)
        {
            ref PosData posdata = ref queue.At(k);
            nuint start = posdata.Pos;
            ushort inscode = Command.GetInsertLengthCode(pos - start);
            float startCostdiff = posdata.Costdiff;
            float baseCost = startCostdiff + (float)Command.GetInsertExtra(inscode) + model.GetLiteralCosts(0, pos);

            // Look for last distance matches using the distance cache from this starting position.
            nuint bestLen = minLen - 1;
            ReadOnlySpan<int> posDistanceCache = posdata.DistanceCache;
            for (int j = 0; j < BrotliConstants.NumDistanceShortCodes && bestLen < maxLen; ++j)
            {
                int idx = DistanceCacheIndex[j];
                nuint backward = (nuint)(posDistanceCache[idx] + DistanceCacheOffset[j]);
                nuint prevIx = curIx - backward;
                nuint len;
                byte continuation = cur[(int)bestLen];
                if (curIxMasked + bestLen > ringbufferMask)
                {
                    break;
                }
                if (backward > dictionaryStart + Gap)
                {
                    // Word dictionary -> ignore.
                    continue;
                }
                if (backward <= maxDistance)
                {
                    // Regular backward reference.
                    if (prevIx >= curIx)
                    {
                        continue;
                    }

                    prevIx &= ringbufferMask;
                    if (prevIx + bestLen > ringbufferMask ||
                        continuation != ringbuffer[(int)(prevIx + bestLen)])
                    {
                        continue;
                    }
                    len = FindMatchLength.FindMatchLengthWithLimit(ringbuffer.Slice((int)prevIx), cur, maxLen);
                }
                else
                {
                    // "Gray" area. It is addressable by decoder, but this encoder
                    // instance does not have that data -> should not touch it.
                    // (Compound dictionary references are not supported.)
                    continue;
                }
                {
                    float distCost = baseCost + costDist[j];
                    for (nuint l = bestLen + 1; l <= len; ++l)
                    {
                        ushort copycode = Command.GetCopyLengthCode(l);
                        ushort cmdcode = Command.CombineLengthCodes(inscode, copycode, j == 0);
                        float cost = (cmdcode < 128 ? baseCost : distCost) +
                            (float)Command.GetCopyExtra(copycode) +
                            costCmd[cmdcode];
                        if (cost < nodes[(int)(pos + l)].Cost)
                        {
                            UpdateZopfliNode(nodes, pos, start, l, l, backward, (nuint)j + 1, cost);
                            result = Math.Max(result, l);
                        }
                        bestLen = l;
                    }
                }
            }

            // At higher iterations look only for new last distance matches, since
            // looking only for new command start positions with the same distances
            // does not help much.
            if (k >= 2)
            {
                continue;
            }

            {
                // Loop through all possible copy lengths at this position.
                nuint len = minLen;
                for (int j = 0; (nuint)j < numMatches; ++j)
                {
                    BackwardMatch match = matches[j];
                    nuint dist = match.Distance;
                    bool isDictionaryMatch = dist > dictionaryStart + Gap;
                    // We already tried all possible last distance matches, so we can use
                    // normal distance code here.
                    nuint distCode = dist + BrotliConstants.NumDistanceShortCodes - 1;
                    Command.PrefixEncodeCopyDistance(distCode, parameters.Dist.NumDirectDistanceCodes,
                        parameters.Dist.DistancePostfixBits, out ushort distSymbol, out _);
                    uint distnumextra = (uint)distSymbol >> 10;
                    float distCost = baseCost + (float)distnumextra +
                        costDist[distSymbol & 0x3FF];

                    // Try all copy lengths up until the maximum copy length corresponding
                    // to this distance. If the distance refers to the static dictionary, or
                    // the maximum length is long enough, try only one maximum length.
                    nuint maxMatchLen = match.Length;
                    if (len < maxMatchLen &&
                        (isDictionaryMatch || maxMatchLen > maxZopfliLen))
                    {
                        len = maxMatchLen;
                    }
                    for (; len <= maxMatchLen; ++len)
                    {
                        nuint lenCode = isDictionaryMatch ? match.LengthCode : len;
                        ushort copycode = Command.GetCopyLengthCode(lenCode);
                        ushort cmdcode = Command.CombineLengthCodes(inscode, copycode, false);
                        float cost = distCost + (float)Command.GetCopyExtra(copycode) +
                            costCmd[cmdcode];
                        if (cost < nodes[(int)(pos + len)].Cost)
                        {
                            UpdateZopfliNode(nodes, pos, start, len, lenCode, dist, 0, cost);
                            result = Math.Max(result, len);
                        }
                    }
                }
            }
        }
        return result;
    }

    private static nuint ComputeShortestPathFromNodes(nuint numBytes, Span<ZopfliNode> nodes)
    {
        nuint index = numBytes;
        nuint numCommands = 0;
        while ((nodes[(int)index].DcodeInsertLength & 0x7FFFFFF) == 0 &&
            nodes[(int)index].Length == 1)
        {
            --index;
        }
        nodes[(int)index].Next = uint.MaxValue;
        while (index != 0)
        {
            nuint len = nodes[(int)index].CommandLength;
            index -= len;
            nodes[(int)index].Next = (uint)len;
            numCommands++;
        }
        return numCommands;
    }

    /// <summary>BrotliZopfliCreateCommands. REQUIRES: nodes.Length &gt;= num_bytes + 1</summary>
    internal static void ZopfliCreateCommands(nuint numBytes, nuint blockStart, ReadOnlySpan<ZopfliNode> nodes,
        Span<int> distCache, ref nuint lastInsertLen, in EncoderParams parameters, Span<Command> commands,
        ref nuint numLiterals)
    {
        nuint streamOffset = parameters.StreamOffset;
        nuint maxBackwardLimit = BrotliConstants.MaxBackwardLimit(parameters.Lgwin);
        nuint pos = 0;
        uint offset = nodes[0].Next;
        // Compound dictionaries are not supported, so their total size (the gap) is always 0.
        const nuint Gap = 0;
        for (int i = 0; offset != uint.MaxValue; i++)
        {
            ref readonly ZopfliNode next = ref nodes[(int)(pos + offset)];
            nuint copyLength = next.CopyLength;
            nuint insertLength = next.DcodeInsertLength & 0x7FFFFFF;
            pos += insertLength;
            offset = next.Next;
            if (i == 0)
            {
                insertLength += lastInsertLen;
                lastInsertLen = 0;
            }
            {
                nuint distance = next.CopyDistance;
                nuint lenCode = next.LengthCode;
                nuint dictionaryStart = Math.Min(blockStart + pos + streamOffset, maxBackwardLimit);
                bool isDictionary = distance > dictionaryStart + Gap;
                nuint distCode = next.DistanceCode;
                commands[i].Init(parameters.Dist, insertLength, copyLength, (int)lenCode - (int)copyLength, distCode);

                if (!isDictionary && distCode > 0)
                {
                    distCache[3] = distCache[2];
                    distCache[2] = distCache[1];
                    distCache[1] = distCache[0];
                    distCache[0] = (int)distance;
                }
            }

            numLiterals += insertLength;
            pos += copyLength;
        }
        lastInsertLen += numBytes - pos;
    }

    private static nuint ZopfliIterate(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer, nuint ringbufferMask,
        in EncoderParams parameters, nuint gap, ReadOnlySpan<int> distCache, ZopfliCostModel model,
        ReadOnlySpan<uint> numMatches, ReadOnlySpan<BackwardMatch> matches, Span<ZopfliNode> nodes)
    {
        nuint streamOffset = parameters.StreamOffset;
        nuint maxBackwardLimit = BrotliConstants.MaxBackwardLimit(parameters.Lgwin);
        nuint maxZopfliLen = Quality.MaxZopfliLen(parameters);
        StartPosQueue queue = default;
        nuint curMatchPos = 0;
        nodes[0].Length = 0;
        nodes[0].Cost = 0;
        queue.Idx = 0;
        for (nuint i = 0; i + 3 < numBytes; i++)
        {
            nuint skip = UpdateNodes(numBytes, position, i, ringbuffer, ringbufferMask, parameters, maxBackwardLimit,
                distCache, numMatches[(int)i], matches.Slice((int)curMatchPos), model, ref queue, nodes);
            if (skip < Quality.LongCopyQuickStep)
            {
                skip = 0;
            }
            curMatchPos += numMatches[(int)i];
            if (numMatches[(int)i] == 1 &&
                matches[(int)curMatchPos - 1].Length > maxZopfliLen)
            {
                skip = Math.Max(matches[(int)curMatchPos - 1].Length, skip);
            }
            if (skip > 1)
            {
                skip--;
                while (skip != 0)
                {
                    i++;
                    if (i + 3 >= numBytes)
                    {
                        break;
                    }
                    EvaluateNode(position + streamOffset, i, maxBackwardLimit, gap, distCache, model, ref queue, nodes);
                    curMatchPos += numMatches[(int)i];
                    skip--;
                }
            }
        }
        return ComputeShortestPathFromNodes(numBytes, nodes);
    }

    /// <summary>
    /// BrotliZopfliComputeShortestPath: computes the shortest path of commands from position to at most
    /// position + num_bytes.
    /// On return, path->size() is the number of commands found and path[i] is the length of the i-th command
    /// (copy length plus insert length). Note that the sum of the lengths of all commands can be less than num_bytes.
    /// On return, the nodes[0..num_bytes] array will have the following "ZopfliNode array invariant":
    /// For each i in [1..num_bytes], if nodes[i].cost &lt; kInfinity, then
    ///   (1) nodes[i].copy_length() &gt;= 2
    ///   (2) nodes[i].command_length() &lt;= i and
    ///   (3) nodes[i - nodes[i].command_length()].cost &lt; kInfinity
    /// REQUIRES: nodes.Length &gt;= num_bytes + 1
    /// </summary>
    internal static nuint ZopfliComputeShortestPath(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer,
        nuint ringbufferMask, in EncoderParams parameters, ReadOnlySpan<int> distCache, Hasher hasher, Span<ZopfliNode> nodes)
    {
        nuint streamOffset = parameters.StreamOffset;
        nuint maxBackwardLimit = BrotliConstants.MaxBackwardLimit(parameters.Lgwin);
        nuint maxZopfliLen = Quality.MaxZopfliLen(parameters);
        StartPosQueue queue = default;
        nuint storeEnd = numBytes >= HashToBinaryTree.StoreLookahead ?
            position + numBytes - HashToBinaryTree.StoreLookahead + 1 : position;
        // Compound dictionaries are not supported, so their total size (the gap) is always 0.
        const nuint Gap = 0;
        const int LzMatchesOffset = 0;
        ZopfliCostModel model = hasher.ZopfliCostModel ??= new ZopfliCostModel();
        Span<BackwardMatch> matches = model.Matches;
        ref HashToBinaryTree h10 = ref hasher.H10;
        nodes[0].Length = 0;
        nodes[0].Cost = 0;
        model.Init(parameters.Dist, numBytes);
        ZopfliCostModelSetFromLiteralCosts(model, position, ringbuffer, ringbufferMask);
        queue.Idx = 0;
        for (nuint i = 0; i + HashToBinaryTree.HashTypeLength - 1 < numBytes; i++)
        {
            nuint pos = position + i;
            nuint maxDistance = Math.Min(pos, maxBackwardLimit);
            nuint dictionaryStart = Math.Min(pos + streamOffset, maxBackwardLimit);
            nuint numMatches = h10.FindAllMatches(ringbuffer, ringbufferMask, pos, numBytes - i, maxDistance,
                dictionaryStart + Gap, parameters, matches.Slice(LzMatchesOffset));
            if (numMatches > 0 &&
                matches[(int)numMatches - 1].Length > maxZopfliLen)
            {
                matches[0] = matches[(int)numMatches - 1];
                numMatches = 1;
            }
            nuint skip = UpdateNodes(numBytes, position, i, ringbuffer, ringbufferMask, parameters, maxBackwardLimit,
                distCache, numMatches, matches, model, ref queue, nodes);
            if (skip < Quality.LongCopyQuickStep)
            {
                skip = 0;
            }
            if (numMatches == 1 && matches[0].Length > maxZopfliLen)
            {
                skip = Math.Max(matches[0].Length, skip);
            }
            if (skip > 1)
            {
                // Add the tail of the copy to the hasher.
                h10.StoreRange(ringbuffer, ringbufferMask, pos + 1, Math.Min(pos + skip, storeEnd));
                skip--;
                while (skip != 0)
                {
                    i++;
                    if (i + HashToBinaryTree.HashTypeLength - 1 >= numBytes)
                    {
                        break;
                    }
                    EvaluateNode(position + streamOffset, i, maxBackwardLimit, Gap, distCache, model, ref queue, nodes);
                    skip--;
                }
            }
        }
        model.Cleanup();
        return ComputeShortestPathFromNodes(numBytes, nodes);
    }

    /// <summary>BrotliCreateZopfliBackwardReferences (quality 10).</summary>
    /// <remarks>
    /// <paramref name="ringbuffer"/> is the whole ring buffer (including the tail and the slack bytes after it).
    /// <paramref name="literalContextLut"/> is unused (only used by contextual dictionaries, which are not supported).
    /// <paramref name="commands"/> is the next output commands to write to; <paramref name="numCommands"/> is
    /// incremented by the number of commands written by this call.
    /// </remarks>
    public static void CreateZopfliBackwardReferences(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer,
        nuint ringbufferMask, ReadOnlySpan<byte> literalContextLut, in EncoderParams parameters, Hasher hasher,
        Span<int> distCache, ref nuint lastInsertLen, Span<Command> commands, ref nuint numCommands, ref nuint numLiterals)
    {
        // The literal context is only used by contextual dictionaries, which are not supported.
        _ = literalContextLut;
        ZopfliNode[] nodesArray = Hash.RentArray<ZopfliNode>(numBytes + 1);
        Span<ZopfliNode> nodes = nodesArray.AsSpan(0, (int)(numBytes + 1));
        InitZopfliNodes(nodes);
        numCommands += ZopfliComputeShortestPath(numBytes, position, ringbuffer, ringbufferMask, parameters, distCache,
            hasher, nodes);
        ZopfliCreateCommands(numBytes, position, nodes, distCache, ref lastInsertLen, parameters, commands, ref numLiterals);
        Hash.ReturnArray(nodesArray);
    }

    /// <summary>BrotliCreateHqZopfliBackwardReferences (quality 11).</summary>
    /// <remarks>
    /// <paramref name="ringbuffer"/> is the whole ring buffer (including the tail and the slack bytes after it).
    /// <paramref name="literalContextLut"/> is unused (only used by contextual dictionaries, which are not supported).
    /// <paramref name="commands"/> is the next output commands to write to; <paramref name="numCommands"/> is
    /// incremented by the number of commands written by this call.
    /// </remarks>
    public static void CreateHqZopfliBackwardReferences(nuint numBytes, nuint position, ReadOnlySpan<byte> ringbuffer,
        nuint ringbufferMask, ReadOnlySpan<byte> literalContextLut, in EncoderParams parameters, Hasher hasher,
        Span<int> distCache, ref nuint lastInsertLen, Span<Command> commands, ref nuint numCommands, ref nuint numLiterals)
    {
        // The literal context is only used by contextual dictionaries, which are not supported.
        _ = literalContextLut;
        nuint streamOffset = parameters.StreamOffset;
        nuint maxBackwardLimit = BrotliConstants.MaxBackwardLimit(parameters.Lgwin);
        uint[] numMatches = Hash.RentArray<uint>(numBytes);
        nuint matchesSize = 4 * numBytes;
        nuint storeEnd = numBytes >= HashToBinaryTree.StoreLookahead ?
            position + numBytes - HashToBinaryTree.StoreLookahead + 1 : position;
        nuint curMatchPos = 0;
        ZopfliCostModel model = hasher.ZopfliCostModel ??= new ZopfliCostModel();
        BackwardMatch[] matches = Hash.RentArray<BackwardMatch>(matchesSize);
        ZopfliNode[]? nodesArray = null;
        // Compound dictionaries are not supported, so their total size (the gap) is always 0.
        const nuint Gap = 0;
        const nuint ShadowMatches = 0;
        ref HashToBinaryTree h10 = ref hasher.H10;
        Span<int> origDistCache = stackalloc int[4];
        for (nuint i = 0; i + HashToBinaryTree.HashTypeLength - 1 < numBytes; ++i)
        {
            nuint pos = position + i;
            nuint maxDistance = Math.Min(pos, maxBackwardLimit);
            nuint dictionaryStart = Math.Min(pos + streamOffset, maxBackwardLimit);
            nuint maxLength = numBytes - i;
            // Ensure that we have enough free slots.
            EnsureCapacity(ref matches, ref matchesSize, curMatchPos + Hash.MaxNumMatchesH10 + ShadowMatches);
            nuint numFoundMatches = h10.FindAllMatches(ringbuffer, ringbufferMask, pos, maxLength, maxDistance,
                dictionaryStart + Gap, parameters, matches.AsSpan((int)(curMatchPos + ShadowMatches)));
            nuint curMatchEnd = curMatchPos + numFoundMatches;
            for (nuint j = curMatchPos; j + 1 < curMatchEnd; ++j)
            {
                Debug.Assert(matches[(int)j].Length <= matches[(int)j + 1].Length);
            }
            numMatches[(int)i] = (uint)numFoundMatches;
            if (numFoundMatches > 0)
            {
                nuint matchLen = matches[(int)curMatchEnd - 1].Length;
                if (matchLen > Quality.MaxZopfliLenQuality11)
                {
                    nuint skip = matchLen - 1;
                    matches[(int)curMatchPos++] = matches[(int)curMatchEnd - 1];
                    numMatches[(int)i] = 1;
                    // Add the tail of the copy to the hasher.
                    h10.StoreRange(ringbuffer, ringbufferMask, pos + 1, Math.Min(pos + matchLen, storeEnd));
                    numMatches.AsSpan((int)i + 1, (int)skip).Clear();
                    i += skip;
                }
                else
                {
                    curMatchPos = curMatchEnd;
                }
            }
        }

        nuint origNumLiterals = numLiterals;
        nuint origLastInsertLen = lastInsertLen;
        distCache.Slice(0, 4).CopyTo(origDistCache);
        nuint origNumCommands = numCommands;
        nodesArray = Hash.RentArray<ZopfliNode>(numBytes + 1);
        Span<ZopfliNode> nodes = nodesArray.AsSpan(0, (int)(numBytes + 1));
        model.Init(parameters.Dist, numBytes);
        for (int i = 0; i < 2; i++)
        {
            InitZopfliNodes(nodes);
            if (i == 0)
            {
                ZopfliCostModelSetFromLiteralCosts(model, position, ringbuffer, ringbufferMask);
            }
            else
            {
                ZopfliCostModelSetFromCommands(model, position, ringbuffer, ringbufferMask, commands,
                    numCommands - origNumCommands, origLastInsertLen);
            }
            numCommands = origNumCommands;
            numLiterals = origNumLiterals;
            lastInsertLen = origLastInsertLen;
            origDistCache.CopyTo(distCache);
            numCommands += ZopfliIterate(numBytes, position, ringbuffer, ringbufferMask, parameters, Gap, distCache,
                model, numMatches, matches, nodes);
            ZopfliCreateCommands(numBytes, position, nodes, distCache, ref lastInsertLen, parameters, commands, ref numLiterals);
        }
        model.Cleanup();
        Hash.ReturnArray(ref nodesArray);
        Hash.ReturnArray(matches);
        Hash.ReturnArray(numMatches);
    }

    /// <summary>BROTLI_ENSURE_CAPACITY: grows the array (with the same growth policy as C) to at least <paramref name="required"/> elements.</summary>
    private static void EnsureCapacity(ref BackwardMatch[] array, ref nuint capacity, nuint required)
    {
        if (capacity < required)
        {
            nuint newSize = (capacity == 0) ? required : capacity;
            while (newSize < required)
            {
                newSize *= 2;
            }
            BackwardMatch[] oldArray = array;
            array = Hash.RentArray<BackwardMatch>(newSize);
            if (capacity != 0)
            {
                oldArray.AsSpan(0, (int)capacity).CopyTo(array);
            }
            Hash.ReturnArray(oldArray);
            capacity = newSize;
        }
    }
}
