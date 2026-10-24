// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/dec/decode.c.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

internal sealed partial class BrotliDecoderState
{
    private const int HuffmanTableBits = 8;
    private const uint HuffmanTableMask = 0xFF;

    /// <summary>
    /// We need the slack region for the following reasons:
    /// - doing up to two 16-byte copies for fast backward copying
    /// - inserting transformed dictionary word: 255 prefix + 32 base + 255 suffix
    /// </summary>
    private const int RingBufferWriteAheadSlack = 542;

    private static ReadOnlySpan<byte> CodeLengthCodeOrder => [1, 2, 3, 4, 0, 5, 17, 6, 16, 7, 8, 9, 10, 11, 12, 13, 14, 15];

    /// <summary>Static prefix code for the complex code length code lengths.</summary>
    private static ReadOnlySpan<byte> CodeLengthPrefixLength => [2, 2, 2, 3, 2, 2, 2, 4, 2, 2, 2, 3, 2, 2, 2, 4];

    private static ReadOnlySpan<byte> CodeLengthPrefixValue => [0, 4, 3, 2, 0, 4, 3, 1, 0, 4, 3, 2, 0, 4, 3, 5];

    /// <summary>Saves error code and converts it to BrotliDecoderResult.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderResult SaveErrorCode(BrotliDecoderErrorCode e, ref BitReader br)
    {
        _errorCode = (int)e;
        if (_bufferLength != 0 && br.NextIn == br.LastIn)
        {
            // If internal buffer is depleted at last, reset it.
            _bufferLength = 0;
        }
        switch (e)
        {
            case BrotliDecoderErrorCode.Success:
                return BrotliDecoderResult.Success;

            case BrotliDecoderErrorCode.NeedsMoreInput:
                return BrotliDecoderResult.NeedsMoreInput;

            case BrotliDecoderErrorCode.NeedsMoreOutput:
                return BrotliDecoderResult.NeedsMoreOutput;

            default:
                return BrotliDecoderResult.Error;
        }
    }

    /// <summary>
    /// Decodes WBITS by reading 1 - 7 bits, or 0x11 for "Large Window Brotli" (not supported: rejected as when
    /// BROTLI_DECODER_PARAM_LARGE_WINDOW is off). Precondition: bit-reader accumulator has at least 8 bits.
    /// </summary>
    private BrotliDecoderErrorCode DecodeWindowBits(ref BitReader br)
    {
        uint n;
        n = br.TakeBits(1);
        if (n == 0)
        {
            _windowBits = 16;
            return BrotliDecoderErrorCode.Success;
        }
        n = br.TakeBits(3);
        if (n != 0)
        {
            _windowBits = (int)((17u + n) & 63u);
            return BrotliDecoderErrorCode.Success;
        }
        n = br.TakeBits(3);
        if (n == 1)
        {
            // Large window brotli is not supported.
            return BrotliDecoderErrorCode.ErrorFormatWindowBits;
        }
        if (n != 0)
        {
            _windowBits = (int)((8u + n) & 63u);
            return BrotliDecoderErrorCode.Success;
        }
        _windowBits = 17;
        return BrotliDecoderErrorCode.Success;
    }

    /// <summary>Decodes a number in the range [0..255], by reading 1 - 11 bits.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode DecodeVarLenUint8(ref BitReader br, ref uint value)
    {
        uint bits;
        switch (_substateDecodeUint8)
        {
            case BrotliRunningDecodeUint8State.None:
                if (!br.SafeReadBits(1, out bits))
                {
                    return BrotliDecoderErrorCode.NeedsMoreInput;
                }
                if (bits == 0)
                {
                    value = 0;
                    return BrotliDecoderErrorCode.Success;
                }
                goto case BrotliRunningDecodeUint8State.Short;

            case BrotliRunningDecodeUint8State.Short:
                if (!br.SafeReadBits(3, out bits))
                {
                    _substateDecodeUint8 = BrotliRunningDecodeUint8State.Short;
                    return BrotliDecoderErrorCode.NeedsMoreInput;
                }
                if (bits == 0)
                {
                    value = 1;
                    _substateDecodeUint8 = BrotliRunningDecodeUint8State.None;
                    return BrotliDecoderErrorCode.Success;
                }
                // Use output value as a temporary storage. It MUST be persisted.
                value = bits;
                goto case BrotliRunningDecodeUint8State.Long;

            case BrotliRunningDecodeUint8State.Long:
                if (!br.SafeReadBits(value, out bits))
                {
                    _substateDecodeUint8 = BrotliRunningDecodeUint8State.Long;
                    return BrotliDecoderErrorCode.NeedsMoreInput;
                }
                value = (1u << (int)value) + bits;
                _substateDecodeUint8 = BrotliRunningDecodeUint8State.None;
                return BrotliDecoderErrorCode.Success;

            default:
                return BrotliDecoderErrorCode.ErrorUnreachable;
        }
    }

