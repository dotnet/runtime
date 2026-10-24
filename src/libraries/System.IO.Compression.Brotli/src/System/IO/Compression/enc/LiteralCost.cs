// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/literal_cost.c and literal_cost.h.

using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Literal cost model to allow backward reference replacement to be efficient.</summary>
internal static class LiteralCost
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint Utf8Position(nuint last, nuint c, nuint clamp)
    {
        if (c < 128)
        {
            return 0;  // Next one is the 'Byte 1' again.
        }
        else if (c >= 192)
        {
            // Next one is the 'Byte 2' of utf-8 encoding.
            return Math.Min(1, clamp);
        }
        else
        {
            // Let's decide over the last byte if this ends the sequence.
            if (last < 0xE0)
            {
                return 0;  // Completed two or three byte coding.
            }
            else
            {
                // Next one is the 'Byte 3' of utf-8 encoding.
                return Math.Min(2, clamp);
            }
        }
    }

    private static nuint DecideMultiByteStatsLevel(nuint pos, nuint len, nuint mask, ReadOnlySpan<byte> data)
    {
        nuint counts0 = 0, counts1 = 0, counts2 = 0;
        nuint maxUtf8 = 1;  // should be 2, but 1 compresses better.
        nuint lastC = 0;
        for (nuint i = 0; i < len; ++i)
        {
            nuint c = data[(int)((pos + i) & mask)];
            switch (Utf8Position(lastC, c, 2))
            {
                case 0: ++counts0; break;
                case 1: ++counts1; break;
                default: ++counts2; break;
            }
            lastC = c;
        }
        if (counts2 < 500)
        {
            maxUtf8 = 1;
        }
        if (counts1 + counts2 < 25)
        {
            maxUtf8 = 0;
        }
        return maxUtf8;
    }

    private static void EstimateBitCostsForLiteralsUtf8(nuint pos, nuint len, nuint mask, ReadOnlySpan<byte> data,
        Span<nuint> histogram, Span<float> cost)
    {
        // max_utf8 is 0 (normal ASCII single byte modeling),
        // 1 (for 2-byte UTF-8 modeling), or 2 (for 3-byte UTF-8 modeling).
        nuint maxUtf8 = DecideMultiByteStatsLevel(pos, len, mask, data);
        const nuint WindowHalf = 495;
        nuint inWindow = Math.Min(WindowHalf, len);
        Span<nuint> inWindowUtf8 = [0, 0, 0];
        nuint i;
        histogram.Slice(0, 3 * 256).Clear();

        {
            // Bootstrap histograms.
            nuint lastC = 0;
            nuint utf8Pos = 0;
            for (i = 0; i < inWindow; ++i)
            {
                nuint c = data[(int)((pos + i) & mask)];
                ++histogram[(int)(256 * utf8Pos + c)];
                ++inWindowUtf8[(int)utf8Pos];
                utf8Pos = Utf8Position(lastC, c, maxUtf8);
                lastC = c;
            }
        }

        // Compute bit costs with sliding window.
        const nuint PrologueLength = 2000;
        const double Multiplier = 0.35 / 2000;
        for (i = 0; i < len; ++i)
        {
            if (i >= WindowHalf)
            {
                // Remove a byte in the past.
                nuint c =
                    i < WindowHalf + 1 ? 0u : data[(int)((pos + i - WindowHalf - 1) & mask)];
                nuint lastC =
                    i < WindowHalf + 2 ? 0u : data[(int)((pos + i - WindowHalf - 2) & mask)];
                nuint utf8Pos2 = Utf8Position(lastC, c, maxUtf8);
                --histogram[(int)(256 * utf8Pos2 + data[(int)((pos + i - WindowHalf) & mask)])];
                --inWindowUtf8[(int)utf8Pos2];
            }
            if (i + WindowHalf < len)
            {
                // Add a byte in the future.
                nuint c = data[(int)((pos + i + WindowHalf - 1) & mask)];
                nuint lastC = data[(int)((pos + i + WindowHalf - 2) & mask)];
                nuint utf8Pos2 = Utf8Position(lastC, c, maxUtf8);
                ++histogram[(int)(256 * utf8Pos2 + data[(int)((pos + i + WindowHalf) & mask)])];
                ++inWindowUtf8[(int)utf8Pos2];
            }
            {
                nuint c = i < 1 ? 0u : data[(int)((pos + i - 1) & mask)];
                nuint lastC = i < 2 ? 0u : data[(int)((pos + i - 2) & mask)];
                nuint utf8Pos = Utf8Position(lastC, c, maxUtf8);
                nuint maskedPos = (pos + i) & mask;
                nuint histo = histogram[(int)(256 * utf8Pos + data[(int)maskedPos])];
                double litCost;
                if (histo == 0)
                {
                    histo = 1;
                }
                litCost = FastLog.FastLog2(inWindowUtf8[(int)utf8Pos]) - FastLog.FastLog2(histo);
                litCost += 0.02905;
                if (litCost < 1.0)
                {
                    litCost *= 0.5;
                    litCost += 0.5;
                }
                // Make the first bytes more expensive -- seems to help, not sure why.
                // Perhaps because the entropy source is changing its properties
                // rapidly in the beginning of the file, perhaps because the beginning
                // of the data is a statistical "anomaly".
                if (i < PrologueLength)
                {
                    litCost += 0.35 + Multiplier * (double)i;
                }
                cost[(int)i] = (float)litCost;
            }
        }
    }

    /// <summary>
    /// Estimates how many bits the literals in the interval [pos, pos + len) in the
    /// ring-buffer (data, mask) will take entropy coded and writes these estimates
    /// to the cost[0..len) array.
    /// </summary>
    /// <remarks>
    /// <paramref name="histogram"/>: Scratch space of at least 3 * 256 elements.<br/>
    /// </remarks>
    public static void EstimateBitCostsForLiterals(nuint pos, nuint len, nuint mask, ReadOnlySpan<byte> data,
        Span<nuint> histogram, Span<float> cost)
    {
        if (Utf8Util.IsMostlyUtf8(data, pos, mask, len, Utf8Util.MinUtf8Ratio))
        {
            EstimateBitCostsForLiteralsUtf8(pos, len, mask, data, histogram, cost);
            return;
        }
        else
        {
            const nuint WindowHalf = 2000;
            nuint inWindow = Math.Min(WindowHalf, len);
            nuint i;
            histogram.Slice(0, 256).Clear();

            // Bootstrap histogram.
            for (i = 0; i < inWindow; ++i)
            {
                ++histogram[data[(int)((pos + i) & mask)]];
            }

            // Compute bit costs with sliding window.
            for (i = 0; i < len; ++i)
            {
                nuint histo;
                if (i >= WindowHalf)
                {
                    // Remove a byte in the past.
                    --histogram[data[(int)((pos + i - WindowHalf) & mask)]];
                    --inWindow;
                }
                if (i + WindowHalf < len)
                {
                    // Add a byte in the future.
                    ++histogram[data[(int)((pos + i + WindowHalf) & mask)]];
                    ++inWindow;
                }
                histo = histogram[data[(int)((pos + i) & mask)]];
                if (histo == 0)
                {
                    histo = 1;
                }
                {
                    double litCost = FastLog.FastLog2(inWindow) - FastLog.FastLog2(histo);
                    litCost += 0.029;
                    if (litCost < 1.0)
                    {
                        litCost *= 0.5;
                        litCost += 0.5;
                    }
                    cost[(int)i] = (float)litCost;
                }
            }
        }
    }
}
