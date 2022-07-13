// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic;

/// <summary>
/// Helper class to convert managed data into QUIC_BUFFER* consumable by MsQuic.
/// It also allows reuse with repeated Reset/Initialize calls, e.g. new, Initialize, (use), Reset, Initialize, (use), Reset, Initialize, (use), Dispose.
/// Note that since this is struct and there's no finalizer, Dispose must be always called to release the unmanaged memory allocated by this struct.
/// </summary>
internal unsafe struct MsQuicBuffers : IDisposable
{
    // Handles to pinned memory blocks from the user.
    private MemoryHandle[] _handles;
    // Native memory block which holds the pinned memory pointers from _handles and can be passed to MsQuic as QUIC_BUFFER*.
    private QUIC_BUFFER* _buffers;
    // Number of QUIC_BUFFER instance currently allocated in _buffers, so that we can reuse the memory instead of reallocating.
    private int _count;

    public MsQuicBuffers()
    {
        _handles = Array.Empty<MemoryHandle>();
        _buffers = null;
        _count = 0;
    }

    public QUIC_BUFFER* Buffers => _buffers;
    public int Count => _count;

    private void FreeNativeMemory()
    {
        QUIC_BUFFER* buffers = _buffers;
        _buffers = null;
        NativeMemory.Free(buffers);
    }

    private void Reserve(int count)
    {
        if (_handles.Length < count)
        {
            _handles = new MemoryHandle[count];
            FreeNativeMemory();
            _buffers = (QUIC_BUFFER*)NativeMemory.Alloc((nuint)count, (nuint)sizeof(QUIC_BUFFER));
        }

        _count = count;
    }

    private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
    {
        MemoryHandle handle = buffer.Pin();

        _handles[index] = handle;
        _buffers[index].Buffer = (byte*)handle.Pointer;
        _buffers[index].Length = (uint)buffer.Length;
    }

    /// <summary>
    /// Initializes QUIC_BUFFER* with data from inputs, converted via toBuffer.
    /// Note that the struct either needs to be freshly created via new or previously cleaned up with Reset.
    /// </summary>
    /// <param name="inputs">Inputs to get their byte array, pin them and pepare them to be passed to MsQuic as QUIC_BUFFER*.</param>
    /// <param name="toBuffer">Method extracting byte array from the inputs, e.g. applicationProtocol.Protocol.</param>
    /// <typeparam name="T">The type of the inputs.</typeparam>
    public void Initialize<T>(IList<T> inputs, Func<T, ReadOnlyMemory<byte>> toBuffer)
    {
        Reserve(inputs.Count);

        for (int i = 0; i < inputs.Count; ++i)
        {
            ReadOnlyMemory<byte> buffer = toBuffer(inputs[i]);
            SetBuffer(i, buffer);
        }
    }

    /// <summary>
    /// Initializes QUIC_BUFFER* with the provided buffer.
    /// Note that the struct either needs to be freshly created via new or previously cleaned up with Reset.
    /// </summary>
    /// <param name="buffer">Buffer to be passed to MsQuic as QUIC_BUFFER*.</param>
    public void Initialize(ReadOnlyMemory<byte> buffer)
    {
        Reserve(1);
        SetBuffer(0, buffer);
    }

    /// <summary>
    /// Unpins the managed memory and allows reuse of this struct.
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < _count; ++i)
        {
            _handles[i].Dispose();
        }
    }

    /// <summary>
    /// Apart from unpinning the managed memory, it returns the shared buffer,
    /// but most importantly it releases the unmanaged memory.
    /// </summary>
    public void Dispose()
    {
        Reset();
        FreeNativeMemory();
    }
}
