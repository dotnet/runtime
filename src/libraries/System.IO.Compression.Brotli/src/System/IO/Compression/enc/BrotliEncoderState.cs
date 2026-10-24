// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/encode.c and c/enc/state.h.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace System.IO.Compression;

internal enum BrotliEncoderStreamState
{
    /// <summary>Default state.</summary>
    Processing = 0,
    /// <summary>
    /// Intermediate state; after next block is emitted, byte-padding should be
    /// performed before getting back to default state.
    /// </summary>
    FlushRequested = 1,
    /// <summary>Last metablock was produced; no more input is acceptable.</summary>
    Finished = 2,
    /// <summary>Flushing compressed block and writing meta-data block header.</summary>
    MetadataHead = 3,
    /// <summary>Writing metadata block body.</summary>
    MetadataBody = 4,
}

/// <summary>The Brotli encoder state machine (BrotliEncoderState).</summary>
internal sealed class BrotliEncoderState
{
    // BrotliEncoderFlintState values (the C field is an int8_t).
    private const sbyte FlintNeeds2Bytes = 2;
    private const sbyte FlintWaitingForProcessing = 0;
    private const sbyte FlintWaitingForFlushing = -1;
    private const sbyte FlintDone = -2;

    // BROTLI_PARAM_* values that have no member in BrotliEncoderParameter.
    private const int ParamLargeWindow = 6;
    private const int ParamNPostfix = 7;
    private const int ParamNDirect = 8;
    private const int ParamStreamOffset = 9;

    private const int DefaultQuality = 11;
    private const int DefaultWindow = 22;

    private EncoderParams _params;

    private ulong _inputPos;
    private readonly RingBuffer _ringBuffer = new RingBuffer();
    private nuint _cmdAllocSize;
    /// <summary>Rented from <see cref="ArrayPool{T}.Shared"/>.</summary>
    private Command[]? _commands;
    private nuint _numCommands;
    private nuint _numLiterals;
    private nuint _lastInsertLen;
    private ulong _lastFlushPos;
    private ulong _lastProcessedPos;
    private readonly int[] _distCache = new int[BrotliConstants.NumDistanceShortCodes];
    private readonly int[] _savedDistCache = new int[4];
    private ushort _lastBytes;
    private byte _lastBytesBits;
    /// <summary>
    /// "Flint" is a tiny uncompressed block emitted before the continuation
    /// block to unwire literal context from previous data. Despite being sbyte,
    /// field is actually BrotliEncoderFlintState enum.
    /// </summary>
    private sbyte _flint;
    private byte _prevByte;
    private byte _prevByte2;
    private nuint _storageSize;
    /// <summary>Rented from <see cref="ArrayPool{T}.Shared"/>.</summary>
    private byte[]? _storage;

    /// <summary>Created on first use (the fast modes don't use it).</summary>
    private Hasher? _hasher;

    /// <summary>Hash table for the fast (quality 0 and 1) modes; owned by the arena of the mode.</summary>
    private int[]? _smallTable;
    /// <summary>Allocated only when needed; rented from <see cref="ArrayPool{T}.Shared"/>.</summary>
    private int[]? _largeTable;
    private nuint _largeTableSize;

    private BrotliOnePassArena? _onePassArena;
    private BrotliTwoPassArena? _twoPassArena;

    /// <summary>Command and literal buffers for FAST_TWO_PASS_COMPRESSION_QUALITY; rented from <see cref="ArrayPool{T}.Shared"/>.</summary>
    private uint[]? _commandBuf;
    private byte[]? _literalBuf;

    private ulong _totalIn;
    /// <summary>The C <c>next_out_</c> pointer is <c>_nextOut + _nextOutOffset</c> (<c>null</c> == NULL).</summary>
    private byte[]? _nextOut;
    private int _nextOutOffset;
    private nuint _availableOut;
    private ulong _totalOut;
    /// <summary>Temporary buffer for padding flush bits or metadata block header / body.</summary>
    private readonly byte[] _tinyBuf = new byte[16];
    private uint _remainingMetadataBytes;
    private BrotliEncoderStreamState _streamState;

    private bool _isLastBlockEmitted;
    private bool _isInitialized;
    private bool _isDisposed;

    // Scratch objects reused between meta-blocks (C allocates them per call / on the stack); they are allocated on
    // first use.
    private MetaBlockSplit? _metaBlockSplit;
    private MetablockArena? _metablockArena;
    private uint[]? _contextArena;

    /// <summary>Port of BrotliEncoderCreateInstance (BrotliEncoderInitState).</summary>
    /// <remarks>The fields that BrotliEncoderInitState sets to 0 / NULL / false keep their default values.</remarks>
    public BrotliEncoderState()
    {
        InitParams(ref _params);
        _streamState = BrotliEncoderStreamState.Processing;

        // Initialize distance cache.
        _distCache[0] = 4;
        _distCache[1] = 11;
        _distCache[2] = 15;
        _distCache[3] = 16;
        // Save the state of the distance cache in case we need to restore it for
        // emitting an uncompressed block.
        _distCache.AsSpan(0, 4).CopyTo(_savedDistCache);
    }

    public bool IsDisposed => _isDisposed;

    /// <summary>Port of BrotliEncoderIsFinished.</summary>
    public bool IsFinished => _streamState == BrotliEncoderStreamState.Finished && !HasMoreOutput;

    /// <summary>Port of BrotliEncoderHasMoreOutput.</summary>
    public bool HasMoreOutput => _availableOut != 0;

    /// <summary>Releases the (pooled) buffers. The state is unusable afterwards.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        _nextOut = null;
        _availableOut = 0;

        byte[]? storage = _storage;
        _storage = null;
        _storageSize = 0;
        if (storage is not null)
        {
            ArrayPool<byte>.Shared.Return(storage);
        }

        Command[]? commands = _commands;
        _commands = null;
        _cmdAllocSize = 0;
        if (commands is not null)
        {
            ArrayPool<Command>.Shared.Return(commands);
        }

        _ringBuffer.Free();
        _hasher?.Destroy();

        int[]? largeTable = _largeTable;
        _largeTable = null;
        _largeTableSize = 0;
        if (largeTable is not null)
        {
            ArrayPool<int>.Shared.Return(largeTable);
        }

        _smallTable = null;
        _onePassArena = null;
        _twoPassArena = null;

        uint[]? commandBuf = _commandBuf;
        byte[]? literalBuf = _literalBuf;
        _commandBuf = null;
        _literalBuf = null;
        if (commandBuf is not null)
        {
            ArrayPool<uint>.Shared.Return(commandBuf);
        }
        if (literalBuf is not null)
        {
            ArrayPool<byte>.Shared.Return(literalBuf);
        }

