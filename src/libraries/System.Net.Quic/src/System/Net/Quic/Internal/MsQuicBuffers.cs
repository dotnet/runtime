// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Handles to pinned memory inside individual buffers, i.e. _buffers[i].Buffer.
    private MemoryHandle[] _handles;
    // Native memory block which holds the pinned memory pointers from _handles and can be passed to MsQuic as QUIC_BUFFER*. Size corresponds to _handles.Length.
    private QUIC_BUFFER* _buffers;
    // Number of actively used QUIC_BUFFER instances in _buffers, maybe smaller than the actual _buffers size.
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
        Debug.Assert(_handles.Length == 0 || _handles[0].Pointer is null);
        Debug.Assert(_handles.Length == 0 || _buffers[0].Buffer is null);
        Debug.Assert(_handles.Length == 0 || _buffers[0].Length == 0);

        if (_handles.Length < count)
        {
            _handles = new MemoryHandle[count];
            FreeNativeMemory();
            _buffers = (QUIC_BUFFER*)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(QUIC_BUFFER));
        }

        _count = count;
    }

    private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
    {
        Debug.Assert(_handles.Length > index);
        Debug.Assert(_handles[index].Pointer is null);

        Debug.Assert(_count > index);
        Debug.Assert(_buffers[index].Buffer is null);
        Debug.Assert(_buffers[index].Length == 0);

        MemoryHandle handle = buffer.Pin();

        _handles[index] = handle;
        _buffers[index].Buffer = (byte*)handle.Pointer;
        _buffers[index].Length = (uint)buffer.Length;
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
        Debug.Assert(_handles is not null);
        Debug.Assert(_buffers is not null || _count == 0);
        Debug.Assert(_handles.Length >= _count);

        Reserve(inputs.Count);
        for (int i = 0; i < inputs.Count; ++i)
        {
            SetBuffer(i, toBuffer(inputs[i]));
        }
    }

    /// <summary>
    /// Initializes QUIC_BUFFER* with the provided buffer.
    /// Note that the struct either needs to be freshly created via new or previously cleaned up with Reset.
    /// </summary>
    /// <param name="buffer">Buffer to be passed to MsQuic as QUIC_BUFFER*.</param>
    public void Initialize(ReadOnlyMemory<byte> buffer)
    {
        Debug.Assert(_handles is not null);
        Debug.Assert(_buffers is not null || _count == 0);
        Debug.Assert(_handles.Length >= _count);

        Reserve(1);
        SetBuffer(0, buffer);
    }

    /// <summary>
    /// Unpin the memory of individual buffers and allows reuse of this struct.
    /// </summary>
    public void Reset()
    {
        Debug.Assert(_handles is not null);
        Debug.Assert(_buffers is not null || _count == 0);
        Debug.Assert(_handles.Length >= _count);

        for (int i = 0; i < _handles.Length; ++i)
        {
            if (i >= _count)
            {
                Debug.Assert(_handles[i].Pointer is null);
                Debug.Assert(_buffers[i].Buffer is null);
                Debug.Assert(_buffers[i].Length == 0);
                continue;
            }

            _handles[i].Dispose();
            _handles[i] = default;
            _buffers[i].Buffer = null;
            _buffers[i].Length = 0;
        }
    }

    /// <summary>
    /// Releases all the native memory.
    /// </summary>
    public void Dispose()
    {
        Reset();
        FreeNativeMemory();
    }
}
