// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Quic;
using System.Threading;

namespace System.Net.Quic;

/// <summary>
/// Helper class to convert managed data into QUIC_BUFFER* consumable by MsQuic.
/// It also allows reuse with repeated Reset/Initialize calls, e.g. new, Initialize, (use), Reset, Initialize, (use), Reset, Initialize, (use), Dispose.
/// Note that since this is struct and there's no finalizer, Dispose must be always called to release the unmanaged memory allocated by this struct.
/// </summary>
internal unsafe struct MsQuicBuffers : IDisposable
{
    // Native memory block which holds the pinned memory pointers from _handles and can be passed to MsQuic as QUIC_BUFFER*.
    private QUIC_BUFFER* _buffers;
    // Number of QUIC_BUFFER instance currently allocated in _buffers, so that we can reuse the memory instead of reallocating.
    private int _count;
    private bool _initialized;
    private bool _disposed;

    public MsQuicBuffers()
    {
        _buffers = null;
        _count = 0;
        _initialized=false;
        _disposed = false;
    }

    public QUIC_BUFFER* Buffers
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(MsQuicBuffers));
            Debug.Assert(_initialized);
            return _buffers;
        }
    }

    public int Count => _count;

    private void FreeNativeMemory()
    {
        QUIC_BUFFER* buffers = _buffers;
        _buffers = null;
        NativeMemory.Free(buffers);
        _count = 0;
    }

    private void Reserve(int count)
    {
        if (count > _count)
        {
            FreeNativeMemory();
            _buffers = (QUIC_BUFFER*)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(QUIC_BUFFER));
            _count = count;
        }
    }

    private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
    {
        Debug.Assert(_buffers[index].Buffer == null);
        _buffers[index].Buffer = (byte*)NativeMemory.Alloc((nuint)buffer.Length, (nuint)sizeof(byte));
        _buffers[index].Length = (uint)buffer.Length;
        buffer.Span.CopyTo(_buffers[index].Span);
    }

    /// <summary>
    /// Initializes QUIC_BUFFER* with data from inputs, converted via toBuffer.
    /// Note that the struct either needs to be freshly created via new or previously cleaned up with Reset.
    /// </summary>
    /// <param name="inputs">Inputs to get their byte array, copy them to be passed to MsQuic as QUIC_BUFFER*.</param>
    /// <param name="toBuffer">Method extracting byte array from the inputs, e.g. applicationProtocol.Protocol.</param>
    /// <typeparam name="T">The type of the inputs.</typeparam>
    public void Initialize<T>(IList<T> inputs, Func<T, ReadOnlyMemory<byte>> toBuffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(MsQuicBuffers));
        Debug.Assert(!_initialized);
        Reserve(inputs.Count);
        for (int i = 0; i < inputs.Count; ++i)
        {
            SetBuffer(i, toBuffer(inputs[i]));
        }
        _initialized = true;
    }

    /// <summary>
    /// Initializes QUIC_BUFFER* with the provided buffer.
    /// Note that the struct either needs to be freshly created via new or previously cleaned up with Reset.
    /// </summary>
    /// <param name="buffer">Buffer to be passed to MsQuic as QUIC_BUFFER*.</param>
    public void Initialize(ReadOnlyMemory<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(MsQuicBuffers));
        Debug.Assert(!_initialized);
        Reserve(1);
        SetBuffer(0, buffer);
        _initialized = true;
    }

    /// <summary>
    /// Release the native memory of individual buffers and allows reuse of this struct.
    /// </summary>
    public void Reset() => Reset(disposing: false);

    private void Reset(bool disposing)
    {
        ObjectDisposedException.ThrowIf(_disposed && !disposing, typeof(MsQuicBuffers));
        for (int i = 0; i < _count; ++i)
        {
            if (_buffers[i].Buffer is null)
            {
                break;
            }
            byte* buffer = _buffers[i].Buffer;
            _buffers[i].Buffer = null;
            NativeMemory.Free(buffer);
            _buffers[i].Length = 0;
        }
        _initialized = false;
    }

    /// <summary>
    /// Releases all the native memory.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        Reset(disposing: true);
        FreeNativeMemory();
    }
}
