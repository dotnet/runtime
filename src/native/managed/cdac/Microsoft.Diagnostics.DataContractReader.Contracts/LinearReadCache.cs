// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Microsoft.Diagnostics.DataContractReader;
public sealed class LinearReadCache : ITargetReadCache, IDisposable
{
    // Typical page size
    private const uint PageSize = 0x1000;
    private readonly byte[] _page = new byte[PageSize];
    private ulong _currPageStart;
    private uint _currPageSize;

    public bool TryRead(ulong addr, Span<byte> dest, RawReadDelegate readDelegate)
    {
        // If the request misses the currently-cached page, try to load the page
        // containing it. If that fails (e.g. the page is unmapped), or the request
        // straddles the end of the cached page, fall back to a direct read.
        if (addr < _currPageStart || addr - _currPageStart >= _currPageSize)
        {
            if (!MoveToPage(addr, readDelegate))
                return DirectRead(addr, dest, readDelegate);
        }

        ulong offset = addr - _currPageStart;
        if (offset + (ulong)dest.Length > _currPageSize)
            return DirectRead(addr, dest, readDelegate);

        _page.AsSpan((int)offset, dest.Length).CopyTo(dest);
        return true;
    }

    public void Invalidate()
    {
        // Clear the current page cache
        _currPageStart = 0;
        _currPageSize = 0;
        Array.Clear(_page, 0, _page.Length);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private bool MoveToPage(ulong addr, RawReadDelegate readDelegate)
    {
        ulong pageStart = addr - (addr % PageSize);
        try
        {
            readDelegate(pageStart, _page.AsSpan(0, (int)PageSize));
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

    private static bool DirectRead(ulong addr, Span<byte> dest, RawReadDelegate readDelegate)
    {
        try
        {
            readDelegate(addr, dest);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
