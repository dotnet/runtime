// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/cluster.c, cluster.h and cluster_inc.h.

using System.Runtime.CompilerServices;

namespace System.IO.Compression;

internal struct HistogramPair
{
    public uint Idx1;
    public uint Idx2;
    public double CostCombo;
    public double CostDiff;
}

/// <summary>Functions for clustering similar histograms together.</summary>
internal static class Cluster
{
    private const nuint MaxInputHistograms = 64;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HistogramPairIsLess(in HistogramPair p1, in HistogramPair p2)
    {
        if (p1.CostDiff != p2.CostDiff)
        {
            return p1.CostDiff > p2.CostDiff;
        }
        return (p1.Idx2 - p1.Idx1) > (p2.Idx2 - p2.Idx1);
    }

    /// <summary>Returns entropy reduction of the context map when we combine two clusters.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ClusterCostDiff(nuint sizeA, nuint sizeB)
    {
        nuint sizeC = sizeA + sizeB;
        return (double)sizeA * FastLog.FastLog2(sizeA) +
            (double)sizeB * FastLog.FastLog2(sizeB) -
            (double)sizeC * FastLog.FastLog2(sizeC);
    }

    /// <summary>
    /// Computes the bit cost reduction by combining out[idx1] and out[idx2] and if
    /// it is below a threshold, stores the pair (idx1, idx2) in the *pairs queue.
    /// </summary>
    public static void CompareAndPushToQueue<THistogram>(
        Span<THistogram> output, ref THistogram tmp, ReadOnlySpan<uint> clusterSize,
        uint idx1, uint idx2, nuint maxNumPairs, Span<HistogramPair> pairs,
        ref nuint numPairs)
        where THistogram : struct, IHistogram<THistogram>
    {
        bool isGoodPair = false;
        HistogramPair p = default;
        if (idx1 == idx2)
        {
            return;
        }
        if (idx2 < idx1)
        {
            uint t = idx2;
            idx2 = idx1;
            idx1 = t;
        }
        p.Idx1 = idx1;
        p.Idx2 = idx2;
        ref THistogram h1 = ref output[(int)idx1];
        ref THistogram h2 = ref output[(int)idx2];
        p.CostDiff = 0.5 * ClusterCostDiff(clusterSize[(int)idx1], clusterSize[(int)idx2]);
        p.CostDiff -= h1.BitCost;
        p.CostDiff -= h2.BitCost;

        if (h1.TotalCount == 0)
        {
            p.CostCombo = h2.BitCost;
            isGoodPair = true;
        }
        else if (h2.TotalCount == 0)
        {
            p.CostCombo = h1.BitCost;
            isGoodPair = true;
        }
        else
        {
            double threshold = numPairs == 0 ? 1e99 :
                Math.Max(0.0, pairs[0].CostDiff);
            double costCombo;
            tmp = h1;
            Histogram.HistogramAddHistogram(ref tmp, ref h2);
            costCombo = BitCost.PopulationCost(ref tmp);
            if (costCombo < threshold - p.CostDiff)
            {
                p.CostCombo = costCombo;
                isGoodPair = true;
            }
        }
        if (isGoodPair)
        {
            p.CostDiff += p.CostCombo;
            if (numPairs > 0 && HistogramPairIsLess(pairs[0], p))
            {
                // Replace the top of the queue if needed.
                if (numPairs < maxNumPairs)
                {
                    pairs[(int)numPairs] = pairs[0];
                    ++numPairs;
                }
                pairs[0] = p;
            }
            else if (numPairs < maxNumPairs)
            {
                pairs[(int)numPairs] = p;
                ++numPairs;
            }
        }
    }

