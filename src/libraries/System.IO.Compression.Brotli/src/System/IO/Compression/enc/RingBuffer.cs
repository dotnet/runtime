// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/ringbuffer.h.

using System.Buffers;
using System.Diagnostics;

namespace System.IO.Compression;

/// <summary>
/// Sliding window over the input data.
/// </summary>
/// <remarks>
/// A RingBuffer(window_bits, tail_bits) contains `1 &lt;&lt; window_bits' bytes of data in a circular manner:
/// writing a byte writes it to `position() % (1 &lt;&lt; window_bits)'.
/// For convenience, the RingBuffer array contains another copy of the first `1 &lt;&lt; tail_bits' bytes:
/// buffer_[i] == buffer_[i + (1 &lt;&lt; window_bits)], if i &lt; (1 &lt;&lt; tail_bits),
/// and another copy of the last two bytes:
/// buffer_[-1] == buffer_[(1 &lt;&lt; window_bits) - 1] and
/// buffer_[-2] == buffer_[(1 &lt;&lt; window_bits) - 2].
/// The C <c>buffer_</c> pointer is represented by <see cref="Data"/> + <see cref="BufferOffset"/>.
/// </remarks>
internal sealed class RingBuffer
{
    /// <summary>Offset of the C <c>buffer_</c> pointer within <see cref="Data"/>.</summary>
    public const int BufferOffset = 2;

    private const int SlackForEightByteHashingEverywhere = 7;

    /// <summary>Size of the ring-buffer is (1 &lt;&lt; window_bits) + tail_size_.</summary>
    public uint Size;
    public uint Mask;
    public uint TailSize;
    public uint TotalSize;

    public uint CurSize;
    /// <summary>Position to write in the ring buffer.</summary>
    public uint Pos;
    /// <summary>
    /// The actual ring buffer containing the copy of the last two bytes, the data, and the copy of the beginning as a tail.
    /// Rented from <see cref="ArrayPool{T}.Shared"/>; <c>null</c> until the first write.
    /// </summary>
    public byte[]? Data;

    /// <summary>The start of the ring-buffer (C <c>buffer_</c>), including the 7 bytes of slack after <see cref="CurSize"/>.</summary>
    public Span<byte> Buffer
    {
        get
        {
            Debug.Assert(Data is not null);
            return Data.AsSpan(BufferOffset, (int)CurSize + SlackForEightByteHashingEverywhere);
        }
    }

    /// <summary>Port of RingBufferInit.</summary>
    public RingBuffer()
    {
        CurSize = 0;
        Pos = 0;
        Data = null;
    }

    /// <summary>Port of RingBufferSetup.</summary>
    public void Setup(in EncoderParams parameters)
    {
        int windowBits = Quality.ComputeRbBits(parameters);
        int tailBits = parameters.Lgblock;
        Size = 1u << windowBits;
        Mask = (1u << windowBits) - 1;
        TailSize = 1u << tailBits;
        TotalSize = Size + TailSize;
    }

    /// <summary>Port of RingBufferFree.</summary>
    public void Free()
    {
        byte[]? data = Data;
        Data = null;
        if (data is not null)
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    /// <summary>
    /// Allocates or re-allocates data_ to the given length + plus some slack
    /// region before and after. Fills the slack regions with zeros.
    /// </summary>
    private void InitBuffer(uint buflen)
    {
        byte[] newData = ArrayPool<byte>.Shared.Rent(BufferOffset + (int)buflen + SlackForEightByteHashingEverywhere);
        byte[]? oldData = Data;
        if (oldData is not null)
        {
            oldData.AsSpan(0, BufferOffset + (int)CurSize + SlackForEightByteHashingEverywhere).CopyTo(newData);
            Data = null;
            ArrayPool<byte>.Shared.Return(oldData);
        }
        Data = newData;
        CurSize = buflen;
        newData[0] = 0;
        newData[1] = 0;
        newData.AsSpan(BufferOffset + (int)CurSize, SlackForEightByteHashingEverywhere).Clear();
    }

    private void WriteTail(ReadOnlySpan<byte> bytes, nuint n)
    {
        nuint maskedPos = Pos & Mask;
        if (maskedPos < TailSize)
        {
            // Just fill the tail buffer with the beginning data.
            nuint p = Size + maskedPos;
            bytes.Slice(0, (int)Math.Min(n, TailSize - maskedPos)).CopyTo(Data.AsSpan(BufferOffset + (int)p));
        }
    }

    /// <summary>Push bytes into the ring buffer (port of RingBufferWrite).</summary>
    public void Write(ReadOnlySpan<byte> bytes, nuint n)
    {
        Debug.Assert((nuint)bytes.Length >= n);
        if (Pos == 0 && n < TailSize)
        {
            // Special case for the first write: to process the first block, we don't
            // need to allocate the whole ring-buffer and we don't need the tail
            // either. However, we do this memory usage optimization only if the
            // first write is less than the tail size, which is also the input block
            // size, otherwise it is likely that other blocks will follow and we
            // will need to reallocate to the full size anyway.
            Pos = (uint)n;
            InitBuffer(Pos);
            bytes.Slice(0, (int)n).CopyTo(Data.AsSpan(BufferOffset));
            return;
        }
        if (CurSize < TotalSize)
        {
            // Lazily allocate the full buffer.
            InitBuffer(TotalSize);
            byte[] d = Data!;
            // Initialize the last two bytes to zero, so that we don't have to worry
            // later when we copy the last two bytes to the first two positions.
            d[BufferOffset + (int)Size - 2] = 0;
            d[BufferOffset + (int)Size - 1] = 0;
            // Initialize tail; might be touched by "best_len++" optimization when
            // ring buffer is "full".
            d[BufferOffset + (int)Size] = 241;
        }
        byte[] data = Data!;
        {
            nuint maskedPos = Pos & Mask;
            // The length of the writes is limited so that we do not need to worry
            // about a write
            WriteTail(bytes, n);
            if (maskedPos + n <= Size)
            {
                // A single write fits.
                bytes.Slice(0, (int)n).CopyTo(data.AsSpan(BufferOffset + (int)maskedPos));
            }
            else
            {
                // Split into two writes.
                // Copy into the end of the buffer, including the tail buffer.
                bytes.Slice(0, (int)Math.Min(n, TotalSize - maskedPos)).CopyTo(data.AsSpan(BufferOffset + (int)maskedPos));
                // Copy into the beginning of the buffer
                bytes.Slice((int)(Size - maskedPos), (int)(n - (Size - maskedPos))).CopyTo(data.AsSpan(BufferOffset));
            }
        }
        {
            bool notFirstLap = (Pos & (1u << 31)) != 0;
            uint rbPosMask = (1u << 31) - 1;
            data[0] = data[BufferOffset + (int)Size - 2];
            data[1] = data[BufferOffset + (int)Size - 1];
            Pos = (Pos & rbPosMask) + (uint)(n & rbPosMask);
            if (notFirstLap)
            {
                // Wrap, but preserve not-a-first-lap feature.
                Pos |= 1u << 31;
            }
        }
    }
}
