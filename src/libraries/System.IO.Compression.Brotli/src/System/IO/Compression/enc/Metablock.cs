// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/metablock.c, metablock.h and metablock_inc.h.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>
/// The block splits, context maps and (clustered) histograms of a meta-block.
/// The constructor is the equivalent of InitMetaBlockSplit. An instance may be reused for several meta-blocks
/// by calling <see cref="Metablock.InitMetaBlockSplit"/> before each one; the arrays are then reused.
/// </summary>
internal sealed class MetaBlockSplit
{
    public readonly BlockSplit LiteralSplit = new();
    public readonly BlockSplit CommandSplit = new();
    public readonly BlockSplit DistanceSplit = new();
    public uint[]? LiteralContextMap;
    public nuint LiteralContextMapSize;
    public uint[]? DistanceContextMap;
    public nuint DistanceContextMapSize;
    public HistogramLiteral[]? LiteralHistograms;
    public nuint LiteralHistogramsSize;
    public HistogramCommand[]? CommandHistograms;
    public nuint CommandHistogramsSize;
    public HistogramDistance[]? DistanceHistograms;
    public nuint DistanceHistogramsSize;

    // Scratch buffers of BrotliBuildMetaBlock (allocated per call in C), kept for reuse.
    internal HistogramLiteral[]? TmpLiteralHistograms;
    internal HistogramDistance[]? TmpDistanceHistograms;

    // The block splitters of BrotliBuildMetaBlockGreedy (allocated per call in C), allocated on first use.
    internal Metablock.GreedyArena? GreedyArena;
}

/// <summary>
/// Algorithms for distributing the literals and commands of a metablock between
/// block types and contexts.
/// </summary>
internal static class Metablock
{
    public const int MaxStaticContexts = 13;

    /// <summary>Port of InitMetaBlockSplit: resets <paramref name="mb"/> for a new meta-block (keeping the arrays for reuse).</summary>
    public static void InitMetaBlockSplit(MetaBlockSplit mb)
    {
        BlockSplitter.InitBlockSplit(mb.LiteralSplit);
        BlockSplitter.InitBlockSplit(mb.CommandSplit);
        BlockSplitter.InitBlockSplit(mb.DistanceSplit);
        mb.LiteralContextMapSize = 0;
        mb.DistanceContextMapSize = 0;
        mb.LiteralHistogramsSize = 0;
        mb.CommandHistogramsSize = 0;
        mb.DistanceHistogramsSize = 0;
    }

    /// <summary>Port of DestroyMetaBlockSplit: releases all arrays.</summary>
    public static void DestroyMetaBlockSplit(MetaBlockSplit mb)
    {
        BlockSplitter.DestroyBlockSplit(mb.LiteralSplit);
        BlockSplitter.DestroyBlockSplit(mb.CommandSplit);
        BlockSplitter.DestroyBlockSplit(mb.DistanceSplit);
        mb.LiteralContextMap = null;
        mb.DistanceContextMap = null;
        mb.LiteralHistograms = null;
        mb.CommandHistograms = null;
        mb.DistanceHistograms = null;
        mb.TmpLiteralHistograms = null;
        mb.TmpDistanceHistograms = null;
        mb.GreedyArena = null;
    }

    /// <summary>
    /// Returns <paramref name="array"/> if it has at least <paramref name="size"/> elements, otherwise a new array
    /// (equivalent of a fresh BROTLI_ALLOC: the contents are never read before being written).
    /// </summary>
    private static T[] Reserve<T>(ref T[]? array, nuint size)
    {
        T[]? a = array;
        if (a is null || (nuint)a.Length < size)
        {
            array = a = new T[size];
        }
        return a;
    }

    public static void InitDistanceParams(ref DistanceParams dist, uint npostfix, uint ndirect, bool largeWindow)
    {
        uint alphabetSizeMax;
        uint alphabetSizeLimit;
        uint maxDistance;

        dist.DistancePostfixBits = npostfix;
        dist.NumDirectDistanceCodes = ndirect;

        alphabetSizeMax = BrotliConstants.DistanceAlphabetSize(npostfix, ndirect, BrotliConstants.MaxDistanceBits);
        alphabetSizeLimit = alphabetSizeMax;
        maxDistance = ndirect + (1U << (int)(BrotliConstants.MaxDistanceBits + npostfix + 2)) - (1U << (int)(npostfix + 2));

        if (largeWindow)
        {
            (uint limitMaxAlphabetSize, uint limitMaxDistance) = BrotliConstants.CalculateDistanceCodeLimit(
                BrotliConstants.MaxAllowedDistance, npostfix, ndirect);
            alphabetSizeMax = BrotliConstants.DistanceAlphabetSize(npostfix, ndirect, BrotliConstants.LargeMaxDistanceBits);
            alphabetSizeLimit = limitMaxAlphabetSize;
            maxDistance = limitMaxDistance;
        }

        dist.AlphabetSizeMax = alphabetSizeMax;
        dist.AlphabetSizeLimit = alphabetSizeLimit;
        dist.MaxDistance = maxDistance;
    }

