// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Managed implementation of the native BitStreamReader class for reading compressed GC info.
/// This class provides methods to read variable-length bit sequences from a memory buffer
/// accessed through the Target abstraction.
/// </summary>
internal struct BitStreamReader
{
    private static readonly int BitsPerSize = IntPtr.Size * 8;

    private readonly Target _target;
    private readonly TargetPointer _buffer;
    private readonly int _initialRelPos;

    private TargetPointer _current;
    private int _relPos;
    private nuint _currentValue;

    /// <summary>
    /// Initializes a new BitStreamReader starting at the specified buffer address.
    /// </summary>
    /// <param name="target">The target process to read from</param>
    /// <param name="buffer">Pointer to the start of the bit stream data</param>
    public BitStreamReader(Target target, TargetPointer buffer)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (buffer == TargetPointer.Null)
            throw new ArgumentException("Buffer pointer cannot be null", nameof(buffer));

        _target = target;

        // Align buffer to pointer size boundary (similar to native implementation)
        nuint pointerMask = (nuint)target.PointerSize - 1;
        TargetPointer alignedBuffer = new(buffer.Value & ~(ulong)pointerMask);

        _buffer = alignedBuffer;
        _current = alignedBuffer;
        _initialRelPos = (int)((buffer.Value % (ulong)target.PointerSize) * 8);
        _relPos = _initialRelPos;

        // Prefetch the first word and position it correctly
        _currentValue = ReadPointerSizedValue(_current);
        _currentValue >>= _relPos;
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="other">The BitStreamReader to copy from</param>
    public BitStreamReader(BitStreamReader other)
    {
        _target = other._target;
        _buffer = other._buffer;
        _initialRelPos = other._initialRelPos;
        _current = other._current;
        _relPos = other._relPos;
        _currentValue = other._currentValue;
    }

    /// <summary>
    /// Reads the specified number of bits from the stream.
    /// </summary>
    /// <param name="numBits">Number of bits to read (1 to pointer size in bits)</param>
    /// <returns>The value read from the stream</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint Read(int numBits)
    {
        Debug.Assert(numBits > 0 && numBits <= BitsPerSize);

        nuint result = _currentValue;
        _currentValue >>= numBits;
        int newRelPos = _relPos + numBits;

        if (newRelPos > BitsPerSize)
        {
            // Need to read from next word
            _current = new TargetPointer(_current.Value + (ulong)_target.PointerSize);
            nuint nextValue = ReadPointerSizedValue(_current);
            newRelPos -= BitsPerSize;
            nuint extraBits = nextValue << (numBits - newRelPos);
            result |= extraBits;
            _currentValue = nextValue >> newRelPos;
        }

        _relPos = newRelPos;

        // Mask to get only the requested bits
        nuint mask = (nuint.MaxValue >> (BitsPerSize - numBits));
        result &= mask;

        return result;
    }

    /// <summary>
    /// Reads a single bit from the stream (optimized version).
    /// </summary>
    /// <returns>The bit value (0 or 1)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint ReadOneFast()
    {
        // Check if we need to fetch the next word
        if (_relPos == BitsPerSize)
        {
            _current = new TargetPointer(_current.Value + (ulong)_target.PointerSize);
            _currentValue = ReadPointerSizedValue(_current);
            _relPos = 0;
        }

        _relPos++;
        nuint result = _currentValue & 1;
        _currentValue >>= 1;

        return result;
    }

    /// <summary>
    /// Gets the current position in bits from the start of the stream.
    /// </summary>
    /// <returns>Current bit position</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint GetCurrentPos()
    {
        long wordOffset = ((long)_current.Value - (long)_buffer.Value) / _target.PointerSize;
        return (nuint)(wordOffset * BitsPerSize + _relPos - _initialRelPos);
    }

    /// <summary>
    /// Sets the current position in the stream to the specified bit offset.
    /// </summary>
    /// <param name="pos">Target bit position from the start of the stream</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCurrentPos(nuint pos)
    {
        nuint adjPos = pos + (nuint)_initialRelPos;
        nuint wordOffset = adjPos / (nuint)BitsPerSize;
        int newRelPos = (int)(adjPos % (nuint)BitsPerSize);

        _current = new TargetPointer(_buffer.Value + wordOffset * (ulong)_target.PointerSize);
        _relPos = newRelPos;

        // Prefetch the new word and position it correctly
        _currentValue = ReadPointerSizedValue(_current) >> newRelPos;
    }

