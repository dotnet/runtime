// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SuperPmi;

// A bump allocator over pinned arrays, for memory that is handed to the JIT. The memory is zeroed,
// and stays valid for as long as the arena is alive.
public sealed unsafe class PinnedArena
{
    private const int ChunkSize = 64 * 1024;

    private readonly List<byte[]> _chunks = [];
    private nuint _next;
    private nuint _end;

    public void* Allocate(nuint size, nuint alignment = 8)
    {
        nuint start = AlignUp(_next, alignment);
        if (_next == 0 || start + size > _end)
        {
            byte[] chunk = GC.AllocateArray<byte>((int)Math.Max(ChunkSize, size + alignment), pinned: true);
            _chunks.Add(chunk);
            _next = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(chunk));
            _end = _next + (nuint)chunk.Length;
            start = AlignUp(_next, alignment);
        }

        _next = start + size;
        return (void*)start;
    }

    public T* Allocate<T>(nuint count = 1) where T : unmanaged => (T*)Allocate((nuint)sizeof(T) * count, (nuint)Math.Min(sizeof(T), 8));

    public byte* Copy(ReadOnlySpan<byte> data)
    {
        byte* result = (byte*)Allocate((nuint)data.Length, 1);
        data.CopyTo(new Span<byte>(result, data.Length));
        return result;
    }

    private static nuint AlignUp(nuint value, nuint alignment) => (value + alignment - 1) & ~(alignment - 1);
}