    private static void RecomputeDistancePrefixes(Span<Command> cmds, nuint numCommands,
        in DistanceParams origParams, in DistanceParams newParams)
    {
        if (origParams.DistancePostfixBits == newParams.DistancePostfixBits &&
            origParams.NumDirectDistanceCodes == newParams.NumDirectDistanceCodes)
        {
            return;
        }

        cmds = cmds.Slice(0, (int)numCommands);
        for (int i = 0; i < cmds.Length; ++i)
        {
            ref Command cmd = ref cmds[i];
            if (cmd.CopyLength != 0 && cmd.CmdPrefix >= 128)
            {
                Command.PrefixEncodeCopyDistance(cmd.RestoreDistanceCode(origParams),
                                                 newParams.NumDirectDistanceCodes,
                                                 newParams.DistancePostfixBits,
                                                 out cmd.DistPrefix,
                                                 out cmd.DistExtra);
            }
        }
    }

    private static bool ComputeDistanceCost(ReadOnlySpan<Command> cmds, nuint numCommands,
        in DistanceParams origParams, in DistanceParams newParams, out double cost, ref HistogramDistance tmp)
    {
        bool equalParams = false;
        ushort distPrefix;
        double extraBits = 0.0;
        Histogram.HistogramClear(ref tmp);

        if (origParams.DistancePostfixBits == newParams.DistancePostfixBits &&
            origParams.NumDirectDistanceCodes == newParams.NumDirectDistanceCodes)
        {
            equalParams = true;
        }

        cmds = cmds.Slice(0, (int)numCommands);
        for (int i = 0; i < cmds.Length; i++)
        {
            ref readonly Command cmd = ref cmds[i];
            if (cmd.CopyLength != 0 && cmd.CmdPrefix >= 128)
            {
                if (equalParams)
                {
                    distPrefix = cmd.DistPrefix;
                }
                else
                {
                    uint distance = cmd.RestoreDistanceCode(origParams);
                    if (distance > newParams.MaxDistance)
                    {
                        cost = 0;
                        return false;
                    }
                    Command.PrefixEncodeCopyDistance(distance,
                                                     newParams.NumDirectDistanceCodes,
                                                     newParams.DistancePostfixBits,
                                                     out distPrefix,
                                                     out _);
                }
                Histogram.HistogramAdd(ref tmp, (nuint)(distPrefix & 0x3FF));
                extraBits += distPrefix >> 10;
            }
        }

        cost = BitCost.PopulationCost(ref tmp) + extraBits;
        return true;
    }

