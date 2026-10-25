// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/dec/state.h and state.c
// (plus the public entry points of c/dec/decode.c).

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Result type for <see cref="BrotliDecoderState.DecompressStream"/> (BrotliDecoderResult).</summary>
internal enum BrotliDecoderResult
{
    /// <summary>Decoding error, e.g. corrupted input or memory allocation problem.</summary>
    Error = 0,
    /// <summary>Decoding successfully completed.</summary>
    Success = 1,
    /// <summary>Partially done; should be called again with more input.</summary>
    NeedsMoreInput = 2,
    /// <summary>Partially done; should be called again with more output.</summary>
    NeedsMoreOutput = 3,
}

/// <summary>Error code for detailed logging / production debugging (BrotliDecoderErrorCode).</summary>
internal enum BrotliDecoderErrorCode
{
    NoError = 0,
    // Same as BrotliDecoderResult values.
    Success = 1,
    NeedsMoreInput = 2,
    NeedsMoreOutput = 3,

    // Errors caused by invalid input.
    ErrorFormatExuberantNibble = -1,
    ErrorFormatReserved = -2,
    ErrorFormatExuberantMetaNibble = -3,
    ErrorFormatSimpleHuffmanAlphabet = -4,
    ErrorFormatSimpleHuffmanSame = -5,
    ErrorFormatClSpace = -6,
    ErrorFormatHuffmanSpace = -7,
    ErrorFormatContextMapRepeat = -8,
    ErrorFormatBlockLength1 = -9,
    ErrorFormatBlockLength2 = -10,
    ErrorFormatTransform = -11,
    ErrorFormatDictionary = -12,
    ErrorFormatWindowBits = -13,
    ErrorFormatPadding1 = -14,
    ErrorFormatPadding2 = -15,
    ErrorFormatDistance = -16,

    // -17 code is reserved.
    ErrorFormatBlockSwitch = -17,

    ErrorCompoundDictionary = -18,
    ErrorDictionaryNotSet = -19,
    ErrorInvalidArguments = -20,

    // Memory allocation problems.
    ErrorAllocContextModes = -21,
    // Literal, insert and distance trees together.
    ErrorAllocTreeGroups = -22,
    // -23..-24 codes are reserved for distinct tree groups.
    ErrorAllocContextMap = -25,
    ErrorAllocRingBuffer1 = -26,
    ErrorAllocRingBuffer2 = -27,
    // -28..-29 codes are reserved for dynamic ring-buffer allocation.
    ErrorAllocBlockTypeTrees = -30,

    // "Impossible" states.
    ErrorUnreachable = -31,
}

internal enum BrotliRunningState
{
    Uninited,
    LargeWindowBits,
    Initialize,
    MetablockBegin,
    MetablockHeader,
    MetablockHeader2,
    ContextModes,
    CommandBegin,
    CommandInner,
    CommandPostDecodeLiterals,
    CommandPostWrapCopy,
    Uncompressed,
    Metadata,
    CommandInnerWrite,
    MetablockDone,
    CommandPostWrite1,
    CommandPostWrite2,
    BeforeCompressedMetablockHeader,
    HuffmanCode0,
    HuffmanCode1,
    HuffmanCode2,
    HuffmanCode3,
    ContextMap1,
    ContextMap2,
    TreeGroup,
    BeforeCompressedMetablockBody,
    Done,
}

internal enum BrotliRunningMetablockHeaderState
{
    None,
    Empty,
    Nibbles,
    Size,
    Uncompressed,
    Reserved,
    Bytes,
    Metadata,
}

internal enum BrotliRunningUncompressedState
{
    None,
    Write,
}

internal enum BrotliRunningTreeGroupState
{
    None,
    Loop,
}

internal enum BrotliRunningContextMapState
{
    None,
    ReadPrefix,
    Huffman,
    Decode,
    Transform,
}

internal enum BrotliRunningHuffmanState
{
    None,
    SimpleSize,
    SimpleRead,
    SimpleBuild,
    Complex,
    LengthSymbols,
}

internal enum BrotliRunningDecodeUint8State
{
    None,
    Short,
    Long,
}

internal enum BrotliRunningReadBlockLengthState
{
    None,
    Suffix,
}