    public static nuint HistogramCombine<THistogram>(
        Span<THistogram> output, ref THistogram tmp, Span<uint> clusterSize,
        Span<uint> symbols, Span<uint> clusters, Span<HistogramPair> pairs,
        nuint numClusters, nuint symbolsSize, nuint maxClusters, nuint maxNumPairs)
        where THistogram : struct, IHistogram<THistogram>
    {
        double costDiffThreshold = 0.0;
        nuint minClusterSize = 1;
        nuint numPairs = 0;

        {
            // We maintain a vector of histogram pairs, with the property that the pair
            // with the maximum bit cost reduction is the first.
            for (nuint idx1 = 0; idx1 < numClusters; ++idx1)
            {
                for (nuint idx2 = idx1 + 1; idx2 < numClusters; ++idx2)
                {
                    CompareAndPushToQueue(output, ref tmp, clusterSize, clusters[(int)idx1],
                        clusters[(int)idx2], maxNumPairs, pairs, ref numPairs);
                }
            }
        }

        while (numClusters > minClusterSize)
        {
            uint bestIdx1;
            uint bestIdx2;
            nuint i;
            if (pairs[0].CostDiff >= costDiffThreshold)
            {
                costDiffThreshold = 1e99;
                minClusterSize = maxClusters;
                continue;
            }
            // Take the best pair from the top of heap.
            bestIdx1 = pairs[0].Idx1;
            bestIdx2 = pairs[0].Idx2;
            Histogram.HistogramAddHistogram(ref output[(int)bestIdx1], ref output[(int)bestIdx2]);
            output[(int)bestIdx1].BitCost = pairs[0].CostCombo;
            clusterSize[(int)bestIdx1] += clusterSize[(int)bestIdx2];
            Span<uint> symbolsSlice = symbols.Slice(0, (int)symbolsSize);
            for (int s = 0; s < symbolsSlice.Length; ++s)
            {
                if (symbolsSlice[s] == bestIdx2)
                {
                    symbolsSlice[s] = bestIdx1;
                }
            }
            for (i = 0; i < numClusters; ++i)
            {
                if (clusters[(int)i] == bestIdx2)
                {
                    clusters.Slice((int)i + 1, (int)(numClusters - i - 1)).CopyTo(clusters.Slice((int)i));
                    break;
                }
            }
            --numClusters;
            {
                // Remove pairs intersecting the just combined best pair.
                nuint copyToIdx = 0;
                for (i = 0; i < numPairs; ++i)
                {
                    HistogramPair p = pairs[(int)i];
                    if (p.Idx1 == bestIdx1 || p.Idx2 == bestIdx1 ||
                        p.Idx1 == bestIdx2 || p.Idx2 == bestIdx2)
                    {
                        // Remove invalid pair from the queue.
                        continue;
                    }
                    if (HistogramPairIsLess(pairs[0], p))
                    {
                        // Replace the top of the queue if needed.
                        HistogramPair front = pairs[0];
                        pairs[0] = p;
                        pairs[(int)copyToIdx] = front;
                    }
                    else
                    {
                        pairs[(int)copyToIdx] = p;
                    }
                    ++copyToIdx;
                }
                numPairs = copyToIdx;
            }

            // Push new pairs formed with the combined histogram to the heap.
            for (i = 0; i < numClusters; ++i)
            {
                CompareAndPushToQueue(output, ref tmp, clusterSize, bestIdx1,
                    clusters[(int)i], maxNumPairs, pairs, ref numPairs);
            }
        }
        return numClusters;
    }

    /// <summary>What is the bit cost of moving histogram from cur_symbol to candidate.</summary>
    public static double HistogramBitCostDistance<THistogram>(
        ref THistogram histogram, ref THistogram candidate, ref THistogram tmp)
        where THistogram : struct, IHistogram<THistogram>
    {
        if (histogram.TotalCount == 0)
        {
            return 0.0;
        }
        else
        {
            tmp = histogram;
            Histogram.HistogramAddHistogram(ref tmp, ref candidate);
            return BitCost.PopulationCost(ref tmp) - candidate.BitCost;
        }
    }

    /// <summary>
    /// Find the best 'out' histogram for each of the 'in' histograms.
    /// When called, clusters[0..num_clusters) contains the unique values from
    /// symbols[0..in_size), but this property is not preserved in this function.
    /// Note: we assume that out[]->bit_cost_ is already up-to-date.
    /// </summary>
    /// <remarks>
    /// <paramref name="input"/>: Input histograms (not modified).<br/>
    /// </remarks>
    public static void HistogramRemap<THistogram>(
        Span<THistogram> input, nuint inSize, ReadOnlySpan<uint> clusters, nuint numClusters,
        Span<THistogram> output, ref THistogram tmp, Span<uint> symbols)
        where THistogram : struct, IHistogram<THistogram>
    {
        nuint i;
        for (i = 0; i < inSize; ++i)
        {
            uint bestOut = i == 0 ? symbols[0] : symbols[(int)i - 1];
            double bestBits = HistogramBitCostDistance(ref input[(int)i], ref output[(int)bestOut], ref tmp);
            for (nuint j = 0; j < numClusters; ++j)
            {
                double curBits = HistogramBitCostDistance(ref input[(int)i], ref output[(int)clusters[(int)j]], ref tmp);
                if (curBits < bestBits)
                {
                    bestBits = curBits;
                    bestOut = clusters[(int)j];
                }
            }
            symbols[(int)i] = bestOut;
        }

        // Recompute each out based on raw and symbols.
        for (i = 0; i < numClusters; ++i)
        {
            Histogram.HistogramClear(ref output[(int)clusters[(int)i]]);
        }
        for (i = 0; i < inSize; ++i)
        {
            Histogram.HistogramAddHistogram(ref output[(int)symbols[(int)i]], ref input[(int)i]);
        }
    }

