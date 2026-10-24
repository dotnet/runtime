// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/block_splitter.c, block_splitter.h and block_splitter_inc.h.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Compression;

/// <summary>
/// Block split: types and lengths of the blocks of one category. The <c>types_alloc_size</c> and
/// <c>lengths_alloc_size</c> fields of the C struct are represented by the array lengths.
/// </summary>
internal sealed class BlockSplit
{
    /// <summary>Amount of distinct types.</summary>
    public nuint NumTypes;
    /// <summary>Amount of values in types and length.</summary>
    public nuint NumBlocks;
    public byte[]? Types;
    public uint[]? Lengths;
}

/// <summary>Block split point selection utilities.</summary>
internal static class BlockSplitter
{
    private const nuint MaxLiteralHistograms = 100;
    private const nuint MaxCommandHistograms = 50;
    private const double LiteralBlockSwitchCost = 28.1;
    private const double CommandBlockSwitchCost = 13.5;
    private const double DistanceBlockSwitchCost = 14.6;
    private const nuint LiteralStrideLength = 70;
    private const nuint CommandStrideLength = 40;
    private const nuint DistanceStrideLength = 40;
    private const nuint SymbolsPerLiteralHistogram = 544;
    private const nuint SymbolsPerCommandHistogram = 530;
    private const nuint SymbolsPerDistanceHistogram = 544;
    private const nuint MinLengthForBlockSplitting = 128;
    private const nuint IterMulForRefining = 2;
    private const nuint MinItersForRefining = 100;

    private const int HistogramsPerBatch = 64;
    private const int ClustersPerBatch = 16;

    /// <summary>
    /// Port of BrotliInitBlockSplit. Resets the split for reuse; the arrays are kept to avoid reallocations
    /// (their contents are never read before being written).
    /// </summary>
    public static void InitBlockSplit(BlockSplit self)
    {
        self.NumTypes = 0;
        self.NumBlocks = 0;
    }

    /// <summary>Port of BrotliDestroyBlockSplit: releases the arrays.</summary>
    public static void DestroyBlockSplit(BlockSplit self)
    {
        self.Types = null;
        self.Lengths = null;
    }

    /// <summary>
    /// Port of BROTLI_ENSURE_CAPACITY: grows <paramref name="array"/> (preserving the first
    /// <paramref name="capacity"/> elements) so that it can hold at least <paramref name="required"/> elements.
    /// </summary>
    internal static void EnsureCapacity<T>(ref T[] array, ref nuint capacity, nuint required)
    {
        if (capacity < required)
        {
            nuint newSize = (capacity == 0) ? required : capacity;
            while (newSize < required)
            {
                newSize *= 2;
            }
            T[] newArray = new T[newSize];
            if (capacity != 0)
            {
                array.AsSpan(0, (int)capacity).CopyTo(newArray);
            }
            array = newArray;
            capacity = newSize;
        }
    }

    /// <summary>
    /// Same as <see cref="EnsureCapacity{T}(ref T[], ref nuint, nuint)"/> with the array length as capacity;
    /// returns the (possibly new) array.
    /// </summary>
    internal static T[] EnsureCapacity<T>(ref T[]? array, nuint required)
    {
        T[] a = array ?? [];
        nuint capacity = (nuint)a.Length;
        EnsureCapacity(ref a, ref capacity, required);
        array = a;
        return a;
    }

    private static nuint CountLiterals(ReadOnlySpan<Command> cmds, nuint numCommands)
    {
        // Count how many we have.
        nuint totalLength = 0;
        cmds = cmds.Slice(0, (int)numCommands);
        for (int i = 0; i < cmds.Length; ++i)
        {
            totalLength += cmds[i].InsertLen;
        }
        return totalLength;
    }

