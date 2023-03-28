// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic;

/// <summary>
/// Simple circular buffer that helps convert managed data into QUIC_BUFFER* consumable by MsQuic.
/// Note that since this is struct and there's no finalizer, Dispose must be always called to release the unmanaged memory allocated by this struct.
/// </summary>
internal unsafe struct SendBuffer : IDisposable
{
    private const int BufferLength = 64 * 1024;

    private readonly object _syncRoot;

    /// <summary>
    /// Natively allocated memory, so that it can be passed to MsQuic without pinning.
    /// Active memory is from <see cref="_start"/> for <see cref="_length"/> bytes and might wrap around the end of the buffer.
    /// </summary>
    private byte* _buffer;
    /// <summary>
    /// Start of the active part of the buffer including data passed into MsQuic that haven't been confirmed yet.
    /// </summary>
    private int _start;
    /// <summary>
    /// Length of the active part of the buffer including data passed into MsQuic that haven't been confirmed yet.
    /// Counts the bytes from <see cref="_start"/> and might wrap around the end of the buffer.
    /// </summary>
    private int _length;
    /// <summary>
    /// Length of the active part of the buffer corresponding to data passed into MsQuic but that haven't been confirmed yet.
    /// Counts the bytes from <see cref="_start"/> and might wrap around the end of the buffer.
    /// </summary>
    private int _sentLength;

    /// <summary>
    ///
    /// </summary>
    public SendBuffer()
    {
        _syncRoot = new object();
        _buffer = (byte*)NativeMemory.Alloc((nuint)BufferLength, (nuint)sizeof(byte));
        _start = 0;
        _length = 0;
        _sentLength = 0;
    }

    public int CopyFrom(ReadOnlyMemory<byte> buffer)
    {
        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_buffer is null, this);

            // There's not enough capacity in the buffer for the whole input, copy what fits and let the caller wait for pending data to be confirmed.
            if (buffer.Length > BufferLength - _length)
            {
                buffer = buffer.Slice(0, BufferLength - _length);
            }

            // Nothing to copy here, just return.
            if (buffer.Length == 0)
            {
                return 0;
            }

            // Start copying from the end of buffered data.
            int start = (_start + _length) % BufferLength;
            // Account for wrapping around the end of the buffer.
            int length = BufferLength - start;
            ReadOnlySpan<byte> source = length >= buffer.Length ? buffer.Span : buffer.Slice(0, length).Span;
            source.CopyTo(new Span<byte>(_buffer + start, length));
            // If necessary, copy the tail of the data at the beginning of the buffer.
            if (source.Length < buffer.Length)
            {
                source = buffer.Slice(length).Span;
                source.CopyTo(new Span<byte>(_buffer, BufferLength));
            }

            // Update the length and remember if this is the last write.
            _length += buffer.Length;
            return buffer.Length;
        }
    }

    public QUIC_BUFFER* GetQuicBuffers(out int count, out int totalBytes)
    {
        lock (_syncRoot)
        {
            // We sent all of the buffered data, nothing to send.
            if (_sentLength == _length)
            {
                count = 0;
                totalBytes = 0;
                return null;
            }

            QUIC_BUFFER* buffers;

            // The data wrap around the end of the buffer and will be sent as 2 buffers.
            int sendStart = (_start + _sentLength) % BufferLength;
            int sendLength = _length - _sentLength;
            buffers = (QUIC_BUFFER*)NativeMemory.AllocZeroed(2, (nuint)sizeof(QUIC_BUFFER));
            if (sendStart + sendLength > BufferLength)
            {
                count = 2;
                buffers[0].Buffer = _buffer + sendStart;
                buffers[0].Length = (uint)(BufferLength - sendStart);
                buffers[1].Buffer = _buffer;
                buffers[1].Length = (uint)(sendLength + sendStart) % BufferLength;
                totalBytes = (int)(buffers[0].Length + buffers[1].Length);
            }
            else
            {
                count = 1;
                buffers[0].Buffer = _buffer + sendStart;
                buffers[0].Length = (uint)sendLength;
                buffers[1].Buffer = null;
                buffers[1].Length = 0;
                totalBytes = (int)buffers[0].Length;
            }
            _sentLength = _length;
            return buffers;
        }
    }

    public void Discard(QUIC_BUFFER* buffers)
    {
        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_buffer is null, this);

            int length = (int)buffers[0].Length + (int)buffers[1].Length;
            NativeMemory.Free(buffers);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, _length, nameof(length));

            _start = (_start + length) % BufferLength;
            _length -= length;
            _sentLength -= length;
        }
    }

    /// <summary>
    /// Releases all the native memory.
    /// </summary>
    public void Dispose()
    {
        lock (_syncRoot)
        {
            NativeMemory.Free(_buffer);
            _buffer = null;
            _start = 0;
            _length = 0;
            _sentLength = 0;
        }
    }
}