    /// <summary>
    /// Skips the specified number of bits in the stream.
    /// </summary>
    /// <param name="numBitsToSkip">Number of bits to skip (can be negative)</param>
    public void Skip(nint numBitsToSkip)
    {
        nuint newPos = (nuint)((nint)GetCurrentPos() + numBitsToSkip);

        nuint adjPos = newPos + (nuint)_initialRelPos;
        nuint wordOffset = adjPos / (nuint)BitsPerSize;
        int newRelPos = (int)(adjPos % (nuint)BitsPerSize);

        _current = new TargetPointer(_buffer.Value + wordOffset * (ulong)_target.PointerSize);
        _relPos = newRelPos;

        // Skipping ahead may go to a position at the edge-exclusive
        // end of the stream. The location may have no more data.
        // We will not prefetch on word boundary - in case
        // the next word is in an unreadable page.
        if (_relPos == 0)
        {
            _current = new TargetPointer(_current.Value - (ulong)_target.PointerSize);
            _relPos = BitsPerSize;
            _currentValue = 0;
        }
        else
        {
            _currentValue = ReadPointerSizedValue(_current) >> _relPos;
        }
    }

    /// <summary>
    /// Decodes a variable-length unsigned integer.
    /// </summary>
    /// <param name="baseValue">Base value for encoding (number of bits per chunk)</param>
    /// <returns>The decoded unsigned integer</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint DecodeVarLengthUnsigned(int baseValue)
    {
        Debug.Assert(baseValue > 0 && baseValue < BitsPerSize);

        nuint result = Read(baseValue + 1);
        if ((result & ((nuint)1 << baseValue)) != 0)
        {
            result ^= DecodeVarLengthUnsignedMore(baseValue);
        }

        return result;
    }

    /// <summary>
    /// Helper method for decoding variable-length unsigned integers with extension bits.
    /// </summary>
    /// <param name="baseValue">Base value for encoding</param>
    /// <returns>The additional bits for the decoded value</returns>
    private nuint DecodeVarLengthUnsignedMore(int baseValue)
    {
        Debug.Assert(baseValue > 0 && baseValue < BitsPerSize);

        nuint numEncodings = (nuint)1 << baseValue;
        nuint result = numEncodings;

        for (int shift = baseValue; ; shift += baseValue)
        {
            Debug.Assert(shift + baseValue <= BitsPerSize);

            nuint currentChunk = Read(baseValue + 1);
            result ^= (currentChunk & (numEncodings - 1)) << shift;

            if ((currentChunk & numEncodings) == 0)
            {
                // Extension bit is not set, we're done
                return result;
            }
        }
    }

    /// <summary>
    /// Decodes a variable-length signed integer.
    /// </summary>
    /// <param name="baseValue">Base value for encoding (number of bits per chunk)</param>
    /// <returns>The decoded signed integer</returns>
    public nint DecodeVarLengthSigned(int baseValue)
    {
        Debug.Assert(baseValue > 0 && baseValue < BitsPerSize);

        nuint numEncodings = (nuint)1 << baseValue;
        nint result = 0;

        for (int shift = 0; ; shift += baseValue)
        {
            Debug.Assert(shift + baseValue <= BitsPerSize);

            nuint currentChunk = Read(baseValue + 1);
            result |= (nint)(currentChunk & (numEncodings - 1)) << shift;

            if ((currentChunk & numEncodings) == 0)
            {
                // Extension bit is not set, sign-extend and we're done
                int signBits = BitsPerSize - (shift + baseValue);
                result <<= signBits;
                result >>= signBits; // Arithmetic right shift for sign extension
                return result;
            }
        }
    }

    /// <summary>
    /// Reads a pointer-sized value from the target at the specified address.
    /// </summary>
    /// <param name="address">Address to read from</param>
    /// <returns>The value read as nuint</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private nuint ReadPointerSizedValue(TargetPointer address)
    {
        if (_target.PointerSize == 4)
        {
            return _target.Read<uint>(address);
        }
        else
        {
            Debug.Assert(_target.PointerSize == 8);
            return (nuint)_target.Read<ulong>(address);
        }
    }
}
