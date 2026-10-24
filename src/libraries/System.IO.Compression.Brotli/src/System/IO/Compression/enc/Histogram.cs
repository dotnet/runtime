// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/histogram.c, histogram.h and histogram_inc.h.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Compression;

/// <summary>
/// Common shape of the histogram types (the <c>histogram_inc.h</c> template). Generic code uses it with a struct
/// type parameter, so the JIT specializes every method per histogram type (no boxing, no virtual calls).
/// Histogram parameters are always passed by <see langword="ref"/> (never <see langword="in"/>) to avoid defensive copies.
/// </summary>
internal interface IHistogram<THistogram> where THistogram : struct, IHistogram<THistogram>
{
    /// <summary>The number of symbols (DATA_SIZE).</summary>
    static abstract int DataSize { get; }

    [UnscopedRef]
    Span<uint> Data { get; }

    nuint TotalCount { get; set; }

    double BitCost { get; set; }
}

/// <summary>A simple container for histograms of literals.</summary>
internal struct HistogramLiteral : IHistogram<HistogramLiteral>
{
    public DataArray Data;
    public nuint TotalCount;
    public double BitCost;

    public static int DataSize => BrotliConstants.NumLiteralSymbols;

    [UnscopedRef]
    Span<uint> IHistogram<HistogramLiteral>.Data => Data;

    nuint IHistogram<HistogramLiteral>.TotalCount { readonly get => TotalCount; set => TotalCount = value; }

    double IHistogram<HistogramLiteral>.BitCost { readonly get => BitCost; set => BitCost = value; }

    [InlineArray(BrotliConstants.NumLiteralSymbols)]
    public struct DataArray
    {
        private uint _element0;
    }
}

/// <summary>A simple container for histograms of insert-and-copy commands.</summary>
internal struct HistogramCommand : IHistogram<HistogramCommand>
{
    public DataArray Data;
    public nuint TotalCount;
    public double BitCost;

    public static int DataSize => BrotliConstants.NumCommandSymbols;

    [UnscopedRef]
    Span<uint> IHistogram<HistogramCommand>.Data => Data;

    nuint IHistogram<HistogramCommand>.TotalCount { readonly get => TotalCount; set => TotalCount = value; }

    double IHistogram<HistogramCommand>.BitCost { readonly get => BitCost; set => BitCost = value; }

    [InlineArray(BrotliConstants.NumCommandSymbols)]
    public struct DataArray
    {
        private uint _element0;
    }
}

/// <summary>A simple container for histograms of distance codes.</summary>
internal struct HistogramDistance : IHistogram<HistogramDistance>
{
    public DataArray Data;
    public nuint TotalCount;
    public double BitCost;

    public static int DataSize => Histogram.NumHistogramDistanceSymbols;

    [UnscopedRef]
    Span<uint> IHistogram<HistogramDistance>.Data => Data;

    nuint IHistogram<HistogramDistance>.TotalCount { readonly get => TotalCount; set => TotalCount = value; }

    double IHistogram<HistogramDistance>.BitCost { readonly get => BitCost; set => BitCost = value; }

    [InlineArray(Histogram.NumHistogramDistanceSymbols)]
    public struct DataArray
    {
        private uint _element0;
    }
}

/// <summary>Models the histograms of literals, commands and distance codes.</summary>
internal static class Histogram
{
    /// <summary>The distance symbols effectively used by "Large Window Brotli" (32-bit).</summary>
    public const int NumHistogramDistanceSymbols = 544;

    /// <summary>Iterates over the blocks of a <see cref="BlockSplit"/>, one symbol at a time.</summary>
    private struct BlockSplitIterator
    {
        // The types and lengths arrays of the split (not owned).
        private readonly byte[] _types;
        private readonly uint[] _lengths;
        public nuint Idx;
        public nuint Type;
        public nuint Length;