    /// <summary>
    /// Uses the slow shortest-path block splitter and does context clustering.
    /// The distance parameters are dynamically selected based on the commands
    /// which get recomputed under the new distance parameters. The new distance
    /// parameters are stored into <paramref name="parameters"/>.
    /// </summary>
    public static void BuildMetaBlock(ReadOnlySpan<byte> ringbuffer, nuint pos, nuint mask,
        ref EncoderParams parameters, byte prevByte, byte prevByte2, Span<Command> cmds,
        nuint numCommands, BrotliContextType literalContextMode, MetaBlockSplit mb)
    {
        // Histogram ids need to fit in one byte.
        const nuint MaxNumberOfHistograms = 256;
        HistogramDistance[] distanceHistograms;
        HistogramLiteral[] literalHistograms;
        BrotliContextType[]? literalContextModes = null;
        nuint literalHistogramsSize;
        nuint distanceHistogramsSize;
        nuint i;
        nuint literalContextMultiplier = 1;
        uint npostfix;
        uint ndirectMsb = 0;
        bool checkOrig = true;
        double bestDistCost = 1e99;
        DistanceParams origParams = parameters.Dist;
        DistanceParams newParams = parameters.Dist;
        HistogramDistance tmp = default;

        for (npostfix = 0; npostfix <= BrotliConstants.MaxNPostfix; npostfix++)
        {
            for (; ndirectMsb < 16; ndirectMsb++)
            {
                uint ndirect = ndirectMsb << (int)npostfix;
                bool skip;
                double distCost;
                InitDistanceParams(ref newParams, npostfix, ndirect, parameters.LargeWindow);
                if (npostfix == origParams.DistancePostfixBits &&
                    ndirect == origParams.NumDirectDistanceCodes)
                {
                    checkOrig = false;
                }
                skip = !ComputeDistanceCost(cmds, numCommands, origParams, newParams, out distCost, ref tmp);
                if (skip || (distCost > bestDistCost))
                {
                    break;
                }
                bestDistCost = distCost;
                parameters.Dist = newParams;
            }
            if (ndirectMsb > 0)
            {
                ndirectMsb--;
            }
            ndirectMsb /= 2;
        }
        if (checkOrig)
        {
            ComputeDistanceCost(cmds, numCommands, origParams, origParams, out double distCost, ref tmp);
            if (distCost < bestDistCost)
            {
                // NB: currently unused; uncomment when more param tuning is added.
                // best_dist_cost = dist_cost;
                parameters.Dist = origParams;
            }
        }
        RecomputeDistancePrefixes(cmds, numCommands, origParams, parameters.Dist);

        BlockSplitter.SplitBlock(cmds, numCommands,
                                 ringbuffer, pos, mask, parameters,
                                 mb.LiteralSplit,
                                 mb.CommandSplit,
                                 mb.DistanceSplit);

        if (!parameters.DisableLiteralContextModeling)
        {
            literalContextMultiplier = 1 << BrotliConstants.LiteralContextBits;
            literalContextModes = new BrotliContextType[mb.LiteralSplit.NumTypes];
            for (i = 0; i < mb.LiteralSplit.NumTypes; ++i)
            {
                literalContextModes[i] = literalContextMode;
            }
        }

        literalHistogramsSize = mb.LiteralSplit.NumTypes * literalContextMultiplier;
        literalHistograms = Reserve(ref mb.TmpLiteralHistograms, literalHistogramsSize);
        Histogram.ClearHistograms<HistogramLiteral>(literalHistograms, literalHistogramsSize);

        distanceHistogramsSize = mb.DistanceSplit.NumTypes << BrotliConstants.DistanceContextBits;
        distanceHistograms = Reserve(ref mb.TmpDistanceHistograms, distanceHistogramsSize);
        Histogram.ClearHistograms<HistogramDistance>(distanceHistograms, distanceHistogramsSize);

        mb.CommandHistogramsSize = mb.CommandSplit.NumTypes;
        HistogramCommand[] commandHistograms = Reserve(ref mb.CommandHistograms, mb.CommandHistogramsSize);
        Histogram.ClearHistograms<HistogramCommand>(commandHistograms, mb.CommandHistogramsSize);

        Histogram.BuildHistogramsWithContext(cmds, numCommands,
            mb.LiteralSplit, mb.CommandSplit, mb.DistanceSplit,
            ringbuffer, pos, mask, prevByte, prevByte2, literalContextModes,
            literalHistograms, commandHistograms, distanceHistograms);

        mb.LiteralContextMapSize = mb.LiteralSplit.NumTypes << BrotliConstants.LiteralContextBits;
        uint[] literalContextMap = Reserve(ref mb.LiteralContextMap, mb.LiteralContextMapSize);

        mb.LiteralHistogramsSize = mb.LiteralContextMapSize;
        HistogramLiteral[] clusteredLiteralHistograms = Reserve(ref mb.LiteralHistograms, mb.LiteralHistogramsSize);

        Cluster.ClusterHistograms<HistogramLiteral>(literalHistograms, literalHistogramsSize,
            MaxNumberOfHistograms, clusteredLiteralHistograms,
            ref mb.LiteralHistogramsSize, literalContextMap);

        if (parameters.DisableLiteralContextModeling)
        {
            // Distribute assignment to all contexts.
            for (i = mb.LiteralSplit.NumTypes; i != 0;)
            {
                nuint j = 0;
                i--;
                for (; j < (1 << BrotliConstants.LiteralContextBits); j++)
                {
                    literalContextMap[(i << BrotliConstants.LiteralContextBits) + j] =
                        literalContextMap[i];
                }
            }
        }

        mb.DistanceContextMapSize = mb.DistanceSplit.NumTypes << BrotliConstants.DistanceContextBits;
        uint[] distanceContextMap = Reserve(ref mb.DistanceContextMap, mb.DistanceContextMapSize);

        mb.DistanceHistogramsSize = mb.DistanceContextMapSize;
        HistogramDistance[] clusteredDistanceHistograms = Reserve(ref mb.DistanceHistograms, mb.DistanceHistogramsSize);

        Cluster.ClusterHistograms<HistogramDistance>(distanceHistograms,
                                                     mb.DistanceContextMapSize,
                                                     MaxNumberOfHistograms,
                                                     clusteredDistanceHistograms,
                                                     ref mb.DistanceHistogramsSize,
                                                     distanceContextMap);
    }