/// <summary>
/// Brotli decoder state for partial streaming decoding (BrotliDecoderStateStruct).
/// </summary>
/// <remarks>
/// Differences from C: large-window streams, shared/compound dictionaries, metadata callbacks and custom allocators
/// are not supported. Pointers into heap buffers are represented as offsets. Only the two large buffers are rented
/// from <see cref="ArrayPool{T}.Shared"/>: the ring buffer and the Huffman tables (block type trees and the three tree
/// groups share one array); small fixed-size tables are inline arrays and the literal context map is a plain array.
/// Buffers that C frees after every metablock (context maps, tree groups) are kept and reused (grown when needed)
/// until <see cref="Dispose"/>.
/// </remarks>
internal sealed partial class BrotliDecoderState
{
    /// <summary>
    /// Literal/Command/Distance block size maximum; same as maximum metablock size; used as block size when there
    /// is no block switching.
    /// </summary>
    private const uint BlockSizeCap = 1u << 24;

    private const int BlockTypeTreesSize = 3 * (Huffman.MaxSize258 + Huffman.MaxSize26);
    private const int BlockLenTreesOffset = 3 * Huffman.MaxSize258;

    [InlineArray(3)]
    private struct UInt3
    {
        private uint _e0;
    }

    [InlineArray(4)]
    private struct Int4
    {
        private int _e0;
    }

    [InlineArray(6)]
    private struct UInt6
    {
        private uint _e0;
    }

    [InlineArray(8)]
    private struct Byte8
    {
        private byte _e0;
    }

    [InlineArray(8)]
    private struct UInt8
    {
        private uint _e0;
    }

    [InlineArray(BrotliConstants.MaxNumberOfBlockTypes)]
    private struct Byte256
    {
        private byte _e0;
    }

    /// <summary>Distance context map: 4 contexts per block type.</summary>
    [InlineArray(BrotliConstants.MaxNumberOfBlockTypes << BrotliConstants.DistanceContextBits)]
    private struct Byte1024
    {
        private byte _e0;
    }

    [InlineArray(1 << BrotliConstants.LiteralContextBits)]
    private struct Int64Contexts
    {
        private int _e0;
    }

    [InlineArray(BrotliConstants.CodeLengthCodes)]
    private struct Byte18
    {
        private byte _e0;
    }

    [InlineArray(16)]
    private struct UShort16
    {
        private ushort _e0;
    }

    [InlineArray(32)]
    private struct Int32x32
    {
        private int _e0;
    }

    [InlineArray(32)]
    private struct HuffmanCode32
    {
        private HuffmanCode _e0;
    }

    [InlineArray(Huffman.MaxCodeLength + 1 + BrotliConstants.NumCommandSymbols)]
    private struct SymbolListsArray
    {
        private ushort _e0;
    }

    [InlineArray(Huffman.MaxSize272)]
    private struct HuffmanCode646
    {
        private HuffmanCode _e0;
    }

    [InlineArray((64 + 1) * 4)]
    private struct MtfArray
    {
        private byte _e0;
    }

    [InlineArray(544)]
    private struct Byte544
    {
        private byte _e0;
    }

    [InlineArray(544)]
    private struct UInt544
    {
        private uint _e0;
    }

    private BrotliRunningState _state;

    /// <summary>This counter is reused for several disjoint loops.</summary>
    private int _loopCounter;

    // Persisted part of the bit reader (BrotliBitReader.val_ and bit_pos_).
    private ulong _brVal;
    private uint _brBitPos;

    /// <summary>
    /// Temporary storage for remaining input. Brotli stream format is designed in a way, that 64 bits are enough to
    /// make progress in decoding.
    /// </summary>
    private Byte8 _buffer;
    private int _bufferLength;

    private int _pos;
    private int _maxBackwardDistance;
    private int _maxDistance;
    private int _ringbufferSize;
    private int _ringbufferMask;
    private int _distRbIdx;
    private Int4 _distRb;
    private int _errorCode;
    private int _metaBlockRemainingLen;

    /// <summary>Ring buffer (rented); ringbuffer_end == offset <see cref="_ringbufferSize"/>.</summary>
    private byte[]? _ringbuffer;
    /// <summary>Offset of the current command tree in <see cref="_insertCopyHgroup"/> codes.</summary>
    private int _htreeCommand;
    /// <summary>Offset of the current literal context lookup table in <see cref="BrotliContext.LookupTable"/>.</summary>
    private int _contextLookup;
    /// <summary>Offset in <see cref="_contextMap"/>.</summary>
    private int _contextMapSlice;
    /// <summary>Offset in <see cref="_distContextMap"/>.</summary>
    private int _distContextMapSlice;