    /// <summary>
    /// Reorders elements of the out[0..length) array and changes values in
    /// symbols[0..length) array in the following way:
    ///   * when called, symbols[] contains indexes into out[], and has N unique
    ///     values (possibly N &lt; length)
    ///   * on return, symbols'[i] = f(symbols[i]) and
    ///                out'[symbols'[i]] = out[symbols[i]], for each 0 &lt;= i &lt; length,
    ///     where f is a bijection between the range of symbols[] and [0..N), and
    ///     the first occurrences of values in symbols'[i] come in consecutive
    ///     increasing order.
    /// Returns N, the number of unique values in symbols[].
    /// </summary>
    public static nuint HistogramReindex<THistogram>(Span<THistogram> output, Span<uint> symbols, nuint length)
        where THistogram : struct, IHistogram<THistogram>
    {
        const uint InvalidIndex = uint.MaxValue;
        uint[] newIndex = new uint[length];
        uint nextIndex;
        nuint i;
        newIndex.AsSpan().Fill(InvalidIndex);
        nextIndex = 0;
        for (i = 0; i < length; ++i)
        {
            if (newIndex[symbols[(int)i]] == InvalidIndex)
            {
                newIndex[symbols[(int)i]] = nextIndex;
                ++nextIndex;
            }
        }
        // TODO(eustas): by using idea of "cycle-sort" we can avoid allocation of
        // tmp and reduce the number of copying by the factor of 2.
        THistogram[] tmp = new THistogram[nextIndex];
        nextIndex = 0;
        for (i = 0; i < length; ++i)
        {
            if (newIndex[symbols[(int)i]] == nextIndex)
            {
                tmp[nextIndex] = output[(int)symbols[(int)i]];
                ++nextIndex;
            }
            symbols[(int)i] = newIndex[symbols[(int)i]];
        }
        for (i = 0; i < nextIndex; ++i)
        {
            output[(int)i] = tmp[i];
        }
        return nextIndex;
    }

    /// <summary>Port of BrotliClusterHistograms{Literal,Command,Distance}.</summary>
    /// <remarks>
    /// <paramref name="input"/>: Input histograms (not modified).<br/>
    /// </remarks>
    public static void ClusterHistograms<THistogram>(
        Span<THistogram> input, nuint inSize, nuint maxHistograms,
        Span<THistogram> output, ref nuint outSize, Span<uint> histogramSymbols)
        where THistogram : struct, IHistogram<THistogram>
    {
        uint[] clusterSize = new uint[inSize];
        uint[] clusters = new uint[inSize];
        nuint numClusters = 0;
        nuint pairsCapacity = MaxInputHistograms * MaxInputHistograms / 2;
        // For the first pass of clustering, we allow all pairs.
        HistogramPair[] pairs = new HistogramPair[pairsCapacity + 1];
        THistogram tmp = default;
        nuint i;

        for (i = 0; i < inSize; ++i)
        {
            clusterSize[i] = 1;
        }

        for (i = 0; i < inSize; ++i)
        {
            ref THistogram o = ref output[(int)i];
            o = input[(int)i];
            o.BitCost = BitCost.PopulationCost(ref input[(int)i]);
            histogramSymbols[(int)i] = (uint)i;
        }

        for (i = 0; i < inSize; i += MaxInputHistograms)
        {
            nuint numToCombine = Math.Min(inSize - i, MaxInputHistograms);
            nuint numNewClusters;
            for (nuint j = 0; j < numToCombine; ++j)
            {
                clusters[numClusters + j] = (uint)(i + j);
            }
            numNewClusters =
                HistogramCombine(output, ref tmp, clusterSize,
                                 histogramSymbols.Slice((int)i),
                                 clusters.AsSpan((int)numClusters), pairs,
                                 numToCombine, numToCombine,
                                 maxHistograms, pairsCapacity);
            numClusters += numNewClusters;
        }

        {
            // For the second pass, we limit the total number of histogram pairs.
            // After this limit is reached, we only keep searching for the best pair.
            nuint maxNumPairs = Math.Min(64 * numClusters, (numClusters / 2) * numClusters);
            BlockSplitter.EnsureCapacity(ref pairs, ref pairsCapacity, maxNumPairs + 1);

            // Collapse similar histograms.
            numClusters = HistogramCombine(output, ref tmp, clusterSize,
                                           histogramSymbols, clusters,
                                           pairs, numClusters, inSize,
                                           maxHistograms, maxNumPairs);
        }
        // Find the optimal map from original histograms to the final ones.
        HistogramRemap(input, inSize, clusters, numClusters, output, ref tmp, histogramSymbols);
        // Convert the context map to a canonical form.
        outSize = HistogramReindex(output, histogramSymbols, inSize);
    }
}