    /// <summary>
    /// Greedy block splitter for one block category (literal, command or distance)
    /// (FN(BlockSplitter) of metablock_inc.h).
    /// </summary>
    internal sealed class BlockSplitter<THistogram>
        where THistogram : struct, IHistogram<THistogram>
    {
        // Alphabet size of particular block category.
        private nuint _alphabetSize;
        // We collect at least this many symbols for each block.
        private nuint _minBlockSize;
        // We merge histograms A and B if
        //   entropy(A+B) < entropy(A) + entropy(B) + split_threshold_,
        // where A is the current histogram and B is the histogram of the last or the
        // second last block type.
        private double _splitThreshold;

        private nuint _numBlocks;
        private BlockSplit _split = s_noSplit;
        private byte[] _types = [];  // _split.Types
        private uint[] _lengths = [];  // _split.Lengths
        private THistogram[] _histograms = [];  // not owned
        // Equivalent of *histograms_size_ (the caller copies it back to the meta-block split).
        public nuint HistogramsSize;

        // Temporary storage for BlockSplitterFinishBlock (allocated on first use: the first block does not need it).
        private THistogram[]? _combinedHisto;

        // The number of symbols that we want to collect before deciding on whether
        // or not to merge the block with a previous one or emit a new block.
        private nuint _targetBlockSize;
        // The number of symbols in the current histogram.
        private nuint _blockSize;
        // Offset of the current histogram.
        private nuint _currHistogramIx;
        // Offset of the histograms of the previous two block types.
        private nuint _lastHistogramIx0;
        private nuint _lastHistogramIx1;
        // Entropy of the previous two block types.
        private double _lastEntropy0;
        private double _lastEntropy1;
        // The number of times we merged the current block with the last one.
        private nuint _mergeLastCount;

        /// <summary>Port of InitBlockSplitter (the splitter objects are reused).</summary>
        public void Init(nuint alphabetSize, nuint minBlockSize, double splitThreshold, nuint numSymbols,
            BlockSplit split, ref THistogram[]? histograms)
        {
            nuint maxNumBlocks = numSymbols / minBlockSize + 1;
            // We have to allocate one more histogram than the maximum number of block
            // types for the current histogram when the meta-block is too big.
            nuint maxNumTypes = Math.Min(maxNumBlocks, BrotliConstants.MaxNumberOfBlockTypes + 1);
            _alphabetSize = alphabetSize;
            _minBlockSize = minBlockSize;
            _splitThreshold = splitThreshold;
            _numBlocks = 0;
            _split = split;
            _targetBlockSize = minBlockSize;
            _blockSize = 0;
            _currHistogramIx = 0;
            _mergeLastCount = 0;
            _types = BlockSplitter.EnsureCapacity(ref split.Types, maxNumBlocks);
            _lengths = BlockSplitter.EnsureCapacity(ref split.Lengths, maxNumBlocks);
            _split.NumBlocks = maxNumBlocks;
            HistogramsSize = maxNumTypes;
            _histograms = Reserve(ref histograms, HistogramsSize);
            // Clear only current histogram.
            Histogram.HistogramClear(ref _histograms[0]);
            _lastHistogramIx0 = _lastHistogramIx1 = 0;
        }

        /// <summary>
        /// Does either of three things:
        ///   (1) emits the current block with a new block type;
        ///   (2) emits the current block with the type of the second last block;
        ///   (3) merges the current block with the last block.
        /// </summary>
        public void FinishBlock(bool isFinal)
        {
            BlockSplit split = _split;
            THistogram[] histograms = _histograms;
            byte[] types = _types;
            uint[] lengths = _lengths;
            _blockSize = Math.Max(_blockSize, _minBlockSize);
            if (_numBlocks == 0)
            {
                // Create first block.
                lengths[0] = (uint)_blockSize;
                types[0] = 0;
                _lastEntropy0 = BitCost.BitsEntropy(histograms[0].Data, _alphabetSize);
                _lastEntropy1 = _lastEntropy0;
                ++_numBlocks;
                ++split.NumTypes;
                ++_currHistogramIx;
                if (_currHistogramIx < HistogramsSize)
                {
                    Histogram.HistogramClear(ref histograms[_currHistogramIx]);
                }
                _blockSize = 0;
            }
            else if (_blockSize > 0)
            {
                double entropy = BitCost.BitsEntropy(histograms[_currHistogramIx].Data, _alphabetSize);
                THistogram[] combinedHisto = _combinedHisto ??= new THistogram[2];
                ref THistogram combined0 = ref combinedHisto[0];
                ref THistogram combined1 = ref combinedHisto[1];
                combined0 = histograms[_currHistogramIx];
                Histogram.HistogramAddHistogram(ref combined0, ref histograms[_lastHistogramIx0]);
                double combinedEntropy0 = BitCost.BitsEntropy(combined0.Data, _alphabetSize);
                double diff0 = combinedEntropy0 - entropy - _lastEntropy0;
                combined1 = histograms[_currHistogramIx];
                Histogram.HistogramAddHistogram(ref combined1, ref histograms[_lastHistogramIx1]);
                double combinedEntropy1 = BitCost.BitsEntropy(combined1.Data, _alphabetSize);
                double diff1 = combinedEntropy1 - entropy - _lastEntropy1;

                if (split.NumTypes < BrotliConstants.MaxNumberOfBlockTypes &&
                    diff0 > _splitThreshold &&
                    diff1 > _splitThreshold)
                {
                    // Create new block.
                    lengths[_numBlocks] = (uint)_blockSize;
                    types[_numBlocks] = (byte)split.NumTypes;
                    _lastHistogramIx1 = _lastHistogramIx0;
                    _lastHistogramIx0 = (byte)split.NumTypes;
                    _lastEntropy1 = _lastEntropy0;
                    _lastEntropy0 = entropy;
                    ++_numBlocks;
                    ++split.NumTypes;
                    ++_currHistogramIx;
                    if (_currHistogramIx < HistogramsSize)
                    {
                        Histogram.HistogramClear(ref histograms[_currHistogramIx]);
                    }
                    _blockSize = 0;
                    _mergeLastCount = 0;
                    _targetBlockSize = _minBlockSize;
                }
                else if (diff1 < diff0 - 20.0)
                {
                    // Combine this block with second last block.
                    lengths[_numBlocks] = (uint)_blockSize;
                    types[_numBlocks] = types[_numBlocks - 2];
                    (_lastHistogramIx0, _lastHistogramIx1) = (_lastHistogramIx1, _lastHistogramIx0);
                    histograms[_lastHistogramIx0] = combined1;
                    _lastEntropy1 = _lastEntropy0;
                    _lastEntropy0 = combinedEntropy1;
                    ++_numBlocks;
                    _blockSize = 0;
                    Histogram.HistogramClear(ref histograms[_currHistogramIx]);
                    _mergeLastCount = 0;
                    _targetBlockSize = _minBlockSize;
                }
                else
                {
                    // Combine this block with last block.
                    lengths[_numBlocks - 1] += (uint)_blockSize;
                    histograms[_lastHistogramIx0] = combined0;
                    _lastEntropy0 = combinedEntropy0;
                    if (split.NumTypes == 1)
                    {
                        _lastEntropy1 = _lastEntropy0;
                    }
                    _blockSize = 0;
                    Histogram.HistogramClear(ref histograms[_currHistogramIx]);
                    if (++_mergeLastCount > 1)
                    {
                        _targetBlockSize += _minBlockSize;
                    }
                }
            }
            if (isFinal)
            {
                HistogramsSize = split.NumTypes;
                split.NumBlocks = _numBlocks;
            }
        }

        /// <summary>
        /// Adds the next symbol to the current histogram. When the current histogram
        /// reaches the target size, decides on merging the block.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSymbol(nuint symbol)
        {
            Histogram.HistogramAdd(ref _histograms[(int)_currHistogramIx], symbol);
            ++_blockSize;
            if (_blockSize == _targetBlockSize)
            {
                FinishBlock(isFinal: false);
            }
        }

        /// <summary>
        /// Calls <see cref="AddSymbol"/> for the <paramref name="count"/> bytes ringbuffer[(pos + i) &amp; mask].
        /// The splitter state is kept in locals between the (rare) FinishBlock calls, and the total count of the
        /// current histogram is updated in bulk (before FinishBlock, which reads it).
        /// </summary>
        public void AddLiterals(ReadOnlySpan<byte> ringbuffer, nuint pos, nuint mask, nuint count)
        {
            THistogram[] histograms = _histograms;
            nuint blockSize = _blockSize;
            nuint targetBlockSize = _targetBlockSize;
            ref THistogram current = ref histograms[(int)_currHistogramIx];
            Span<uint> currentData = current.Data;
            nuint addedCount = 0;
            for (; count != 0; --count)
            {
                ++currentData[ringbuffer[(int)(pos & mask)]];
                ++addedCount;
                ++pos;
                if (++blockSize == targetBlockSize)
                {
                    current.TotalCount += addedCount;
                    addedCount = 0;
                    _blockSize = blockSize;
                    FinishBlock(isFinal: false);
                    blockSize = _blockSize;
                    targetBlockSize = _targetBlockSize;
                    current = ref histograms[(int)_currHistogramIx];
                    currentData = current.Data;
                }
            }
            current.TotalCount += addedCount;
            _blockSize = blockSize;
        }
    }

