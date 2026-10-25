// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/dec/decode.c
// (ProcessCommandsInternal with safe == 0, hand-specialized for performance).

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

internal sealed partial class BrotliDecoderState
{
    // Bit reader primitives operating on locals (val = br->val_, bitPos = br->bit_pos_, nextIn = br->next_in).
    // They mirror BrotliFillBitWindow for the 64-bit / fast unaligned read configuration.

    /// <summary>BrotliFillBitWindow(br, n) for constant n &lt;= 8.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillBitWindow8(ref ulong val, ref uint bitPos, ref int nextIn, ReadOnlySpan<byte> input)
    {
        if (bitPos <= 8)
        {
            val |= BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(nextIn, 8)) << (int)bitPos;
            bitPos += 56;
            nextIn += 7;
        }
    }

    /// <summary>BrotliFillBitWindow(br, n) for constant n &lt;= 16.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillBitWindow16(ref ulong val, ref uint bitPos, ref int nextIn, ReadOnlySpan<byte> input)
    {
        if (bitPos <= 16)
        {
            val |= BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(nextIn, 8)) << (int)bitPos;
            bitPos += 48;
            nextIn += 6;
        }
    }

    /// <summary>BrotliFillBitWindow(br, n) for non-constant n (up to 32 bits).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillBitWindow32(ref ulong val, ref uint bitPos, ref int nextIn, ReadOnlySpan<byte> input)
    {
        if (bitPos <= 32)
        {
            val |= (ulong)BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(nextIn, 4)) << (int)bitPos;
            bitPos += 32;
            nextIn += BitReader.ShortFillBitWindowRead;
        }
    }

    /// <summary>BrotliReadBits24 / BrotliReadBits32 (64-bit build).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadBitsFast(uint nBits, ref ulong val, ref uint bitPos, ref int nextIn, ReadOnlySpan<byte> input)
    {
        FillBitWindow32(ref val, ref bitPos, ref nextIn, input);
        uint result = (uint)val & ~(uint.MaxValue << (int)nBits);
        val >>= (int)nBits;
        bitPos -= nBits;
        return result;
    }

    /// <summary>ReadSymbol: reads and decodes the next Huffman code from the table at <paramref name="table"/> in <paramref name="codes"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadSymbolFast(HuffmanCode[] codes, int table, ref ulong val, ref uint bitPos, ref int nextIn, ReadOnlySpan<byte> input)
    {
        FillBitWindow16(ref val, ref bitPos, ref nextIn, input);
        ulong bits = val;
        int index = table + (int)(bits & HuffmanTableMask);
        HuffmanCode code = codes[index];
        uint codeBits = code.Bits;
        if (codeBits > HuffmanTableBits)
        {
            uint nbits = codeBits - HuffmanTableBits;
            index += code.Value + (int)((uint)(bits >> HuffmanTableBits) & ~(uint.MaxValue << (int)nbits));
            code = codes[index];
            codeBits = (uint)code.Bits + HuffmanTableBits;
        }
        val = bits >> (int)codeBits;
        bitPos -= codeBits;
        return code.Value;
    }

    /// <summary>
    /// Copies <paramref name="length"/> bytes from <paramref name="src"/> to <paramref name="dst"/> where
    /// <paramref name="src"/> &lt; <paramref name="dst"/> &lt; <paramref name="src"/> + <paramref name="length"/>,
    /// with the semantics of a forward byte-by-byte copy (i.e. the pattern of dst - src bytes is repeated).
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyOverlapping(byte[] ringbuffer, int src, int dst, int length)
    {
        int distance = dst - src;
        if (distance == 1)
        {
            ringbuffer.AsSpan(dst, length).Fill(ringbuffer[src]);
            return;
        }
        // [src, dst) is a whole number of pattern periods; doubling it keeps that property.
        while (length > 0)
        {
            int n = Math.Min(distance, length);
            ringbuffer.AsSpan(src, n).CopyTo(ringbuffer.AsSpan(dst, n));
            dst += n;
            length -= n;
            distance += n;
        }
    }

    /// <summary>Selects the literal decoding loop of <see cref="ProcessCommandsFast{TLiteralMode}"/>.</summary>
    private interface ILiteralMode
    {
        static abstract bool TrivialContext { get; }
    }

    /// <summary>The literal context map histogram type always matches the block type (_trivialLiteralContext).</summary>
    private readonly struct TrivialLiteralContext : ILiteralMode
    {
        public static bool TrivialContext => true;
    }

    /// <summary>Literals are decoded with context modeling.</summary>
    private readonly struct ModeledLiteralContext : ILiteralMode
    {
        public static bool TrivialContext => false;
    }

    /// <summary>
    /// Internal (non-C) result of <see cref="ProcessCommandsFast{TLiteralMode}"/>: the literal context mode
    /// changed; the state was saved at BROTLI_STATE_COMMAND_INNER and the other variant must continue.
    /// </summary>
    private const BrotliDecoderErrorCode SwitchLiteralMode = (BrotliDecoderErrorCode)100;

    /// <summary>
    /// ProcessCommandsInternal(safe = 0). The method is specialized for the trivial / modeled literal context modes
    /// so that each variant is smaller (better register allocation) and gets its own execution profile; the
    /// variants hand over at BROTLI_STATE_COMMAND_INNER when the mode changes at a literal block switch.
    /// </summary>
    private BrotliDecoderErrorCode ProcessCommandsFast(ref BitReader br)
    {
        while (true)
        {
            BrotliDecoderErrorCode result = _trivialLiteralContext ?
                ProcessCommandsFast<TrivialLiteralContext>(ref br) :
                ProcessCommandsFast<ModeledLiteralContext>(ref br);
            if (result != SwitchLiteralMode)
            {
                return result;
            }
        }
    }

    /// <summary>
    /// ProcessCommandsInternal(safe = 0): used when there is enough input (BrotliCheckInputAmount). The bit reader
    /// and the per-literal state live in locals (written back to the instance before calling out and before
    /// returning); per-command state is accessed through fields, as in C.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode ProcessCommandsFast<TLiteralMode>(ref BitReader br)
        where TLiteralMode : struct, ILiteralMode
    {
        ReadOnlySpan<byte> input = br.Input;
        ulong val = br.Val;
        uint bitPos = br.BitPos;
        int nextIn = br.NextIn;
        int guardIn = br.GuardIn;

        int pos = _pos;
        int i = _loopCounter;
        BrotliDecoderErrorCode result = BrotliDecoderErrorCode.Success;
        BrotliDecoderErrorCode status;
        byte[] ringbuffer = _ringbuffer!;

        if (nextIn >= guardIn)
        {
            result = BrotliDecoderErrorCode.NeedsMoreInput;
            goto saveStateAndReturn;
        }

        // BrotliWarmupBitReader: input is guaranteed to be available.
        if (bitPos == 0)
        {
            val = input[nextIn];
            bitPos = 8;
            nextIn++;
        }

        // Jump into state machine.
        switch (_state)
        {
            case BrotliRunningState.CommandBegin:
                goto CommandBegin;
            case BrotliRunningState.CommandInner:
                goto CommandInner;
            case BrotliRunningState.CommandPostDecodeLiterals:
                goto CommandPostDecodeLiterals;
            case BrotliRunningState.CommandPostWrapCopy:
                goto CommandPostWrapCopy;
            default:
                return BrotliDecoderErrorCode.ErrorUnreachable;
        }

    CommandBegin:
        if (nextIn >= guardIn)
        {
            _state = BrotliRunningState.CommandBegin;
            result = BrotliDecoderErrorCode.NeedsMoreInput;
            goto saveStateAndReturn;
        }
        if (_blockLength[1] == 0)
        {
            br.Val = val;
            br.BitPos = bitPos;
            br.NextIn = nextIn;
            status = DecodeCommandBlockSwitch(ref br);
            val = br.Val;
            bitPos = br.BitPos;
            nextIn = br.NextIn;
            if (status != BrotliDecoderErrorCode.Success)
            {
                result = status;
                goto saveStateAndReturn;
            }
            goto CommandBegin;
        }

        // Read the insert/copy length in the command (ReadCommand).
        {
            uint cmdCode = ReadSymbolFast(_insertCopyHgroup.Codes!, _htreeCommand, ref val, ref bitPos, ref nextIn, input);
            CmdLutElement v = Prefix.GetCmdLut(cmdCode);
            _distanceCode = v.DistanceCode;
            int distanceContext = v.Context;
            _distanceContext = distanceContext;
            _distTreeOffset = _distanceTreesForContexts[distanceContext & 3];
            i = v.InsertLenOffset;
            uint insertLenExtraBits = v.InsertLenExtraBits;
            if (insertLenExtraBits != 0)
            {
                i += (int)ReadBitsFast(insertLenExtraBits, ref val, ref bitPos, ref nextIn, input);
            }
            _copyLength = (int)ReadBitsFast(v.CopyLenExtraBits, ref val, ref bitPos, ref nextIn, input) + v.CopyLenOffset;
            --_blockLength[1];
        }
        if (i == 0)
        {
            goto CommandPostDecodeLiterals;
        }
        _metaBlockRemainingLen -= i;

    CommandInner:
        {
            // Read the literals in the command.
            HuffmanCode[] literalCodes = _literalHgroup.Codes!;
            uint blockLength0 = _blockLength[0];
            int ringbufferSize = _ringbufferSize;

            // This is a hottest part of the decode, so (as C does for the trivial literal context) we calculate the
            // number of steps where all checks evaluate to false (ringbuffer size/block size/input size) and run a
            // simple loop for them; the full version handles the remainder.
            int numSteps = i;
            if (numSteps > 0 && ((uint)numSteps > blockLength0))
            {
                // Safe cast, since block_length < steps
                numSteps = (int)blockLength0;
            }
            if (ringbufferSize >= pos && (ringbufferSize - pos) <= numSteps)
            {
                numSteps = ringbufferSize - pos - 1;
            }
            if (numSteps < 0)
            {
                numSteps = 0;
            }
            // Conservatively budget two bytes per symbol (codes use at most 15 bits), plus overread slack.
            {
                const int MaximalOverread = 4;
                int inputLimit = Math.Max(0, ((guardIn - nextIn) >> 1) - MaximalOverread);
                numSteps = Math.Min(numSteps, inputLimit);
            }

            if (_trivialLiteralContext != TLiteralMode.TrivialContext)
            {
                // Hand over to the other variant (see ProcessCommandsFast).
                _state = BrotliRunningState.CommandInner;
                result = SwitchLiteralMode;
                goto saveStateAndReturn;
            }

            if (TLiteralMode.TrivialContext)
            {
                int literalTree = _literalHtree;

                // PreloadSymbol
                ReadOnlySpan<HuffmanCode> literalTable = literalCodes.AsSpan(literalTree);
                FillBitWindow8(ref val, ref bitPos, ref nextIn, input);
                HuffmanCode preloaded = literalTable[(int)(val & HuffmanTableMask)];
                uint bits = preloaded.Bits;
                uint value = preloaded.Value;

                // BrotliCopyPreloadedSymbolsToU8: fast path, it is safe to write and to read.
                int posLimit = pos + numSteps;
                for (; pos < posLimit; pos++)
                {
                    ringbuffer[pos] = (byte)ReadPreloadedSymbolFast(literalTable, ref bits, ref value, ref val, ref bitPos, ref nextIn, input);
                }
                blockLength0 -= (uint)numSteps;
                i -= numSteps;
                // Full version for the remainder.
                while (i != 0)
                {
                    if (nextIn >= guardIn)
                    {
                        _blockLength[0] = blockLength0;
                        _state = BrotliRunningState.CommandInner;
                        result = BrotliDecoderErrorCode.NeedsMoreInput;
                        goto saveStateAndReturn;
                    }
                    if (blockLength0 == 0)
                    {
                        _blockLength[0] = blockLength0;
                        goto NextLiteralBlock;
                    }
                    ringbuffer[pos] = (byte)ReadPreloadedSymbolFast(literalTable, ref bits, ref value, ref val, ref bitPos, ref nextIn, input);
                    --blockLength0;
                    ++pos;
                    if (pos == ringbufferSize)
                    {
                        _blockLength[0] = blockLength0;
                        _state = BrotliRunningState.CommandInnerWrite;
                        --i;
                        goto saveStateAndReturn;
                    }
                    --i;
                }
            }
            else
            {
                ReadOnlySpan<byte> contextLut = BrotliContext.LookupTable.Slice(_contextLookup, 512);
                ReadOnlySpan<int> literalTrees = _literalTreesForContexts;
                int ringbufferMask = _ringbufferMask;
                byte p1 = ringbuffer[(pos - 1) & ringbufferMask];
                byte p2 = ringbuffer[(pos - 2) & ringbufferMask];

                // Fast path, it is safe to write and to read.
                int posLimit = pos + numSteps;
                for (; pos < posLimit; pos++)
                {
                    int context = contextLut[p1] | contextLut[256 + p2];
                    p2 = p1;
                    p1 = (byte)ReadSymbolFast(literalCodes, literalTrees[context], ref val, ref bitPos, ref nextIn, input);
                    ringbuffer[pos] = p1;
                }
                blockLength0 -= (uint)numSteps;
                i -= numSteps;
                // Full version for the remainder.
                while (i != 0)
                {
                    if (nextIn >= guardIn)
                    {
                        _blockLength[0] = blockLength0;
                        _state = BrotliRunningState.CommandInner;
                        result = BrotliDecoderErrorCode.NeedsMoreInput;
                        goto saveStateAndReturn;
                    }
                    if (blockLength0 == 0)
                    {
                        _blockLength[0] = blockLength0;
                        goto NextLiteralBlock;
                    }
                    int context = contextLut[p1] | contextLut[256 + p2];
                    p2 = p1;
                    p1 = (byte)ReadSymbolFast(literalCodes, literalTrees[context], ref val, ref bitPos, ref nextIn, input);
                    ringbuffer[pos] = p1;
                    --blockLength0;
                    ++pos;
                    if (pos == ringbufferSize)
                    {
                        _blockLength[0] = blockLength0;
                        _state = BrotliRunningState.CommandInnerWrite;
                        --i;
                        goto saveStateAndReturn;
                    }
                    --i;
                }
            }
            _blockLength[0] = blockLength0;
        }
        if (_metaBlockRemainingLen <= 0)
        {
            _state = BrotliRunningState.MetablockDone;
            goto saveStateAndReturn;
        }

    CommandPostDecodeLiterals:
        {
            int distanceCode = _distanceCode;
            if (distanceCode >= 0)
            {
                // Implicit distance case.
                _distanceContext = distanceCode != 0 ? 0 : 1;
                int distRbIdx = _distRbIdx - 1;
                _distRbIdx = distRbIdx;
                distanceCode = _distRb[distRbIdx & 3];
            }
            else
            {
                // Read distance code in the command, unless it was implicitly zero.
                if (_blockLength[2] == 0)
                {
                    br.Val = val;
                    br.BitPos = bitPos;
                    br.NextIn = nextIn;
                    status = DecodeDistanceBlockSwitch(ref br);
                    val = br.Val;
                    bitPos = br.BitPos;
                    nextIn = br.NextIn;
                    if (status != BrotliDecoderErrorCode.Success)
                    {
                        result = status;
                        goto saveStateAndReturn;
                    }
                }

                // ReadDistance.
                uint code = ReadSymbolFast(_distanceHgroup.Codes!, _distTreeOffset, ref val, ref bitPos, ref nextIn, input);
                --_blockLength[2];
                // Convert the distance code to the actual distance by possibly looking up past distances from the
                // s->dist_rb.
                _distanceContext = 0;
                if ((code & ~0xFu) == 0)
                {
                    // TakeDistanceFromRingBuffer
                    distanceCode = (int)code;
                    int offset = distanceCode - 3;
                    if (distanceCode <= 3)
                    {
                        // Compensate double distance-ring-buffer roll for dictionary items.
                        int distanceContext = 1 >> distanceCode;
                        _distanceContext = distanceContext;
                        distanceCode = _distRb[(_distRbIdx - offset) & 3];
                        _distRbIdx -= distanceContext;
                    }
                    else
                    {
                        int indexDelta = 3;
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
                        int delta = ((0x605142 >> (4 * baseValue)) & 0xF) - 3;
                        distanceCode = _distRb[(_distRbIdx + indexDelta) & 0x3] + delta;
                        if (distanceCode <= 0)
                        {
                            // A huge distance will cause a BROTLI_FAILURE() soon. This is a little faster than failing here.
                            distanceCode = 0x7FFFFFFF;
                        }
                    }
                }
                else
                {
                    uint extra = ReadBitsFast(_distExtraBits[(int)code], ref val, ref bitPos, ref nextIn, input);
                    distanceCode = (int)(_distOffset[(int)code] + (extra << (int)_distancePostfixBits));
                }
            }
            _distanceCode = distanceCode;

            int maxDistance = _maxDistance;
            int maxBackwardDistance = _maxBackwardDistance;
            if (maxDistance != maxBackwardDistance)
            {
                maxDistance = (pos < maxBackwardDistance) ? pos : maxBackwardDistance;
                _maxDistance = maxDistance;
            }
            i = _copyLength;
            // Apply copy of LZ77 back-reference, or static dictionary reference if the distance is larger than the
            // max LZ77 distance.
            if (distanceCode > maxDistance)
            {
                // The maximum allowed distance is BROTLI_MAX_ALLOWED_DISTANCE = 0x7FFFFFFC. With this choice, no
                // signed overflow can occur after decoding a special distance code (e.g., after adding 3 to the last
                // distance).
                if (distanceCode > BrotliConstants.MaxAllowedDistance)
                {
                    result = BrotliDecoderErrorCode.ErrorFormatDistance;
                    goto saveBitReaderAndFail;
                }
                if (i >= BrotliDictionary.MinWordLength && i <= BrotliDictionary.MaxWordLength)
                {
                    int offset = (int)BrotliDictionary.OffsetsByLength[i];
                    int shift = BrotliDictionary.SizeBitsByLength[i];
                    int address = distanceCode - maxDistance - 1;
                    int mask = (int)BitReader.BitMask((uint)shift);
                    int wordIdx = address & mask;
                    int transformIdx = address >> shift;
                    // Compensate double distance-ring-buffer roll.
                    _distRbIdx += _distanceContext;
                    offset += wordIdx * i;
                    if (shift == 0)
                    {
                        result = BrotliDecoderErrorCode.ErrorFormatDictionary;
                        goto saveBitReaderAndFail;
                    }
                    if (transformIdx < BrotliTransforms.NumTransforms)
                    {
                        ReadOnlySpan<byte> dictionary = BrotliDictionary.Data;
                        int len = i;
                        if (transformIdx == BrotliTransforms.CutOffTransforms[0])
                        {
                            // Identity transform: words are 4..24 bytes long. Fixed-size copies may write up to 31
                            // bytes past pos; that is within the ring buffer slack and is overwritten later.
                            if (offset <= dictionary.Length - 32)
                            {
                                dictionary.Slice(offset, 16).CopyTo(ringbuffer.AsSpan(pos, 16));
                                if (i > 16)
                                {
                                    dictionary.Slice(offset + 16, 16).CopyTo(ringbuffer.AsSpan(pos + 16, 16));
                                }
                            }
                            else
                            {
                                dictionary.Slice(offset, i).CopyTo(ringbuffer.AsSpan(pos));
                            }
                        }
                        else
                        {
                            len = BrotliTransforms.TransformDictionaryWord(ringbuffer.AsSpan(pos), dictionary.Slice(offset, i), len, transformIdx);
                            if (len == 0 && distanceCode <= 120)
                            {
                                result = BrotliDecoderErrorCode.ErrorFormatTransform;
                                goto saveBitReaderAndFail;
                            }
                        }
                        pos += len;
                        _metaBlockRemainingLen -= len;
                        if (pos >= _ringbufferSize)
                        {
                            _state = BrotliRunningState.CommandPostWrite1;
                            goto saveStateAndReturn;
                        }
                    }
                    else
                    {
                        result = BrotliDecoderErrorCode.ErrorFormatTransform;
                        goto saveBitReaderAndFail;
                    }
                }
                else
                {
                    result = BrotliDecoderErrorCode.ErrorFormatDictionary;
                    goto saveBitReaderAndFail;
                }
            }
            else
            {
                int srcStart = (pos - distanceCode) & _ringbufferMask;
                // Update the recent distances cache.
                _distRb[_distRbIdx & 3] = distanceCode;
                _distRbIdx++;
                _metaBlockRemainingLen -= i;
                // There are 32+ bytes of slack in the ring-buffer allocation. Also, we have 16 short codes, that make
                // these 16 bytes irrelevant in the ring-buffer. Let's copy over them as a first guess.
                ringbuffer.AsSpan(srcStart, 16).CopyTo(ringbuffer.AsSpan(pos, 16));
                int dstEnd = pos + i;
                int srcEnd = srcStart + i;
                if (srcEnd > pos && dstEnd > srcStart)
                {
                    // Regions intersect.
                    if (srcStart < pos && dstEnd < _ringbufferSize)
                    {
                        // No wrapping: equivalent to the byte-by-byte copy of CommandPostWrapCopy.
                        CopyOverlapping(ringbuffer, srcStart, pos, i);
                        pos = dstEnd;
                        goto CommandPostCopy;
                    }
                    goto CommandPostWrapCopy;
                }
                if (dstEnd >= _ringbufferSize || srcEnd >= _ringbufferSize)
                {
                    // At least one region wraps.
                    goto CommandPostWrapCopy;
                }
                if (i > 16)
                {
                    if (i > 32)
                    {
                        ringbuffer.AsSpan(srcStart + 16, i - 16).CopyTo(ringbuffer.AsSpan(pos + 16));
                    }
                    else
                    {
                        // This branch covers about 45% cases. Fixed size short copy allows more compiler optimizations.
                        ringbuffer.AsSpan(srcStart + 16, 16).CopyTo(ringbuffer.AsSpan(pos + 16, 16));
                    }
                }
                pos = dstEnd;
            }
        }

    CommandPostCopy:
        if (_metaBlockRemainingLen <= 0)
        {
            // Next metablock, if any.
            _state = BrotliRunningState.MetablockDone;
            goto saveStateAndReturn;
        }
        else
        {
            goto CommandBegin;
        }

    CommandPostWrapCopy:
        {
            int wrapGuard = _ringbufferSize - pos;
            int ringbufferMask = _ringbufferMask;
            int distanceCode = _distanceCode;
            while (--i >= 0)
            {
                ringbuffer[pos] = ringbuffer[(pos - distanceCode) & ringbufferMask];
                ++pos;
                if (--wrapGuard == 0)
                {
                    _state = BrotliRunningState.CommandPostWrite2;
                    goto saveStateAndReturn;
                }
            }
        }
        if (_metaBlockRemainingLen <= 0)
        {
            // Next metablock, if any.
            _state = BrotliRunningState.MetablockDone;
            goto saveStateAndReturn;
        }
        else
        {
            goto CommandBegin;
        }

    NextLiteralBlock:
        br.Val = val;
        br.BitPos = bitPos;
        br.NextIn = nextIn;
        status = DecodeLiteralBlockSwitch(ref br);
        val = br.Val;
        bitPos = br.BitPos;
        nextIn = br.NextIn;
        if (status != BrotliDecoderErrorCode.Success)
        {
            result = status;
            goto saveStateAndReturn;
        }
        goto CommandInner;

    saveBitReaderAndFail:
        // C returns errors here without saving the position (the state is unusable after an error anyway), but the
        // bit reader position is observable (amount of consumed input).
        br.Val = val;
        br.BitPos = bitPos;
        br.NextIn = nextIn;
        return result;

    saveStateAndReturn:
        br.Val = val;
        br.BitPos = bitPos;
        br.NextIn = nextIn;
        _pos = pos;
        _loopCounter = i;
        return result;
    }

    /// <summary>ReadPreloadedSymbol + PreloadSymbol: decodes the next Huffman code using data prepared by PreloadSymbol.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadPreloadedSymbolFast(ReadOnlySpan<HuffmanCode> table, ref uint bits, ref uint value,
        ref ulong val, ref uint bitPos, ref int nextIn, ReadOnlySpan<byte> input)
    {
        uint result = value;
        if (bits > HuffmanTableBits)
        {
            FillBitWindow16(ref val, ref bitPos, ref nextIn, input);
            ulong v = val;
            int ext = (int)(v & HuffmanTableMask) + (int)value;
            uint mask = ~(uint.MaxValue << (int)(bits - HuffmanTableBits));
            ext += (int)((uint)(v >> HuffmanTableBits) & mask);
            HuffmanCode code = table[ext];
            uint dropped = HuffmanTableBits + (uint)code.Bits;
            val = v >> (int)dropped;
            bitPos -= dropped;
            result = code.Value;
        }
        else
        {
            val >>= (int)bits;
            bitPos -= bits;
        }
        // PreloadSymbol.
        FillBitWindow8(ref val, ref bitPos, ref nextIn, input);
        HuffmanCode next = table[(int)(val & HuffmanTableMask)];
        bits = next.Bits;
        value = next.Value;
        return result;
    }
}