        public BlockSplitIterator(BlockSplit split)
        {
            _types = split.Types ?? [];
            _lengths = split.Lengths ?? [];
            Idx = 0;
            Type = 0;
            Length = split.NumBlocks != 0 ? _lengths[0] : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Next()
        {
            if (Length == 0)
            {
                ++Idx;
                Type = _types[(int)Idx];
                Length = _lengths[(int)Idx];
            }
            --Length;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HistogramClear<THistogram>(ref THistogram self)
        where THistogram : struct, IHistogram<THistogram>
    {
        self.Data.Clear();
        self.TotalCount = 0;
        self.BitCost = double.PositiveInfinity;
    }

    public static void ClearHistograms<THistogram>(Span<THistogram> array, nuint length)
        where THistogram : struct, IHistogram<THistogram>
    {
        array = array.Slice(0, (int)length);
        for (int i = 0; i < array.Length; ++i)
        {
            HistogramClear(ref array[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HistogramAdd<THistogram>(ref THistogram self, nuint val)
        where THistogram : struct, IHistogram<THistogram>
    {
        ++self.Data[(int)val];
        ++self.TotalCount;
    }

    public static void HistogramAddVector<THistogram, TData>(ref THistogram self, ReadOnlySpan<TData> p, nuint n)
        where THistogram : struct, IHistogram<THistogram>
        where TData : unmanaged, IBinaryInteger<TData>
    {
        self.TotalCount += n;
        Span<uint> data = self.Data;
        p = p.Slice(0, (int)n);
        for (int i = 0; i < p.Length; i++)
        {
            ++data[int.CreateTruncating(p[i])];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HistogramAddHistogram<THistogram>(ref THistogram self, ref THistogram v)
        where THistogram : struct, IHistogram<THistogram>
    {
        self.TotalCount += v.TotalCount;
        AddCounts(self.Data, v.Data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint HistogramDataSize<THistogram>()
        where THistogram : struct, IHistogram<THistogram> => (nuint)THistogram.DataSize;

    /// <summary>dst[i] += src[i] (with uint32 wrap-around) for all i.</summary>
    private static void AddCounts(Span<uint> dst, ReadOnlySpan<uint> src)
    {
        src = src.Slice(0, dst.Length);
        int i = 0;
        if (Vector256.IsHardwareAccelerated)
        {
            for (; i <= dst.Length - Vector256<uint>.Count; i += Vector256<uint>.Count)
            {
                Span<uint> d = dst.Slice(i, Vector256<uint>.Count);
                (Vector256.Create<uint>(d) + Vector256.Create(src.Slice(i, Vector256<uint>.Count))).CopyTo(d);
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= dst.Length - Vector128<uint>.Count; i += Vector128<uint>.Count)
            {
                Span<uint> d = dst.Slice(i, Vector128<uint>.Count);
                (Vector128.Create<uint>(d) + Vector128.Create(src.Slice(i, Vector128<uint>.Count))).CopyTo(d);
            }
        }
        for (; i < dst.Length; i++)
        {
            dst[i] += src[i];
        }
    }

    /// <summary>Build per-context histograms of literals, commands and distance codes.</summary>
    /// <remarks>
    /// <paramref name="contextModes"/>: Context mode per literal block type; empty when the C pointer is NULL.<br/>
    /// </remarks>
    public static void BuildHistogramsWithContext(
        ReadOnlySpan<Command> cmds, nuint numCommands,
        BlockSplit literalSplit, BlockSplit insertAndCopySplit,
        BlockSplit distSplit, ReadOnlySpan<byte> ringbuffer, nuint startPos,
        nuint mask, byte prevByte, byte prevByte2,
        ReadOnlySpan<BrotliContextType> contextModes, Span<HistogramLiteral> literalHistograms,
        Span<HistogramCommand> insertAndCopyHistograms,
        Span<HistogramDistance> copyDistHistograms)
    {
        nuint pos = startPos;
        BlockSplitIterator literalIt = new BlockSplitIterator(literalSplit);
        BlockSplitIterator insertAndCopyIt = new BlockSplitIterator(insertAndCopySplit);
        BlockSplitIterator distIt = new BlockSplitIterator(distSplit);
        cmds = cmds.Slice(0, (int)numCommands);
        bool hasContextModes = !contextModes.IsEmpty;
        for (int i = 0; i < cmds.Length; ++i)
        {
            ref readonly Command cmd = ref cmds[i];
            insertAndCopyIt.Next();
            HistogramAdd(ref insertAndCopyHistograms[(int)insertAndCopyIt.Type], cmd.CmdPrefix);
            for (nuint j = cmd.InsertLen; j != 0; --j)
            {
                nuint context;
                literalIt.Next();
                context = literalIt.Type;
                if (hasContextModes)
                {
                    ReadOnlySpan<byte> lut = BrotliContext.GetLut(contextModes[(int)context]);
                    context = (context << BrotliConstants.LiteralContextBits) +
                        (nuint)BrotliContext.GetContext(prevByte, prevByte2, lut);
                }
                byte literal = ringbuffer[(int)(pos & mask)];
                HistogramAdd(ref literalHistograms[(int)context], literal);
                prevByte2 = prevByte;
                prevByte = literal;
                ++pos;
            }
            uint copyLen = cmd.CopyLength;
            pos += copyLen;
            if (copyLen != 0)
            {
                prevByte2 = ringbuffer[(int)((pos - 2) & mask)];
                prevByte = ringbuffer[(int)((pos - 1) & mask)];
                if (cmd.CmdPrefix >= 128)
                {
                    nuint context;
                    distIt.Next();
                    context = (distIt.Type << BrotliConstants.DistanceContextBits) + cmd.DistanceContext();
                    HistogramAdd(ref copyDistHistograms[(int)context], (nuint)(cmd.DistPrefix & 0x3FF));
                }
            }
        }
    }
}