    /// <summary>
    /// Greedy block splitter for one block category (literal, command or distance).
    /// Gathers histograms for all context buckets.
    /// </summary>
    internal sealed class ContextBlockSplitter
    {
        // Alphabet size of particular block category.
        private nuint _alphabetSize;
        private nuint _numContexts;
        private nuint _maxBlockTypes;
        // We collect at least this many symbols for each block.
        private nuint _minBlockSize;
        // We merge histograms A and B if
        //   entropy(A+B) < entropy(A) + entropy(B) + split_threshold_,
        // where A is the current histogram and B is the histogram of the last or the
        // second last block type.
        private double _splitThreshold;

        private nuint _numBlocks;
        private BlockSplit _split = s_noSplit;
        private byte[] _types = [];  // _split.Types
        private uint[] _lengths = [];  // _split.Lengths
        private HistogramLiteral[] _histograms = [];  // not owned
        // Equivalent of *histograms_size_ (the caller copies it back to the meta-block split).
        public nuint HistogramsSize;

        // The number of symbols that we want to collect before deciding on whether
        // or not to merge the block with a previous one or emit a new block.
        private nuint _targetBlockSize;
        // The number of symbols in the current histogram.
        private nuint _blockSize;
        // Offset of the current histogram.
        private nuint _currHistogramIx;
        // Offset of the histograms of the previous two block types.
        private nuint _lastHistogramIx0;
        private nuint _lastHistogramIx1;
        // Entropy of the previous two block types.
        private readonly double[] _lastEntropy = new double[2 * MaxStaticContexts];
        // The number of times we merged the current block with the last one.
        private nuint _mergeLastCount;

        // Temporary storage of ContextBlockSplitterFinishBlock (allocated on each call in C; here on first use, as
        // the first block does not need it).
        private HistogramLiteral[]? _combinedHisto;
        private readonly double[] _entropy = new double[MaxStaticContexts];
        private readonly double[] _combinedEntropy = new double[2 * MaxStaticContexts];

        /// <summary>Port of InitContextBlockSplitter (the splitter objects are reused).</summary>
        public void Init(nuint alphabetSize, nuint numContexts, nuint minBlockSize, double splitThreshold,
            nuint numSymbols, BlockSplit split, ref HistogramLiteral[]? histograms)
        {
            nuint maxNumBlocks = numSymbols / minBlockSize + 1;
            nuint maxNumTypes;
            Debug.Assert(numContexts <= MaxStaticContexts);

            _alphabetSize = alphabetSize;
            _numContexts = numContexts;
            _maxBlockTypes = BrotliConstants.MaxNumberOfBlockTypes / numContexts;
            _minBlockSize = minBlockSize;
            _splitThreshold = splitThreshold;
            _numBlocks = 0;
            _split = split;
            _targetBlockSize = minBlockSize;
            _blockSize = 0;
            _currHistogramIx = 0;
            _mergeLastCount = 0;

            // We have to allocate one more histogram than the maximum number of block
            // types for the current histogram when the meta-block is too big.
            maxNumTypes = Math.Min(maxNumBlocks, _maxBlockTypes + 1);
            _types = BlockSplitter.EnsureCapacity(ref split.Types, maxNumBlocks);
            _lengths = BlockSplitter.EnsureCapacity(ref split.Lengths, maxNumBlocks);
            split.NumBlocks = maxNumBlocks;
            HistogramsSize = maxNumTypes * numContexts;
            _histograms = Reserve(ref histograms, HistogramsSize);
            // Clear only current histogram.
            Histogram.ClearHistograms<HistogramLiteral>(_histograms, numContexts);
            _lastHistogramIx0 = _lastHistogramIx1 = 0;
        }