    private readonly HuffmanTreeGroup _literalHgroup = new HuffmanTreeGroup();
    private readonly HuffmanTreeGroup _insertCopyHgroup = new HuffmanTreeGroup();
    private readonly HuffmanTreeGroup _distanceHgroup = new HuffmanTreeGroup();
    /// <summary>
    /// block_type_trees followed by block_len_trees (at <see cref="BlockLenTreesOffset"/>), followed by the codes of
    /// the literal, insert-and-copy and distance tree groups (C allocates these separately).
    /// </summary>
    private HuffmanCode[]? _blockTypeTrees;

    /// <summary>
    /// This is true if the literal context map histogram type always matches the block type. It is then not needed
    /// to keep the context (faster decoding).
    /// </summary>
    private bool _trivialLiteralContext;
    /// <summary>
    /// Distance context is actual after command is decoded and before distance is computed. After distance
    /// computation it is used as a temporary variable.
    /// </summary>
    private int _distanceContext;
    private UInt3 _blockLength;
    private uint _blockLengthIndex;
    private UInt3 _numBlockTypes;
    private UInt6 _blockTypeRb;
    private uint _distancePostfixBits;
    private uint _numDirectDistanceCodes;
    private uint _numDistHtrees;
    private Byte1024 _distContextMap;
    /// <summary>Offset of the current literal tree in <see cref="_literalHgroup"/> codes.</summary>
    private int _literalHtree;
    /// <summary>
    /// Offsets (in <see cref="_literalHgroup"/> codes) of the literal trees for each of the 64 literal contexts of
    /// the current literal block type, i.e. htrees[context_map_slice[context]] (not in C; saves an indirection).
    /// </summary>
    private Int64Contexts _literalTreesForContexts;
    /// <summary>
    /// Offsets (in <see cref="_distanceHgroup"/> codes) of the distance trees for the 4 distance contexts of the
    /// current distance block type, i.e. htrees[dist_context_map_slice[context]] (not in C; saves indirections).
    /// </summary>
    private Int4 _distanceTreesForContexts;

    // For partial write operations.
    /// <summary>How many times we went around the ring-buffer.</summary>
    private nuint _rbRoundtrips;
    /// <summary>How much output to the user in total.</summary>
    private nuint _partialPosOut;

    // For InverseMoveToFrontTransform.
    private uint _mtfUpperBound;
    /// <summary>uint32_t mtf[64 + 1] viewed as bytes.</summary>
    private MtfArray _mtf;

    private int _copyLength;
    private int _distanceCode;

    /// <summary>
    /// dist_htree_index, represented by the offset of the selected tree in the <see cref="_distanceHgroup"/> codes,
    /// i.e. htrees[dist_htree_index].
    /// </summary>
    private int _distTreeOffset;


    // States inside function calls.
    private BrotliRunningMetablockHeaderState _substateMetablockHeader;
    private BrotliRunningUncompressedState _substateUncompressed;
    private BrotliRunningDecodeUint8State _substateDecodeUint8;
    private BrotliRunningReadBlockLengthState _substateReadBlockLength;

    private int _newRingbufferSize;

    private bool _isLastMetablock;
    private bool _isUncompressed;
    private bool _isMetadata;
    private bool _shouldWrapRingbuffer;
    private bool _cannyRingbufferAllocation;
    private int _windowBits;
    private int _sizeNibbles;

    private uint _numLiteralHtrees;
    private byte[]? _contextMap;
    private Byte256 _contextModes;

    /// <summary>256 bits.</summary>
    private UInt8 _trivialLiteralContexts;

    // BrotliMetablockHeaderArena.
    private BrotliRunningTreeGroupState _substateTreeGroup;
    private BrotliRunningContextMapState _substateContextMap;
    private BrotliRunningHuffmanState _substateHuffman;

    private uint _subLoopCounter;

    private uint _repeatCodeLen;
    private uint _prevCodeLen;