    private static void CopyLiteralsToByteArray(ReadOnlySpan<Command> cmds, nuint numCommands, ReadOnlySpan<byte> data,
        nuint offset, nuint mask, Span<byte> literals)
    {
        nuint pos = 0;
        nuint fromPos = offset & mask;
        cmds = cmds.Slice(0, (int)numCommands);
        for (int i = 0; i < cmds.Length; ++i)
        {
            nuint insertLen = cmds[i].InsertLen;
            if (fromPos + insertLen > mask)
            {
                nuint headSize = mask + 1 - fromPos;
                data.Slice((int)fromPos, (int)headSize).CopyTo(literals.Slice((int)pos));
                fromPos = 0;
                pos += headSize;
                insertLen -= headSize;
            }
            if (insertLen > 0)
            {
                data.Slice((int)fromPos, (int)insertLen).CopyTo(literals.Slice((int)pos));
                pos += insertLen;
            }
            fromPos = (fromPos + insertLen + cmds[i].CopyLength) & mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MyRand(ref uint seed)
    {
        // Initial seed should be 7. In this case, loop length is (1 << 29).
        seed *= 16807U;
        return seed;
    }

    /// <summary>Port of the BitCost helper of block_splitter.c (renamed to avoid a clash with the BitCost class).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double BitCostOfCount(nuint count) => count == 0 ? -2.0 : FastLog.FastLog2(count);

    private static void InitialEntropyCodes<THistogram, TData>(ReadOnlySpan<TData> data, nuint length,
        nuint stride, nuint numHistograms, Span<THistogram> histograms)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        uint seed = 7;
        nuint blockLength = length / numHistograms;
        Histogram.ClearHistograms(histograms, numHistograms);
        for (nuint i = 0; i < numHistograms; ++i)
        {
            nuint pos = length * i / numHistograms;
            if (i != 0)
            {
                pos += MyRand(ref seed) % blockLength;
            }
            if (pos + stride >= length)
            {
                pos = length - stride - 1;
            }
            Histogram.HistogramAddVector(ref histograms[(int)i], data.Slice((int)pos), stride);
        }
    }

    private static void RandomSample<THistogram, TData>(ref uint seed, ReadOnlySpan<TData> data, nuint length,
        nuint stride, ref THistogram sample)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        nuint pos = 0;
        if (stride >= length)
        {
            stride = length;
        }
        else
        {
            pos = MyRand(ref seed) % (length - stride + 1);
        }
        Histogram.HistogramAddVector(ref sample, data.Slice((int)pos), stride);
    }