        /// <summary>
        /// Does either of three things:
        ///   (1) emits the current block with a new block type;
        ///   (2) emits the current block with the type of the second last block;
        ///   (3) merges the current block with the last block.
        /// </summary>
        public void FinishBlock(bool isFinal)
        {
            BlockSplit split = _split;
            nuint numContexts = _numContexts;
            double[] lastEntropy = _lastEntropy;
            HistogramLiteral[] histograms = _histograms;
            byte[] types = _types;
            uint[] lengths = _lengths;

            if (_blockSize < _minBlockSize)
            {
                _blockSize = _minBlockSize;
            }
            if (_numBlocks == 0)
            {
                nuint i;
                // Create first block.
                lengths[0] = (uint)_blockSize;
                types[0] = 0;

                for (i = 0; i < numContexts; ++i)
                {
                    lastEntropy[i] = BitCost.BitsEntropy(histograms[i].Data, _alphabetSize);
                    lastEntropy[numContexts + i] = lastEntropy[i];
                }
                ++_numBlocks;
                ++split.NumTypes;
                _currHistogramIx += numContexts;
                if (_currHistogramIx < HistogramsSize)
                {
                    Histogram.ClearHistograms<HistogramLiteral>(histograms.AsSpan((int)_currHistogramIx), _numContexts);
                }
                _blockSize = 0;
            }
            else if (_blockSize > 0)
            {
                // Try merging the set of histograms for the current block type with the
                // respective set of histograms for the last and second last block types.
                // Decide over the split based on the total reduction of entropy across
                // all contexts.
                double[] entropy = _entropy;
                HistogramLiteral[] combinedHisto = _combinedHisto ??= new HistogramLiteral[2 * MaxStaticContexts];
                double[] combinedEntropy = _combinedEntropy;
                double diff0 = 0.0;
                double diff1 = 0.0;
                nuint i;
                for (i = 0; i < numContexts; ++i)
                {
                    nuint currHistoIx = _currHistogramIx + i;
                    entropy[i] = BitCost.BitsEntropy(histograms[currHistoIx].Data, _alphabetSize);
                    for (nuint j = 0; j < 2; ++j)
                    {
                        nuint jx = j * numContexts + i;
                        nuint lastHistogramIx = (j == 0 ? _lastHistogramIx0 : _lastHistogramIx1) + i;
                        ref HistogramLiteral combined = ref combinedHisto[jx];
                        combined = histograms[currHistoIx];
                        Histogram.HistogramAddHistogram(ref combined, ref histograms[lastHistogramIx]);
                        combinedEntropy[jx] = BitCost.BitsEntropy(combined.Data, _alphabetSize);
                        double d = combinedEntropy[jx] - entropy[i] - lastEntropy[jx];
                        if (j == 0)
                        {
                            diff0 += d;
                        }
                        else
                        {
                            diff1 += d;
                        }
                    }
                }

                if (split.NumTypes < _maxBlockTypes &&
                    diff0 > _splitThreshold &&
                    diff1 > _splitThreshold)
                {
                    // Create new block.
                    lengths[_numBlocks] = (uint)_blockSize;
                    types[_numBlocks] = (byte)split.NumTypes;
                    _lastHistogramIx1 = _lastHistogramIx0;
                    _lastHistogramIx0 = split.NumTypes * numContexts;
                    for (i = 0; i < numContexts; ++i)
                    {
                        lastEntropy[numContexts + i] = lastEntropy[i];
                        lastEntropy[i] = entropy[i];
                    }
                    ++_numBlocks;
                    ++split.NumTypes;
                    _currHistogramIx += numContexts;
                    if (_currHistogramIx < HistogramsSize)
                    {
                        Histogram.ClearHistograms<HistogramLiteral>(histograms.AsSpan((int)_currHistogramIx), _numContexts);
                    }
                    _blockSize = 0;
                    _mergeLastCount = 0;
                    _targetBlockSize = _minBlockSize;
                }
                else if (diff1 < diff0 - 20.0)
                {
                    // Combine this block with second last block.
                    lengths[_numBlocks] = (uint)_blockSize;
                    types[_numBlocks] = types[_numBlocks - 2];
                    (_lastHistogramIx0, _lastHistogramIx1) = (_lastHistogramIx1, _lastHistogramIx0);
                    for (i = 0; i < numContexts; ++i)
                    {
                        histograms[_lastHistogramIx0 + i] = combinedHisto[numContexts + i];
                        lastEntropy[numContexts + i] = lastEntropy[i];
                        lastEntropy[i] = combinedEntropy[numContexts + i];
                        Histogram.HistogramClear(ref histograms[_currHistogramIx + i]);
                    }
                    ++_numBlocks;
                    _blockSize = 0;
                    _mergeLastCount = 0;
                    _targetBlockSize = _minBlockSize;
                }
                else
                {
                    // Combine this block with last block.
                    lengths[_numBlocks - 1] += (uint)_blockSize;
                    for (i = 0; i < numContexts; ++i)
                    {
                        histograms[_lastHistogramIx0 + i] = combinedHisto[i];
                        lastEntropy[i] = combinedEntropy[i];
                        if (split.NumTypes == 1)
                        {
                            lastEntropy[numContexts + i] = lastEntropy[i];
                        }
                        Histogram.HistogramClear(ref histograms[_currHistogramIx + i]);
                    }
                    _blockSize = 0;
                    if (++_mergeLastCount > 1)
                    {
                        _targetBlockSize += _minBlockSize;
                    }
                }
            }
            if (isFinal)
            {
                HistogramsSize = split.NumTypes * numContexts;
                split.NumBlocks = _numBlocks;
            }
        }

