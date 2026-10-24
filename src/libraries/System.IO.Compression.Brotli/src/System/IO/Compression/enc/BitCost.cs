// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/bit_cost.c, bit_cost.h and bit_cost_inc.h.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Compression;

/// <summary>Functions to estimate the bit cost of Huffman trees.</summary>
internal static class BitCost
{
    private const double OneSymbolHistogramCost = 12;
    private const double TwoSymbolHistogramCost = 20;
    private const double ThreeSymbolHistogramCost = 28;
    private const double FourSymbolHistogramCost = 37;

    public static double BitsEntropy(ReadOnlySpan<uint> population, nuint size)
    {
        // The reference implementation computes retval -= p * FastLog2(p) for every element in order.
        // For p <= 1 the term is exactly +0.0 (FastLog2(0) == FastLog2(1) == 0), and subtracting +0.0 is an identity
        // in IEEE arithmetic, so only the terms of counts >= 2 are computed and subtracted (in the same order).
        // The counts are processed in chunks: the counts >= 2 are first collected (skipping groups of counts <= 1
        // with vector compares) and then their terms are subtracted in a separate loop. The Math.Log2 call of
        // FastLog2 (only needed for counts >= 256) is kept out of the hot loops, so their state stays in registers.
        const int ChunkSize = 64;
        population = population.Slice(0, (int)size);
        nuint sum = SumCounts(population);
        double retval = 0;
        Span<uint> counts = stackalloc uint[ChunkSize];
        Span<double> terms = stackalloc double[ChunkSize];
        while (!population.IsEmpty)
        {
            ReadOnlySpan<uint> chunk = population.Slice(0, Math.Min(population.Length, ChunkSize));
            int n = CollectCountsAbove1(chunk, counts, out uint orCounts);
            if (orCounts < (uint)FastLog.Log2TableSize)
            {
                // Common case: all counts are small, no Math.Log2 calls.
                retval = SubtractSmallCountTerms(retval, counts.Slice(0, n));
            }
            else
            {
                Span<double> chunkTerms = terms.Slice(0, n);
                ComputeEntropyTerms(counts.Slice(0, n), chunkTerms);
                retval = SubtractTerms(retval, chunkTerms);
            }
            population = population.Slice(chunk.Length);
        }
        if (sum != 0)
        {
            retval += (double)sum * FastLog.FastLog2(sum);
        }

        if (retval < (double)sum)
        {
            // At least one bit per literal is needed.
            retval = (double)sum;
        }

        return retval;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint SumCounts(ReadOnlySpan<uint> population)
    {
        ulong sum = 0;
        int i = 0;
        if (Vector128.IsHardwareAccelerated && population.Length >= Vector128<uint>.Count)
        {
            Vector128<ulong> acc = Vector128<ulong>.Zero;
            for (; i <= population.Length - Vector128<uint>.Count; i += Vector128<uint>.Count)
            {
                Vector128<uint> v = Vector128.Create(population.Slice(i, Vector128<uint>.Count));
                acc += Vector128.WidenLower(v) + Vector128.WidenUpper(v);
            }
            sum = Vector128.Sum(acc);
        }
        for (; i < population.Length; i++)
        {
            sum += population[i];
        }
        return (nuint)sum;
    }

    /// <summary>
    /// Copies the counts &gt; 1 of <paramref name="chunk"/> (in order) to <paramref name="counts"/> and returns their number.
    /// <paramref name="counts"/> must be at least chunk.Length long (slot n is written before n is known to be final).
    /// <paramref name="orCounts"/> receives the bitwise or of the collected counts (possibly including some counts &lt;= 1).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CollectCountsAbove1(ReadOnlySpan<uint> chunk, Span<uint> counts, out uint orCounts)
    {
        int n = 0;
        uint or = 0;
        int i = 0;
        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= chunk.Length - Vector128<uint>.Count; i += Vector128<uint>.Count)
            {
                ReadOnlySpan<uint> group = chunk.Slice(i, Vector128<uint>.Count);
                if (Vector128.GreaterThanAny(Vector128.Create(group), Vector128<uint>.One))
                {
                    for (int k = 0; k < group.Length; k++)
                    {
                        uint p = group[k];
                        or |= p;
                        // Branchless compaction: the slot is overwritten by the next count unless p > 1.
                        counts[n] = p;
                        n += p > 1 ? 1 : 0;
                    }
                }
            }
        }
        for (; i < chunk.Length; i++)
        {
            uint p = chunk[i];
            or |= p;
            counts[n] = p;
            n += p > 1 ? 1 : 0;
        }
        orCounts = or;
        return n;
    }

    /// <summary>Returns retval - sum of counts[k] * FastLog2(counts[k]) (subtracted in order); all counts must be &lt; 256.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SubtractSmallCountTerms(double retval, ReadOnlySpan<uint> counts)
    {
        ReadOnlySpan<double> log2Table = FastLog.Log2Table;
        for (int k = 0; k < counts.Length; k++)
        {
            uint p = counts[k];
            retval -= (double)p * log2Table[(int)(byte)p];
        }
        return retval;
    }

    /// <summary>Returns retval - sum of terms (subtracted in order).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SubtractTerms(double retval, ReadOnlySpan<double> terms)
    {
        for (int k = 0; k < terms.Length; k++)
        {
            retval -= terms[k];
        }
        return retval;
    }

    /// <summary>terms[k] = counts[k] * FastLog2(counts[k]); kept separate so that few values are live across the Math.Log2 calls.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ComputeEntropyTerms(ReadOnlySpan<uint> counts, Span<double> terms)
    {
        terms = terms.Slice(0, counts.Length);
        for (int k = 0; k < counts.Length; k++)
        {
            uint p = counts[k];
            terms[k] = (double)p * FastLog.FastLog2(p);
        }
    }

    /// <summary>Returns true if any of the counts is &gt;= 256 (i.e. FastLog2 needs Math.Log2).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasLargeCounts(ReadOnlySpan<uint> counts)
    {
        uint or = 0;
        int i = 0;
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<uint> acc = Vector128<uint>.Zero;
            for (; i <= counts.Length - Vector128<uint>.Count; i += Vector128<uint>.Count)
            {
                acc |= Vector128.Create(counts.Slice(i, Vector128<uint>.Count));
            }
            or = acc[0] | acc[1] | acc[2] | acc[3];
        }
        for (; i < counts.Length; i++)
        {
            or |= counts[i];
        }
        return or >= FastLog.Log2TableSize;
    }

    /// <summary>logs[k] = FastLog2(counts[k]).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ComputeLogs(ReadOnlySpan<uint> counts, Span<double> logs)
    {
        logs = logs.Slice(0, counts.Length);
        for (int k = 0; k < counts.Length; k++)
        {
            logs[k] = FastLog.FastLog2(counts[k]);
        }
    }

    /// <summary>Port of BrotliPopulationCost{Literal,Command,Distance}.</summary>
    public static double PopulationCost<THistogram>(ref THistogram histogram)
        where THistogram : struct, IHistogram<THistogram>
    {
        int dataSize = THistogram.DataSize;
        int count = 0;
        int s0 = 0, s1 = 0, s2 = 0, s3 = 0;
        double bits = 0.0;
        if (histogram.TotalCount == 0)
        {
            return OneSymbolHistogramCost;
        }
        ReadOnlySpan<uint> data = histogram.Data.Slice(0, dataSize);
        for (int i = 0; i < data.Length; ++i)
        {
            if (data[i] > 0)
            {
                switch (count)
                {
                    case 0: s0 = i; break;
                    case 1: s1 = i; break;
                    case 2: s2 = i; break;
                    case 3: s3 = i; break;
                }
                ++count;
                if (count > 4)
                {
                    break;
                }
            }
        }
        if (count == 1)
        {
            return OneSymbolHistogramCost;
        }
        if (count == 2)
        {
            return TwoSymbolHistogramCost + (double)histogram.TotalCount;
        }
        if (count == 3)
        {
            uint histo0 = data[s0];
            uint histo1 = data[s1];
            uint histo2 = data[s2];
            uint histomax = Math.Max(histo0, Math.Max(histo1, histo2));
            return ThreeSymbolHistogramCost +
                   2 * (histo0 + histo1 + histo2) - histomax;
        }
        if (count == 4)
        {
            Span<uint> histo = [data[s0], data[s1], data[s2], data[s3]];
            // Sort
            for (int i = 0; i < 4; ++i)
            {
                for (int j = i + 1; j < 4; ++j)
                {
                    if (histo[j] > histo[i])
                    {
                        (histo[j], histo[i]) = (histo[i], histo[j]);
                    }
                }
            }
            uint h23 = histo[2] + histo[3];
            uint histomax = Math.Max(h23, histo[0]);
            return FourSymbolHistogramCost +
                   3 * h23 + 2 * (histo[0] + histo[1]) - histomax;
        }

        {
            // In this loop we compute the entropy of the histogram and simultaneously
            // build a simplified histogram of the code length codes where we use the
            // zero repeat code 17, but we don't use the non-zero repeat code 16.
            // The data is processed in chunks: when a chunk contains counts >= 256, their logarithms (Math.Log2 calls)
            // are computed upfront, so that the main loop does not contain calls and its state stays in registers.
            // The computations and their order are the same as in the reference implementation.
            const int ChunkSize = 64;
            nuint maxDepth = 1;
            Span<uint> depthHisto = stackalloc uint[BrotliConstants.CodeLengthCodes];
            Span<double> logs = stackalloc double[ChunkSize];
            depthHisto.Clear();
            double log2total = FastLog.FastLog2(histogram.TotalCount);
            int i = 0;
            bool done = false;
            while (!done && i < data.Length)
            {
                int chunkStart = i;
                int chunkEnd = Math.Min(i + ChunkSize, data.Length);
                bool useLogs = false;
                if (HasLargeCounts(data.Slice(chunkStart, chunkEnd - chunkStart)))
                {
                    ComputeLogs(data.Slice(chunkStart, chunkEnd - chunkStart), logs);
                    useLogs = true;
                }
                ReadOnlySpan<double> log2Table = FastLog.Log2Table;
                double l2t = log2total;
                double b = bits;
                nuint md = maxDepth;
                while (i < chunkEnd)
                {
                    uint d = data[i];
                    if (d > 0)
                    {
                        // Compute -log2(P(symbol)) = -log2(count(symbol)/total_count) =
                        //                          = log2(total_count) - log2(count(symbol))
                        double log2p = l2t - (useLogs ? logs[i - chunkStart] : log2Table[(int)(byte)d]);
                        // Approximate the bit depth by round(-log2(P(symbol))).
                        // NB: 0 <= log2p + 0.5 < 2^31, so the conversion via int is exact.
                        nuint depth = (nuint)(int)(log2p + 0.5);
                        b += d * log2p;
                        if (depth > 15)
                        {
                            depth = 15;
                        }
                        if (depth > md)
                        {
                            md = depth;
                        }
                        ++depthHisto[(int)depth];
                        ++i;
                    }
                    else
                    {
                        // Compute the run length of zeros and add the appropriate number of 0
                        // and 17 code length codes to the code length code histogram.
                        uint reps = 1;
                        for (int k = i + 1; k < data.Length && data[k] == 0; ++k)
                        {
                            ++reps;
                        }
                        i += (int)reps;
                        if (i == data.Length)
                        {
                            // Don't add any cost for the last zero run, since these are encoded
                            // only implicitly.
                            done = true;
                            break;
                        }
                        if (reps < 3)
                        {
                            depthHisto[0] += reps;
                        }
                        else
                        {
                            reps -= 2;
                            while (reps > 0)
                            {
                                ++depthHisto[BrotliConstants.RepeatZeroCodeLength];
                                // Add the 3 extra bits for the 17 code length code.
                                b += 3;
                                reps >>= 3;
                            }
                        }
                    }
                }
                bits = b;
                maxDepth = md;
            }
            // Add the estimated encoding cost of the code length code histogram.
            bits += (double)(18 + 2 * maxDepth);
            // Add the entropy of the code length code histogram.
            bits += BitsEntropy(depthHisto, BrotliConstants.CodeLengthCodes);
        }
        return bits;
    }
}