    private static void RefineEntropyCodes<THistogram, TData>(ReadOnlySpan<TData> data, nuint length,
        nuint stride, nuint numHistograms, Span<THistogram> histograms, ref THistogram tmp)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        nuint iters = IterMulForRefining * length / stride + MinItersForRefining;
        uint seed = 7;
        iters = ((iters + numHistograms - 1) / numHistograms) * numHistograms;
        for (nuint iter = 0; iter < iters; ++iter)
        {
            Histogram.HistogramClear(ref tmp);
            RandomSample(ref seed, data, length, stride, ref tmp);
            Histogram.HistogramAddHistogram(ref histograms[(int)(iter % numHistograms)], ref tmp);
        }
    }

    /// <summary>
    /// Assigns a block id from the range [0, num_histograms) to each data element
    /// in data[0..length) and fills in block_id[0..length) with the assigned values.
    /// Returns the number of blocks, i.e. one plus the number of block switches.
    /// </summary>
    private static nuint FindBlocks<THistogram, TData>(ReadOnlySpan<TData> data, nuint length,
        double blockSwitchBitcost, nuint numHistograms, Span<THistogram> histograms,
        Span<double> insertCost, Span<double> cost, Span<byte> switchSignal, Span<byte> blockId)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        nuint alphabetSize = Histogram.HistogramDataSize<THistogram>();
        nuint bitmapLen = (numHistograms + 7) >> 3;
        nuint numBlocks = 1;
        nuint byteIx;
        nuint i;
        Debug.Assert(numHistograms <= 256);

        // Trivial case: single histogram -> single block type.
        if (numHistograms <= 1)
        {
            blockId.Slice(0, (int)length).Clear();
            return 1;
        }

        // Fill bitcost for each symbol of all histograms.
        // Non-existing symbol cost: 2 + log2(total_count).
        // Regular symbol cost: -log2(symbol_count / total_count).
        insertCost.Slice(0, (int)(alphabetSize * numHistograms)).Clear();
        for (i = 0; i < numHistograms; ++i)
        {
            insertCost[(int)i] = FastLog.FastLog2((uint)histograms[(int)i].TotalCount);
        }
        for (i = alphabetSize; i != 0;)
        {
            // Reverse order to use the 0-th row as a temporary storage.
            --i;
            Span<double> row = insertCost.Slice((int)(i * numHistograms), (int)numHistograms);
            for (int j = 0; j < row.Length; ++j)
            {
                row[j] = insertCost[j] - BitCostOfCount(histograms[j].Data[(int)i]);
            }
        }

        // After each iteration of this loop, cost[k] will contain the difference
        // between the minimum cost of arriving at the current byte position using
        // entropy code k, and the minimum cost of arriving at the current byte
        // position. This difference is capped at the block switch cost, and if it
        // reaches block switch cost, it means that when we trace back from the last
        // position, we need to switch here.
        Span<double> costs = cost.Slice(0, (int)numHistograms);
        costs.Clear();
        switchSignal.Slice(0, (int)(length * bitmapLen)).Clear();
        const nuint PrologueLength = 2000;
        const double Multiplier = 0.07 / 2000;
        for (byteIx = 0; byteIx < length; ++byteIx)
        {
            nuint ix = byteIx * bitmapLen;
            nuint symbol = nuint.CreateTruncating(data[(int)byteIx]);
            nuint insertCostIx = symbol * numHistograms;
            double minCost = 1e99;
            double blockSwitchCost = blockSwitchBitcost;
            ReadOnlySpan<double> insertCostRow = insertCost.Slice((int)insertCostIx, costs.Length);
            // We are coding the symbol with entropy code k.
            int bestK = AddCostsAndFindMin(costs, insertCostRow);
            if (bestK >= 0)
            {
                minCost = costs[bestK];
                blockId[(int)byteIx] = (byte)bestK;
            }
            // More blocks for the beginning.
            if (byteIx < PrologueLength)
            {
                blockSwitchCost *= 0.77 + Multiplier * (double)byteIx;
            }
            SubtractMinAndCapCosts(costs, minCost, blockSwitchCost, switchSignal.Slice((int)ix, (int)bitmapLen));
        }

        byteIx = length - 1;
        {
            // Trace back from the last position and switch at the marked places.
            nuint ix = byteIx * bitmapLen;
            byte curId = blockId[(int)byteIx];
            while (byteIx > 0)
            {
                byte mask = (byte)(1u << (curId & 7));
                Debug.Assert(((nuint)curId >> 3) < bitmapLen);
                --byteIx;
                ix -= bitmapLen;
                if ((switchSignal[(int)(ix + (nuint)(curId >> 3))] & mask) != 0)
                {
                    if (curId != blockId[(int)byteIx])
                    {
                        curId = blockId[(int)byteIx];
                        ++numBlocks;
                    }
                }
                blockId[(int)byteIx] = curId;
            }
        }
        return numBlocks;
    }

    /// <summary>
    /// costs[k] += row[k] for all k; returns the first k with the minimal cost if that cost is &lt; 1e99, otherwise -1.
    /// This is equivalent to the sequential loop of FindBlocks updating min_cost/block_id on cost[k] &lt; min_cost:
    /// the element-wise additions are the same IEEE operations, and the index found by the strict comparison is the
    /// first index holding the minimal value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AddCostsAndFindMin(Span<double> costs, ReadOnlySpan<double> row)
    {
        row = row.Slice(0, costs.Length);
        int k = 0;
        double minCost = 1e99;
        if (Vector256.IsHardwareAccelerated && costs.Length >= Vector256<double>.Count)
        {
            Vector256<double> vmin = Vector256.Create(1e99);
            for (; k <= costs.Length - Vector256<double>.Count; k += Vector256<double>.Count)
            {
                Span<double> dst = costs.Slice(k, Vector256<double>.Count);
                Vector256<double> c = Vector256.Create<double>(dst) + Vector256.Create(row.Slice(k, Vector256<double>.Count));
                c.CopyTo(dst);
                vmin = Vector256.Min(vmin, c);
            }
            minCost = Math.Min(Math.Min(vmin[0], vmin[1]), Math.Min(vmin[2], vmin[3]));
        }
        else if (Vector128.IsHardwareAccelerated && costs.Length >= Vector128<double>.Count)
        {
            Vector128<double> vmin = Vector128.Create(1e99);
            for (; k <= costs.Length - Vector128<double>.Count; k += Vector128<double>.Count)
            {
                Span<double> dst = costs.Slice(k, Vector128<double>.Count);
                Vector128<double> c = Vector128.Create<double>(dst) + Vector128.Create(row.Slice(k, Vector128<double>.Count));
                c.CopyTo(dst);
                vmin = Vector128.Min(vmin, c);
            }
            minCost = Math.Min(vmin[0], vmin[1]);
        }
        for (; k < costs.Length; ++k)
        {
            double c = costs[k] + row[k];
            costs[k] = c;
            minCost = Math.Min(minCost, c);
        }
        if (!(minCost < 1e99))
        {
            return -1;
        }
        k = 0;
        while (costs[k] != minCost)
        {
            k++;
        }
        return k;
    }

    /// <summary>
    /// For all k: costs[k] -= minCost; if it is &gt;= blockSwitchCost, sets it to blockSwitchCost and sets bit k of
    /// <paramref name="signal"/> (which must be zeroed). Same IEEE operations as the sequential loop of FindBlocks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubtractMinAndCapCosts(Span<double> costs, double minCost, double blockSwitchCost, Span<byte> signal)
    {
        int k = 0;
        if (Vector256.IsHardwareAccelerated && costs.Length >= Vector256<double>.Count)
        {
            Vector256<double> vmin = Vector256.Create(minCost);
            Vector256<double> vswitch = Vector256.Create(blockSwitchCost);
            for (; k <= costs.Length - Vector256<double>.Count; k += Vector256<double>.Count)
            {
                Span<double> dst = costs.Slice(k, Vector256<double>.Count);
                Vector256<double> c = Vector256.Create<double>(dst) - vmin;
                Vector256<double> capped = Vector256.GreaterThanOrEqual(c, vswitch);
                Vector256.ConditionalSelect(capped, vswitch, c).CopyTo(dst);
                uint bits = capped.ExtractMostSignificantBits();
                if (bits != 0)
                {
                    // k is a multiple of 4, so the 4 bits belong to the same byte.
                    signal[k >> 3] |= (byte)(bits << (k & 7));
                }
            }
        }
        else if (Vector128.IsHardwareAccelerated && costs.Length >= Vector128<double>.Count)
        {
            Vector128<double> vmin = Vector128.Create(minCost);
            Vector128<double> vswitch = Vector128.Create(blockSwitchCost);
            for (; k <= costs.Length - Vector128<double>.Count; k += Vector128<double>.Count)
            {
                Span<double> dst = costs.Slice(k, Vector128<double>.Count);
                Vector128<double> c = Vector128.Create<double>(dst) - vmin;
                Vector128<double> capped = Vector128.GreaterThanOrEqual(c, vswitch);
                Vector128.ConditionalSelect(capped, vswitch, c).CopyTo(dst);
                uint bits = capped.ExtractMostSignificantBits();
                if (bits != 0)
                {
                    // k is a multiple of 2, so the 2 bits belong to the same byte.
                    signal[k >> 3] |= (byte)(bits << (k & 7));
                }
            }
        }
        for (; k < costs.Length; ++k)
        {
            double c = costs[k] - minCost;
            costs[k] = c;
            if (c >= blockSwitchCost)
            {
                byte mask = (byte)(1u << (k & 7));
                costs[k] = blockSwitchCost;
                signal[k >> 3] |= mask;
            }
        }
    }

    private static nuint RemapBlockIds(Span<byte> blockIds, nuint length, Span<ushort> newId, nuint numHistograms)
    {
        const ushort InvalidId = 256;
        ushort nextId = 0;
        nuint i;
        for (i = 0; i < numHistograms; ++i)
        {
            newId[(int)i] = InvalidId;
        }
        blockIds = blockIds.Slice(0, (int)length);
        for (int b = 0; b < blockIds.Length; ++b)
        {
            Debug.Assert(blockIds[b] < numHistograms);
            if (newId[blockIds[b]] == InvalidId)
            {
                newId[blockIds[b]] = nextId++;
            }
        }
        for (int b = 0; b < blockIds.Length; ++b)
        {
            blockIds[b] = (byte)newId[blockIds[b]];
            Debug.Assert(blockIds[b] < numHistograms);
        }
        Debug.Assert(nextId <= numHistograms);
        return nextId;
    }

    private static void BuildBlockHistograms<THistogram, TData>(ReadOnlySpan<TData> data, nuint length,
        ReadOnlySpan<byte> blockIds, nuint numHistograms, Span<THistogram> histograms)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        Histogram.ClearHistograms(histograms, numHistograms);
        data = data.Slice(0, (int)length);
        for (int i = 0; i < data.Length; ++i)
        {
            Histogram.HistogramAdd(ref histograms[blockIds[i]], nuint.CreateTruncating(data[i]));
        }
    }

    /// <summary>
    /// Given the initial partitioning build partitioning with limited number
    /// of histograms (and block types).
    /// </summary>
    private static void ClusterBlocks<THistogram, TData>(ReadOnlySpan<TData> data, nuint length,
        nuint numBlocks, ReadOnlySpan<byte> blockIds, BlockSplit split)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        uint[] histogramSymbols = new uint[numBlocks];
        uint[] u32 = new uint[numBlocks + 4 * HistogramsPerBatch];
        nuint expectedNumClusters = ClustersPerBatch *
            (numBlocks + HistogramsPerBatch - 1) / HistogramsPerBatch;
        nuint allHistogramsSize = 0;
        nuint allHistogramsCapacity = expectedNumClusters;
        THistogram[] allHistograms = new THistogram[allHistogramsCapacity];
        nuint clusterSizeSize = 0;
        nuint clusterSizeCapacity = expectedNumClusters;
        uint[] clusterSize = new uint[clusterSizeCapacity];
        nuint numClusters = 0;
        THistogram[] histograms = new THistogram[Math.Min(numBlocks, HistogramsPerBatch)];
        nuint maxNumPairs = HistogramsPerBatch * HistogramsPerBatch / 2;
        nuint pairsCapacity = maxNumPairs + 1;
        HistogramPair[] pairs = new HistogramPair[pairsCapacity];
        nuint pos = 0;
        uint[] clusters;
        nuint numFinalClusters;
        const uint InvalidIndex = uint.MaxValue;
        uint[] newIndex;
        nuint i;
        Span<uint> sizes = u32.AsSpan(0 * HistogramsPerBatch, HistogramsPerBatch);
        Span<uint> newClusters = u32.AsSpan(1 * HistogramsPerBatch, HistogramsPerBatch);
        Span<uint> symbols = u32.AsSpan(2 * HistogramsPerBatch, HistogramsPerBatch);
        Span<uint> remap = u32.AsSpan(3 * HistogramsPerBatch, HistogramsPerBatch);
        Span<uint> blockLengths = u32.AsSpan(4 * HistogramsPerBatch);
        THistogram[] tmp = new THistogram[2];

        // Calculate block lengths (convert repeating values -> series length).
        {
            nuint blockIdx = 0;
            for (i = 0; i < length; ++i)
            {
                Debug.Assert(blockIdx < numBlocks);
                ++blockLengths[(int)blockIdx];
                if (i + 1 == length || blockIds[(int)i] != blockIds[(int)i + 1])
                {
                    ++blockIdx;
                }
            }
            Debug.Assert(blockIdx == numBlocks);
        }

        // Pre-cluster blocks (cluster batches).
        for (i = 0; i < numBlocks; i += HistogramsPerBatch)
        {
            nuint numToCombine = Math.Min(numBlocks - i, HistogramsPerBatch);
            nuint numNewClusters;
            nuint j;
            for (j = 0; j < numToCombine; ++j)
            {
                nuint blockLength = blockLengths[(int)(i + j)];
                ref THistogram h = ref histograms[j];
                Histogram.HistogramClear(ref h);
                for (nuint k = 0; k < blockLength; ++k)
                {
                    Histogram.HistogramAdd(ref h, nuint.CreateTruncating(data[(int)pos++]));
                }
                h.BitCost = BitCost.PopulationCost(ref h);
                newClusters[(int)j] = (uint)j;
                symbols[(int)j] = (uint)j;
                sizes[(int)j] = 1;
            }
            numNewClusters = Cluster.HistogramCombine<THistogram>(
                histograms, ref tmp[0], sizes, symbols, newClusters, pairs, numToCombine,
                numToCombine, HistogramsPerBatch, maxNumPairs);
            EnsureCapacity(ref allHistograms, ref allHistogramsCapacity, allHistogramsSize + numNewClusters);
            EnsureCapacity(ref clusterSize, ref clusterSizeCapacity, clusterSizeSize + numNewClusters);
            for (j = 0; j < numNewClusters; ++j)
            {
                allHistograms[allHistogramsSize++] = histograms[newClusters[(int)j]];
                clusterSize[clusterSizeSize++] = sizes[(int)newClusters[(int)j]];
                remap[(int)newClusters[(int)j]] = (uint)j;
            }
            for (j = 0; j < numToCombine; ++j)
            {
                histogramSymbols[i + j] = (uint)numClusters + remap[(int)symbols[(int)j]];
            }
            numClusters += numNewClusters;
            Debug.Assert(numClusters == clusterSizeSize);
            Debug.Assert(numClusters == allHistogramsSize);
        }

        // Final clustering.
        maxNumPairs = Math.Min(64 * numClusters, (numClusters / 2) * numClusters);
        if (pairsCapacity < maxNumPairs + 1)
        {
            pairs = new HistogramPair[maxNumPairs + 1];
        }
        clusters = new uint[numClusters];
        for (i = 0; i < numClusters; ++i)
        {
            clusters[i] = (uint)i;
        }
        numFinalClusters = Cluster.HistogramCombine<THistogram>(
            allHistograms, ref tmp[0], clusterSize, histogramSymbols, clusters, pairs,
            numClusters, numBlocks, BrotliConstants.MaxNumberOfBlockTypes,
            maxNumPairs);

        // Assign blocks to final histograms.
        newIndex = new uint[numClusters];
        newIndex.AsSpan().Fill(InvalidIndex);
        pos = 0;
        {
            uint nextIndex = 0;
            for (i = 0; i < numBlocks; ++i)
            {
                uint bestOut;
                double bestBits;
                Histogram.HistogramClear(ref tmp[0]);
                for (nuint j = 0; j < blockLengths[(int)i]; ++j)
                {
                    Histogram.HistogramAdd(ref tmp[0], nuint.CreateTruncating(data[(int)pos++]));
                }
                // Among equally good histograms prefer last used.
                // TODO(eustas): should we give a block-switch discount here?
                bestOut = (i == 0) ? histogramSymbols[0] : histogramSymbols[i - 1];
                bestBits = Cluster.HistogramBitCostDistance(ref tmp[0], ref allHistograms[bestOut], ref tmp[1]);
                for (nuint j = 0; j < numFinalClusters; ++j)
                {
                    double curBits = Cluster.HistogramBitCostDistance(ref tmp[0], ref allHistograms[clusters[j]], ref tmp[1]);
                    if (curBits < bestBits)
                    {
                        bestBits = curBits;
                        bestOut = clusters[j];
                    }
                }
                histogramSymbols[i] = bestOut;
                if (newIndex[bestOut] == InvalidIndex)
                {
                    newIndex[bestOut] = nextIndex++;
                }
            }
        }
        byte[] types = EnsureCapacity(ref split.Types, numBlocks);
        uint[] lengths = EnsureCapacity(ref split.Lengths, numBlocks);

        // Rewrite final assignment to block-split. There might be less blocks
        // than |num_blocks| due to clustering.
        {
            uint curLength = 0;
            nuint blockIdx = 0;
            byte maxType = 0;
            for (i = 0; i < numBlocks; ++i)
            {
                curLength += blockLengths[(int)i];
                if (i + 1 == numBlocks ||
                    histogramSymbols[i] != histogramSymbols[i + 1])
                {
                    byte id = (byte)newIndex[histogramSymbols[i]];
                    types[blockIdx] = id;
                    lengths[blockIdx] = curLength;
                    maxType = Math.Max(maxType, id);
                    curLength = 0;
                    ++blockIdx;
                }
            }
            split.NumBlocks = blockIdx;
            split.NumTypes = (nuint)maxType + 1;
        }
    }

    /// <summary>
    /// Create BlockSplit (partitioning) given the limits, estimates and "effort"
    /// parameters.
    ///
    /// NB: max_histograms is often less than number of histograms allowed by format;
    ///     this is done intentionally, to save some "space" for context-aware
    ///     clustering (here entropy is estimated for context-free symbols).
    /// </summary>
    private static void SplitByteVector<THistogram, TData>(ReadOnlySpan<TData> data, nuint length,
        nuint symbolsPerHistogram, nuint maxHistograms, nuint samplingStrideLength,
        double blockSwitchCost, in EncoderParams parameters, BlockSplit split)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        nuint dataSize = Histogram.HistogramDataSize<THistogram>();
        // Calculate number of histograms; initial estimate is one histogram per
        // specified amount of symbols; however, this value is capped.
        nuint numHistograms = length / symbolsPerHistogram + 1;
        if (numHistograms > maxHistograms)
        {
            numHistograms = maxHistograms;
        }

        // Corner case: no input.
        if (length == 0)
        {
            split.NumTypes = 1;
            return;
        }

        if (length < MinLengthForBlockSplitting)
        {
            byte[] types = EnsureCapacity(ref split.Types, split.NumBlocks + 1);
            uint[] lengths = EnsureCapacity(ref split.Lengths, split.NumBlocks + 1);
            split.NumTypes = 1;
            types[split.NumBlocks] = 0;
            lengths[split.NumBlocks] = (uint)length;
            split.NumBlocks++;
            return;
        }
        THistogram[] histograms = new THistogram[numHistograms + 1];
        // Find good entropy codes.
        InitialEntropyCodes<THistogram, TData>(data, length,
                                              samplingStrideLength,
                                              numHistograms, histograms);
        RefineEntropyCodes(data, length,
                           samplingStrideLength,
                           numHistograms, histograms, ref histograms[numHistograms]);
        {
            // Find a good path through literals with the good entropy codes.
            byte[] blockIds = new byte[length];
            nuint numBlocks = 0;
            nuint bitmaplen = (numHistograms + 7) >> 3;
            double[] insertCost = new double[dataSize * numHistograms];
            double[] cost = new double[numHistograms];
            byte[] switchSignal = new byte[length * bitmaplen];
            ushort[] newId = new ushort[numHistograms];
            nuint iters = parameters.Quality < Quality.HqZopflificationQuality ? 3u : 10u;
            for (nuint i = 0; i < iters; ++i)
            {
                numBlocks = FindBlocks<THistogram, TData>(data, length,
                                                         blockSwitchCost,
                                                         numHistograms, histograms,
                                                         insertCost, cost, switchSignal,
                                                         blockIds);
                numHistograms = RemapBlockIds(blockIds, length,
                                              newId, numHistograms);
                BuildBlockHistograms<THistogram, TData>(data, length, blockIds,
                                                       numHistograms, histograms);
            }
            ClusterBlocks<THistogram, TData>(data, length, numBlocks, blockIds, split);
        }
    }

    /// <summary>
    /// Extracts literals, command distance and prefix codes, then applies
    /// SplitByteVector to create partitioning.
    /// </summary>
    public static void SplitBlock(ReadOnlySpan<Command> cmds, nuint numCommands, ReadOnlySpan<byte> data,
        nuint pos, nuint mask, in EncoderParams parameters,
        BlockSplit literalSplit, BlockSplit insertAndCopySplit, BlockSplit distSplit)
    {
        {
            nuint literalsCount = CountLiterals(cmds, numCommands);
            byte[] literals = new byte[literalsCount];
            // Create a continuous array of literals.
            CopyLiteralsToByteArray(cmds, numCommands, data, pos, mask, literals);
            // Create the block split on the array of literals.
            // Literal histograms can have alphabet size up to 256.
            // Though, to accommodate context modeling, less than half of maximum size
            // is allowed.
            SplitByteVector<HistogramLiteral, byte>(
                literals, literalsCount,
                SymbolsPerLiteralHistogram, MaxLiteralHistograms,
                LiteralStrideLength, LiteralBlockSwitchCost, parameters,
                literalSplit);
            // NB: this might be a good place for injecting extra splitting without
            //     increasing encoder complexity; however, output partition would be less
            //     optimal than one produced with forced splitting inside
            //     SplitByteVector (FindBlocks / ClusterBlocks).
        }

        {
            // Compute prefix codes for commands.
            ushort[] insertAndCopyCodes = new ushort[numCommands];
            for (nuint i = 0; i < numCommands; ++i)
            {
                insertAndCopyCodes[i] = cmds[(int)i].CmdPrefix;
            }
            // Create the block split on the array of command prefixes.
            SplitByteVector<HistogramCommand, ushort>(
                insertAndCopyCodes, numCommands,
                SymbolsPerCommandHistogram, MaxCommandHistograms,
                CommandStrideLength, CommandBlockSwitchCost, parameters,
                insertAndCopySplit);
        }

        {
            // Create a continuous array of distance prefixes.
            ushort[] distancePrefixes = new ushort[numCommands];
            nuint j = 0;
            for (nuint i = 0; i < numCommands; ++i)
            {
                ref readonly Command cmd = ref cmds[(int)i];
                if (cmd.CopyLength != 0 && cmd.CmdPrefix >= 128)
                {
                    distancePrefixes[j++] = (ushort)(cmd.DistPrefix & 0x3FF);
                }
            }
            // Create the block split on the array of distance prefixes.
            SplitByteVector<HistogramDistance, ushort>(
                distancePrefixes, j,
                SymbolsPerDistanceHistogram, MaxCommandHistograms,
                DistanceStrideLength, DistanceBlockSwitchCost, parameters,
                distSplit);
        }
    }
}
