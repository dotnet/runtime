// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/entropy_encode.c and entropy_encode.h.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Compression;

/// <summary>A node of a Huffman tree.</summary>
internal struct HuffmanTree
{
    public uint TotalCount;
    public short IndexLeft;
    public short IndexRightOrValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HuffmanTree(uint count, short left, short right)
    {
        TotalCount = count;
        IndexLeft = left;
        IndexRightOrValue = right;
    }
}

/// <summary>Comparator used by <see cref="EntropyEncode.SortHuffmanTreeItems{TComparator}"/>.</summary>
internal interface IHuffmanTreeComparator
{
    static abstract bool Less(in HuffmanTree v0, in HuffmanTree v1);
}

/// <summary>Entropy encoding (Huffman) utilities.</summary>
internal static class EntropyEncode
{
    // 0..15 are values for bits
    private const int MaxHuffmanBits = 16;

    public static ReadOnlySpan<byte> ShellGaps => [132, 57, 23, 10, 4, 1];

    /// <summary>Sort the root nodes, least popular first.</summary>
    private struct SortHuffmanTreeComparator : IHuffmanTreeComparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Less(in HuffmanTree v0, in HuffmanTree v1)
        {
            if (v0.TotalCount != v1.TotalCount)
            {
                return v0.TotalCount < v1.TotalCount;
            }
            return v0.IndexRightOrValue > v1.IndexRightOrValue;
        }
    }

    /// <summary>Input size optimized Shell sort.</summary>
    public static void SortHuffmanTreeItems<TComparator>(Span<HuffmanTree> items, nuint n)
        where TComparator : struct, IHuffmanTreeComparator
    {
        items = items.Slice(0, (int)n);
        if (items.Length < 13)
        {
            // Insertion sort.
            for (int i = 1; i < items.Length; ++i)
            {
                HuffmanTree tmp = items[i];
                int k = i;
                int j = i - 1;
                while (TComparator.Less(tmp, items[j]))
                {
                    items[k] = items[j];
                    k = j;
                    if (j == 0)
                    {
                        break;
                    }
                    j--;
                }
                items[k] = tmp;
            }
        }
        else
        {
            // Shell sort.
            ReadOnlySpan<byte> gaps = ShellGaps;
            int g = items.Length < 57 ? 2 : 0;
            for (; g < gaps.Length; ++g)
            {
                int gap = gaps[g];
                for (int i = gap; i < items.Length; ++i)
                {
                    int j = i;
                    HuffmanTree tmp = items[i];
                    for (; j >= gap && TComparator.Less(tmp, items[j - gap]); j -= gap)
                    {
                        items[j] = items[j - gap];
                    }
                    items[j] = tmp;
                }
            }
        }
    }

    /// <summary>Returns true if assignment of depths succeeded, otherwise false.</summary>
    public static bool SetDepth(int p, Span<HuffmanTree> pool, Span<byte> depth, int maxDepth)
    {
        Span<int> stack = stackalloc int[16];
        int level = 0;
        Debug.Assert(maxDepth <= 15);
        stack[0] = -1;
        while (true)
        {
            ref HuffmanTree node = ref pool[p];
            if (node.IndexLeft >= 0)
            {
                level++;
                if (level > maxDepth)
                {
                    return false;
                }
                stack[level] = node.IndexRightOrValue;
                p = node.IndexLeft;
                continue;
            }
            else
            {
                depth[node.IndexRightOrValue] = (byte)level;
            }
            while (level >= 0 && stack[level] == -1)
            {
                level--;
            }
            if (level < 0)
            {
                return true;
            }
            p = stack[level];
            stack[level] = -1;
        }
    }

    /// <summary>
    /// This function will create a Huffman tree.
    ///
    /// The catch here is that the tree cannot be arbitrarily deep.
    /// Brotli specifies a maximum depth of 15 bits for "code trees"
    /// and 7 bits for "code length code trees."
    ///
    /// count_limit is the value that is to be faked as the minimum value
    /// and this minimum value is raised until the tree matches the
    /// maximum length requirement.
    ///
    /// The <paramref name="tree"/> has to be at least 2 * length + 1 long.
    /// </summary>
    public static void CreateHuffmanTree(ReadOnlySpan<uint> data, nuint length, int treeLimit, Span<HuffmanTree> tree, Span<byte> depth)
    {
        HuffmanTree sentinel = new HuffmanTree(uint.MaxValue, -1, -1);
        // For block sizes below 64 kB, we never need to do a second iteration
        // of this loop. Probably all of our block sizes will be smaller than
        // that, so this loop is mostly of academic interest. If we actually
        // would need this, we would be better off with the Katajainen algorithm.
        for (uint countLimit = 1; ; countLimit *= 2)
        {
            nuint n = 0;
            nuint i;
            nuint j;
            nuint k;
            for (i = length; i != 0;)
            {
                --i;
                uint d = data[(int)i];
                if (d != 0)
                {
                    uint count = Math.Max(d, countLimit);
                    tree[(int)n++] = new HuffmanTree(count, -1, (short)i);
                }
            }

            if (n == 1)
            {
                // Only one element.
                depth[tree[0].IndexRightOrValue] = 1;
                break;
            }

            SortHuffmanTreeItems<SortHuffmanTreeComparator>(tree, n);

            // The nodes are:
            // [0, n): the sorted leaf nodes that we start with.
            // [n]: we add a sentinel here.
            // [n + 1, 2n): new parent nodes are added here, starting from
            //              (n+1). These are naturally in ascending order.
            // [2n]: we add a sentinel at the end as well.
            // There will be (2n+1) elements at the end.
            tree[(int)n] = sentinel;
            tree[(int)n + 1] = sentinel;

            i = 0;      // Points to the next leaf node.
            j = n + 1;  // Points to the next non-leaf node.
            for (k = n - 1; k != 0; --k)
            {
                nuint left, right;
                if (tree[(int)i].TotalCount <= tree[(int)j].TotalCount)
                {
                    left = i;
                    ++i;
                }
                else
                {
                    left = j;
                    ++j;
                }
                if (tree[(int)i].TotalCount <= tree[(int)j].TotalCount)
                {
                    right = i;
                    ++i;
                }
                else
                {
                    right = j;
                    ++j;
                }

                // The sentinel node becomes the parent node.
                nuint jEnd = 2 * n - k;
                ref HuffmanTree parent = ref tree[(int)jEnd];
                parent.TotalCount = tree[(int)left].TotalCount + tree[(int)right].TotalCount;
                parent.IndexLeft = (short)left;
                parent.IndexRightOrValue = (short)right;

                // Add back the last sentinel node.
                tree[(int)jEnd + 1] = sentinel;
            }
            if (SetDepth((int)(2 * n - 1), tree, depth, treeLimit))
            {
                // We need to pack the Huffman tree in tree_limit bits. If this was not
                // successful, add fake entities to the lowest values and retry.
                break;
            }
        }
    }

    private static void Reverse(Span<byte> v, nuint start, nuint end)
    {
        --end;
        while (start < end)
        {
            byte tmp = v[(int)start];
            v[(int)start] = v[(int)end];
            v[(int)end] = tmp;
            ++start;
            --end;
        }
    }

    private static void WriteHuffmanTreeRepetitions(byte previousValue, byte value, nuint repetitions, ref nuint treeSize, Span<byte> tree, Span<byte> extraBitsData)
    {
        Debug.Assert(repetitions > 0);
        if (previousValue != value)
        {
            tree[(int)treeSize] = value;
            extraBitsData[(int)treeSize] = 0;
            ++treeSize;
            --repetitions;
        }
        if (repetitions == 7)
        {
            tree[(int)treeSize] = value;
            extraBitsData[(int)treeSize] = 0;
            ++treeSize;
            --repetitions;
        }
        if (repetitions < 3)
        {
            for (nuint i = 0; i < repetitions; ++i)
            {
                tree[(int)treeSize] = value;
                extraBitsData[(int)treeSize] = 0;
                ++treeSize;
            }
        }
        else
        {
            nuint start = treeSize;
            repetitions -= 3;
            while (true)
            {
                tree[(int)treeSize] = BrotliConstants.RepeatPreviousCodeLength;
                extraBitsData[(int)treeSize] = (byte)(repetitions & 0x3);
                ++treeSize;
                repetitions >>= 2;
                if (repetitions == 0)
                {
                    break;
                }
                --repetitions;
            }
            Reverse(tree, start, treeSize);
            Reverse(extraBitsData, start, treeSize);
        }
    }

    private static void WriteHuffmanTreeRepetitionsZeros(nuint repetitions, ref nuint treeSize, Span<byte> tree, Span<byte> extraBitsData)
    {
        if (repetitions == 11)
        {
            tree[(int)treeSize] = 0;
            extraBitsData[(int)treeSize] = 0;
            ++treeSize;
            --repetitions;
        }
        if (repetitions < 3)
        {
            for (nuint i = 0; i < repetitions; ++i)
            {
                tree[(int)treeSize] = 0;
                extraBitsData[(int)treeSize] = 0;
                ++treeSize;
            }
        }
        else
        {
            nuint start = treeSize;
            repetitions -= 3;
            while (true)
            {
                tree[(int)treeSize] = BrotliConstants.RepeatZeroCodeLength;
                extraBitsData[(int)treeSize] = (byte)(repetitions & 0x7);
                ++treeSize;
                repetitions >>= 3;
                if (repetitions == 0)
                {
                    break;
                }
                --repetitions;
            }
            Reverse(tree, start, treeSize);
            Reverse(extraBitsData, start, treeSize);
        }
    }

    /// <summary>
    /// Change the population counts in a way that the consequent
    /// Huffman tree compression, especially its RLE-part will be more
    /// likely to compress this data more efficiently.
    /// </summary>
    /// <param name="length">The size of the histogram.</param>
    /// <param name="counts">The population counts.</param>
    /// <param name="goodForRle">A buffer of at least <paramref name="length"/> size.</param>
    public static void OptimizeHuffmanCountsForRle(nuint length, Span<uint> counts, Span<byte> goodForRle)
    {
        int nonzeroCount = 0;
        nuint stride;
        nuint limit;
        nuint sum;
        const nuint StreakLimit = 1240;
        // Let's make the Huffman code more compatible with RLE encoding.
        int len = (int)length;
        int i = 0;
        ReadOnlySpan<uint> c = counts.Slice(0, len);
        if (Vector256.IsHardwareAccelerated)
        {
            for (; i <= c.Length - Vector256<uint>.Count; i += Vector256<uint>.Count)
            {
                uint zeroMask = Vector256.Equals(Vector256.Create(c.Slice(i, Vector256<uint>.Count)), Vector256<uint>.Zero).ExtractMostSignificantBits();
                nonzeroCount += Vector256<uint>.Count - (int)uint.PopCount(zeroMask);
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= c.Length - Vector128<uint>.Count; i += Vector128<uint>.Count)
            {
                uint zeroMask = Vector128.Equals(Vector128.Create(c.Slice(i, Vector128<uint>.Count)), Vector128<uint>.Zero).ExtractMostSignificantBits();
                nonzeroCount += Vector128<uint>.Count - (int)uint.PopCount(zeroMask);
            }
        }
        for (; i < c.Length; i++)
        {
            if (c[i] != 0)
            {
                ++nonzeroCount;
            }
        }
        if (nonzeroCount < 16)
        {
            return;
        }
        while (len != 0 && counts[len - 1] == 0)
        {
            --len;
        }
        if (len == 0)
        {
            return;  // All zeros.
        }
        // Trimming trailing zeros does not change the nonzero count.
        if (len - nonzeroCount < 6 && counts.Slice(0, len).ContainsAnyInRange(1u, 3u))
        {
            for (i = 1; i < len - 1; ++i)
            {
                if (counts[i - 1] != 0 && counts[i] == 0 && counts[i + 1] != 0)
                {
                    counts[i] = 1;
                }
            }
        }
        if (nonzeroCount < 28)
        {
            return;
        }
        // 2) Let's mark all population counts that already can be encoded
        // with an RLE code.
        Span<byte> good = goodForRle.Slice(0, len);
        good.Clear();
        {
            // Let's not spoil any of the existing good RLE codes.
            // Mark any seq of 0's that is longer as 5 as a good_for_rle.
            // Mark any seq of non-0's that is longer as 7 as a good_for_rle.
            uint symbol = counts[0];
            int step = 0;
            for (i = 0; i <= len; ++i)
            {
                if (i == len || counts[i] != symbol)
                {
                    if ((symbol == 0 && step >= 5) ||
                        (symbol != 0 && step >= 7))
                    {
                        good.Slice(i - step, step).Fill(1);
                    }
                    step = 1;
                    if (i != len)
                    {
                        symbol = counts[i];
                    }
                }
                else
                {
                    ++step;
                }
            }
        }
        // 3) Let's replace those population counts that lead to more RLE codes.
        // Math here is in 24.8 fixed point representation.
        stride = 0;
        limit = 256 * (counts[0] + counts[1] + counts[2]) / 3 + 420;
        sum = 0;
        for (i = 0; i <= len; ++i)
        {
            if (i == len || good[i] != 0 ||
                (i != 0 && good[i - 1] != 0) ||
                ((nuint)(256 * counts[i]) - limit + StreakLimit) >= 2 * StreakLimit)
            {
                if (stride >= 4 || (stride >= 3 && sum == 0))
                {
                    // The stride must end, collapse what we have, if we have enough (4).
                    nuint count = (sum + stride / 2) / stride;
                    if (count == 0)
                    {
                        count = 1;
                    }
                    if (sum == 0)
                    {
                        // Don't make an all zeros stride to be upgraded to ones.
                        count = 0;
                    }
                    // We don't want to change value at counts[i],
                    // that is already belonging to the next stride. Thus - 1.
                    counts.Slice(i - (int)stride, (int)stride).Fill((uint)count);
                }
                stride = 0;
                sum = 0;
                if (i < len - 2)
                {
                    // All interesting strides have a count of at least 4,
                    // at least when non-zeros.
                    limit = 256 * (counts[i] + counts[i + 1] + counts[i + 2]) / 3 + 420;
                }
                else if (i < len)
                {
                    limit = 256 * counts[i];
                }
                else
                {
                    limit = 0;
                }
            }
            ++stride;
            if (i != len)
            {
                sum += counts[i];
                if (stride >= 4)
                {
                    limit = (256 * sum + stride / 2) / stride;
                }
                if (stride == 4)
                {
                    limit += 120;
                }
            }
        }
    }

    private static void DecideOverRleUse(ReadOnlySpan<byte> depth, nuint length, out bool useRleForNonZero, out bool useRleForZero)
    {
        nuint totalRepsZero = 0;
        nuint totalRepsNonZero = 0;
        nuint countRepsZero = 1;
        nuint countRepsNonZero = 1;
        for (nuint i = 0; i < length;)
        {
            byte value = depth[(int)i];
            nuint reps = 1;
            for (nuint k = i + 1; k < length && depth[(int)k] == value; ++k)
            {
                ++reps;
            }
            if (reps >= 3 && value == 0)
            {
                totalRepsZero += reps;
                ++countRepsZero;
            }
            if (reps >= 4 && value != 0)
            {
                totalRepsNonZero += reps;
                ++countRepsNonZero;
            }
            i += reps;
        }
        useRleForNonZero = totalRepsNonZero > countRepsNonZero * 2;
        useRleForZero = totalRepsZero > countRepsZero * 2;
    }

    /// <summary>
    /// Write a Huffman tree from bit depths into the bit-stream representation
    /// of a Huffman tree. The generated Huffman tree is to be compressed once
    /// more using a Huffman tree.
    /// </summary>
    public static void WriteHuffmanTree(ReadOnlySpan<byte> depth, nuint length, ref nuint treeSize, Span<byte> tree, Span<byte> extraBitsData)
    {
        byte previousValue = BrotliConstants.InitialRepeatedCodeLength;
        nuint i;
        bool useRleForNonZero = false;
        bool useRleForZero = false;

        // Throw away trailing zeros.
        nuint newLength = length;
        for (i = 0; i < length; ++i)
        {
            if (depth[(int)(length - i - 1)] == 0)
            {
                --newLength;
            }
            else
            {
                break;
            }
        }

        // First gather statistics on if it is a good idea to do RLE.
        if (length > 50)
        {
            // Find RLE coding for longer codes.
            // Shorter codes seem not to benefit from RLE.
            DecideOverRleUse(depth, newLength, out useRleForNonZero, out useRleForZero);
        }

        // Actual RLE coding.
        for (i = 0; i < newLength;)
        {
            byte value = depth[(int)i];
            nuint reps = 1;
            if ((value != 0 && useRleForNonZero) ||
                (value == 0 && useRleForZero))
            {
                for (nuint k = i + 1; k < newLength && depth[(int)k] == value; ++k)
                {
                    ++reps;
                }
            }
            if (value == 0)
            {
                WriteHuffmanTreeRepetitionsZeros(reps, ref treeSize, tree, extraBitsData);
            }
            else
            {
                WriteHuffmanTreeRepetitions(previousValue, value, reps, ref treeSize, tree, extraBitsData);
                previousValue = value;
            }
            i += reps;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReverseBits(nuint numBits, ushort bits)
    {
        // Pre-reversed 4-bit values.
        ReadOnlySpan<byte> lut = [0x00, 0x08, 0x04, 0x0C, 0x02, 0x0A, 0x06, 0x0E, 0x01, 0x09, 0x05, 0x0D, 0x03, 0x0B, 0x07, 0x0F];
        nuint retval = lut[bits & 0x0F];
        for (nuint i = 4; i < numBits; i += 4)
        {
            retval <<= 4;
            bits = (ushort)(bits >> 4);
            retval |= lut[bits & 0x0F];
        }
        retval >>= (int)((0 - numBits) & 0x03);
        return (ushort)retval;
    }

    /// <summary>Get the actual bit values for a tree of bit depths.</summary>
    public static void ConvertBitDepthsToSymbols(ReadOnlySpan<byte> depth, nuint len, Span<ushort> bits)
    {
        // In Brotli, all bit depths are [1..15]
        // 0 bit depth means that the symbol does not exist.
        Span<ushort> blCount = stackalloc ushort[MaxHuffmanBits];
        Span<ushort> nextCode = stackalloc ushort[MaxHuffmanBits];
        blCount.Clear();
        int code = 0;
        for (nuint i = 0; i < len; ++i)
        {
            ++blCount[depth[(int)i]];
        }
        blCount[0] = 0;
        nextCode[0] = 0;
        for (int i = 1; i < MaxHuffmanBits; ++i)
        {
            code = (code + blCount[i - 1]) << 1;
            nextCode[i] = (ushort)code;
        }
        for (nuint i = 0; i < len; ++i)
        {
            byte d = depth[(int)i];
            if (d != 0)
            {
                bits[(int)i] = ReverseBits(d, nextCode[d]++);
            }
        }
    }
}
