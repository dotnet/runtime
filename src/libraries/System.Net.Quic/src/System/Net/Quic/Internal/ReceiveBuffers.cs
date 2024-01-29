// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Quic;

namespace System.Net.Quic;

internal struct ReceiveBuffers
{
    private const int MaxBufferedBytes = 64 * 1024;

    private readonly object _syncRoot;
    private MultiArrayBuffer _buffer;
    private bool _final;

    public ReceiveBuffers()
    {
        _syncRoot = new object();
        _buffer = default;
        _final = default;
    }

    public void SetFinal()
    {
        lock (_syncRoot)
        {
            _final = true;
        }
    }

    public bool HasCapacity()
    {
        lock (_syncRoot)
        {
            return _buffer.ActiveMemory.Length < MaxBufferedBytes;
        }
    }

    public int CopyFrom(ReadOnlySpan<QUIC_BUFFER> quicBuffers, int totalLength, bool final)
    {
        lock (_syncRoot)
        {
            if (_buffer.ActiveMemory.Length > MaxBufferedBytes - totalLength)
            {
                totalLength = MaxBufferedBytes - _buffer.ActiveMemory.Length;
                final = false;
            }

            _final = final;
            _buffer.EnsureAvailableSpace(totalLength);

            int totalCopied = 0;
            for (int i = 0; i < quicBuffers.Length; ++i)
            {
                Span<byte> quicBuffer = quicBuffers[i].Span;
                if (totalLength < quicBuffer.Length)
                {
                    quicBuffer = quicBuffer.Slice(0, totalLength);
                }
                _buffer.AvailableMemory.CopyFrom(quicBuffer);
                _buffer.Commit(quicBuffer.Length);
                totalCopied += quicBuffer.Length;
                totalLength -= quicBuffer.Length;
            }
            return totalCopied;
        }
    }

    public int CopyTo(Memory<byte> buffer, out bool completed, out bool empty)
    {
        lock (_syncRoot)
        {
            int copied = 0;
            if (!_buffer.IsEmpty)
            {
                MultiMemory activeBuffer = _buffer.ActiveMemory;
                copied = Math.Min(buffer.Length, activeBuffer.Length);
                activeBuffer.Slice(0, copied).CopyTo(buffer.Span);
                _buffer.Discard(copied);
            }

            completed = _buffer.IsEmpty && _final;
            empty = _buffer.IsEmpty;

            return copied;
        }
    }
}
