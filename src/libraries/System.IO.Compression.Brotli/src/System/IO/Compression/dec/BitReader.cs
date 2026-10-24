// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/dec/bit_reader.h and bit_reader.c.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>
/// Bit reading helpers (BrotliBitReader). The C reader holds raw pointers into the input; here the reader holds the
/// input span and absolute offsets into it. It only lives for the duration of one DecompressStream call; the
/// accumulator (<see cref="Val"/>, <see cref="BitPos"/>) is persisted in the decoder state between calls.
/// This mirrors the 64-bit, fast-unaligned-read configuration of the C implementation.
/// </summary>
internal ref struct BitReader
{
    /// <summary>sizeof(brotli_reg_t) >> 1.</summary>
    public const int ShortFillBitWindowRead = sizeof(ulong) >> 1;

    /// <summary>162 bits + 7 bytes.</summary>
    public const int FastInputSlack = 28;

    /// <summary>Pre-fetched bits.</summary>
    public ulong Val;
    /// <summary>Current bit-reading position in <see cref="Val"/>.</summary>
    public uint BitPos;
    /// <summary>The byte we're reading from (offset in <see cref="Input"/>).</summary>
    public int NextIn;
    /// <summary>Position from which "fast-path" is prohibited.</summary>
    public int GuardIn;
    /// <summary>== NextIn + avail_in.</summary>
    public int LastIn;
    public ReadOnlySpan<byte> Input;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong BitMask(uint n) => ~(ulong.MaxValue << (int)n);

    /// <summary>Initializes the BrotliBitReader fields.</summary>
    public void Init()
    {
        Val = 0;
        BitPos = 0;
    }

    /// <summary>
    /// Ensures that accumulator is not empty. May consume up to sizeof(brotli_reg_t) - 1 bytes of input.
    /// Returns false if data is required but there is no input available.
    /// </summary>
    public bool Warmup()
    {
        if (BitPos == 0)
        {
            Val = 0;
            if (!PullByte())
            {
                return false;
            }
        }
        // Unaligned reads are fast: no alignment fixup is required.
        return true;
    }

    public readonly int AvailIn
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LastIn - NextIn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveState(out BitReaderState to)
    {
        to.Val = Val;
        to.BitPos = BitPos;
        to.NextIn = NextIn;
        to.AvailIn = AvailIn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetInput(ReadOnlySpan<byte> input, int nextIn, int availIn)
    {
        Input = input;
        NextIn = nextIn;
        LastIn = nextIn + availIn;
        if (availIn + 1 > FastInputSlack)
        {
            GuardIn = nextIn + (availIn + 1 - FastInputSlack);
        }
        else
        {
            GuardIn = nextIn;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RestoreState(in BitReaderState from)
    {
        Val = from.Val;
        BitPos = from.BitPos;
        NextIn = from.NextIn;
        SetInput(Input, from.NextIn, from.AvailIn);
    }

    public readonly uint AvailableBits
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitPos;
    }

    /// <summary>
    /// Returns amount of unread bytes the bit reader still has buffered from the BrotliInput, including whole
    /// bytes in Val. Result is capped with maximal ring-buffer size (larger number won't be utilized anyway).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetRemainingBytes()
    {
        const int Cap = 1 << BrotliConstants.LargeMaxWBits;
        int availIn = AvailIn;
        if (availIn > Cap)
        {
            return Cap;
        }
        return availIn + (int)(BitPos >> 3);
    }

    /// <summary>BrotliFillBitWindow for a constant n_bits &lt;= 16: guarantees at least 17 bits in accumulator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillBitWindowConst16()
    {
        uint bitPos = BitPos;
        if (bitPos <= 16)
        {
            Val |= BinaryPrimitives.ReadUInt64LittleEndian(Input.Slice(NextIn)) << (int)bitPos;
            BitPos = bitPos + 48;
            NextIn += 6;
        }
    }

    /// <summary>
    /// BrotliFillBitWindow for non-constant n_bits (and BrotliFillBitWindow16): guarantees that there are at least
    /// 33 bits in accumulator. Precondition: accumulator contains at least 1 bit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillBitWindow()
    {
        uint bitPos = BitPos;
        if (bitPos <= 32)
        {
            Val |= (ulong)BinaryPrimitives.ReadUInt32LittleEndian(Input.Slice(NextIn)) << (int)bitPos;
            BitPos = bitPos + 32;
            NextIn += ShortFillBitWindowRead;
        }
    }

    /// <summary>Tries to pull one byte of input to accumulator. Returns false if there is no input available.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool PullByte()
    {
        int nextIn = NextIn;
        if (nextIn == LastIn)
        {
            return false;
        }
        Val |= (ulong)Input[nextIn] << (int)BitPos;
        BitPos += 8;
        NextIn = nextIn + 1;
        return true;
    }

    /// <summary>Returns currently available bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ulong GetBitsUnmasked() => Val;

    /// <summary>Like GetBits, but does not mask the result. The result contains at least 16 valid bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Get16BitsUnmasked()
    {
        FillBitWindowConst16();
        return Val;
    }

    /// <summary>Tries to peek the specified amount of bits. Returns false, if there is not enough input.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SafeGetBits(uint nBits, out uint val)
    {
        while (BitPos < nBits)
        {
            if (!PullByte())
            {
                val = 0;
                return false;
            }
        }
        val = (uint)(Val & BitMask(nBits));
        return true;
    }

    /// <summary>Advances the bit pos by <paramref name="nBits"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DropBits(uint nBits)
    {
        BitPos -= nBits;
        Val >>= (int)nBits;
    }

    /// <summary>
    /// Make sure that there are no spectre bits in accumulator. This is important for the cases when some bytes
    /// are skipped (i.e. never placed into accumulator).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        // Actually, it is enough to normalize when BitPos == 0.
        if (BitPos < sizeof(ulong) * 8)
        {
            Val &= (1UL << (int)BitPos) - 1;
        }
    }

    public void Unload()
    {
        uint unusedBytes = AvailableBits >> 3;
        uint unusedBits = unusedBytes << 3;
        NextIn -= (int)unusedBytes;
        BitPos -= unusedBits;
        Normalize();
    }

    /// <summary>
    /// Reads the specified number of bits and advances the bit pos.
    /// Precondition: accumulator MUST contain at least <paramref name="nBits"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint TakeBits(uint nBits)
    {
        uint val = (uint)(Val & BitMask(nBits));
        DropBits(nBits);
        return val;
    }

    /// <summary>
    /// Reads the specified number of bits and advances the bit pos. Assumes that there is enough input to perform
    /// FillBitWindow. Up to 24 bits are allowed to be requested from this method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadBits24(uint nBits)
    {
        Debug.Assert(nBits <= 24);
        FillBitWindow();
        return TakeBits(nBits);
    }

    /// <summary>
    /// Tries to read the specified amount of bits. Returns false, if there is not enough input.
    /// Up to 24 bits are allowed to be requested from this method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SafeReadBits(uint nBits, out uint val)
    {
        Debug.Assert(nBits <= 24);
        while (BitPos < nBits)
        {
            if (!PullByte())
            {
                val = 0;
                return false;
            }
        }
        val = TakeBits(nBits);
        return true;
    }

    /// <summary>Same as <see cref="SafeReadBits"/>, but allows reading up to 32 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SafeReadBits32(uint nBits, out uint val)
    {
        Debug.Assert(nBits <= 32);
        while (BitPos < nBits)
        {
            if (!PullByte())
            {
                val = 0;
                return false;
            }
        }
        val = TakeBits(nBits);
        return true;
    }

    /// <summary>
    /// Advances the bit reader position to the next byte boundary and verifies that any skipped bits are set to zero.
    /// </summary>
    public bool JumpToByteBoundary()
    {
        uint padBitsCount = AvailableBits & 0x7;
        uint padBits = 0;
        if (padBitsCount != 0)
        {
            padBits = TakeBits(padBitsCount);
        }
        Normalize();
        return padBits == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DropBytes(int num)
    {
        // Check detour is legal: accumulator must to be empty.
        Debug.Assert(BitPos == 0);
        Debug.Assert(Val == 0);
        NextIn += num;
    }

    /// <summary>
    /// Copies remaining input bytes stored in the bit reader to the output. Value <paramref name="num"/> may not be
    /// larger than GetRemainingBytes. The bit reader must be warmed up again after this.
    /// </summary>
    public void CopyBytes(scoped Span<byte> dest, int num)
    {
        int d = 0;
        while (AvailableBits >= 8 && num > 0)
        {
            dest[d] = (byte)Val;
            DropBits(8);
            ++d;
            --num;
        }
        Normalize();
        if (num > 0)
        {
            Input.Slice(NextIn, num).CopyTo(dest.Slice(d));
            DropBytes(num);
        }
    }
}

/// <summary>BrotliBitReaderState: a memento of the bit reader.</summary>
internal struct BitReaderState
{
    public ulong Val;
    public uint BitPos;
    public int NextIn;
    public int AvailIn;
}