    /// <summary>Decodes a metablock length and flags by reading 2 - 31 bits.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode DecodeMetaBlockLength(ref BitReader br)
    {
        uint bits;
        int i;
        while (true)
        {
            switch (_substateMetablockHeader)
            {
                case BrotliRunningMetablockHeaderState.None:
                    if (!br.SafeReadBits(1, out bits))
                    {
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    _isLastMetablock = bits != 0;
                    _metaBlockRemainingLen = 0;
                    _isUncompressed = false;
                    _isMetadata = false;
                    if (!_isLastMetablock)
                    {
                        _substateMetablockHeader = BrotliRunningMetablockHeaderState.Nibbles;
                        break;
                    }
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.Empty;
                    goto case BrotliRunningMetablockHeaderState.Empty;

                case BrotliRunningMetablockHeaderState.Empty:
                    if (!br.SafeReadBits(1, out bits))
                    {
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    if (bits != 0)
                    {
                        _substateMetablockHeader = BrotliRunningMetablockHeaderState.None;
                        return BrotliDecoderErrorCode.Success;
                    }
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.Nibbles;
                    goto case BrotliRunningMetablockHeaderState.Nibbles;

                case BrotliRunningMetablockHeaderState.Nibbles:
                    if (!br.SafeReadBits(2, out bits))
                    {
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    _sizeNibbles = (byte)(bits + 4);
                    _loopCounter = 0;
                    if (bits == 3)
                    {
                        _isMetadata = true;
                        _substateMetablockHeader = BrotliRunningMetablockHeaderState.Reserved;
                        break;
                    }
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.Size;
                    goto case BrotliRunningMetablockHeaderState.Size;

                case BrotliRunningMetablockHeaderState.Size:
                    i = _loopCounter;
                    for (; i < _sizeNibbles; ++i)
                    {
                        if (!br.SafeReadBits(4, out bits))
                        {
                            _loopCounter = i;
                            return BrotliDecoderErrorCode.NeedsMoreInput;
                        }
                        if (i + 1 == _sizeNibbles && _sizeNibbles > 4 && bits == 0)
                        {
                            return BrotliDecoderErrorCode.ErrorFormatExuberantNibble;
                        }
                        _metaBlockRemainingLen |= (int)(bits << (i * 4));
                    }
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.Uncompressed;
                    goto case BrotliRunningMetablockHeaderState.Uncompressed;

                case BrotliRunningMetablockHeaderState.Uncompressed:
                    if (!_isLastMetablock)
                    {
                        if (!br.SafeReadBits(1, out bits))
                        {
                            return BrotliDecoderErrorCode.NeedsMoreInput;
                        }
                        _isUncompressed = bits != 0;
                    }
                    ++_metaBlockRemainingLen;
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.None;
                    return BrotliDecoderErrorCode.Success;

                case BrotliRunningMetablockHeaderState.Reserved:
                    if (!br.SafeReadBits(1, out bits))
                    {
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    if (bits != 0)
                    {
                        return BrotliDecoderErrorCode.ErrorFormatReserved;
                    }
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.Bytes;
                    goto case BrotliRunningMetablockHeaderState.Bytes;

                case BrotliRunningMetablockHeaderState.Bytes:
                    if (!br.SafeReadBits(2, out bits))
                    {
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    if (bits == 0)
                    {
                        _substateMetablockHeader = BrotliRunningMetablockHeaderState.None;
                        return BrotliDecoderErrorCode.Success;
                    }
                    _sizeNibbles = (byte)bits;
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.Metadata;
                    goto case BrotliRunningMetablockHeaderState.Metadata;

                case BrotliRunningMetablockHeaderState.Metadata:
                    i = _loopCounter;
                    for (; i < _sizeNibbles; ++i)
                    {
                        if (!br.SafeReadBits(8, out bits))
                        {
                            _loopCounter = i;
                            return BrotliDecoderErrorCode.NeedsMoreInput;
                        }
                        if (i + 1 == _sizeNibbles && _sizeNibbles > 1 && bits == 0)
                        {
                            return BrotliDecoderErrorCode.ErrorFormatExuberantMetaNibble;
                        }
                        _metaBlockRemainingLen |= (int)(bits << (i * 8));
                    }
                    ++_metaBlockRemainingLen;
                    _substateMetablockHeader = BrotliRunningMetablockHeaderState.None;
                    return BrotliDecoderErrorCode.Success;

                default:
                    return BrotliDecoderErrorCode.ErrorUnreachable;
            }
        }
    }

    /// <summary>
    /// Decodes the Huffman code. This method doesn't read data from the bit reader, BUT drops the amount of bits
    /// that correspond to the decoded symbol. <paramref name="bits"/> MUST contain at least 15
    /// (BROTLI_HUFFMAN_MAX_CODE_LENGTH) valid bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DecodeSymbol(ulong bits, ReadOnlySpan<HuffmanCode> table, ref BitReader br)
    {
        int index = (int)(bits & HuffmanTableMask);
        HuffmanCode code = table[index];
        if (code.Bits > HuffmanTableBits)
        {
            uint nbits = (uint)code.Bits - HuffmanTableBits;
            br.DropBits(HuffmanTableBits);
            index += code.Value + (int)((uint)(bits >> HuffmanTableBits) & (uint)BitReader.BitMask(nbits));
            code = table[index];
        }
        br.DropBits(code.Bits);
        return code.Value;
    }

    /// <summary>
    /// Reads and decodes the next Huffman code from bit-stream. This method peeks 16 bits of input and drops
    /// 0 - 15 of them.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadSymbol(ReadOnlySpan<HuffmanCode> table, ref BitReader br) =>
        DecodeSymbol(br.Get16BitsUnmasked(), table, ref br);

    /// <summary>Same as DecodeSymbol, but it is known that there is less than 15 bits of input are currently available.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SafeDecodeSymbol(ReadOnlySpan<HuffmanCode> table, ref BitReader br, out uint result)
    {
        ulong val;
        uint availableBits = br.AvailableBits;
        if (availableBits == 0)
        {
            if (table[0].Bits == 0)
            {
                result = table[0].Value;
                return true;
            }
            result = 0;
            return false; // No valid bits at all.
        }
        val = br.GetBitsUnmasked();
        int index = (int)(val & HuffmanTableMask);
        HuffmanCode code = table[index];
        if (code.Bits <= HuffmanTableBits)
        {
            if (code.Bits <= availableBits)
            {
                br.DropBits(code.Bits);
                result = code.Value;
                return true;
            }
            else
            {
                result = 0;
                return false; // Not enough bits for the first level.
            }
        }
        if (availableBits <= HuffmanTableBits)
        {
            result = 0;
            return false; // Not enough bits to move to the second level.
        }

        // Speculatively drop HUFFMAN_TABLE_BITS.
        val = (val & BitReader.BitMask(code.Bits)) >> HuffmanTableBits;
        availableBits -= HuffmanTableBits;
        index += code.Value + (int)val;
        code = table[index];
        if (availableBits < code.Bits)
        {
            result = 0;
            return false; // Not enough bits for the second level.
        }

        br.DropBits(HuffmanTableBits + (uint)code.Bits);
        result = code.Value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SafeReadSymbol(ReadOnlySpan<HuffmanCode> table, ref BitReader br, out uint result)
    {
        if (br.SafeGetBits(15, out uint val))
        {
            result = DecodeSymbol(val, table, ref br);
            return true;
        }
        return SafeDecodeSymbol(table, ref br, out result);
    }

    private static uint Log2Floor(uint x)
    {
        uint result = 0;
        while (x != 0)
        {
            x >>= 1;
            ++result;
        }
        return result;
    }

    /// <summary>
    /// Reads (s->symbol + 1) symbols. Totally 1..4 symbols are read, 1..11 bits each.
    /// The list of symbols MUST NOT contain duplicates.
    /// </summary>
    private BrotliDecoderErrorCode ReadSimpleHuffmanSymbols(uint alphabetSizeMax, uint alphabetSizeLimit, ref BitReader br)
    {
        // max_bits == 1..11; symbol == 0..3; 1..44 bits will be read.
        uint maxBits = Log2Floor(alphabetSizeMax - 1);
        uint i = _subLoopCounter;
        uint numSymbols = _symbol;
        Span<ushort> symbolsListsArray = _symbolListsArray;
        while (i <= numSymbols)
        {
            if (!br.SafeReadBits(maxBits, out uint v))
            {
                _subLoopCounter = i;
                _substateHuffman = BrotliRunningHuffmanState.SimpleRead;
                return BrotliDecoderErrorCode.NeedsMoreInput;
            }
            if (v >= alphabetSizeLimit)
            {
                return BrotliDecoderErrorCode.ErrorFormatSimpleHuffmanAlphabet;
            }
            symbolsListsArray[(int)i] = (ushort)v;
            ++i;
        }

        for (i = 0; i < numSymbols; ++i)
        {
            uint k = i + 1;
            for (; k <= numSymbols; ++k)
            {
                if (symbolsListsArray[(int)i] == symbolsListsArray[(int)k])
                {
                    return BrotliDecoderErrorCode.ErrorFormatSimpleHuffmanSame;
                }
            }
        }

        return BrotliDecoderErrorCode.Success;
    }

    /// <summary>
    /// Process single decoded symbol code length:
    /// A) reset the repeat variable
    /// B) remember code length (if it is not 0)
    /// C) extend corresponding index-chain
    /// D) reduce the Huffman space
    /// E) update the histogram
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessSingleCodeLength(uint codeLen, ref uint symbol, ref uint repeat, ref uint space,
        ref uint prevCodeLen, Span<ushort> symbolListsArray, Span<ushort> codeLengthHisto, Span<int> nextSymbol)
    {
        repeat = 0;
        if (codeLen != 0)
        {
            // code_len == 1..15
            symbolListsArray[Huffman.MaxCodeLength + 1 + nextSymbol[(int)codeLen]] = (ushort)symbol;
            nextSymbol[(int)codeLen] = (int)symbol;
            prevCodeLen = codeLen;
            space -= 32768u >> (int)codeLen;
            codeLengthHisto[(int)codeLen]++;
        }
        symbol++;
    }

    /// <summary>
    /// Process repeated symbol code length.
    /// A) Check if it is the extension of previous repeat sequence; if the decoded value is not
    ///    BROTLI_REPEAT_PREVIOUS_CODE_LENGTH, then it is a new symbol-skip
    /// B) Update repeat variable
    /// C) Check if operation is feasible (fits alphabet)
    /// D) For each symbol do the same operations as in ProcessSingleCodeLength
    /// PRECONDITION: code_len == BROTLI_REPEAT_PREVIOUS_CODE_LENGTH or code_len == BROTLI_REPEAT_ZERO_CODE_LENGTH
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessRepeatedCodeLength(uint codeLen, uint repeatDelta, uint alphabetSize, ref uint symbol,
        ref uint repeat, ref uint space, ref uint prevCodeLen, ref uint repeatCodeLen, Span<ushort> symbolListsArray,
        Span<ushort> codeLengthHisto, Span<int> nextSymbol)
    {
        uint oldRepeat;
        uint extraBits = 3; // for BROTLI_REPEAT_ZERO_CODE_LENGTH
        uint newLen = 0;    // for BROTLI_REPEAT_ZERO_CODE_LENGTH
        if (codeLen == BrotliConstants.RepeatPreviousCodeLength)
        {
            newLen = prevCodeLen;
            extraBits = 2;
        }
        if (repeatCodeLen != newLen)
        {
            repeat = 0;
            repeatCodeLen = newLen;
        }
        oldRepeat = repeat;
        if (repeat > 0)
        {
            repeat -= 2;
            repeat <<= (int)extraBits;
        }
        repeat += repeatDelta + 3u;
        repeatDelta = repeat - oldRepeat;
        if (symbol + repeatDelta > alphabetSize)
        {
            symbol = alphabetSize;
            space = 0xFFFFF;
            return;
        }
        if (repeatCodeLen != 0)
        {
            uint last = symbol + repeatDelta;
            int next = nextSymbol[(int)repeatCodeLen];
            do
            {
                symbolListsArray[Huffman.MaxCodeLength + 1 + next] = (ushort)symbol;
                next = (int)symbol;
            }
            while (++symbol != last);
            nextSymbol[(int)repeatCodeLen] = next;
            space -= repeatDelta << (15 - (int)repeatCodeLen);
            codeLengthHisto[(int)repeatCodeLen] = (ushort)(codeLengthHisto[(int)repeatCodeLen] + repeatDelta);
        }
        else
        {
            symbol += repeatDelta;
        }
    }

    /// <summary>Reads and decodes symbol codelengths.</summary>
    /// <remarks>The bit reader is kept in locals (like the C compiler does) and written back on exit.</remarks>
    private BrotliDecoderErrorCode ReadSymbolCodeLengths(uint alphabetSize, ref BitReader br)
    {
        uint symbol = _symbol;
        uint repeat = _repeat;
        uint space = _space;
        uint prevCodeLen = _prevCodeLen;
        uint repeatCodeLen = _repeatCodeLen;
        Span<ushort> symbolListsArray = _symbolListsArray;
        Span<ushort> codeLengthHisto = _codeLengthHisto;
        Span<int> nextSymbol = _nextSymbol;
        ReadOnlySpan<HuffmanCode> table = _table;
        if (!br.Warmup())
        {
            return BrotliDecoderErrorCode.NeedsMoreInput;
        }

        ReadOnlySpan<byte> input = br.Input;
        ulong val = br.Val;
        uint bitPos = br.BitPos;
        int nextIn = br.NextIn;
        int guardIn = br.GuardIn;
        BrotliDecoderErrorCode result = BrotliDecoderErrorCode.Success;
        while (symbol < alphabetSize && space > 0)
        {
            if (nextIn >= guardIn)
            {
                _symbol = symbol;
                _repeat = repeat;
                _prevCodeLen = prevCodeLen;
                _repeatCodeLen = repeatCodeLen;
                _space = space;
                result = BrotliDecoderErrorCode.NeedsMoreInput;
                break;
            }
            FillBitWindow32(ref val, ref bitPos, ref nextIn, input);
            HuffmanCode p = table[(int)val & ((1 << Huffman.MaxCodeLengthCodeLength) - 1)];
            uint pBits = p.Bits;
            val >>= (int)pBits; // Use 1..5 bits.
            bitPos -= pBits;
            uint codeLen = p.Value; // code_len == 0..17
            if (codeLen < BrotliConstants.RepeatPreviousCodeLength)
            {
                ProcessSingleCodeLength(codeLen, ref symbol, ref repeat, ref space, ref prevCodeLen,
                    symbolListsArray, codeLengthHisto, nextSymbol);
            }
            else
            {
                // code_len == 16..17, extra_bits == 2..3
                uint extraBits = (codeLen == BrotliConstants.RepeatPreviousCodeLength) ? 2u : 3u;
                uint repeatDelta = (uint)val & ~(uint.MaxValue << (int)extraBits);
                val >>= (int)extraBits;
                bitPos -= extraBits;
                ProcessRepeatedCodeLength(codeLen, repeatDelta, alphabetSize, ref symbol, ref repeat, ref space,
                    ref prevCodeLen, ref repeatCodeLen, symbolListsArray, codeLengthHisto, nextSymbol);
            }
        }
        br.Val = val;
        br.BitPos = bitPos;
        br.NextIn = nextIn;
        if (result == BrotliDecoderErrorCode.Success)
        {
            _space = space;
        }
        return result;
    }

    private BrotliDecoderErrorCode SafeReadSymbolCodeLengths(uint alphabetSize, ref BitReader br)
    {
        bool getByte = false;
        Span<ushort> symbolListsArray = _symbolListsArray;
        Span<ushort> codeLengthHisto = _codeLengthHisto;
        Span<int> nextSymbol = _nextSymbol;
        ReadOnlySpan<HuffmanCode> table = _table;
        while (_symbol < alphabetSize && _space > 0)
        {
            uint codeLen;
            uint availableBits;
            uint bits = 0;
            if (getByte && !br.PullByte())
            {
                return BrotliDecoderErrorCode.NeedsMoreInput;
            }
            getByte = false;
            availableBits = br.AvailableBits;
            if (availableBits != 0)
            {
                bits = (uint)br.GetBitsUnmasked();
            }
            HuffmanCode p = table[(int)(bits & BitReader.BitMask(Huffman.MaxCodeLengthCodeLength))];
            if (p.Bits > availableBits)
            {
                getByte = true;
                continue;
            }
            codeLen = p.Value; // code_len == 0..17
            if (codeLen < BrotliConstants.RepeatPreviousCodeLength)
            {
                br.DropBits(p.Bits);
                ProcessSingleCodeLength(codeLen, ref _symbol, ref _repeat, ref _space, ref _prevCodeLen,
                    symbolListsArray, codeLengthHisto, nextSymbol);
            }
            else
            {
                // code_len == 16..17, extra_bits == 2..3
                uint extraBits = codeLen - 14u;
                uint repeatDelta = (bits >> p.Bits) & (uint)BitReader.BitMask(extraBits);
                if (availableBits < p.Bits + extraBits)
                {
                    getByte = true;
                    continue;
                }
                br.DropBits(p.Bits + extraBits);
                ProcessRepeatedCodeLength(codeLen, repeatDelta, alphabetSize, ref _symbol, ref _repeat, ref _space,
                    ref _prevCodeLen, ref _repeatCodeLen, symbolListsArray, codeLengthHisto, nextSymbol);
            }
        }
        return BrotliDecoderErrorCode.Success;
    }

    /// <summary>
    /// Reads and decodes 15..18 codes using static prefix code. Each code is 2..4 bits long. In total 30..72 bits
    /// are used.
    /// </summary>
    private BrotliDecoderErrorCode ReadCodeLengthCodeLengths(ref BitReader br)
    {
        uint numCodes = _repeat;
        uint space = _space;
        uint i = _subLoopCounter;
        for (; i < BrotliConstants.CodeLengthCodes; ++i)
        {
            byte codeLenIdx = CodeLengthCodeOrder[(int)i];
            uint ix;
            uint v;
            if (!br.SafeGetBits(4, out ix))
            {
                uint availableBits = br.AvailableBits;
                if (availableBits != 0)
                {
                    ix = (uint)br.GetBitsUnmasked() & 0xF;
                }
                else
                {
                    ix = 0;
                }
                if (CodeLengthPrefixLength[(int)ix] > availableBits)
                {
                    _subLoopCounter = i;
                    _repeat = numCodes;
                    _space = space;
                    _substateHuffman = BrotliRunningHuffmanState.Complex;
                    return BrotliDecoderErrorCode.NeedsMoreInput;
                }
            }
            v = CodeLengthPrefixValue[(int)ix];
            br.DropBits(CodeLengthPrefixLength[(int)ix]);
            _codeLengthCodeLengths[codeLenIdx] = (byte)v;
            if (v != 0)
            {
                space -= 32u >> (int)v;
                ++numCodes;
                ++_codeLengthHisto[(int)v];
                if (space - 1u >= 32u)
                {
                    // space is 0 or wrapped around.
                    break;
                }
            }
        }
        if (!(numCodes == 1 || space == 0))
        {
            return BrotliDecoderErrorCode.ErrorFormatClSpace;
        }
        return BrotliDecoderErrorCode.Success;
    }

    /// <summary>
    /// Decodes the Huffman tables.
    /// There are 2 scenarios:
    /// A) Huffman code contains only few symbols (1..4). Those symbols are read directly; their code lengths are
    ///    defined by the number of symbols. For this scenario 4 - 49 bits will be read.
    /// B) 2-phase decoding:
    /// B.1) Small Huffman table is decoded; it is specified with code lengths encoded with predefined entropy code.
    ///      32 - 74 bits are used.
    /// B.2) Decoded table is used to decode code lengths of symbols in resulting Huffman table. In worst case 3520
    ///      bits are read.
    /// </summary>
    private BrotliDecoderErrorCode ReadHuffmanCode(uint alphabetSizeMax, uint alphabetSizeLimit, Span<HuffmanCode> table,
        ref uint optTableSize, ref BitReader br)
    {
        // State machine.
        while (true)
        {
            switch (_substateHuffman)
            {
                case BrotliRunningHuffmanState.None:
                    if (!br.SafeReadBits(2, out _subLoopCounter))
                    {
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    // The value is used as follows:
                    // 1 for simple code;
                    // 0 for no skipping, 2 skips 2 code lengths, 3 skips 3 code lengths
                    if (_subLoopCounter != 1)
                    {
                        _space = 32;
                        _repeat = 0; // num_codes
                        ((Span<ushort>)_codeLengthHisto).Slice(0, Huffman.MaxCodeLengthCodeLength + 1).Clear();
                        ((Span<byte>)_codeLengthCodeLengths).Clear();
                        _substateHuffman = BrotliRunningHuffmanState.Complex;
                        continue;
                    }
                    goto case BrotliRunningHuffmanState.SimpleSize;

                case BrotliRunningHuffmanState.SimpleSize:
                    // Read symbols, codes & code lengths directly.
                    if (!br.SafeReadBits(2, out _symbol))
                    {
                        // num_symbols
                        _substateHuffman = BrotliRunningHuffmanState.SimpleSize;
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    _subLoopCounter = 0;
                    goto case BrotliRunningHuffmanState.SimpleRead;

                case BrotliRunningHuffmanState.SimpleRead:
                    {
                        BrotliDecoderErrorCode result = ReadSimpleHuffmanSymbols(alphabetSizeMax, alphabetSizeLimit, ref br);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            return result;
                        }
                        goto case BrotliRunningHuffmanState.SimpleBuild;
                    }

                case BrotliRunningHuffmanState.SimpleBuild:
                    {
                        uint tableSize;
                        if (_symbol == 3)
                        {
                            if (!br.SafeReadBits(1, out uint bits))
                            {
                                _substateHuffman = BrotliRunningHuffmanState.SimpleBuild;
                                return BrotliDecoderErrorCode.NeedsMoreInput;
                            }
                            _symbol += bits;
                        }
                        tableSize = Huffman.BuildSimpleHuffmanTable(table, HuffmanTableBits, _symbolListsArray, _symbol);
                        optTableSize = tableSize;
                        _substateHuffman = BrotliRunningHuffmanState.None;
                        return BrotliDecoderErrorCode.Success;
                    }

                // Decode Huffman-coded code lengths.
                case BrotliRunningHuffmanState.Complex:
                    {
                        BrotliDecoderErrorCode result = ReadCodeLengthCodeLengths(ref br);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            return result;
                        }
                        Huffman.BuildCodeLengthsHuffmanTable(_table, _codeLengthCodeLengths, _codeLengthHisto);
                        ((Span<ushort>)_codeLengthHisto).Clear();
                        Span<int> nextSymbol = _nextSymbol;
                        Span<ushort> symbolListsArray = _symbolListsArray;
                        for (int i = 0; i <= Huffman.MaxCodeLength; ++i)
                        {
                            nextSymbol[i] = i - (Huffman.MaxCodeLength + 1);
                            symbolListsArray[Huffman.MaxCodeLength + 1 + nextSymbol[i]] = 0xFFFF;
                        }

                        _symbol = 0;
                        _prevCodeLen = BrotliConstants.InitialRepeatedCodeLength;
                        _repeat = 0;
                        _repeatCodeLen = 0;
                        _space = 32768;
                        _substateHuffman = BrotliRunningHuffmanState.LengthSymbols;
                        goto case BrotliRunningHuffmanState.LengthSymbols;
                    }

                case BrotliRunningHuffmanState.LengthSymbols:
                    {
                        uint tableSize;
                        BrotliDecoderErrorCode result = ReadSymbolCodeLengths(alphabetSizeLimit, ref br);
                        if (result == BrotliDecoderErrorCode.NeedsMoreInput)
                        {
                            result = SafeReadSymbolCodeLengths(alphabetSizeLimit, ref br);
                        }
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            return result;
                        }

                        if (_space != 0)
                        {
                            return BrotliDecoderErrorCode.ErrorFormatHuffmanSpace;
                        }
                        tableSize = Huffman.BuildHuffmanTable(table, HuffmanTableBits, _symbolListsArray, _codeLengthHisto);
                        optTableSize = tableSize;
                        _substateHuffman = BrotliRunningHuffmanState.None;
                        return BrotliDecoderErrorCode.Success;
                    }

                default:
                    return BrotliDecoderErrorCode.ErrorUnreachable;
            }
        }
    }

    /// <summary>Decodes a block length by reading 3..39 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadBlockLength(ReadOnlySpan<HuffmanCode> table, ref BitReader br)
    {
        uint code = ReadSymbol(table, ref br);
        uint nbits = Prefix.PrefixCodeRangeNBits[(int)code]; // nbits == 2..24
        return Prefix.PrefixCodeRangeOffset[(int)code] + br.ReadBits24(nbits);
    }

    /// <summary>
    /// WARNING: if state is not BROTLI_STATE_READ_BLOCK_LENGTH_NONE, then reading can't be continued with
    /// ReadBlockLength.
    /// </summary>
    private bool SafeReadBlockLength(ref uint result, ReadOnlySpan<HuffmanCode> table, ref BitReader br)
    {
        uint index;
        if (_substateReadBlockLength == BrotliRunningReadBlockLengthState.None)
        {
            if (!SafeReadSymbol(table, ref br, out index))
            {
                return false;
            }
        }
        else
        {
            index = _blockLengthIndex;
        }
        {
            uint nbits = Prefix.PrefixCodeRangeNBits[(int)index];
            uint offset = Prefix.PrefixCodeRangeOffset[(int)index];
            if (!br.SafeReadBits(nbits, out uint bits))
            {
                _blockLengthIndex = index;
                _substateReadBlockLength = BrotliRunningReadBlockLengthState.Suffix;
                return false;
            }
            result = offset + bits;
            _substateReadBlockLength = BrotliRunningReadBlockLengthState.None;
            return true;
        }
    }

    /// <summary>
    /// Transform:
    /// 1) initialize list L with values 0, 1,... 255
    /// 2) For each input element X:
    /// 2.1) let Y = L[X]
    /// 2.2) remove X-th element from L
    /// 2.3) prepend Y to L
    /// 2.4) append Y to output
    /// In most cases max(Y) &lt;= 7, so most of L remains intact. To reduce the cost of initialization, we reuse L,
    /// remember the upper bound of Y values, and reinitialize only first elements in L.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InverseMoveToFrontTransform(Span<byte> v, uint vLen)
    {
        // mtf_u8 == &mtf[1] viewed as bytes, so that mtf_u8[-1] is addressable.
        const int MtfBase = 4;
        Span<byte> mtf = _mtf;

        // Reinitialize elements that could have been changed.
        uint i = 1;
        uint upperBound = _mtfUpperBound;
        // Initialize list using 4 consequent values pattern.
        uint pattern = 0x03020100;
        WriteUInt32(mtf, MtfBase, pattern);
        do
        {
            pattern += 0x04040404; // Advance all 4 values by 4.
            WriteUInt32(mtf, MtfBase + (int)(i * 4), pattern);
            i++;
        }
        while (i <= upperBound);

        // Transform the input.
        upperBound = 0;
        for (i = 0; i < vLen; ++i)
        {
            int index = v[(int)i];
            byte value = mtf[MtfBase + index];
            upperBound |= v[(int)i];
            v[(int)i] = value;
            mtf[MtfBase - 1] = value;
            // do { index--; mtf_u8[index + 1] = mtf_u8[index]; } while (index >= 0);
            mtf.Slice(MtfBase - 1, index + 1).CopyTo(mtf.Slice(MtfBase));
        }
        // Remember amount of elements to be reinitialized.
        _mtfUpperBound = upperBound >> 2;

        static void WriteUInt32(Span<byte> span, int offset, uint value) =>
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset), value);
    }

    /// <summary>Decodes a series of Huffman table using ReadHuffmanCode function.</summary>
    private BrotliDecoderErrorCode HuffmanTreeGroupDecode(HuffmanTreeGroup group, ref BitReader br)
    {
        if (_substateTreeGroup != BrotliRunningTreeGroupState.Loop)
        {
            _next = group.Base;
            _htreeIndex = 0;
            _substateTreeGroup = BrotliRunningTreeGroupState.Loop;
        }
        HuffmanCode[] codes = group.Codes!;
        Span<int> htrees = group.Htrees;
        while (_htreeIndex < group.NumHtrees)
        {
            uint tableSize = 0;
            BrotliDecoderErrorCode result = ReadHuffmanCode(group.AlphabetSizeMax, group.AlphabetSizeLimit,
                codes.AsSpan(_next), ref tableSize, ref br);
            if (result != BrotliDecoderErrorCode.Success)
            {
                return result;
            }
            htrees[_htreeIndex] = _next;
            _next += (int)tableSize;
            ++_htreeIndex;
        }
        _substateTreeGroup = BrotliRunningTreeGroupState.None;
        return BrotliDecoderErrorCode.Success;
    }

    /// <summary>
    /// Decodes a context map. Decoding is done in 4 phases:
    /// 1) Read auxiliary information (6..16 bits) and allocate memory. In case of trivial context map, decoding is
    ///    finished at this phase.
    /// 2) Decode Huffman table using ReadHuffmanCode function. This table will be used for reading context map items.
    /// 3) Read context map items; "0" values could be run-length encoded.
    /// 4) Optionally, apply InverseMoveToFront transform to the resulting map.
    /// </summary>
    /// <remarks><paramref name="contextMap"/> must have at least <paramref name="contextMapSize"/> elements.</remarks>
    private BrotliDecoderErrorCode DecodeContextMap(uint contextMapSize, ref uint numHtrees, Span<byte> contextMap, ref BitReader br)
    {
        BrotliDecoderErrorCode result;

        switch (_substateContextMap)
        {
            case BrotliRunningContextMapState.None:
                result = DecodeVarLenUint8(ref br, ref numHtrees);
                if (result != BrotliDecoderErrorCode.Success)
                {
                    return result;
                }
                numHtrees++;
                _contextIndex = 0;
                if (numHtrees <= 1)
                {
                    contextMap.Slice(0, (int)contextMapSize).Clear();
                    return BrotliDecoderErrorCode.Success;
                }
                _substateContextMap = BrotliRunningContextMapState.ReadPrefix;
                goto case BrotliRunningContextMapState.ReadPrefix;

            case BrotliRunningContextMapState.ReadPrefix:
                {
                    // In next stage ReadHuffmanCode uses at least 4 bits, so it is safe to peek 4 bits ahead.
                    if (!br.SafeGetBits(5, out uint bits))
                    {
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    if ((bits & 1) != 0)
                    {
                        // Use RLE for zeros.
                        _maxRunLengthPrefix = (bits >> 1) + 1;
                        br.DropBits(5);
                    }
                    else
                    {
                        _maxRunLengthPrefix = 0;
                        br.DropBits(1);
                    }
                    _substateContextMap = BrotliRunningContextMapState.Huffman;
                    goto case BrotliRunningContextMapState.Huffman;
                }

            case BrotliRunningContextMapState.Huffman:
                {
                    uint alphabetSize = numHtrees + _maxRunLengthPrefix;
                    uint unused = 0;
                    result = ReadHuffmanCode(alphabetSize, alphabetSize, _contextMapTable, ref unused, ref br);
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        return result;
                    }
                    _code = 0xFFFF;
                    _substateContextMap = BrotliRunningContextMapState.Decode;
                    goto case BrotliRunningContextMapState.Decode;
                }

            case BrotliRunningContextMapState.Decode:
                {
                    uint contextIndex = _contextIndex;
                    uint maxRunLengthPrefix = _maxRunLengthPrefix;
                    uint code = _code;
                    bool skipPreamble = code != 0xFFFF;
                    ReadOnlySpan<HuffmanCode> contextMapTable = _contextMapTable;
                    while (contextIndex < contextMapSize || skipPreamble)
                    {
                        if (!skipPreamble)
                        {
                            if (!SafeReadSymbol(contextMapTable, ref br, out code))
                            {
                                _code = 0xFFFF;
                                _contextIndex = contextIndex;
                                return BrotliDecoderErrorCode.NeedsMoreInput;
                            }

                            if (code == 0)
                            {
                                contextMap[(int)contextIndex++] = 0;
                                continue;
                            }
                            if (code > maxRunLengthPrefix)
                            {
                                contextMap[(int)contextIndex++] = (byte)(code - maxRunLengthPrefix);
                                continue;
                            }
                        }
                        else
                        {
                            skipPreamble = false;
                        }

                        // RLE sub-stage.
                        {
                            if (!br.SafeReadBits(code, out uint reps))
                            {
                                _code = code;
                                _contextIndex = contextIndex;
                                return BrotliDecoderErrorCode.NeedsMoreInput;
                            }
                            reps += 1u << (int)code;
                            if (contextIndex + reps > contextMapSize)
                            {
                                return BrotliDecoderErrorCode.ErrorFormatContextMapRepeat;
                            }
                            do
                            {
                                contextMap[(int)contextIndex++] = 0;
                            }
                            while (--reps != 0);
                        }
                    }
                    goto case BrotliRunningContextMapState.Transform;
                }

            case BrotliRunningContextMapState.Transform:
                {
                    if (!br.SafeReadBits(1, out uint bits))
                    {
                        _substateContextMap = BrotliRunningContextMapState.Transform;
                        return BrotliDecoderErrorCode.NeedsMoreInput;
                    }
                    if (bits != 0)
                    {
                        InverseMoveToFrontTransform(contextMap, contextMapSize);
                    }
                    _substateContextMap = BrotliRunningContextMapState.None;
                    return BrotliDecoderErrorCode.Success;
                }

            default:
                return BrotliDecoderErrorCode.ErrorUnreachable;
        }
    }

    /// <summary>Decodes a command or literal and updates block type ring-buffer. Reads 3..54 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BrotliDecoderErrorCode DecodeBlockTypeAndLength<TMode>(int treeType, ref BitReader br)
        where TMode : struct, IDecodeMode
    {
        uint maxBlockType = _numBlockTypes[treeType];
        ReadOnlySpan<HuffmanCode> typeTree = _blockTypeTrees.AsSpan(treeType * Huffman.MaxSize258);
        ReadOnlySpan<HuffmanCode> lenTree = _blockTypeTrees.AsSpan(BlockLenTreesOffset + treeType * Huffman.MaxSize26);
        uint blockType;
        if (maxBlockType <= 1)
        {
            return BrotliDecoderErrorCode.ErrorFormatBlockSwitch;
        }

        // Read 0..15 + 3..39 bits.
        if (!TMode.Safe)
        {
            blockType = ReadSymbol(typeTree, ref br);
            _blockLength[treeType] = ReadBlockLength(lenTree, ref br);
        }
        else
        {
            br.SaveState(out BitReaderState memento);
            if (!SafeReadSymbol(typeTree, ref br, out blockType))
            {
                return BrotliDecoderErrorCode.NeedsMoreInput;
            }
            if (!SafeReadBlockLength(ref _blockLength[treeType], lenTree, ref br))
            {
                _substateReadBlockLength = BrotliRunningReadBlockLengthState.None;
                br.RestoreState(memento);
                return BrotliDecoderErrorCode.NeedsMoreInput;
            }
        }

        if (blockType == 1)
        {
            blockType = _blockTypeRb[treeType * 2 + 1] + 1;
        }
        else if (blockType == 0)
        {
            blockType = _blockTypeRb[treeType * 2];
        }
        else
        {
            blockType -= 2;
        }
        if (blockType >= maxBlockType)
        {
            blockType -= maxBlockType;
        }
        _blockTypeRb[treeType * 2] = _blockTypeRb[treeType * 2 + 1];
        _blockTypeRb[treeType * 2 + 1] = blockType;
        return BrotliDecoderErrorCode.Success;
    }

    private void DetectTrivialLiteralBlockTypes()
    {
        Span<uint> trivialLiteralContexts = _trivialLiteralContexts;
        trivialLiteralContexts.Clear();
        byte[] contextMap = _contextMap!;
        for (int i = 0; i < _numBlockTypes[0]; i++)
        {
            int offset = i << BrotliConstants.LiteralContextBits;
            int error = 0;
            int sample = contextMap[offset];
            for (int j = 0; j < (1 << BrotliConstants.LiteralContextBits); j++)
            {
                error |= contextMap[offset + j] ^ sample;
            }
            if (error == 0)
            {
                trivialLiteralContexts[i >> 5] |= 1u << (i & 31);
            }
        }
    }

    private void PrepareLiteralDecoding()
    {
        uint blockType = _blockTypeRb[1];
        int contextOffset = (int)(blockType << BrotliConstants.LiteralContextBits);
        _contextMapSlice = contextOffset;
        uint trivial = _trivialLiteralContexts[(int)(blockType >> 5)];
        _trivialLiteralContext = ((trivial >> (int)(blockType & 31)) & 1) != 0;
        ReadOnlySpan<int> htrees = _literalHgroup.Htrees;
        ReadOnlySpan<byte> contextMapSlice = _contextMap.AsSpan(contextOffset, 1 << BrotliConstants.LiteralContextBits);
        _literalHtree = htrees[contextMapSlice[0]];
        int contextMode = _contextModes[(int)blockType] & 3;
        _contextLookup = contextMode << 9;
        if (!_trivialLiteralContext)
        {
            Span<int> literalTrees = _literalTreesForContexts;
            for (int i = 0; i < literalTrees.Length; i++)
            {
                literalTrees[i] = htrees[contextMapSlice[i]];
            }
        }
    }

    /// <summary>Decodes the block type and updates the state for literal context. Reads 3..54 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BrotliDecoderErrorCode DecodeLiteralBlockSwitchInternal<TMode>(ref BitReader br)
        where TMode : struct, IDecodeMode
    {
        BrotliDecoderErrorCode result = DecodeBlockTypeAndLength<TMode>(0, ref br);
        if (result != BrotliDecoderErrorCode.Success)
        {
            return result;
        }
        PrepareLiteralDecoding();
        return BrotliDecoderErrorCode.Success;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode DecodeLiteralBlockSwitch(ref BitReader br) => DecodeLiteralBlockSwitchInternal<FastDecode>(ref br);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode SafeDecodeLiteralBlockSwitch(ref BitReader br) => DecodeLiteralBlockSwitchInternal<SafeDecode>(ref br);

    /// <summary>Block switch for insert/copy length. Reads 3..54 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BrotliDecoderErrorCode DecodeCommandBlockSwitchInternal<TMode>(ref BitReader br)
        where TMode : struct, IDecodeMode
    {
        BrotliDecoderErrorCode result = DecodeBlockTypeAndLength<TMode>(1, ref br);
        if (result != BrotliDecoderErrorCode.Success)
        {
            return result;
        }
        _htreeCommand = _insertCopyHgroup.Htrees[(int)_blockTypeRb[3]];
        return BrotliDecoderErrorCode.Success;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode DecodeCommandBlockSwitch(ref BitReader br) => DecodeCommandBlockSwitchInternal<FastDecode>(ref br);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode SafeDecodeCommandBlockSwitch(ref BitReader br) => DecodeCommandBlockSwitchInternal<SafeDecode>(ref br);

    /// <summary>Block switch for distance codes. Reads 3..54 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BrotliDecoderErrorCode DecodeDistanceBlockSwitchInternal<TMode>(ref BitReader br)
        where TMode : struct, IDecodeMode
    {
        BrotliDecoderErrorCode result = DecodeBlockTypeAndLength<TMode>(2, ref br);
        if (result != BrotliDecoderErrorCode.Success)
        {
            return result;
        }
        _distContextMapSlice = (int)(_blockTypeRb[5] << BrotliConstants.DistanceContextBits);
        PrepareDistanceTrees();
        _distTreeOffset = _distanceTreesForContexts[_distanceContext & 3];
        return BrotliDecoderErrorCode.Success;
    }

    /// <summary>
    /// Fills <see cref="_distanceTreesForContexts"/> (htrees[dist_context_map_slice[context]]) for the current
    /// distance block type.
    /// </summary>
    private void PrepareDistanceTrees()
    {
        ReadOnlySpan<int> htrees = _distanceHgroup.Htrees;
        ReadOnlySpan<byte> slice = ((ReadOnlySpan<byte>)_distContextMap).Slice(_distContextMapSlice, 1 << BrotliConstants.DistanceContextBits);
        Span<int> trees = _distanceTreesForContexts;
        for (int i = 0; i < trees.Length; i++)
        {
            trees[i] = htrees[slice[i]];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode DecodeDistanceBlockSwitch(ref BitReader br) => DecodeDistanceBlockSwitchInternal<FastDecode>(ref br);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode SafeDecodeDistanceBlockSwitch(ref BitReader br) => DecodeDistanceBlockSwitchInternal<SafeDecode>(ref br);

    private nuint UnwrittenBytes(bool wrap)
    {
        nuint pos = wrap && _pos > _ringbufferSize ? (nuint)_ringbufferSize : (nuint)_pos;
        nuint partialPosRb = (_rbRoundtrips * (nuint)_ringbufferSize) + pos;
        return partialPosRb - _partialPosOut;
    }

    /// <summary>
    /// Dumps output. Returns BROTLI_DECODER_NEEDS_MORE_OUTPUT only if there is more output to push and either
    /// ring-buffer is as big as window size, or <paramref name="force"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode WriteRingBuffer(Span<byte> output, ref int outPos, bool force)
    {
        int start = (int)(_partialPosOut & (nuint)_ringbufferMask);
        nuint toWrite = UnwrittenBytes(true);
        nuint numWritten = (nuint)(output.Length - outPos);
        if (numWritten > toWrite)
        {
            numWritten = toWrite;
        }
        if (_metaBlockRemainingLen < 0)
        {
            return BrotliDecoderErrorCode.ErrorFormatBlockLength1;
        }
        if (numWritten != 0)
        {
            _ringbuffer.AsSpan(start, (int)numWritten).CopyTo(output.Slice(outPos));
            outPos += (int)numWritten;
        }
        _partialPosOut += numWritten;
        if (numWritten < toWrite)
        {
            if (_ringbufferSize == (1 << _windowBits) || force)
            {
                return BrotliDecoderErrorCode.NeedsMoreOutput;
            }
            else
            {
                return BrotliDecoderErrorCode.Success;
            }
        }
        // Wrap ring buffer only if it has reached its maximal size.
        if (_ringbufferSize == (1 << _windowBits) && _pos >= _ringbufferSize)
        {
            _pos -= _ringbufferSize;
            _rbRoundtrips++;
            _shouldWrapRingbuffer = _pos != 0;
        }
        return BrotliDecoderErrorCode.Success;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WrapRingBuffer()
    {
        if (_shouldWrapRingbuffer)
        {
            _ringbuffer.AsSpan(_ringbufferSize, _pos).CopyTo(_ringbuffer);
            _shouldWrapRingbuffer = false;
        }
    }

    /// <summary>
    /// Allocates ring-buffer. s->ringbuffer_size MUST be updated by BrotliCalculateRingBufferSize before this
    /// function is called. Last two bytes of ring-buffer are initialized to 0, so context calculation could be done
    /// uniformly for the first two and all other positions.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool EnsureRingBuffer()
    {
        byte[]? oldRingbuffer = _ringbuffer;
        if (_ringbufferSize == _newRingbufferSize)
        {
            return true;
        }

        byte[] ringbuffer = ArrayPool<byte>.Shared.Rent(_newRingbufferSize + RingBufferWriteAheadSlack);
        _ringbuffer = ringbuffer;
        ringbuffer[_newRingbufferSize - 2] = 0;
        ringbuffer[_newRingbufferSize - 1] = 0;

        if (oldRingbuffer is not null)
        {
            oldRingbuffer.AsSpan(0, _pos).CopyTo(ringbuffer);
            ArrayPool<byte>.Shared.Return(oldRingbuffer);
        }

        _ringbufferSize = _newRingbufferSize;
        _ringbufferMask = _newRingbufferSize - 1;

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode SkipMetadataBlock(ref BitReader br)
    {
        int nbytes;

        if (_metaBlockRemainingLen == 0)
        {
            return BrotliDecoderErrorCode.Success;
        }

        Debug.Assert((br.AvailableBits & 7) == 0);

        // Drain accumulator.
        if (br.AvailableBits >= 8)
        {
            Span<byte> buffer = stackalloc byte[8];
            nbytes = (int)br.AvailableBits >> 3;
            Debug.Assert(nbytes <= 8);
            if (nbytes > _metaBlockRemainingLen)
            {
                nbytes = _metaBlockRemainingLen;
            }
            br.CopyBytes(buffer, nbytes);
            _metaBlockRemainingLen -= nbytes;
            if (_metaBlockRemainingLen == 0)
            {
                return BrotliDecoderErrorCode.Success;
            }
        }

        // Direct access to metadata is possible.
        nbytes = br.GetRemainingBytes();
        if (nbytes > _metaBlockRemainingLen)
        {
            nbytes = _metaBlockRemainingLen;
        }
        if (nbytes > 0)
        {
            br.DropBytes(nbytes);
            _metaBlockRemainingLen -= nbytes;
            if (_metaBlockRemainingLen == 0)
            {
                return BrotliDecoderErrorCode.Success;
            }
        }

        Debug.Assert(br.GetRemainingBytes() == 0);

        return BrotliDecoderErrorCode.NeedsMoreInput;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode CopyUncompressedBlockToOutput(Span<byte> output, ref int outPos, ref BitReader br)
    {
        if (!EnsureRingBuffer())
        {
            return BrotliDecoderErrorCode.ErrorAllocRingBuffer1;
        }

        // State machine
        while (true)
        {
            switch (_substateUncompressed)
            {
                case BrotliRunningUncompressedState.None:
                    {
                        int nbytes = br.GetRemainingBytes();
                        if (nbytes > _metaBlockRemainingLen)
                        {
                            nbytes = _metaBlockRemainingLen;
                        }
                        if (_pos + nbytes > _ringbufferSize)
                        {
                            nbytes = _ringbufferSize - _pos;
                        }
                        // Copy remaining bytes from s->br.buf_ to ring-buffer.
                        br.CopyBytes(_ringbuffer.AsSpan(_pos), nbytes);
                        _pos += nbytes;
                        _metaBlockRemainingLen -= nbytes;
                        if (_pos < (1 << _windowBits))
                        {
                            if (_metaBlockRemainingLen == 0)
                            {
                                return BrotliDecoderErrorCode.Success;
                            }
                            return BrotliDecoderErrorCode.NeedsMoreInput;
                        }
                        _substateUncompressed = BrotliRunningUncompressedState.Write;
                        goto case BrotliRunningUncompressedState.Write;
                    }

                case BrotliRunningUncompressedState.Write:
                    {
                        BrotliDecoderErrorCode result = WriteRingBuffer(output, ref outPos, false);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            return result;
                        }
                        if (_ringbufferSize == 1 << _windowBits)
                        {
                            _maxDistance = _maxBackwardDistance;
                        }
                        _substateUncompressed = BrotliRunningUncompressedState.None;
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Calculates the smallest feasible ring buffer. If we know the data size is small, do not allocate more ring
    /// buffer size than needed to reduce memory usage. When this method is called, metablock size and flags MUST be
    /// decoded.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CalculateRingBufferSize()
    {
        int windowSize = 1 << _windowBits;
        int newRingbufferSize = windowSize;
        // We need at least 2 bytes of ring buffer size to get the last two bytes for context from there.
        int minSize = _ringbufferSize != 0 ? _ringbufferSize : 1024;
        int outputSize;

        // If maximum is already reached, no further extension is retired.
        if (_ringbufferSize == windowSize)
        {
            return;
        }

        // Metadata blocks does not touch ring buffer.
        if (_isMetadata)
        {
            return;
        }

        if (_ringbuffer is null)
        {
            outputSize = 0;
        }
        else
        {
            outputSize = _pos;
        }
        outputSize += _metaBlockRemainingLen;
        minSize = minSize < outputSize ? outputSize : minSize;

        if (_cannyRingbufferAllocation)
        {
            // Reduce ring buffer size to save memory when server is unscrupulous. In worst case memory usage might
            // be 1.5x bigger for a short period of ring buffer reallocation.
            while ((newRingbufferSize >> 1) >= minSize)
            {
                newRingbufferSize >>= 1;
            }
        }

        _newRingbufferSize = newRingbufferSize;
    }

    /// <summary>Reads 1..256 2-bit context modes.</summary>
    private BrotliDecoderErrorCode ReadContextModes(ref BitReader br)
    {
        int i = _loopCounter;
        Span<byte> contextModes = _contextModes;

        while (i < (int)_numBlockTypes[0])
        {
            if (!br.SafeReadBits(2, out uint bits))
            {
                _loopCounter = i;
                return BrotliDecoderErrorCode.NeedsMoreInput;
            }
            contextModes[i] = (byte)bits;
            i++;
        }
        return BrotliDecoderErrorCode.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TakeDistanceFromRingBuffer()
    {
        int distanceCode = _distanceCode;
        int offset = distanceCode - 3;
        if (distanceCode <= 3)
        {
            // Compensate double distance-ring-buffer roll for dictionary items.
            _distanceContext = 1 >> distanceCode;
            _distanceCode = _distRb[(_distRbIdx - offset) & 3];
            _distRbIdx -= _distanceContext;
        }
        else
        {
            int indexDelta = 3;
            int delta;
            int baseValue = distanceCode - 10;
            if (distanceCode < 10)
            {
                baseValue = distanceCode - 4;
            }
            else
            {
                indexDelta = 2;
            }
            // Unpack one of six 4-bit values.
            delta = ((0x605142 >> (4 * baseValue)) & 0xF) - 3;
            distanceCode = _distRb[(_distRbIdx + indexDelta) & 0x3] + delta;
            if (distanceCode <= 0)
            {
                // A huge distance will cause a BROTLI_FAILURE() soon. This is a little faster than failing here.
                distanceCode = 0x7FFFFFFF;
            }
            _distanceCode = distanceCode;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SafeReadBits(ref BitReader br, uint nBits, out uint val)
    {
        if (nBits != 0)
        {
            return br.SafeReadBits(nBits, out val);
        }
        else
        {
            val = 0;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SafeReadBits32(ref BitReader br, uint nBits, out uint val)
    {
        if (nBits != 0)
        {
            return br.SafeReadBits32(nBits, out val);
        }
        else
        {
            val = 0;
            return true;
        }
    }

    /// <summary>
    /// Calculates distance lookup table (see RFC 7932 Section 4 and the comments in decode.c).
    /// NB: it is possible to have all 64 tables precalculated.
    /// </summary>
    private void CalculateDistanceLut()
    {
        Span<byte> distExtraBits = _distExtraBits;
        Span<uint> distOffset = _distOffset;
        uint npostfix = _distancePostfixBits;
        uint ndirect = _numDirectDistanceCodes;
        uint alphabetSizeLimit = _distanceHgroup.AlphabetSizeLimit;
        uint postfix = 1u << (int)npostfix;
        uint j;
        uint bits = 1;
        uint half = 0;

        // Skip short codes.
        int i = BrotliConstants.NumDistanceShortCodes;

        // Fill direct codes.
        for (j = 0; j < ndirect; ++j)
        {
            distExtraBits[i] = 0;
            distOffset[i] = j + 1;
            ++i;
        }

        // Fill regular distance codes.
        while (i < alphabetSizeLimit)
        {
            uint baseValue = ndirect + ((((2 + half) << (int)bits) - 4) << (int)npostfix) + 1;
            // Always fill the complete group.
            for (j = 0; j < postfix; ++j)
            {
                distExtraBits[i] = (byte)bits;
                distOffset[i] = baseValue + j;
                ++i;
            }
            bits += half;
            half ^= 1;
        }
    }

    /// <summary>SafeReadDistance (ReadDistanceInternal with safe == 1). Precondition: s->distance_code &lt; 0.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SafeReadDistance(ref BitReader br)
    {
        ReadOnlySpan<HuffmanCode> distanceTree = _distanceHgroup.Codes.AsSpan(_distTreeOffset);
        br.SaveState(out BitReaderState memento);
        if (!SafeReadSymbol(distanceTree, ref br, out uint code))
        {
            return false;
        }
        --_blockLength[2];
        // Convert the distance code to the actual distance by possibly looking up past distances from the s->dist_rb.
        _distanceContext = 0;
        if ((code & ~0xFu) == 0)
        {
            _distanceCode = (int)code;
            TakeDistanceFromRingBuffer();
            return true;
        }
        if (!SafeReadBits32(ref br, _distExtraBits[(int)code], out uint bits))
        {
            ++_blockLength[2];
            br.RestoreState(memento);
            return false;
        }
        _distanceCode = (int)(_distOffset[(int)code] + (bits << (int)_distancePostfixBits));
        return true;
    }

    /// <summary>SafeReadCommand (ReadCommandInternal with safe == 1).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SafeReadCommand(ref BitReader br, ref int insertLength)
    {
        ReadOnlySpan<HuffmanCode> commandTree = _insertCopyHgroup.Codes.AsSpan(_htreeCommand);
        br.SaveState(out BitReaderState memento);
        if (!SafeReadSymbol(commandTree, ref br, out uint cmdCode))
        {
            return false;
        }
        CmdLutElement v = Prefix.GetCmdLut(cmdCode);
        _distanceCode = v.DistanceCode;
        _distanceContext = v.Context;
        _distTreeOffset = _distanceTreesForContexts[_distanceContext & 3];
        insertLength = v.InsertLenOffset;
        if (!SafeReadBits(ref br, v.InsertLenExtraBits, out uint insertLenExtra) ||
            !SafeReadBits(ref br, v.CopyLenExtraBits, out uint copyLength))
        {
            br.RestoreState(memento);
            return false;
        }
        _copyLength = (int)copyLength + v.CopyLenOffset;
        --_blockLength[1];
        insertLength += (int)insertLenExtra;
        return true;
    }

    /// <summary>
    /// BrotliDecoderDecompressStream. <paramref name="nextIn"/>/<paramref name="availableIn"/> mirror C's
    /// *next_in / *available_in (offset into <paramref name="input"/> and remaining length);
    /// <paramref name="outPos"/> mirrors *next_out (available_out == output.Length - outPos).
    /// </summary>
    /// <remarks>
    /// Invariant: input stream is never overconsumed:
    /// - invalid input implies that the whole stream is invalid -> any amount of input could be read and discarded
    /// - when result is "needs more input", then at least one more byte is REQUIRED to complete decoding; all input
    ///   data MUST be consumed by decoder, so client could swap the input buffer
    /// - when result is "needs more output" decoder MUST ensure that it doesn't hold more than 7 bits in bit reader;
    ///   this saves client from swapping input buffer ahead of time
    /// - when result is "success" decoder MUST return all unused data back to input buffer; this is possible because
    ///   the invariant is held on enter
    /// </remarks>
    private BrotliDecoderResult DecompressStreamCore(ref BitReader br, ReadOnlySpan<byte> input, ref int nextIn, ref int availableIn,
        Span<byte> output, ref int outPos)
    {
        BrotliDecoderErrorCode result = BrotliDecoderErrorCode.Success;
        Span<byte> buffer = _buffer;

        // Do not try to process further in a case of unrecoverable error.
        if (_errorCode < 0)
        {
            return BrotliDecoderResult.Error;
        }
        if (_bufferLength == 0)
        {
            // Just connect bit reader to input stream.
            br.SetInput(input, nextIn, availableIn);
        }
        else
        {
            // At least one byte of input is required. More than one byte of input may be required to complete the
            // transaction -> reading more data must be done in a loop -> do it in a main loop.
            result = BrotliDecoderErrorCode.NeedsMoreInput;
            br.SetInput(buffer, 0, _bufferLength);
        }

        // State machine
        while (true)
        {
            if (result != BrotliDecoderErrorCode.Success)
            {
                // Error, needs more input/output.
                if (result == BrotliDecoderErrorCode.NeedsMoreInput)
                {
                    if (_ringbuffer is not null)
                    {
                        // Pro-actively push output.
                        BrotliDecoderErrorCode intermediateResult = WriteRingBuffer(output, ref outPos, true);
                        // WriteRingBuffer checks s->meta_block_remaining_len validity.
                        if ((int)intermediateResult < 0)
                        {
                            result = intermediateResult;
                            break;
                        }
                    }
                    if (_bufferLength != 0)
                    {
                        // Used with internal buffer.
                        if (br.NextIn == br.LastIn)
                        {
                            // Successfully finished read transaction. Accumulator contains less than 8 bits, because
                            // internal buffer is expanded byte-by-byte until it is enough to complete read.
                            _bufferLength = 0;
                            // Switch to input stream and restart.
                            result = BrotliDecoderErrorCode.Success;
                            br.SetInput(input, nextIn, availableIn);
                            continue;
                        }
                        else if (availableIn != 0)
                        {
                            // Not enough data in buffer, but can take one more byte from input stream.
                            result = BrotliDecoderErrorCode.Success;
                            Debug.Assert(_bufferLength < 8);
                            buffer[_bufferLength] = input[nextIn];
                            _bufferLength++;
                            br.SetInput(buffer, 0, _bufferLength);
                            nextIn++;
                            availableIn--;
                            // Retry with more data in buffer.
                            continue;
                        }
                        // Can't finish reading and no more input.
                        break;
                    }
                    else
                    {
                        // Input stream doesn't contain enough input. Copy tail to internal buffer and return.
                        nextIn = br.NextIn;
                        availableIn = br.AvailIn;
                        while (availableIn != 0)
                        {
                            buffer[_bufferLength] = input[nextIn];
                            _bufferLength++;
                            nextIn++;
                            availableIn--;
                        }
                        break;
                    }
                    // Unreachable.
                }

                // Fail or needs more output.

                if (_bufferLength != 0)
                {
                    // Just consumed the buffered input and produced some output. Otherwise it would result in
                    // "needs more input". Reset internal buffer.
                    _bufferLength = 0;
                }
                else
                {
                    // Using input stream in last iteration. When decoder switches to input stream it has less than
                    // 8 bits in accumulator, so it is safe to return unused accumulator bits there.
                    br.Unload();
                    availableIn = br.AvailIn;
                    nextIn = br.NextIn;
                }
                break;
            }

            switch (_state)
            {
                case BrotliRunningState.Uninited:
                    // Prepare to the first read.
                    if (!br.Warmup())
                    {
                        result = BrotliDecoderErrorCode.NeedsMoreInput;
                        break;
                    }
                    // Decode window size.
                    result = DecodeWindowBits(ref br); // Reads 1..8 bits.
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        break;
                    }
                    _state = BrotliRunningState.Initialize;
                    break;

                case BrotliRunningState.Initialize:
                    // Maximum distance, see section 9.1. of the spec.
                    _maxBackwardDistance = (1 << _windowBits) - BrotliConstants.WindowGap;

                    // Allocate memory for both block_type_trees and block_len_trees.
                    EnsureArray(ref _blockTypeTrees, BlockTypeTreesSize);

                    _state = BrotliRunningState.MetablockBegin;
                    goto case BrotliRunningState.MetablockBegin;

                case BrotliRunningState.MetablockBegin:
                    MetablockBegin();
                    _state = BrotliRunningState.MetablockHeader;
                    goto case BrotliRunningState.MetablockHeader;

                case BrotliRunningState.MetablockHeader:
                    result = DecodeMetaBlockLength(ref br); // Reads 2 - 31 bits.
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        break;
                    }
                    Debug.Assert(_metaBlockRemainingLen <= (int)BlockSizeCap);
                    if (_isMetadata || _isUncompressed)
                    {
                        if (!br.JumpToByteBoundary())
                        {
                            result = BrotliDecoderErrorCode.ErrorFormatPadding1;
                            break;
                        }
                    }
                    if (_isMetadata)
                    {
                        _state = BrotliRunningState.Metadata;
                        break;
                    }
                    if (_metaBlockRemainingLen == 0)
                    {
                        _state = BrotliRunningState.MetablockDone;
                        break;
                    }
                    CalculateRingBufferSize();
                    if (_isUncompressed)
                    {
                        _state = BrotliRunningState.Uncompressed;
                        break;
                    }
                    _state = BrotliRunningState.BeforeCompressedMetablockHeader;
                    goto case BrotliRunningState.BeforeCompressedMetablockHeader;

                case BrotliRunningState.BeforeCompressedMetablockHeader:
                    _loopCounter = 0;
                    // Initialize compressed metablock header arena.
                    _subLoopCounter = 0;
                    _substateHuffman = BrotliRunningHuffmanState.None;
                    _substateTreeGroup = BrotliRunningTreeGroupState.None;
                    _substateContextMap = BrotliRunningContextMapState.None;
                    _state = BrotliRunningState.HuffmanCode0;
                    goto case BrotliRunningState.HuffmanCode0;

                case BrotliRunningState.HuffmanCode0:
                    if (_loopCounter >= 3)
                    {
                        _state = BrotliRunningState.MetablockHeader2;
                        break;
                    }
                    // Reads 1..11 bits.
                    result = DecodeVarLenUint8(ref br, ref _numBlockTypes[_loopCounter]);
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        break;
                    }
                    _numBlockTypes[_loopCounter]++;
                    if (_numBlockTypes[_loopCounter] < 2)
                    {
                        _loopCounter++;
                        break;
                    }
                    _state = BrotliRunningState.HuffmanCode1;
                    goto case BrotliRunningState.HuffmanCode1;

                case BrotliRunningState.HuffmanCode1:
                    {
                        uint alphabetSize = _numBlockTypes[_loopCounter] + 2;
                        int treeOffset = _loopCounter * Huffman.MaxSize258;
                        uint unused = 0;
                        result = ReadHuffmanCode(alphabetSize, alphabetSize, _blockTypeTrees.AsSpan(treeOffset), ref unused, ref br);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            break;
                        }
                        _state = BrotliRunningState.HuffmanCode2;
                        goto case BrotliRunningState.HuffmanCode2;
                    }

                case BrotliRunningState.HuffmanCode2:
                    {
                        uint alphabetSize = BrotliConstants.NumBlockLenSymbols;
                        int treeOffset = _loopCounter * Huffman.MaxSize26;
                        uint unused = 0;
                        result = ReadHuffmanCode(alphabetSize, alphabetSize, _blockTypeTrees.AsSpan(BlockLenTreesOffset + treeOffset), ref unused, ref br);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            break;
                        }
                        _state = BrotliRunningState.HuffmanCode3;
                        goto case BrotliRunningState.HuffmanCode3;
                    }

                case BrotliRunningState.HuffmanCode3:
                    {
                        int treeOffset = _loopCounter * Huffman.MaxSize26;
                        if (!SafeReadBlockLength(ref _blockLength[_loopCounter], _blockTypeTrees.AsSpan(BlockLenTreesOffset + treeOffset), ref br))
                        {
                            result = BrotliDecoderErrorCode.NeedsMoreInput;
                            break;
                        }
                        _loopCounter++;
                        _state = BrotliRunningState.HuffmanCode0;
                        break;
                    }

                case BrotliRunningState.Uncompressed:
                    {
                        result = CopyUncompressedBlockToOutput(output, ref outPos, ref br);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            break;
                        }
                        _state = BrotliRunningState.MetablockDone;
                        break;
                    }

                case BrotliRunningState.Metadata:
                    result = SkipMetadataBlock(ref br);
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        break;
                    }
                    _state = BrotliRunningState.MetablockDone;
                    break;

                case BrotliRunningState.MetablockHeader2:
                    {
                        if (!br.SafeReadBits(6, out uint bits))
                        {
                            result = BrotliDecoderErrorCode.NeedsMoreInput;
                            break;
                        }
                        _distancePostfixBits = bits & (uint)BitReader.BitMask(2);
                        bits >>= 2;
                        _numDirectDistanceCodes = bits << (int)_distancePostfixBits;
                        _loopCounter = 0;
                        _state = BrotliRunningState.ContextModes;
                        goto case BrotliRunningState.ContextModes;
                    }

                case BrotliRunningState.ContextModes:
                    result = ReadContextModes(ref br);
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        break;
                    }
                    _state = BrotliRunningState.ContextMap1;
                    goto case BrotliRunningState.ContextMap1;

                case BrotliRunningState.ContextMap1:
                    {
                        int contextMapSize = (int)(_numBlockTypes[0] << BrotliConstants.LiteralContextBits);
                        if (_contextMap is null || _contextMap.Length < contextMapSize)
                        {
                            _contextMap = new byte[contextMapSize];
                        }
                    }
                    result = DecodeContextMap(_numBlockTypes[0] << BrotliConstants.LiteralContextBits,
                        ref _numLiteralHtrees, _contextMap, ref br);
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        break;
                    }
                    DetectTrivialLiteralBlockTypes();
                    _state = BrotliRunningState.ContextMap2;
                    goto case BrotliRunningState.ContextMap2;

                case BrotliRunningState.ContextMap2:
                    {
                        uint npostfix = _distancePostfixBits;
                        uint ndirect = _numDirectDistanceCodes;
                        uint distanceAlphabetSizeMax = BrotliConstants.DistanceAlphabetSize(npostfix, ndirect, BrotliConstants.MaxDistanceBits);
                        uint distanceAlphabetSizeLimit = distanceAlphabetSizeMax;
                        result = DecodeContextMap(_numBlockTypes[2] << BrotliConstants.DistanceContextBits,
                            ref _numDistHtrees, _distContextMap, ref br);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            break;
                        }
                        HuffmanTreeGroupsInit(distanceAlphabetSizeMax, distanceAlphabetSizeLimit);
                        _loopCounter = 0;
                        _state = BrotliRunningState.TreeGroup;
                        goto case BrotliRunningState.TreeGroup;
                    }

                case BrotliRunningState.TreeGroup:
                    {
                        HuffmanTreeGroup hgroup;
                        switch (_loopCounter)
                        {
                            case 0: hgroup = _literalHgroup; break;
                            case 1: hgroup = _insertCopyHgroup; break;
                            case 2: hgroup = _distanceHgroup; break;
                            default: return SaveErrorCode(BrotliDecoderErrorCode.ErrorUnreachable, ref br);
                        }
                        result = HuffmanTreeGroupDecode(hgroup, ref br);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            break;
                        }
                        _loopCounter++;
                        if (_loopCounter < 3)
                        {
                            break;
                        }
                        _state = BrotliRunningState.BeforeCompressedMetablockBody;
                        goto case BrotliRunningState.BeforeCompressedMetablockBody;
                    }

                case BrotliRunningState.BeforeCompressedMetablockBody:
                    PrepareLiteralDecoding();
                    _distContextMapSlice = 0;
                    PrepareDistanceTrees();
                    _htreeCommand = _insertCopyHgroup.Htrees[0];
                    if (!EnsureRingBuffer())
                    {
                        result = BrotliDecoderErrorCode.ErrorAllocRingBuffer2;
                        break;
                    }
                    CalculateDistanceLut();
                    _state = BrotliRunningState.CommandBegin;
                    goto case BrotliRunningState.CommandBegin;

                case BrotliRunningState.CommandBegin:
                case BrotliRunningState.CommandInner:
                case BrotliRunningState.CommandPostDecodeLiterals:
                case BrotliRunningState.CommandPostWrapCopy:
                    result = ProcessCommands(ref br);
                    if (result == BrotliDecoderErrorCode.NeedsMoreInput)
                    {
                        result = SafeProcessCommands(ref br);
                    }
                    break;

                case BrotliRunningState.CommandInnerWrite:
                case BrotliRunningState.CommandPostWrite1:
                case BrotliRunningState.CommandPostWrite2:
                    result = WriteRingBuffer(output, ref outPos, false);
                    if (result != BrotliDecoderErrorCode.Success)
                    {
                        break;
                    }
                    WrapRingBuffer();
                    if (_ringbufferSize == 1 << _windowBits)
                    {
                        _maxDistance = _maxBackwardDistance;
                    }
                    if (_state == BrotliRunningState.CommandPostWrite1)
                    {
                        if (_metaBlockRemainingLen == 0)
                        {
                            // Next metablock, if any.
                            _state = BrotliRunningState.MetablockDone;
                        }
                        else
                        {
                            _state = BrotliRunningState.CommandBegin;
                        }
                        break;
                    }
                    else if (_state == BrotliRunningState.CommandPostWrite2)
                    {
                        _state = BrotliRunningState.CommandPostWrapCopy;
                    }
                    else
                    {
                        // BROTLI_STATE_COMMAND_INNER_WRITE
                        if (_loopCounter == 0)
                        {
                            if (_metaBlockRemainingLen == 0)
                            {
                                _state = BrotliRunningState.MetablockDone;
                            }
                            else
                            {
                                _state = BrotliRunningState.CommandPostDecodeLiterals;
                            }
                            break;
                        }
                        _state = BrotliRunningState.CommandInner;
                    }
                    break;

                case BrotliRunningState.MetablockDone:
                    if (_metaBlockRemainingLen < 0)
                    {
                        result = BrotliDecoderErrorCode.ErrorFormatBlockLength2;
                        break;
                    }
                    CleanupAfterMetablock();
                    if (!_isLastMetablock)
                    {
                        _state = BrotliRunningState.MetablockBegin;
                        break;
                    }
                    if (!br.JumpToByteBoundary())
                    {
                        result = BrotliDecoderErrorCode.ErrorFormatPadding2;
                        break;
                    }
                    if (_bufferLength == 0)
                    {
                        br.Unload();
                        availableIn = br.AvailIn;
                        nextIn = br.NextIn;
                    }
                    _state = BrotliRunningState.Done;
                    goto case BrotliRunningState.Done;

                case BrotliRunningState.Done:
                    if (_ringbuffer is not null)
                    {
                        result = WriteRingBuffer(output, ref outPos, true);
                        if (result != BrotliDecoderErrorCode.Success)
                        {
                            break;
                        }
                    }
                    return SaveErrorCode(result, ref br);

                default:
                    // BROTLI_STATE_LARGE_WINDOW_BITS is unreachable: large window streams are not supported.
                    result = BrotliDecoderErrorCode.ErrorUnreachable;
                    break;
            }
        }
        return SaveErrorCode(result, ref br);
    }
}