    // For ReadHuffmanCode.
    private uint _symbol;
    private uint _repeat;
    private uint _space;

    /// <summary>Huffman table for "histograms".</summary>
    private HuffmanCode32 _table;
    /// <summary>
    /// Storage for symbol_lists; C's <c>symbol_lists</c> points to element MaxCodeLength + 1 of it
    /// (makes small negative indexes addressable).
    /// </summary>
    private SymbolListsArray _symbolListsArray;
    /// <summary>Tails of symbol chains.</summary>
    private Int32x32 _nextSymbol;
    private Byte18 _codeLengthCodeLengths;
    /// <summary>Population counts for the code lengths.</summary>
    private UShort16 _codeLengthHisto;

    // For HuffmanTreeGroupDecode.
    private int _htreeIndex;
    /// <summary>Offset in the codes of the tree group being decoded.</summary>
    private int _next;

    // For DecodeContextMap.
    private uint _contextIndex;
    private uint _maxRunLengthPrefix;
    private uint _code;
    private HuffmanCode646 _contextMapTable;

    // BrotliMetablockBodyArena.
    private Byte544 _distExtraBits;
    private UInt544 _distOffset;

    private bool _disposed;

    /// <summary>Creates and initializes a decoder instance (BrotliDecoderCreateInstance / BrotliDecoderStateInit).</summary>
    /// <remarks>All the other fields of BrotliDecoderStateInit are zero, false or null initially.</remarks>
    public BrotliDecoderState()
    {
        _cannyRingbufferAllocation = true;
        _distRb[0] = 16;
        _distRb[1] = 15;
        _distRb[2] = 11;
        _distRb[3] = 4;
        _mtfUpperBound = 63;
    }

    /// <summary>
    /// Returns true if the decoder is in a state where it reached the end of the input and produced all of the
    /// output (BrotliDecoderIsFinished).
    /// </summary>
    public bool IsFinished => _state == BrotliRunningState.Done && !HasMoreOutput();

    /// <summary>Returns true if <see cref="Dispose"/> has been called.</summary>
    public bool IsDisposed => _disposed;

    /// <summary>Checks if decoder has more output (BrotliDecoderHasMoreOutput).</summary>
    private bool HasMoreOutput()
    {
        // After unrecoverable error remaining output is considered nonsensical.
        if (_errorCode < 0)
        {
            return false;
        }
        return _ringbuffer is not null && UnwrittenBytes(false) != 0;
    }

