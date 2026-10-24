// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/dec/decode.c
// (ProcessCommandsInternal with safe == 1).

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

internal sealed partial class BrotliDecoderState
{
    /// <summary>Selects the "safe" (input may be short) or the fast variant of the decoding routines.</summary>
    private interface IDecodeMode
    {
        static abstract bool Safe { get; }
    }

    private readonly struct FastDecode : IDecodeMode
    {
        public static bool Safe => false;
    }

    private readonly struct SafeDecode : IDecodeMode
    {
        public static bool Safe => true;
    }

    /// <summary>ProcessCommands: the fast variant is <see cref="ProcessCommandsFast(ref BitReader)"/>.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode ProcessCommands(ref BitReader br) => ProcessCommandsFast(ref br);

    /// <summary>
    /// SafeProcessCommands: ProcessCommandsInternal with safe == 1, i.e. used when the input may run out at any point.
    /// The bit reader is accessed through <paramref name="br"/>; all reads are "safe" and resumable.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BrotliDecoderErrorCode SafeProcessCommands(ref BitReader br)
    {
        int pos = _pos;
        int i = _loopCounter;
        BrotliDecoderErrorCode result = BrotliDecoderErrorCode.Success;
        BrotliDecoderErrorCode status;
        byte[] ringbuffer = _ringbuffer!;
        ReadOnlySpan<byte> contextLut;
        uint literal;

        // Jump into state machine.
        if (_state == BrotliRunningState.CommandBegin)
        {
            goto CommandBegin;
        }
        else if (_state == BrotliRunningState.CommandInner)
        {
            goto CommandInner;
        }
        else if (_state == BrotliRunningState.CommandPostDecodeLiterals)
        {
            goto CommandPostDecodeLiterals;
        }
        else if (_state == BrotliRunningState.CommandPostWrapCopy)
        {
            goto CommandPostWrapCopy;
        }
        else
        {
            return BrotliDecoderErrorCode.ErrorUnreachable;
        }

    CommandBegin:
        _state = BrotliRunningState.CommandBegin;
        if (_blockLength[1] == 0)
        {
            status = SafeDecodeCommandBlockSwitch(ref br);
            if (status != BrotliDecoderErrorCode.Success)
            {
                result = status;
                goto saveStateAndReturn;
            }
            goto CommandBegin;
        }
        // Read the insert/copy length in the command.
        if (!SafeReadCommand(ref br, ref i))
        {
            result = BrotliDecoderErrorCode.NeedsMoreInput;
            goto saveStateAndReturn;
        }
        if (i == 0)
        {
            goto CommandPostDecodeLiterals;
        }
        _metaBlockRemainingLen -= i;

    CommandInner:
        _state = BrotliRunningState.CommandInner;
        // Read the literals in the command.
        if (_trivialLiteralContext)
        {
            ReadOnlySpan<HuffmanCode> literalTree = _literalHgroup.Codes.AsSpan(_literalHtree);
            do
            {
                if (_blockLength[0] == 0)
                {
                    goto NextLiteralBlock;
                }
                if (!SafeReadSymbol(literalTree, ref br, out literal))
                {
                    result = BrotliDecoderErrorCode.NeedsMoreInput;
                    goto saveStateAndReturn;
                }
                ringbuffer[pos] = (byte)literal;
                --_blockLength[0];
                ++pos;
                if (pos == _ringbufferSize)
                {
                    _state = BrotliRunningState.CommandInnerWrite;
                    --i;
                    goto saveStateAndReturn;
                }
            }
            while (--i != 0);
        }
        else
        {
            byte p1 = ringbuffer[(pos - 1) & _ringbufferMask];
            byte p2 = ringbuffer[(pos - 2) & _ringbufferMask];
            contextLut = BrotliContext.LookupTable.Slice(_contextLookup, 512);
            do
            {
                if (_blockLength[0] == 0)
                {
                    goto NextLiteralBlock;
                }
                int context = contextLut[p1] | contextLut[256 + p2];
                ReadOnlySpan<HuffmanCode> hc = _literalHgroup.Codes.AsSpan(_literalHgroup.Htrees[_contextMap![_contextMapSlice + context]]);
                p2 = p1;
                if (!SafeReadSymbol(hc, ref br, out literal))
                {
                    result = BrotliDecoderErrorCode.NeedsMoreInput;
                    goto saveStateAndReturn;
                }
                p1 = (byte)literal;
                ringbuffer[pos] = p1;
                --_blockLength[0];
                ++pos;
                if (pos == _ringbufferSize)
                {
                    _state = BrotliRunningState.CommandInnerWrite;
                    --i;
                    goto saveStateAndReturn;
                }
            }
            while (--i != 0);
        }
        if (_metaBlockRemainingLen <= 0)
        {
            _state = BrotliRunningState.MetablockDone;
            goto saveStateAndReturn;
        }

    CommandPostDecodeLiterals:
        _state = BrotliRunningState.CommandPostDecodeLiterals;
        if (_distanceCode >= 0)
        {
            // Implicit distance case.
            _distanceContext = _distanceCode != 0 ? 0 : 1;
            --_distRbIdx;
            _distanceCode = _distRb[_distRbIdx & 3];
        }
        else
        {
            // Read distance code in the command, unless it was implicitly zero.
            if (_blockLength[2] == 0)
            {
                status = SafeDecodeDistanceBlockSwitch(ref br);
                if (status != BrotliDecoderErrorCode.Success)
                {
                    result = status;
                    goto saveStateAndReturn;
                }
            }
            if (!SafeReadDistance(ref br))
            {
                result = BrotliDecoderErrorCode.NeedsMoreInput;
                goto saveStateAndReturn;
            }
        }
        if (_maxDistance != _maxBackwardDistance)
        {
            _maxDistance = (pos < _maxBackwardDistance) ? pos : _maxBackwardDistance;
        }
        i = _copyLength;
        // Apply copy of LZ77 back-reference, or static dictionary reference if the distance is larger than the max
        // LZ77 distance.
        if (_distanceCode > _maxDistance)
        {
            // The maximum allowed distance is BROTLI_MAX_ALLOWED_DISTANCE = 0x7FFFFFFC. With this choice, no signed
            // overflow can occur after decoding a special distance code (e.g., after adding 3 to the last distance).
            if (_distanceCode > BrotliConstants.MaxAllowedDistance)
            {
                return BrotliDecoderErrorCode.ErrorFormatDistance;
            }
            if (i >= BrotliDictionary.MinWordLength && i <= BrotliDictionary.MaxWordLength)
            {
                int offset = (int)BrotliDictionary.OffsetsByLength[i];
                uint shift = BrotliDictionary.SizeBitsByLength[i];
                int address = _distanceCode - _maxDistance - 1;
                int mask = (int)BitReader.BitMask(shift);
                int wordIdx = address & mask;
                int transformIdx = address >> (int)shift;
                // Compensate double distance-ring-buffer roll.
                _distRbIdx += _distanceContext;
                offset += wordIdx * i;
                if (shift == 0)
                {
                    return BrotliDecoderErrorCode.ErrorFormatDictionary;
                }
                if (transformIdx < BrotliTransforms.NumTransforms)
                {
                    ReadOnlySpan<byte> word = BrotliDictionary.Data.Slice(offset);
                    int len = i;
                    if (transformIdx == BrotliTransforms.CutOffTransforms[0])
                    {
                        word.Slice(0, len).CopyTo(ringbuffer.AsSpan(pos));
                    }
                    else
                    {
                        len = BrotliTransforms.TransformDictionaryWord(ringbuffer.AsSpan(pos), word, len, transformIdx);
                        if (len == 0 && _distanceCode <= 120)
                        {
                            return BrotliDecoderErrorCode.ErrorFormatTransform;
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
                    return BrotliDecoderErrorCode.ErrorFormatTransform;
                }
            }
            else
            {
                return BrotliDecoderErrorCode.ErrorFormatDictionary;
            }
        }
        else
        {
            int srcStart = (pos - _distanceCode) & _ringbufferMask;
            int dstEnd = pos + i;
            int srcEnd = srcStart + i;
            // Update the recent distances cache.
            _distRb[_distRbIdx & 3] = _distanceCode;
            ++_distRbIdx;
            _metaBlockRemainingLen -= i;
            // There are 32+ bytes of slack in the ring-buffer allocation. Also, we have 16 short codes, that make
            // these 16 bytes irrelevant in the ring-buffer. Let's copy over them as a first guess.
            ringbuffer.AsSpan(srcStart, 16).CopyTo(ringbuffer.AsSpan(pos, 16));
            if (srcEnd > pos && dstEnd > srcStart)
            {
                // Regions intersect.
                goto CommandPostWrapCopy;
            }
            if (dstEnd >= _ringbufferSize || srcEnd >= _ringbufferSize)
            {
                // At least one region wraps.
                goto CommandPostWrapCopy;
            }
            pos += i;
            if (i > 16)
            {
                if (i > 32)
                {
                    ringbuffer.AsSpan(srcStart + 16, i - 16).CopyTo(ringbuffer.AsSpan(pos - i + 16));
                }
                else
                {
                    // This branch covers about 45% cases. Fixed size short copy allows more compiler optimizations.
                    ringbuffer.AsSpan(srcStart + 16, 16).CopyTo(ringbuffer.AsSpan(pos - i + 16, 16));
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

    CommandPostWrapCopy:
        {
            int wrapGuard = _ringbufferSize - pos;
            while (--i >= 0)
            {
                ringbuffer[pos] = ringbuffer[(pos - _distanceCode) & _ringbufferMask];
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
        status = SafeDecodeLiteralBlockSwitch(ref br);
        if (status != BrotliDecoderErrorCode.Success)
        {
            result = status;
            goto saveStateAndReturn;
        }
        goto CommandInner;

    saveStateAndReturn:
        _pos = pos;
        _loopCounter = i;
        return result;
    }
}