        /// <summary>
        /// Adds the <paramref name="count"/> literals ringbuffer[(pos + i) &amp; mask] with their static contexts
        /// (the loop of BrotliBuildMetaBlockGreedyInternal calling ContextBlockSplitterAddSymbol, which adds the
        /// symbol to the histogram of the current block type and context and decides on merging the block when
        /// the current block reaches the target size), updating the two previous bytes.
        /// The splitter state is kept in locals between the (rare) FinishBlock calls.
        /// </summary>
        public void AddLiterals(ReadOnlySpan<byte> ringbuffer, nuint pos, nuint mask, nuint count,
            ref byte prevByte, ref byte prevByte2, ReadOnlySpan<byte> literalContextLut, ReadOnlySpan<uint> staticContextMap)
        {
            HistogramLiteral[] histograms = _histograms;
            nuint blockSize = _blockSize;
            nuint targetBlockSize = _targetBlockSize;
            nuint currHistogramIx = _currHistogramIx;
            byte p1 = prevByte;
            byte p2 = prevByte2;
            for (; count != 0; --count)
            {
                byte literal = ringbuffer[(int)(pos & mask)];
                int context = BrotliContext.GetContext(p1, p2, literalContextLut);
                ref HistogramLiteral histogram = ref histograms[(int)(currHistogramIx + staticContextMap[context])];
                ++histogram.Data[literal];
                ++histogram.TotalCount;
                if (++blockSize == targetBlockSize)
                {
                    _blockSize = blockSize;
                    FinishBlock(isFinal: false);
                    blockSize = _blockSize;
                    targetBlockSize = _targetBlockSize;
                    currHistogramIx = _currHistogramIx;
                }
                p2 = p1;
                p1 = literal;
                ++pos;
            }
            _blockSize = blockSize;
            prevByte = p1;
            prevByte2 = p2;
        }
    }

    // Placeholder for the split of a splitter that has not been initialized yet (never written to).
    private static readonly BlockSplit s_noSplit = new();

    /// <summary>The greedy block splitters of BrotliBuildMetaBlockGreedy (allocated per call in C).</summary>
    internal sealed class GreedyArena
    {
        public readonly BlockSplitter<HistogramLiteral> Literal = new();
        public readonly BlockSplitter<HistogramCommand> Command = new();
        public readonly BlockSplitter<HistogramDistance> Distance = new();
        private ContextBlockSplitter? _context;

        public ContextBlockSplitter Context => _context ??= new ContextBlockSplitter();
    }

    private static void MapStaticContexts(nuint numContexts, ReadOnlySpan<uint> staticContextMap, MetaBlockSplit mb)
    {
        mb.LiteralContextMapSize = mb.LiteralSplit.NumTypes << BrotliConstants.LiteralContextBits;
        uint[] literalContextMap = Reserve(ref mb.LiteralContextMap, mb.LiteralContextMapSize);

        for (nuint i = 0; i < mb.LiteralSplit.NumTypes; ++i)
        {
            uint offset = (uint)(i * numContexts);
            for (nuint j = 0; j < (1u << BrotliConstants.LiteralContextBits); ++j)
            {
                literalContextMap[(i << BrotliConstants.LiteralContextBits) + j] =
                    offset + staticContextMap[(int)j];
            }
        }
    }

    /// <summary>
    /// Uses a fast greedy block splitter that tries to merge current block with the
    /// last or the second last block and uses a static context clustering which
    /// is the same for all block types.
    /// </summary>
    /// <remarks>
    /// <paramref name="staticContextMap"/>: The static context map; may be empty when <paramref name="numContexts"/> is 1.<br/>
    /// </remarks>
    public static void BuildMetaBlockGreedy(ReadOnlySpan<byte> ringbuffer, nuint pos, nuint mask,
        byte prevByte, byte prevByte2, ReadOnlySpan<byte> literalContextLut, nuint numContexts,
        ReadOnlySpan<uint> staticContextMap, ReadOnlySpan<Command> commands, nuint nCommands,
        MetaBlockSplit mb)
    {
        nuint numLiterals = 0;
        commands = commands.Slice(0, (int)nCommands);
        for (int i = 0; i < commands.Length; ++i)
        {
            numLiterals += commands[i].InsertLen;
        }

        GreedyArena arena = mb.GreedyArena ??= new GreedyArena();
        if (numContexts == 1)
        {
            BlockSplitter<HistogramLiteral> litBlocks = arena.Literal;
            litBlocks.Init(256, 512, 400.0, numLiterals, mb.LiteralSplit, ref mb.LiteralHistograms);
            BuildMetaBlockGreedyInternal(ringbuffer, pos, mask, prevByte, prevByte2, litBlocks, null,
                literalContextLut, staticContextMap, commands, mb, arena);
            mb.LiteralHistogramsSize = litBlocks.HistogramsSize;
        }
        else
        {
            ContextBlockSplitter litBlocks = arena.Context;
            litBlocks.Init(256, numContexts, 512, 400.0, numLiterals, mb.LiteralSplit, ref mb.LiteralHistograms);
            BuildMetaBlockGreedyInternal(ringbuffer, pos, mask, prevByte, prevByte2, null, litBlocks,
                literalContextLut, staticContextMap, commands, mb, arena);
            mb.LiteralHistogramsSize = litBlocks.HistogramsSize;
            MapStaticContexts(numContexts, staticContextMap, mb);
        }
    }

