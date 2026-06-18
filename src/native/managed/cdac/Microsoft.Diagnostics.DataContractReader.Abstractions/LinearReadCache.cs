// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Linear page cache used by the per-object heap walk.
using System;
using System.Buffers.Binary;
using Microsoft.Diagnostics.DataContractReader;
namespace Microsoft.Diagnostics.DataContractReader.Abstractions;

public sealed class LinearReadCache
{
    // Typical page size
    private const uint PageSize = 0x1000;

    private readonly Target _target;
    private readonly byte[] _page = new byte[PageSize];
    private ulong _currPageStart;
    private uint _currPageSize;

    public LinearReadCache(Target target)
    {
        _target = target;
    }

    public bool TryReadPointer(ulong addr, out TargetPointer value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        buffer = buffer.Slice(0, _target.PointerSize);
        if (!TryRead(addr, buffer))
        {
            value = TargetPointer.Null;
            return false;
        }
        value = _target.ReadPointerFromSpan(buffer);
        return true;
    }

    public bool TryRead(ulong addr, Span<byte> dest)
    {
        // If the request misses the currently-cached page, try to load the page
        // containing it. If that fails (e.g. the page is unmapped), or the request
        // straddles the end of the cached page, fall back to a direct read.
        if (addr < _currPageStart || addr - _currPageStart >= _currPageSize)
        {
            if (!MoveToPage(addr))
                return DirectRead(addr, dest);
        }

        ulong offset = addr - _currPageStart;
        if (offset + (ulong)dest.Length > _currPageSize)
            return DirectRead(addr, dest);

        _page.AsSpan((int)offset, dest.Length).CopyTo(dest);
        return true;
    }

    public void Flush()
    {
        // Clear the current page cache
        _currPageStart = 0;
        _currPageSize = 0;
        Array.Clear(_page, 0, _page.Length);
    }

    private bool MoveToPage(ulong addr)
    {
        ulong pageStart = addr - (addr % PageSize);
        try
        {
            _target.ReadBuffer(pageStart, _page.AsSpan(0, (int)PageSize));
            _currPageStart = pageStart;
            _currPageSize = PageSize;
            return true;
        }
        catch
        {
            _currPageStart = 0;
            _currPageSize = 0;
            return false;
        }
    }

    private bool DirectRead(ulong addr, Span<byte> dest)
    {
        try
        {
            _target.ReadBuffer(addr, dest);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
