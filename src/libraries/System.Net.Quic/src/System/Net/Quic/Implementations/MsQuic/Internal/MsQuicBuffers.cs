// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

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

    /// <summary>
    /// The method initializes QUIC_BUFFER* with data from inputs, converted via toBuffer.
    /// Note that the struct either needs to be freshly created via new or previously cleaned up with Reset.
    /// </summary>
    /// <param name="inputs">Inputs to get their byte array, pin them and pepare them to be passed to MsQuic as QUIC_BUFFER*.</param>
    /// <param name="toBuffer">Method extracting byte array from the inputs, e.g. applicationProtocol.Protocol.</param>
    /// <typeparam name="T">The type of the inputs.</typeparam>
    public void Initialize<T>(IList<T> inputs, Func<T, ReadOnlyMemory<byte>> toBuffer)
    {
        if (_handles.Length < inputs.Count)
        {
            _handles = new MemoryHandle[inputs.Count];
        }
        if (_count < inputs.Count)
        {
            FreeNativeMemory();
            _buffers = (QUIC_BUFFER*)NativeMemory.Alloc((nuint)sizeof(QUIC_BUFFER), (nuint)inputs.Count);
        }

        _count = inputs.Count;

        for (int i = 0; i < inputs.Count; ++i)
        {
            ReadOnlyMemory<byte> buffer = toBuffer(inputs[i]);
            MemoryHandle handle = buffer.Pin();

            _handles[i] = handle;
            _buffers[i].Buffer = (byte*)handle.Pointer;
            _buffers[i].Length = (uint)buffer.Length;
        }
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