    /// <summary>
    /// Port of BrotliBuildMetaBlockGreedyInternal; exactly one of <paramref name="plainLitBlocks"/> (num_contexts == 1)
    /// and <paramref name="ctxLitBlocks"/> is non-null.
    /// </summary>
    private static void BuildMetaBlockGreedyInternal(ReadOnlySpan<byte> ringbuffer, nuint pos, nuint mask,
        byte prevByte, byte prevByte2, BlockSplitter<HistogramLiteral>? plainLitBlocks, ContextBlockSplitter? ctxLitBlocks,
        ReadOnlySpan<byte> literalContextLut, ReadOnlySpan<uint> staticContextMap, ReadOnlySpan<Command> commands,
        MetaBlockSplit mb, GreedyArena arena)
    {
        BlockSplitter<HistogramCommand> cmdBlocks = arena.Command;
        cmdBlocks.Init(BrotliConstants.NumCommandSymbols, 1024, 500.0, (nuint)commands.Length, mb.CommandSplit,
            ref mb.CommandHistograms);
        BlockSplitter<HistogramDistance> distBlocks = arena.Distance;
        distBlocks.Init(64, 512, 100.0, (nuint)commands.Length, mb.DistanceSplit, ref mb.DistanceHistograms);

        for (int i = 0; i < commands.Length; ++i)
        {
            ref readonly Command cmd = ref commands[i];
            cmdBlocks.AddSymbol(cmd.CmdPrefix);
            nuint insertLen = cmd.InsertLen;
            if (insertLen != 0)
            {
                if (plainLitBlocks is not null)
                {
                    plainLitBlocks.AddLiterals(ringbuffer, pos, mask, insertLen);
                }
                else
                {
                    Debug.Assert(ctxLitBlocks is not null);
                    ctxLitBlocks.AddLiterals(ringbuffer, pos, mask, insertLen, ref prevByte, ref prevByte2,
                        literalContextLut, staticContextMap);
                }
                pos += insertLen;
            }
            uint copyLen = cmd.CopyLength;
            pos += copyLen;
            if (copyLen != 0)
            {
                // The previous bytes are only used for the literal contexts (dead in the single context case).
                if (plainLitBlocks is null)
                {
                    prevByte2 = ringbuffer[(int)((pos - 2) & mask)];
                    prevByte = ringbuffer[(int)((pos - 1) & mask)];
                }
                if (cmd.CmdPrefix >= 128)
                {
                    distBlocks.AddSymbol((nuint)(cmd.DistPrefix & 0x3FF));
                }
            }
        }

        if (plainLitBlocks is not null)
        {
            plainLitBlocks.FinishBlock(isFinal: true);
        }
        else
        {
            Debug.Assert(ctxLitBlocks is not null);
            ctxLitBlocks.FinishBlock(isFinal: true);
        }
        cmdBlocks.FinishBlock(isFinal: true);
        mb.CommandHistogramsSize = cmdBlocks.HistogramsSize;
        distBlocks.FinishBlock(isFinal: true);
        mb.DistanceHistogramsSize = distBlocks.HistogramsSize;
    }

    public static void OptimizeHistograms(uint numDistanceCodes, MetaBlockSplit mb)
    {
        Span<byte> goodForRle = stackalloc byte[BrotliConstants.NumCommandSymbols];
        Span<HistogramLiteral> literalHistograms = mb.LiteralHistograms.AsSpan(0, (int)mb.LiteralHistogramsSize);
        for (int i = 0; i < literalHistograms.Length; ++i)
        {
            EntropyEncode.OptimizeHuffmanCountsForRle(256, literalHistograms[i].Data, goodForRle);
        }
        Span<HistogramCommand> commandHistograms = mb.CommandHistograms.AsSpan(0, (int)mb.CommandHistogramsSize);
        for (int i = 0; i < commandHistograms.Length; ++i)
        {
            EntropyEncode.OptimizeHuffmanCountsForRle(BrotliConstants.NumCommandSymbols, commandHistograms[i].Data, goodForRle);
        }
        Span<HistogramDistance> distanceHistograms = mb.DistanceHistograms.AsSpan(0, (int)mb.DistanceHistogramsSize);
        for (int i = 0; i < distanceHistograms.Length; ++i)
        {
            EntropyEncode.OptimizeHuffmanCountsForRle(numDistanceCodes, distanceHistograms[i].Data, goodForRle);
        }
    }
}