    /// <summary>Releases (pooled) buffers (BrotliDecoderStateCleanup); the instance is unusable afterwards.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Cleanup();
    }

    /// <summary>
    /// Decompresses the input stream to the output buffer (BrotliDecoderDecompressStream with
    /// available_in = input.Length and available_out = output.Length).
    /// </summary>
    public BrotliDecoderResult DecompressStream(ReadOnlySpan<byte> input, Span<byte> output, out int bytesConsumed, out int bytesWritten)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int nextIn = 0;
        int availableIn = input.Length;
        int outPos = 0;
        BitReader br = default;
        br.Val = _brVal;
        br.BitPos = _brBitPos;
        BrotliDecoderResult result = DecompressStreamCore(ref br, input, ref nextIn, ref availableIn, output, ref outPos);
        _brVal = br.Val;
        _brBitPos = br.BitPos;
        Debug.Assert(nextIn + availableIn == input.Length);
        bytesConsumed = nextIn;
        bytesWritten = outPos;
        return result;
    }

    /// <summary>
    /// Decompresses the input buffer into the output buffer (BrotliDecoderDecompress). Returns true only if the
    /// whole stream was decoded successfully; <paramref name="decodedSize"/> is the total number of bytes produced.
    /// </summary>
    public static bool Decompress(ReadOnlySpan<byte> input, Span<byte> output, out int decodedSize)
    {
        var s = new BrotliDecoderState();
        BrotliDecoderResult result = s.DecompressStream(input, output, out _, out _);
        decodedSize = (int)s._partialPosOut;
        s.Dispose();
        return result == BrotliDecoderResult.Success;
    }

    private void MetablockBegin()
    {
        _metaBlockRemainingLen = 0;
        _blockLength[0] = BlockSizeCap;
        _blockLength[1] = BlockSizeCap;
        _blockLength[2] = BlockSizeCap;
        _numBlockTypes[0] = 1;
        _numBlockTypes[1] = 1;
        _numBlockTypes[2] = 1;
        _blockTypeRb[0] = 1;
        _blockTypeRb[1] = 0;
        _blockTypeRb[2] = 1;
        _blockTypeRb[3] = 0;
        _blockTypeRb[4] = 1;
        _blockTypeRb[5] = 0;
        _contextMapSlice = 0;
        _literalHtree = 0;
        _distContextMapSlice = 0;
        _distTreeOffset = 0;
        _contextLookup = 0;
    }

    /// <summary>
    /// BrotliDecoderStateCleanupAfterMetablock: C frees context modes, context maps and tree groups here; they are
    /// kept for reuse by the next metablock instead (they are fully re-initialized before use).
    /// </summary>
    private static void CleanupAfterMetablock()
    {
    }

    private void Cleanup()
    {
        _literalHgroup.Codes = _insertCopyHgroup.Codes = _distanceHgroup.Codes = null;
        ReturnArray(ref _ringbuffer);
        ReturnArray(ref _blockTypeTrees);
    }

    private static void ReturnArray<T>(ref T[]? array)
    {
        T[]? a = array;
        if (a is not null)
        {
            array = null;
            ArrayPool<T>.Shared.Return(a);
        }
    }

    /// <summary>Makes sure <paramref name="array"/> has at least <paramref name="size"/> elements.</summary>
    private static T[] EnsureArray<T>(ref T[]? array, int size)
    {
        T[]? a = array;
        if (a is null || a.Length < size)
        {
            ReturnArray(ref array);
            a = ArrayPool<T>.Shared.Rent(size);
            array = a;
        }
        return a;
    }

    /// <summary>
    /// BrotliDecoderHuffmanTreeGroupInit for all the three groups: the codes of the groups are placed after the
    /// block type trees in <see cref="_blockTypeTrees"/> (whose block type trees are preserved).
    /// </summary>
    private void HuffmanTreeGroupsInit(uint distanceAlphabetSizeMax, uint distanceAlphabetSizeLimit)
    {
        int literalBase = BlockTypeTreesSize;
        int insertCopyBase = literalBase + HuffmanTreeGroupInit(_literalHgroup, BrotliConstants.NumLiteralSymbols, BrotliConstants.NumLiteralSymbols, _numLiteralHtrees, literalBase);
        int distanceBase = insertCopyBase + HuffmanTreeGroupInit(_insertCopyHgroup, BrotliConstants.NumCommandSymbols, BrotliConstants.NumCommandSymbols, _numBlockTypes[1], insertCopyBase);
        int size = distanceBase + HuffmanTreeGroupInit(_distanceHgroup, distanceAlphabetSizeMax, distanceAlphabetSizeLimit, _numDistHtrees, distanceBase);

        HuffmanCode[] codes = _blockTypeTrees!;
        if (codes.Length < size)
        {
            HuffmanCode[] newCodes = ArrayPool<HuffmanCode>.Shared.Rent(size);
            codes.AsSpan(0, BlockTypeTreesSize).CopyTo(newCodes);
            _blockTypeTrees = newCodes;
            ArrayPool<HuffmanCode>.Shared.Return(codes);
            codes = newCodes;
        }
        _literalHgroup.Codes = codes;
        _insertCopyHgroup.Codes = codes;
        _distanceHgroup.Codes = codes;
    }

    /// <summary>Returns the maximal size of the codes of the group.</summary>
    private static int HuffmanTreeGroupInit(HuffmanTreeGroup group, uint alphabetSizeMax, uint alphabetSizeLimit, uint ntrees, int codesBase)
    {
        // 376 = 256 (1-st level table) + 4 + 7 + 15 + 31 + 63 (2-nd level mix-tables)
        // This number is discovered "unlimited" "enough" calculator; it is actually a wee bigger than required in
        // several cases (especially for alphabets with less than 16 symbols).
        int maxTableSize = (int)alphabetSizeLimit + 376;
        group.AlphabetSizeMax = (ushort)alphabetSizeMax;
        group.AlphabetSizeLimit = (ushort)alphabetSizeLimit;
        group.NumHtrees = (ushort)ntrees;
        group.Base = codesBase;
        return (int)ntrees * maxTableSize;
    }
}