        MetaBlockSplit? mb = _metaBlockSplit;
        _metaBlockSplit = null;
        if (mb is not null)
        {
            Metablock.DestroyMetaBlockSplit(mb);
        }
        _metablockArena = null;
        _contextArena = null;
    }

    private nuint InputBlockSize() => (nuint)1 << _params.Lgblock;

    private ulong UnprocessedInputSize() => _inputPos - _lastProcessedPos;

    private nuint RemainingInputBlockSize()
    {
        ulong delta = UnprocessedInputSize();
        nuint blockSize = InputBlockSize();
        if (delta >= blockSize)
        {
            return 0;
        }
        return blockSize - (nuint)delta;
    }

    /// <summary>Port of BrotliEncoderSetParameter.</summary>
    public bool SetParameter(BrotliEncoderParameter p, uint value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        // Changing parameters on the fly is not implemented yet.
        if (_isInitialized)
        {
            return false;
        }
        switch ((int)p)
        {
            case (int)BrotliEncoderParameter.Mode:
                _params.Mode = (EncoderMode)value;
                return true;

            case (int)BrotliEncoderParameter.Quality:
                _params.Quality = (int)value;
                return true;

            case (int)BrotliEncoderParameter.LGWin:
                _params.Lgwin = (int)value;
                return true;

            case (int)BrotliEncoderParameter.LGBlock:
                _params.Lgblock = (int)value;
                return true;

            case (int)BrotliEncoderParameter.LCModeling:
                if ((value != 0) && (value != 1))
                {
                    return false;
                }
                _params.DisableLiteralContextModeling = value != 0;
                return true;

            case (int)BrotliEncoderParameter.SizeHint:
                _params.SizeHint = value;
                return true;

            case ParamLargeWindow:
                // Large window brotli is not supported.
                return value == 0;

            case ParamNPostfix:
                _params.Dist.DistancePostfixBits = value;
                return true;

            case ParamNDirect:
                _params.Dist.NumDirectDistanceCodes = value;
                return true;

            case ParamStreamOffset:
                // Not supported; only the default (0) is accepted.
                return value == 0;

            default:
                return false;
        }
    }

    /// <summary>
    /// Wraps 64-bit input position to 32-bit ring-buffer position preserving
    /// "not-a-first-lap" feature.
    /// </summary>
    private static uint WrapPosition(ulong position)
    {
        uint result = (uint)position;
        ulong gb = position >> 30;
        if (gb > 2)
        {
            // Wrap every 2GiB; The first 3GB are continuous.
            result = (result & ((1u << 30) - 1)) | (((uint)((gb - 1) & 1) + 1) << 30);
        }
        return result;
    }

    private Span<byte> GetBrotliStorage(nuint size)
    {
        if (_storageSize < size)
        {
            byte[]? old = _storage;
            _storage = null;
            if (old is not null)
            {
                if (_nextOut == old)
                {
                    // Never keep a reference to an array returned to the pool.
                    _nextOut = null;
                    _nextOutOffset = 0;
                }
                ArrayPool<byte>.Shared.Return(old);
            }
            _storage = ArrayPool<byte>.Shared.Rent((int)size);
            _storageSize = size;
        }
        return _storage.AsSpan(0, (int)_storageSize);
    }

    private static nuint HashTableSize(nuint maxTableSize, nuint inputSize)
    {
        nuint htsize = 256;
        while (htsize < maxTableSize && htsize < inputSize)
        {
            htsize <<= 1;
        }
        return htsize;
    }

    private Span<int> GetHashTable(int quality, nuint inputSize, out nuint tableSize)
    {
        // Use smaller hash table when input.size() is smaller, since we
        // fill the table, incurring O(hash table size) overhead for
        // compression, and if the input is short, we won't need that
        // many hash table entries anyway.
        nuint maxTableSize = Quality.MaxHashTableSize(quality);
        nuint htsize = HashTableSize(maxTableSize, inputSize);
        int[] table;
        Debug.Assert(maxTableSize >= 256);
        if (quality == Quality.FastOnePassCompressionQuality)
        {
            // Only odd shifts are supported by fast-one-pass.
            if ((htsize & 0xAAAAA) == 0)
            {
                htsize <<= 1;
            }
        }

        int[] smallTable = _smallTable!;
        if (htsize <= (nuint)smallTable.Length)
        {
            table = smallTable;
        }
        else
        {
            if (htsize > _largeTableSize)
            {
                _largeTableSize = htsize;
                int[]? old = _largeTable;
                _largeTable = null;
                if (old is not null)
                {
                    ArrayPool<int>.Shared.Return(old);
                }
                _largeTable = ArrayPool<int>.Shared.Rent((int)htsize);
            }
            table = _largeTable!;
        }

        tableSize = htsize;
        Span<int> result = table.AsSpan(0, (int)htsize);
        result.Clear();
        return result;
    }

    private static void EncodeWindowBits(int lgwin, bool largeWindow, out ushort lastBytes, out byte lastBytesBits)
    {
        if (largeWindow)
        {
            lastBytes = (ushort)(((lgwin & 0x3F) << 8) | 0x11);
            lastBytesBits = 14;
        }
        else
        {
            if (lgwin == 16)
            {
                lastBytes = 0;
                lastBytesBits = 1;
            }
            else if (lgwin == 17)
            {
                lastBytes = 1;
                lastBytesBits = 7;
            }
            else if (lgwin > 17)
            {
                lastBytes = (ushort)(((lgwin - 17) << 1) | 0x01);
                lastBytesBits = 4;
            }
            else
            {
                lastBytes = (ushort)(((lgwin - 8) << 4) | 0x01);
                lastBytesBits = 7;
            }
        }
    }

    private static ReadOnlySpan<byte> DefaultCommandDepths =>
    [
        0, 4, 4, 5, 6, 6, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8,
        0, 0, 0, 4, 4, 4, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7,
        7, 7, 10, 10, 10, 10, 10, 10, 0, 4, 4, 5, 5, 5, 6, 6,
        7, 8, 8, 9, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
        5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        6, 6, 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 4, 4, 4, 4,
        4, 4, 4, 5, 5, 5, 5, 5, 5, 6, 6, 7, 7, 7, 8, 10,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 0, 0, 0, 0,
    ];

    private static ReadOnlySpan<ushort> DefaultCommandBits =>
    [
        0, 0, 8, 9, 3, 35, 7, 71,
        39, 103, 23, 47, 175, 111, 239, 31,
        0, 0, 0, 4, 12, 2, 10, 6,
        13, 29, 11, 43, 27, 59, 87, 55,
        15, 79, 319, 831, 191, 703, 447, 959,
        0, 14, 1, 25, 5, 21, 19, 51,
        119, 159, 95, 223, 479, 991, 63, 575,
        127, 639, 383, 895, 255, 767, 511, 1023,
        14, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        27, 59, 7, 39, 23, 55, 30, 1, 17, 9, 25, 5, 0, 8, 4, 12,
        2, 10, 6, 21, 13, 29, 3, 19, 11, 15, 47, 31, 95, 63, 127, 255,
        767, 2815, 1791, 3839, 511, 2559, 1535, 3583, 1023, 3071, 2047, 4095, 0, 0, 0, 0,
    ];

    private static ReadOnlySpan<byte> DefaultCommandCode =>
    [
        0xff, 0x77, 0xd5, 0xbf, 0xe7, 0xde, 0xea, 0x9e, 0x51, 0x5d, 0xde, 0xc6,
        0x70, 0x57, 0xbc, 0x58, 0x58, 0x58, 0xd8, 0xd8, 0x58, 0xd5, 0xcb, 0x8c,
        0xea, 0xe0, 0xc3, 0x87, 0x1f, 0x83, 0xc1, 0x60, 0x1c, 0x67, 0xb2, 0xaa,
        0x06, 0x83, 0xc1, 0x60, 0x30, 0x18, 0xcc, 0xa1, 0xce, 0x88, 0x54, 0x94,
        0x46, 0xe1, 0xb0, 0xd0, 0x4e, 0xb2, 0xf7, 0x04, 0x00,
    ];

    private const nuint DefaultCommandCodeNumBits = 448;

    /// <summary>Initializes the command and distance prefix codes for the first block.</summary>
    private static void InitCommandPrefixCodes(BrotliOnePassArena s)
    {
        DefaultCommandDepths.CopyTo(s.CmdDepth);
        DefaultCommandBits.CopyTo(s.CmdBits);

        // Initialize the pre-compressed form of the command and distance prefix codes.
        DefaultCommandCode.CopyTo(s.CmdCode);
        s.CmdCodeNumbits = DefaultCommandCodeNumBits;
    }

    private static double EstimateEntropy(ReadOnlySpan<uint> population, nuint size)
    {
        nuint total = 0;
        double result = 0;
        for (nuint i = 0; i < size; ++i)
        {
            uint p = population[(int)i];
            total += p;
            result += (double)p * FastLog.FastLog2(p);
        }
        result = (double)total * FastLog.FastLog2(total) - result;
        return result;
    }

    private static ReadOnlySpan<uint> StaticContextMapContinuation =>
    [
        1, 1, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ];

    private static ReadOnlySpan<uint> StaticContextMapSimpleUtf8 =>
    [
        0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ];

    private static ReadOnlySpan<uint> StaticContextMapComplexUtf8 =>
    [
        11, 11, 12, 12, // 0 special
        0, 0, 0, 0, // 4 lf
        1, 1, 9, 9, // 8 space
        2, 2, 2, 2, // !, first after space/lf and after something else.
        1, 1, 1, 1, // "
        8, 3, 3, 3, // %
        1, 1, 1, 1, // ({[
        2, 2, 2, 2, // }])
        8, 4, 4, 4, // :;
        8, 7, 4, 4, // .
        8, 0, 0, 0, // >
        3, 3, 3, 3, // [0..9]
        5, 5, 10, 5, // [A-Z]
        5, 5, 10, 5,
        6, 6, 6, 6, // [a-z]
        6, 6, 6, 6,
    ];

    /// <summary>
    /// Decide about the context map based on the ability of the prediction
    /// ability of the previous byte UTF8-prefix on the next byte. The
    /// prediction ability is calculated as Shannon entropy. Here we need
    /// Shannon entropy instead of 'BrotliBitsEntropy' since the prefix will be
    /// encoded with the remaining 6 bits of the following byte, and
    /// BrotliBitsEntropy will assume that symbol to be stored alone using Huffman
    /// coding.
    /// </summary>
    private static void ChooseContextMap(int quality, ReadOnlySpan<uint> bigramHisto, ref nuint numLiteralContexts,
        ref ReadOnlySpan<uint> literalContextMap)
    {
        Span<uint> monogramHisto = stackalloc uint[3];
        Span<uint> twoPrefixHisto = stackalloc uint[6];
        monogramHisto.Clear();
        twoPrefixHisto.Clear();
        nuint total;
        Span<double> entropy = stackalloc double[4];
        for (int i = 0; i < 9; ++i)
        {
            monogramHisto[i % 3] += bigramHisto[i];
            twoPrefixHisto[i % 6] += bigramHisto[i];
        }
        entropy[1] = EstimateEntropy(monogramHisto, 3);
        entropy[2] = EstimateEntropy(twoPrefixHisto, 3) + EstimateEntropy(twoPrefixHisto.Slice(3), 3);
        entropy[3] = 0;
        for (int i = 0; i < 3; ++i)
        {
            entropy[3] += EstimateEntropy(bigramHisto.Slice(3 * i), 3);
        }

        total = (nuint)monogramHisto[0] + monogramHisto[1] + monogramHisto[2];
        Debug.Assert(total != 0);
        entropy[0] = 1.0 / (double)total;
        entropy[1] *= entropy[0];
        entropy[2] *= entropy[0];
        entropy[3] *= entropy[0];

        if (quality < Quality.MinQualityForHqContextModeling)
        {
            // 3 context models is a bit slower, don't use it at lower qualities.
            entropy[3] = entropy[1] * 10;
        }
        // If expected savings by symbol are less than 0.2 bits, skip the
        // context modeling -- in exchange for faster decoding speed.
        if (entropy[1] - entropy[2] < 0.2 &&
            entropy[1] - entropy[3] < 0.2)
        {
            numLiteralContexts = 1;
        }
        else if (entropy[2] - entropy[3] < 0.02)
        {
            numLiteralContexts = 2;
            literalContextMap = StaticContextMapSimpleUtf8;
        }
        else
        {
            numLiteralContexts = 3;
            literalContextMap = StaticContextMapContinuation;
        }
    }

    /// <summary>
    /// Decide if we want to use a more complex static context map containing 13
    /// context values, based on the entropy reduction of histograms over the
    /// first 5 bits of literals.
    /// </summary>
    private static bool ShouldUseComplexStaticContextMap(ReadOnlySpan<byte> input, nuint startPos, nuint length, nuint mask,
        int quality, nuint sizeHint, ref nuint numLiteralContexts, ref ReadOnlySpan<uint> literalContextMap, Span<uint> arena)
    {
        _ = quality;
        // Try the more complex static context map only for long data.
        if (sizeHint < (1 << 20))
        {
            return false;
        }
        else
        {
            nuint endPos = startPos + length;
            // To make entropy calculations faster, we collect histograms
            // over the 5 most significant bits of literals. One histogram
            // without context and 13 additional histograms for each context value.
            Span<uint> combinedHisto = arena.Slice(0, 32);
            Span<uint> contextHisto = arena.Slice(32, 32 * Metablock.MaxStaticContexts);
            uint total = 0;
            double entropy1;
            double entropy2;
            double entropy0;
            ReadOnlySpan<byte> utf8Lut = BrotliContext.GetLut(BrotliContextType.Utf8);
            ReadOnlySpan<uint> complexMap = StaticContextMapComplexUtf8;
            arena.Slice(0, 32 * (Metablock.MaxStaticContexts + 1)).Clear();
            for (; startPos + 64 <= endPos; startPos += 4096)
            {
                nuint strideEndPos = startPos + 64;
                byte prev2 = input[(int)(startPos & mask)];
                byte prev1 = input[(int)((startPos + 1) & mask)];
                // To make the analysis of the data faster we only examine 64 byte long
                // strides at every 4kB intervals.
                for (nuint pos = startPos + 2; pos < strideEndPos; ++pos)
                {
                    byte literal = input[(int)(pos & mask)];
                    byte context = (byte)complexMap[BrotliContext.GetContext(prev1, prev2, utf8Lut)];
                    ++total;
                    ++combinedHisto[literal >> 3];
                    ++contextHisto[(context << 5) + (literal >> 3)];
                    prev2 = prev1;
                    prev1 = literal;
                }
            }
            entropy1 = EstimateEntropy(combinedHisto, 32);
            entropy2 = 0;
            for (int i = 0; i < Metablock.MaxStaticContexts; ++i)
            {
                entropy2 += EstimateEntropy(contextHisto.Slice(i << 5), 32);
            }
            entropy0 = 1.0 / (double)total;
            entropy1 *= entropy0;
            entropy2 *= entropy0;
            // The triggering heuristics below were tuned by compressing the individual
            // files of the silesia corpus. If we skip this kind of context modeling
            // for not very well compressible input (i.e. entropy using context modeling
            // is 60% of maximal entropy) or if expected savings by symbol are less
            // than 0.2 bits, then in every case when it triggers, the final compression
            // ratio is improved. Note however that this heuristics might be too strict
            // for some cases and could be tuned further.
            if (entropy2 > 3.0 || entropy1 - entropy2 < 0.2)
            {
                return false;
            }
            else
            {
                numLiteralContexts = Metablock.MaxStaticContexts;
                literalContextMap = StaticContextMapComplexUtf8;
                return true;
            }
        }
    }

    private static ReadOnlySpan<int> PrefixLut => [0, 0, 1, 2];

    private static void DecideOverLiteralContextModeling(ReadOnlySpan<byte> input, nuint startPos, nuint length, nuint mask,
        int quality, nuint sizeHint, ref nuint numLiteralContexts, ref ReadOnlySpan<uint> literalContextMap, Span<uint> arena)
    {
        if (quality < Quality.MinQualityForContextModeling || length < 64)
        {
            return;
        }
        else if (ShouldUseComplexStaticContextMap(input, startPos, length, mask, quality, sizeHint,
            ref numLiteralContexts, ref literalContextMap, arena))
        {
            // Context map was already set, nothing else to do.
        }
        else
        {
            // Gather bi-gram data of the UTF8 byte prefixes. To make the analysis of
            // UTF8 data faster we only examine 64 byte long strides at every 4kB
            // intervals.
            nuint endPos = startPos + length;
            Span<uint> bigramPrefixHisto = arena.Slice(0, 9);
            ReadOnlySpan<int> lut = PrefixLut;
            bigramPrefixHisto.Clear();
            for (; startPos + 64 <= endPos; startPos += 4096)
            {
                nuint strideEndPos = startPos + 64;
                int prev = lut[input[(int)(startPos & mask)] >> 6] * 3;
                for (nuint pos = startPos + 1; pos < strideEndPos; ++pos)
                {
                    byte literal = input[(int)(pos & mask)];
                    ++bigramPrefixHisto[prev + lut[literal >> 6]];
                    prev = lut[literal >> 6] * 3;
                }
            }
            ChooseContextMap(quality, bigramPrefixHisto, ref numLiteralContexts, ref literalContextMap);
        }
    }

    private static bool ShouldCompress(ReadOnlySpan<byte> data, nuint mask, ulong lastFlushPos, nuint bytes,
        nuint numLiterals, nuint numCommands)
    {
        // TODO(eustas): find more precise minimal block overhead.
        if (bytes <= 2)
        {
            return false;
        }
        if (numCommands < (bytes >> 8) + 2)
        {
            if ((double)numLiterals > 0.99 * (double)bytes)
            {
                const uint SampleRate = 13;
                const double InvSampleRate = 1.0 / 13.0;
                const double MinEntropy = 7.92;
                double bitCostThreshold = (double)bytes * MinEntropy * InvSampleRate;
                nuint t = (bytes + SampleRate - 1) / SampleRate;
                uint pos = (uint)lastFlushPos;
                Span<uint> literalHisto = stackalloc uint[256];
                literalHisto.Clear();
                for (nuint i = 0; i < t; i++)
                {
                    ++literalHisto[data[(int)(pos & mask)]];
                    pos += SampleRate;
                }
                if (BitCost.BitsEntropy(literalHisto, 256) > bitCostThreshold)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>Chooses the literal context mode for a metablock.</summary>
    private static BrotliContextType ChooseContextMode(in EncoderParams parameters, ReadOnlySpan<byte> data, nuint pos,
        nuint mask, nuint length)
    {
        // We only do the computation for the option of something else than
        // CONTEXT_UTF8 for the highest qualities
        if (parameters.Quality >= Quality.MinQualityForHqBlockSplitting &&
            !Utf8Util.IsMostlyUtf8(data, pos, mask, length, Utf8Util.MinUtf8Ratio))
        {
            return BrotliContextType.Signed;
        }
        return BrotliContextType.Utf8;
    }

    private void WriteMetaBlockInternal(ReadOnlySpan<byte> data, nuint mask, ulong lastFlushPos, nuint bytes, bool isLast,
        BrotliContextType literalContextMode, in EncoderParams parameters, byte prevByte, byte prevByte2, nuint numLiterals,
        nuint numCommands, Span<Command> commands, ReadOnlySpan<int> savedDistCache, Span<int> distCache,
        ref nuint storageIx, Span<byte> storage)
    {
        uint wrappedLastFlushPos = WrapPosition(lastFlushPos);
        ushort lastBytes;
        byte lastBytesBits;
        ReadOnlySpan<byte> literalContextLut = BrotliContext.GetLut(literalContextMode);
        EncoderParams blockParams = parameters;

        if (bytes == 0)
        {
            // Write the ISLAST and ISEMPTY bits.
            BitWriter.WriteBits(2, 3, ref storageIx, storage);
            storageIx = (storageIx + 7u) & ~(nuint)7u;
            return;
        }

        if (!ShouldCompress(data, mask, lastFlushPos, bytes, numLiterals, numCommands))
        {
            // Restore the distance cache, as its last update by
            // CreateBackwardReferences is now unused.
            savedDistCache.Slice(0, 4).CopyTo(distCache);
            BrotliBitStream.StoreUncompressedMetaBlock(isLast, data, wrappedLastFlushPos, mask, bytes, ref storageIx, storage);
            return;
        }

        Debug.Assert(storageIx <= 14);
        lastBytes = BinaryPrimitives.ReadUInt16LittleEndian(storage);
        lastBytesBits = (byte)storageIx;
        if (parameters.Quality <= Quality.MaxQualityForStaticEntropyCodes)
        {
            BrotliBitStream.StoreMetaBlockFast(data, wrappedLastFlushPos, bytes, mask, isLast, parameters,
                commands, numCommands, ref storageIx, storage, _metablockArena ??= new MetablockArena());
        }
        else if (parameters.Quality < Quality.MinQualityForBlockSplit)
        {
            BrotliBitStream.StoreMetaBlockTrivial(data, wrappedLastFlushPos, bytes, mask, isLast, parameters,
                commands, numCommands, ref storageIx, storage, _metablockArena ??= new MetablockArena());
        }
        else
        {
            MetaBlockSplit mb = _metaBlockSplit ??= new MetaBlockSplit();
            Metablock.InitMetaBlockSplit(mb);
            if (parameters.Quality < Quality.MinQualityForHqBlockSplitting)
            {
                nuint numLiteralContexts = 1;
                ReadOnlySpan<uint> literalContextMap = default;
                if (!parameters.DisableLiteralContextModeling)
                {
                    uint[] arena = _contextArena ??= new uint[32 * (Metablock.MaxStaticContexts + 1)];
                    DecideOverLiteralContextModeling(data, wrappedLastFlushPos, bytes, mask, parameters.Quality,
                        parameters.SizeHint, ref numLiteralContexts, ref literalContextMap, arena);
                }
                Metablock.BuildMetaBlockGreedy(data, wrappedLastFlushPos, mask, prevByte, prevByte2, literalContextLut,
                    numLiteralContexts, literalContextMap, commands, numCommands, mb);
            }
            else
            {
                Metablock.BuildMetaBlock(data, wrappedLastFlushPos, mask, ref blockParams, prevByte, prevByte2,
                    commands, numCommands, literalContextMode, mb);
            }
            if (parameters.Quality >= Quality.MinQualityForOptimizeHistograms)
            {
                // The number of distance symbols effectively used for distance
                // histograms. It might be less than distance alphabet size
                // for "Large Window Brotli" (32-bit).
                Metablock.OptimizeHistograms(blockParams.Dist.AlphabetSizeLimit, mb);
            }
            BrotliBitStream.StoreMetaBlock(data, wrappedLastFlushPos, bytes, mask, prevByte, prevByte2, isLast,
                blockParams, literalContextMode, commands, numCommands, mb, ref storageIx, storage,
                _metablockArena ??= new MetablockArena());
            // C calls DestroyMetaBlockSplit here; the split (and its arrays) is kept for reuse by the next
            // meta-block instead and released in Dispose.
        }
        if (bytes + 4 < (storageIx >> 3))
        {
            // Restore the distance cache and last byte.
            savedDistCache.Slice(0, 4).CopyTo(distCache);
            BinaryPrimitives.WriteUInt16LittleEndian(storage, lastBytes);
            storageIx = lastBytesBits;
            BrotliBitStream.StoreUncompressedMetaBlock(isLast, data, wrappedLastFlushPos, mask, bytes, ref storageIx, storage);
        }
    }

    private static void ChooseDistanceParams(ref EncoderParams parameters)
    {
        uint distancePostfixBits = 0;
        uint numDirectDistanceCodes = 0;

        if (parameters.Quality >= Quality.MinQualityForNonzeroDistanceParams)
        {
            uint ndirectMsb;
            if (parameters.Mode == EncoderMode.Font)
            {
                distancePostfixBits = 1;
                numDirectDistanceCodes = 12;
            }
            else
            {
                distancePostfixBits = parameters.Dist.DistancePostfixBits;
                numDirectDistanceCodes = parameters.Dist.NumDirectDistanceCodes;
            }
            ndirectMsb = (numDirectDistanceCodes >> (int)distancePostfixBits) & 0x0F;
            if (distancePostfixBits > BrotliConstants.MaxNPostfix ||
                numDirectDistanceCodes > BrotliConstants.MaxNDirect ||
                (ndirectMsb << (int)distancePostfixBits) != numDirectDistanceCodes)
            {
                distancePostfixBits = 0;
                numDirectDistanceCodes = 0;
            }
        }

        Metablock.InitDistanceParams(ref parameters.Dist, distancePostfixBits, numDirectDistanceCodes, parameters.LargeWindow);
    }

    private bool EnsureInitialized()
    {
        if (_isInitialized)
        {
            return true;
        }

        _lastBytesBits = 0;
        _lastBytes = 0;
        _flint = FlintDone;
        _remainingMetadataBytes = uint.MaxValue;

        Quality.SanitizeParams(ref _params);
        _params.Lgblock = Quality.ComputeLgBlock(_params);
        ChooseDistanceParams(ref _params);

        if (_params.StreamOffset != 0)
        {
            _flint = FlintNeeds2Bytes;
            // Poison the distance cache. -16 +- 3 is still less than zero (invalid).
            _distCache[0] = -16;
            _distCache[1] = -16;
            _distCache[2] = -16;
            _distCache[3] = -16;
            _distCache.AsSpan(0, 4).CopyTo(_savedDistCache);
        }

        _ringBuffer.Setup(_params);

        // Initialize last byte with stream header.
        {
            int lgwin = _params.Lgwin;
            if (_params.Quality == Quality.FastOnePassCompressionQuality ||
                _params.Quality == Quality.FastTwoPassCompressionQuality)
            {
                lgwin = Math.Max(lgwin, 18);
            }
            if (_params.StreamOffset == 0)
            {
                EncodeWindowBits(lgwin, _params.LargeWindow, out _lastBytes, out _lastBytesBits);
            }
            else
            {
                // Bigger values have the same effect, but could cause overflows.
                _params.StreamOffset = Math.Min(_params.StreamOffset, BrotliConstants.MaxBackwardLimit(lgwin));
            }
        }

        if (_params.Quality == Quality.FastOnePassCompressionQuality)
        {
            BrotliOnePassArena arena = new BrotliOnePassArena();
            _onePassArena = arena;
            _smallTable = arena.SmallTable;
            InitCommandPrefixCodes(arena);
        }
        else if (_params.Quality == Quality.FastTwoPassCompressionQuality)
        {
            BrotliTwoPassArena arena = new BrotliTwoPassArena();
            _twoPassArena = arena;
            _smallTable = arena.SmallTable;
        }

        _isInitialized = true;
        return true;
    }

    private static void InitParams(ref EncoderParams parameters)
    {
        parameters.Mode = EncoderMode.Generic;
        parameters.LargeWindow = false;
        parameters.Quality = DefaultQuality;
        parameters.Lgwin = DefaultWindow;
        parameters.Lgblock = 0;
        parameters.StreamOffset = 0;
        parameters.SizeHint = 0;
        parameters.DisableLiteralContextModeling = false;
        parameters.Dist.DistancePostfixBits = 0;
        parameters.Dist.NumDirectDistanceCodes = 0;
        parameters.Dist.AlphabetSizeMax = BrotliConstants.DistanceAlphabetSize(0, 0, BrotliConstants.MaxDistanceBits);
        parameters.Dist.AlphabetSizeLimit = parameters.Dist.AlphabetSizeMax;
        parameters.Dist.MaxDistance = BrotliConstants.MaxDistance;
    }

    /// <summary>
    /// Copies the given input data to the internal ring buffer of the compressor.
    /// No processing of the data occurs at this time and this function can be
    /// called multiple times before calling WriteBrotliData() to process the
    /// accumulated input. At most input_block_size() bytes of input data can be
    /// copied to the ring buffer, otherwise the next WriteBrotliData() will fail.
    /// </summary>
    private void CopyInputToRingBuffer(nuint inputSize, ReadOnlySpan<byte> inputBuffer)
    {
        RingBuffer ringBuffer = _ringBuffer;
        ringBuffer.Write(inputBuffer, inputSize);
        _inputPos += inputSize;

        // TL;DR: If needed, initialize 7 more bytes in the ring buffer to make the
        // hashing not depend on uninitialized data. This makes compression
        // deterministic and it prevents uninitialized memory warnings in Valgrind.
        // Even without erasing, the output would be valid (but nondeterministic).
        //
        // Background information: The compressor stores short (at most 8 bytes)
        // substrings of the input already read in a hash table, and detects
        // repetitions by looking up such substrings in the hash table. If it
        // can find a substring, it checks whether the substring is really there
        // in the ring buffer (or it's just a hash collision). Should the hash
        // table become corrupt, this check makes sure that the output is
        // still valid, albeit the compression ratio would be bad.
        //
        // The compressor populates the hash table from the ring buffer as it's
        // reading new bytes from the input. However, at the last few indexes of
        // the ring buffer, there are not enough bytes to build full-length
        // substrings from. Since the hash table always contains full-length
        // substrings, we overwrite with zeros here to make sure that those
        // substrings will contain zeros at the end instead of uninitialized
        // data.
        //
        // Please note that erasing is not necessary (because the
        // memory region is already initialized since he ring buffer
        // has a `tail' that holds a copy of the beginning,) so we
        // skip erasing if we have already gone around at least once in
        // the ring buffer.
        //
        // Only clear during the first round of ring-buffer writes. On
        // subsequent rounds data in the ring-buffer would be affected.
        if (ringBuffer.Pos <= ringBuffer.Mask)
        {
            // This is the first time when the ring buffer is being written.
            // We clear 7 bytes just after the bytes that have been copied from
            // the input buffer.
            //
            // The ring-buffer has a "tail" that holds a copy of the beginning,
            // but only once the ring buffer has been fully written once, i.e.,
            // pos <= mask. For the first time, we need to write values
            // in this tail (where index may be larger than mask), so that
            // we have exactly defined behavior and don't read uninitialized
            // memory. Due to performance reasons, hashing reads data using a
            // LOAD64, which can go 7 bytes beyond the bytes written in the
            // ring-buffer.
            ringBuffer.Data.AsSpan(RingBuffer.BufferOffset + (int)ringBuffer.Pos, 7).Clear();
        }
    }

    /// <summary>
    /// Marks all input as processed.
    /// Returns true if position wrapping occurs.
    /// </summary>
    private bool UpdateLastProcessedPos()
    {
        uint wrappedLastProcessedPos = WrapPosition(_lastProcessedPos);
        uint wrappedInputPos = WrapPosition(_inputPos);
        _lastProcessedPos = _inputPos;
        return wrappedInputPos < wrappedLastProcessedPos;
    }

    private void ExtendLastCommand(ref uint bytes, ref uint wrappedLastProcessedPos)
    {
        ref Command lastCommand = ref _commands![(int)_numCommands - 1];
        ReadOnlySpan<byte> data = _ringBuffer.Buffer;
        uint mask = _ringBuffer.Mask;
        ulong maxBackwardDistance = (1UL << _params.Lgwin) - BrotliConstants.WindowGap;
        ulong lastCopyLen = lastCommand.CopyLen & 0x1FFFFFF;
        ulong lastProcessedPos = _lastProcessedPos - lastCopyLen;
        ulong maxDistance = lastProcessedPos < maxBackwardDistance ? lastProcessedPos : maxBackwardDistance;
        ulong cmdDist = (ulong)_distCache[0];
        uint distanceCode = lastCommand.RestoreDistanceCode(_params.Dist);
        if (distanceCode < BrotliConstants.NumDistanceShortCodes ||
            distanceCode - (BrotliConstants.NumDistanceShortCodes - 1) == cmdDist)
        {
            if (cmdDist <= maxDistance)
            {
                while (bytes != 0 &&
                    data[(int)(wrappedLastProcessedPos & mask)] == data[(int)((wrappedLastProcessedPos - cmdDist) & mask)])
                {
                    lastCommand.CopyLen++;
                    bytes--;
                    wrappedLastProcessedPos++;
                }
            }
            // The C code also extends into the compound (custom) dictionary here when cmd_dist > max_distance;
            // custom dictionaries are not supported (their total size is 0), so that branch never applies.

            // The copy length is at most the metablock size, and thus expressible.
            Command.GetLengthCode(lastCommand.InsertLen,
                (nuint)((int)(lastCommand.CopyLen & 0x1FFFFFF) + (int)(lastCommand.CopyLen >> 25)),
                (lastCommand.DistPrefix & 0x3FF) == 0,
                out lastCommand.CmdPrefix);
        }
    }

    /// <summary>
    /// Processes the accumulated input data and sets _availableOut to the length of
    /// the new output meta-block, or to zero if no new output meta-block has been
    /// created (in this case the processed input data is buffered internally).
    /// If _availableOut is positive, _nextOut points to the start of the output
    /// data. If |is_last| or |force_flush| is true, an output meta-block is
    /// always created. However, until |is_last| is true encoder may retain up
    /// to 7 bits of the last byte of output. Byte-alignment could be enforced by
    /// emitting an empty meta-data block.
    /// Returns false if the size of the input data is larger than
    /// input_block_size().
    /// </summary>
    /// <remarks>
    /// Both C call sites pass <c>&amp;s-&gt;available_out_</c> and <c>&amp;s-&gt;next_out_</c> as the output arguments,
    /// so they are written directly here.
    /// </remarks>
    private bool EncodeData(bool isLast, bool forceFlush)
    {
        ulong delta = UnprocessedInputSize();
        uint bytes = (uint)delta;
        uint wrappedLastProcessedPos = WrapPosition(_lastProcessedPos);
        uint mask;
        BrotliContextType literalContextMode;
        ReadOnlySpan<byte> literalContextLut;
        bool fastCompress =
            _params.Quality == Quality.FastOnePassCompressionQuality ||
            _params.Quality == Quality.FastTwoPassCompressionQuality;

        mask = _ringBuffer.Mask;

        if (delta == 0)
        {
            // No new input; still might want to flush or finish.
            if (_ringBuffer.Data is null)
            {
                // No input has been processed so far.
                if (isLast)
                {
                    // Emit complete finalized stream.
                    Debug.Assert(_lastBytesBits <= 14);
                    _lastBytes |= (ushort)(3u << _lastBytesBits);
                    _lastBytesBits = (byte)(_lastBytesBits + 2u);
                    BinaryPrimitives.WriteUInt16LittleEndian(_tinyBuf, _lastBytes);
                    _nextOut = _tinyBuf;
                    _nextOutOffset = 0;
                    _availableOut = (_lastBytesBits + 7u) >> 3;
                    return true;
                }
                else
                {
                    // No data, not last -> no-op.
                    _availableOut = 0;
                    return true;
                }
            }
            else
            {
                // Fast compress performs flush every block -> flush is no-op.
                if (!isLast && (!forceFlush || fastCompress))
                {
                    // Another no-op.
                    _availableOut = 0;
                    return true;
                }
            }
        }
        Debug.Assert(_ringBuffer.Data is not null);
        Span<byte> data = _ringBuffer.Buffer;

        // The C code checks the quality against the (custom) dictionary max_quality here;
        // for the built-in dictionary it is BROTLI_MAX_QUALITY, so the check never fails.

        // Adding more blocks after "last" block is forbidden.
        if (_isLastBlockEmitted)
        {
            return false;
        }
        if (isLast)
        {
            _isLastBlockEmitted = true;
        }

        if (delta > InputBlockSize())
        {
            return false;
        }
        if (_params.Quality == Quality.FastTwoPassCompressionQuality && _commandBuf is null)
        {
            _commandBuf = ArrayPool<uint>.Shared.Rent(CompressFragmentTwoPass.BlockSize);
            _literalBuf = ArrayPool<byte>.Shared.Rent(CompressFragmentTwoPass.BlockSize);
        }

        if (fastCompress)
        {
            nuint storageIx = _lastBytesBits;
            Span<byte> storage = GetBrotliStorage(2 * bytes + 503);
            BinaryPrimitives.WriteUInt16LittleEndian(storage, _lastBytes);
            Span<int> table = GetHashTable(_params.Quality, bytes, out nuint tableSize);
            ReadOnlySpan<byte> fragment = data.Slice((int)(wrappedLastProcessedPos & mask), (int)bytes);
            if (_params.Quality == Quality.FastOnePassCompressionQuality)
            {
                CompressFragment.CompressFragmentFast(_onePassArena!, fragment, bytes, isLast, table, tableSize,
                    ref storageIx, storage);
            }
            else
            {
                CompressFragmentTwoPass.Compress(_twoPassArena!, fragment, bytes, isLast,
                    _commandBuf.AsSpan(0, CompressFragmentTwoPass.BlockSize), _literalBuf.AsSpan(0, CompressFragmentTwoPass.BlockSize),
                    table, tableSize, ref storageIx, storage);
            }
            _lastBytes = storage[(int)(storageIx >> 3)];
            _lastBytesBits = (byte)(storageIx & 7u);
            UpdateLastProcessedPos();
            _nextOut = _storage;
            _nextOutOffset = 0;
            _availableOut = storageIx >> 3;
            return true;
        }

        {
            // Theoretical max number of commands is 1 per 2 bytes.
            nuint newsize = _numCommands + bytes / 2 + 1;
            if (newsize > _cmdAllocSize)
            {
                Command[] newCommands;
                // Reserve a bit more memory to allow merging with a next block
                // without reallocation: that would impact speed.
                newsize += (bytes / 4) + 16;
                _cmdAllocSize = newsize;
                newCommands = ArrayPool<Command>.Shared.Rent((int)newsize);
                Command[]? oldCommands = _commands;
                _commands = null;
                if (oldCommands is not null)
                {
                    oldCommands.AsSpan(0, (int)_numCommands).CopyTo(newCommands);
                    ArrayPool<Command>.Shared.Return(oldCommands);
                }
                _commands = newCommands;
            }
        }
        Command[] commands = _commands!;

        Hasher hasher = _hasher ??= new Hasher();
        Hasher.InitOrStitchToPreviousBlock(hasher, data, mask, ref _params, wrappedLastProcessedPos, bytes, isLast);

        literalContextMode = ChooseContextMode(_params, data, WrapPosition(_lastFlushPos), mask, (nuint)(_inputPos - _lastFlushPos));
        literalContextLut = BrotliContext.GetLut(literalContextMode);

        if (_numCommands != 0 && _lastInsertLen == 0)
        {
            ExtendLastCommand(ref bytes, ref wrappedLastProcessedPos);
        }

        Span<Command> newCommandsSpan = commands.AsSpan((int)_numCommands, (int)(_cmdAllocSize - _numCommands));
        if (_params.Quality == Quality.ZopflificationQuality)
        {
            Debug.Assert(_params.Hasher.Type == 10);
            BackwardReferencesHq.CreateZopfliBackwardReferences(bytes, wrappedLastProcessedPos, data, mask, literalContextLut,
                _params, hasher, _distCache, ref _lastInsertLen, newCommandsSpan, ref _numCommands, ref _numLiterals);
        }
        else if (_params.Quality == Quality.HqZopflificationQuality)
        {
            Debug.Assert(_params.Hasher.Type == 10);
            BackwardReferencesHq.CreateHqZopfliBackwardReferences(bytes, wrappedLastProcessedPos, data, mask, literalContextLut,
                _params, hasher, _distCache, ref _lastInsertLen, newCommandsSpan, ref _numCommands, ref _numLiterals);
        }
        else
        {
            BackwardReferences.CreateBackwardReferences(bytes, wrappedLastProcessedPos, data, mask, literalContextLut,
                _params, hasher, _distCache, ref _lastInsertLen, newCommandsSpan, ref _numCommands, ref _numLiterals);
        }

        {
            nuint maxLength = Quality.MaxMetablockSize(_params);
            nuint maxLiterals = maxLength / 8;
            nuint maxCommands = maxLength / 8;
            nuint processedBytes = (nuint)(_inputPos - _lastFlushPos);
            // If maximal possible additional block doesn't fit metablock, flush now.
            // TODO(eustas): Postpone decision until next block arrives?
            bool nextInputFitsMetablock = processedBytes + InputBlockSize() <= maxLength;
            // If block splitting is not used, then flush as soon as there is some
            // amount of commands / literals produced.
            bool shouldFlush =
                _params.Quality < Quality.MinQualityForBlockSplit &&
                _numLiterals + _numCommands >= Quality.MaxNumDelayedSymbols;
            if (!isLast && !forceFlush && !shouldFlush &&
                nextInputFitsMetablock &&
                _numLiterals < maxLiterals &&
                _numCommands < maxCommands)
            {
                // Merge with next input block. Everything will happen later.
                if (UpdateLastProcessedPos())
                {
                    hasher.Reset();
                }
                _availableOut = 0;
                return true;
            }
        }

        // Create the last insert-only command.
        if (_lastInsertLen > 0)
        {
            commands[(int)_numCommands++].InitInsert(_lastInsertLen);
            _numLiterals += _lastInsertLen;
            _lastInsertLen = 0;
        }

        if (!isLast && _inputPos == _lastFlushPos)
        {
            // We have no new input data and we don't have to finish the stream, so
            // nothing to do.
            _availableOut = 0;
            return true;
        }
        Debug.Assert(_inputPos >= _lastFlushPos);
        Debug.Assert(_inputPos > _lastFlushPos || isLast);
        Debug.Assert(_inputPos - _lastFlushPos <= 1u << 24);
        {
            uint metablockSize = (uint)(_inputPos - _lastFlushPos);
            Span<byte> storage = GetBrotliStorage(2 * metablockSize + 503);
            nuint storageIx = _lastBytesBits;
            BinaryPrimitives.WriteUInt16LittleEndian(storage, _lastBytes);
            WriteMetaBlockInternal(data, mask, _lastFlushPos, metablockSize, isLast, literalContextMode, _params,
                _prevByte, _prevByte2, _numLiterals, _numCommands, commands.AsSpan(0, (int)_numCommands),
                _savedDistCache, _distCache, ref storageIx, storage);
            _lastBytes = storage[(int)(storageIx >> 3)];
            _lastBytesBits = (byte)(storageIx & 7u);
            _lastFlushPos = _inputPos;
            if (UpdateLastProcessedPos())
            {
                hasher.Reset();
            }
            if (_lastFlushPos > 0)
            {
                _prevByte = data[(int)(((uint)_lastFlushPos - 1) & mask)];
            }
            if (_lastFlushPos > 1)
            {
                _prevByte2 = data[(int)((uint)(_lastFlushPos - 2) & mask)];
            }
            _numCommands = 0;
            _numLiterals = 0;
            // Save the state of the distance cache in case we need to restore it for
            // emitting an uncompressed block.
            _distCache.AsSpan(0, 4).CopyTo(_savedDistCache);
            _nextOut = _storage;
            _nextOutOffset = 0;
            _availableOut = storageIx >> 3;
            return true;
        }
    }

    /// <summary>
    /// Dumps remaining output bits and metadata header to |header|.
    /// Returns number of produced bytes.
    /// REQUIRED: |header| should be at least 16 bytes long.
    /// REQUIRED: |block_size| &lt;= (1 &lt;&lt; 24).
    /// </summary>
    private nuint WriteMetadataHeader(nuint blockSize, Span<byte> header)
    {
        nuint storageIx;
        storageIx = _lastBytesBits;
        BinaryPrimitives.WriteUInt16LittleEndian(header, _lastBytes);
        _lastBytes = 0;
        _lastBytesBits = 0;

        BitWriter.WriteBits(1, 0, ref storageIx, header);
        BitWriter.WriteBits(2, 3, ref storageIx, header);
        BitWriter.WriteBits(1, 0, ref storageIx, header);
        if (blockSize == 0)
        {
            BitWriter.WriteBits(2, 0, ref storageIx, header);
        }
        else
        {
            uint nbits = (blockSize == 1) ? 1 : (FastLog.Log2FloorNonZero((uint)blockSize - 1) + 1);
            uint nbytes = (nbits + 7) / 8;
            BitWriter.WriteBits(2, nbytes, ref storageIx, header);
            BitWriter.WriteBits(8 * nbytes, blockSize - 1, ref storageIx, header);
        }
        return (storageIx + 7u) >> 3;
    }

    /// <summary>Port of BrotliEncoderMaxCompressedSize.</summary>
    public static nuint MaxCompressedSize(nuint inputSize)
    {
        // [window bits / empty metadata] + N * [uncompressed] + [last empty]
        nuint numLargeBlocks = inputSize >> 14;
        nuint overhead = 2 + (4 * numLargeBlocks) + 3 + 1;
        nuint result = inputSize + overhead;
        if (inputSize == 0)
        {
            return 2;
        }
        return (result < inputSize) ? 0 : result;
    }

    /// <summary>
    /// Wraps data to uncompressed brotli stream with minimal window size.
    /// |output| should point at region with at least BrotliEncoderMaxCompressedSize
    /// addressable bytes.
    /// Returns the length of stream.
    /// </summary>
    private static nuint MakeUncompressedStream(ReadOnlySpan<byte> input, nuint inputSize, Span<byte> output)
    {
        nuint size = inputSize;
        nuint result = 0;
        nuint offset = 0;
        if (inputSize == 0)
        {
            output[0] = 6;
            return 1;
        }
        output[(int)result++] = 0x21; // window bits = 10, is_last = false
        output[(int)result++] = 0x03; // empty metadata, padding
        while (size > 0)
        {
            uint nibbles = 0;
            uint chunkSize;
            uint bits;
            chunkSize = (size > (1u << 24)) ? (1u << 24) : (uint)size;
            if (chunkSize > (1u << 16))
            {
                nibbles = (chunkSize > (1u << 20)) ? 2u : 1u;
            }
            bits = (nibbles << 1) | ((chunkSize - 1) << 3) | (1u << (int)(19 + 4 * nibbles));
            output[(int)result++] = (byte)bits;
            output[(int)result++] = (byte)(bits >> 8);
            output[(int)result++] = (byte)(bits >> 16);
            if (nibbles == 2)
            {
                output[(int)result++] = (byte)(bits >> 24);
            }
            input.Slice((int)offset, (int)chunkSize).CopyTo(output.Slice((int)result));
            result += chunkSize;
            offset += chunkSize;
            size -= chunkSize;
        }
        output[(int)result++] = 3;
        return result;
    }

    /// <summary>
    /// Port of BrotliEncoderCompress: one-shot compression with a fallback to an uncompressed stream.
    /// </summary>
    public static bool Compress(int quality, int lgwin, EncoderMode mode, ReadOnlySpan<byte> input, Span<byte> output, out int encodedSize)
    {
        nuint inputSize = (nuint)input.Length;
        nuint outSize = (nuint)output.Length;
        nuint maxOutSize = MaxCompressedSize(inputSize);
        encodedSize = 0;
        if (outSize == 0)
        {
            // Output buffer needs at least one byte.
            return false;
        }
        if (inputSize == 0)
        {
            // Handle the special case of empty input.
            encodedSize = 1;
            output[0] = 6;
            return true;
        }

        {
            BrotliEncoderState s = new BrotliEncoderState();
            bool result;
            try
            {
                ReadOnlySpan<byte> nextIn = input;
                Span<byte> nextOut = output;
                // TODO(eustas): check that parameters are sane.
                s.SetParameter(BrotliEncoderParameter.Quality, (uint)quality);
                s.SetParameter(BrotliEncoderParameter.LGWin, (uint)lgwin);
                s.SetParameter(BrotliEncoderParameter.Mode, (uint)mode);
                s.SetParameter(BrotliEncoderParameter.SizeHint, (uint)inputSize);
                // Large window brotli (lgwin > BROTLI_MAX_WINDOW_BITS) is not supported; lgwin is clamped instead.
                result = s.CompressStreamCore(BrotliEncoderOperation.Finish, ref nextIn, ref nextOut);
                if (!s.IsFinished)
                {
                    result = false;
                }
                encodedSize = output.Length - nextOut.Length;
            }
            finally
            {
                s.Dispose();
            }
            if (!result || (maxOutSize != 0 && (nuint)encodedSize > maxOutSize))
            {
                goto fallback;
            }
            return true;
        }
    fallback:
        encodedSize = 0;
        if (maxOutSize == 0)
        {
            return false;
        }
        if (outSize >= maxOutSize)
        {
            encodedSize = (int)MakeUncompressedStream(input, inputSize, output);
            return true;
        }
        return false;
    }

    private void InjectBytePaddingBlock()
    {
        uint seal = _lastBytes;
        nuint sealBits = _lastBytesBits;
        byte[] destination;
        int destinationOffset;
        _lastBytes = 0;
        _lastBytesBits = 0;
        // is_last = 0, data_nibbles = 11, reserved = 0, meta_nibbles = 00
        seal |= 0x6u << (int)sealBits;
        sealBits += 6;
        // If we have already created storage, then append to it.
        // Storage is valid until next block is being compressed.
        if (_nextOut is not null)
        {
            destination = _nextOut;
            destinationOffset = _nextOutOffset + (int)_availableOut;
        }
        else
        {
            destination = _tinyBuf;
            destinationOffset = 0;
            _nextOut = destination;
            _nextOutOffset = 0;
        }
        destination[destinationOffset] = (byte)seal;
        if (sealBits > 8)
        {
            destination[destinationOffset + 1] = (byte)(seal >> 8);
        }
        if (sealBits > 16)
        {
            destination[destinationOffset + 2] = (byte)(seal >> 16);
        }
        _availableOut += (sealBits + 7) >> 3;
    }

    /// <summary>
    /// Injects padding bits or pushes compressed data to output.
    /// Returns false if nothing is done.
    /// </summary>
    private bool InjectFlushOrPushOutput(ref Span<byte> output)
    {
        if (_streamState == BrotliEncoderStreamState.FlushRequested && _lastBytesBits != 0)
        {
            InjectBytePaddingBlock();
            return true;
        }

        if (_availableOut != 0 && output.Length != 0)
        {
            int copyOutputSize = (int)Math.Min(_availableOut, (nuint)output.Length);
            _nextOut.AsSpan(_nextOutOffset, copyOutputSize).CopyTo(output);
            output = output.Slice(copyOutputSize);
            _nextOutOffset += copyOutputSize;
            _availableOut -= (nuint)copyOutputSize;
            _totalOut += (ulong)copyOutputSize;
            return true;
        }

        return false;
    }

    private void CheckFlushComplete()
    {
        if (_streamState == BrotliEncoderStreamState.FlushRequested && _availableOut == 0)
        {
            _streamState = BrotliEncoderStreamState.Processing;
            _nextOut = null;
            _nextOutOffset = 0;
        }
    }

    private bool CompressStreamFast(BrotliEncoderOperation op, ref ReadOnlySpan<byte> input, ref Span<byte> output)
    {
        nuint blockSizeLimit = (nuint)1 << _params.Lgwin;
        nuint bufSize = Math.Min((nuint)CompressFragmentTwoPass.BlockSize, Math.Min((nuint)input.Length, blockSizeLimit));
        uint[]? tmpCommandBuf = null;
        uint[]? commandBuf = null;
        byte[]? tmpLiteralBuf = null;
        byte[]? literalBuf = null;
        int commandBufSize = 0;
        if (_params.Quality != Quality.FastOnePassCompressionQuality &&
            _params.Quality != Quality.FastTwoPassCompressionQuality)
        {
            return false;
        }
        if (_params.Quality == Quality.FastTwoPassCompressionQuality)
        {
            if (_commandBuf is null && bufSize == CompressFragmentTwoPass.BlockSize)
            {
                _commandBuf = ArrayPool<uint>.Shared.Rent(CompressFragmentTwoPass.BlockSize);
                _literalBuf = ArrayPool<byte>.Shared.Rent(CompressFragmentTwoPass.BlockSize);
            }
            if (_commandBuf is not null)
            {
                commandBuf = _commandBuf;
                literalBuf = _literalBuf;
                commandBufSize = CompressFragmentTwoPass.BlockSize;
            }
            else if (bufSize > 0)
            {
                tmpCommandBuf = ArrayPool<uint>.Shared.Rent((int)bufSize);
                tmpLiteralBuf = ArrayPool<byte>.Shared.Rent((int)bufSize);
                commandBuf = tmpCommandBuf;
                literalBuf = tmpLiteralBuf;
                commandBufSize = (int)bufSize;
            }
        }

        while (true)
        {
            if (InjectFlushOrPushOutput(ref output))
            {
                continue;
            }

            // Compress block only when internal output buffer is empty, stream is not
            // finished, there is no pending flush request, and there is either
            // additional input or pending operation.
            if (_availableOut == 0 &&
                _streamState == BrotliEncoderStreamState.Processing &&
                (input.Length != 0 || op != BrotliEncoderOperation.Process))
            {
                nuint blockSize = Math.Min(blockSizeLimit, (nuint)input.Length);
                bool isLast = ((nuint)input.Length == blockSize) && (op == BrotliEncoderOperation.Finish);
                bool forceFlush = ((nuint)input.Length == blockSize) && (op == BrotliEncoderOperation.Flush);
                nuint maxOutSize = 2 * blockSize + 503;
                bool inplace = true;
                Span<byte> storage;
                nuint storageIx = _lastBytesBits;

                if (forceFlush && blockSize == 0)
                {
                    _streamState = BrotliEncoderStreamState.FlushRequested;
                    continue;
                }
                if (maxOutSize <= (nuint)output.Length)
                {
                    storage = output;
                }
                else
                {
                    inplace = false;
                    storage = GetBrotliStorage(maxOutSize);
                }
                BinaryPrimitives.WriteUInt16LittleEndian(storage, _lastBytes);
                Span<int> table = GetHashTable(_params.Quality, blockSize, out nuint tableSize);

                if (_params.Quality == Quality.FastOnePassCompressionQuality)
                {
                    CompressFragment.CompressFragmentFast(_onePassArena!, input, blockSize, isLast, table, tableSize,
                        ref storageIx, storage);
                }
                else
                {
                    CompressFragmentTwoPass.Compress(_twoPassArena!, input, blockSize, isLast,
                        commandBuf.AsSpan(0, commandBufSize), literalBuf.AsSpan(0, commandBufSize),
                        table, tableSize, ref storageIx, storage);
                }
                if (blockSize != 0)
                {
                    input = input.Slice((int)blockSize);
                    _totalIn += blockSize;
                }
                if (inplace)
                {
                    nuint outBytes = storageIx >> 3;
                    Debug.Assert(outBytes <= (nuint)output.Length);
                    Debug.Assert((storageIx & 7) == 0 || outBytes < (nuint)output.Length);
                    output = output.Slice((int)outBytes);
                    _totalOut += outBytes;
                }
                else
                {
                    nuint outBytes = storageIx >> 3;
                    _nextOut = _storage;
                    _nextOutOffset = 0;
                    _availableOut = outBytes;
                }
                _lastBytes = storage[(int)(storageIx >> 3)];
                _lastBytesBits = (byte)(storageIx & 7u);

                if (forceFlush)
                {
                    _streamState = BrotliEncoderStreamState.FlushRequested;
                }
                if (isLast)
                {
                    _streamState = BrotliEncoderStreamState.Finished;
                }
                continue;
            }
            break;
        }
        if (tmpCommandBuf is not null)
        {
            ArrayPool<uint>.Shared.Return(tmpCommandBuf);
        }
        if (tmpLiteralBuf is not null)
        {
            ArrayPool<byte>.Shared.Return(tmpLiteralBuf);
        }
        CheckFlushComplete();
        return true;
    }

    private bool ProcessMetadata(ref ReadOnlySpan<byte> input, ref Span<byte> output)
    {
        if ((nuint)input.Length > (1u << 24))
        {
            return false;
        }
        // Switch to metadata block workflow, if required.
        if (_streamState == BrotliEncoderStreamState.Processing)
        {
            _remainingMetadataBytes = (uint)input.Length;
            _streamState = BrotliEncoderStreamState.MetadataHead;
        }
        if (_streamState != BrotliEncoderStreamState.MetadataHead &&
            _streamState != BrotliEncoderStreamState.MetadataBody)
        {
            return false;
        }

        while (true)
        {
            if (InjectFlushOrPushOutput(ref output))
            {
                continue;
            }
            if (_availableOut != 0)
            {
                break;
            }

            if (_inputPos != _lastFlushPos)
            {
                bool result = EncodeData(false, true);
                if (!result)
                {
                    return false;
                }
                continue;
            }

            if (_streamState == BrotliEncoderStreamState.MetadataHead)
            {
                _nextOut = _tinyBuf;
                _nextOutOffset = 0;
                _availableOut = WriteMetadataHeader(_remainingMetadataBytes, _tinyBuf);
                _streamState = BrotliEncoderStreamState.MetadataBody;
                continue;
            }
            else
            {
                // Exit workflow only when there is no more input and no more output.
                // Otherwise client may continue producing empty metadata blocks.
                if (_remainingMetadataBytes == 0)
                {
                    _remainingMetadataBytes = uint.MaxValue;
                    _streamState = BrotliEncoderStreamState.Processing;
                    break;
                }
                if (output.Length != 0)
                {
                    // Directly copy input to output.
                    int copy = (int)Math.Min(_remainingMetadataBytes, (uint)output.Length);
                    input.Slice(0, copy).CopyTo(output);
                    input = input.Slice(copy);
                    _totalIn += (ulong)copy; // not actually data input, though
                    _remainingMetadataBytes -= (uint)copy;
                    output = output.Slice(copy);
                }
                else
                {
                    // This guarantees progress in "TakeOutput" workflow.
                    int copy = (int)Math.Min(_remainingMetadataBytes, 16u);
                    _nextOut = _tinyBuf;
                    _nextOutOffset = 0;
                    input.Slice(0, copy).CopyTo(_tinyBuf);
                    input = input.Slice(copy);
                    _totalIn += (ulong)copy; // not actually data input, though
                    _remainingMetadataBytes -= (uint)copy;
                    _availableOut = (nuint)copy;
                }
                continue;
            }
        }

        return true;
    }

    private void UpdateSizeHint(nuint availableIn)
    {
        if (_params.SizeHint == 0)
        {
            ulong delta = UnprocessedInputSize();
            ulong tail = availableIn;
            uint limit = 1u << 30;
            uint total;
            if ((delta >= limit) || (tail >= limit) || ((delta + tail) >= limit))
            {
                total = limit;
            }
            else
            {
                total = (uint)(delta + tail);
            }
            _params.SizeHint = total;
        }
    }

    /// <summary>
    /// Port of BrotliEncoderCompressStream: one call with <c>available_in = input.Length</c> and
    /// <c>available_out = output.Length</c>. Returns false on error (or if the state was disposed).
    /// </summary>
    public bool CompressStream(BrotliEncoderOperation op, ReadOnlySpan<byte> input, Span<byte> output, out int bytesConsumed, out int bytesWritten)
    {
        if (_isDisposed)
        {
            bytesConsumed = 0;
            bytesWritten = 0;
            return false;
        }

        ReadOnlySpan<byte> nextIn = input;
        Span<byte> nextOut = output;
        bool result = CompressStreamCore(op, ref nextIn, ref nextOut);
        bytesConsumed = input.Length - nextIn.Length;
        bytesWritten = output.Length - nextOut.Length;
        return result;
    }

    /// <summary>
    /// Port of BrotliEncoderCompressStream. <c>*next_in</c>/<c>*available_in</c> are represented by
    /// <paramref name="input"/> and <c>*next_out</c>/<c>*available_out</c> by <paramref name="output"/>;
    /// both are advanced as data is consumed / produced.
    /// </summary>
    private bool CompressStreamCore(BrotliEncoderOperation op, ref ReadOnlySpan<byte> input, ref Span<byte> output)
    {
        if (!EnsureInitialized())
        {
            return false;
        }

        // Unfinished metadata block; check requirements.
        if (_remainingMetadataBytes != uint.MaxValue)
        {
            if ((nuint)input.Length != _remainingMetadataBytes)
            {
                return false;
            }
            if (op != BrotliEncoderOperation.EmitMetadata)
            {
                return false;
            }
        }

        if (op == BrotliEncoderOperation.EmitMetadata)
        {
            UpdateSizeHint(0); // First data metablock might be emitted here.
            return ProcessMetadata(ref input, ref output);
        }

        if (_streamState == BrotliEncoderStreamState.MetadataHead ||
            _streamState == BrotliEncoderStreamState.MetadataBody)
        {
            return false;
        }

        if (_streamState != BrotliEncoderStreamState.Processing && input.Length != 0)
        {
            return false;
        }
        if (_params.Quality == Quality.FastOnePassCompressionQuality ||
            _params.Quality == Quality.FastTwoPassCompressionQuality)
        {
            return CompressStreamFast(op, ref input, ref output);
        }
        while (true)
        {
            nuint remainingBlockSize = RemainingInputBlockSize();
            // Shorten input to flint size.
            if (_flint >= 0 && remainingBlockSize > (nuint)_flint)
            {
                remainingBlockSize = (nuint)_flint;
            }

            if (remainingBlockSize != 0 && input.Length != 0)
            {
                nuint copyInputSize = Math.Min(remainingBlockSize, (nuint)input.Length);
                CopyInputToRingBuffer(copyInputSize, input);
                input = input.Slice((int)copyInputSize);
                _totalIn += copyInputSize;
                if (_flint > 0)
                {
                    _flint = (sbyte)(_flint - (int)copyInputSize);
                }
                continue;
            }

            if (InjectFlushOrPushOutput(ref output))
            {
                // Exit the "emit flint" workflow.
                if (_flint == FlintWaitingForFlushing)
                {
                    CheckFlushComplete();
                    if (_streamState == BrotliEncoderStreamState.Processing)
                    {
                        _flint = FlintDone;
                    }
                }
                continue;
            }

            // Compress data only when internal output buffer is empty, stream is not
            // finished and there is no pending flush request.
            if (_availableOut == 0 &&
                _streamState == BrotliEncoderStreamState.Processing)
            {
                if (remainingBlockSize == 0 || op != BrotliEncoderOperation.Process)
                {
                    bool isLast = input.Length == 0 && op == BrotliEncoderOperation.Finish;
                    bool forceFlush = input.Length == 0 && op == BrotliEncoderOperation.Flush;
                    bool result;
                    // Force emitting (uncompressed) piece containing flint.
                    if (!isLast && _flint == FlintWaitingForProcessing)
                    {
                        _flint = FlintWaitingForFlushing;
                        forceFlush = true;
                    }
                    UpdateSizeHint((nuint)input.Length);
                    result = EncodeData(isLast, forceFlush);
                    if (!result)
                    {
                        return false;
                    }
                    if (forceFlush)
                    {
                        _streamState = BrotliEncoderStreamState.FlushRequested;
                    }
                    if (isLast)
                    {
                        _streamState = BrotliEncoderStreamState.Finished;
                    }
                    continue;
                }
            }
            break;
        }
        CheckFlushComplete();
        return true;
    }
}
