// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// A page-buffered <see cref="ITargetReadCache"/> tuned for spatial locality: the cache holds a
/// single fixed-size page and serves any read that lies entirely within the currently-cached
/// page. Reads that miss the cache - or that span page boundaries, or that are larger than the
/// page itself - fall through to the supplied <see cref="RawReadDelegate"/>.
/// </summary>
/// <remarks>
/// Best suited to walks over contiguous memory (for example a heap walk that touches each
/// object's header and a couple of trailing fields). Caches that need random-access locality or
/// multi-page coverage should implement <see cref="ITargetReadCache"/> directly.
/// </remarks>
public sealed class LinearReadCache : ITargetReadCache
{
    /// <summary>Default page size used when none is supplied to the constructor.</summary>
    public const uint DefaultPageSize = 0x1000;

    private readonly uint _pageSize;
    private readonly byte[] _page;
    private ulong _currPageStart;
    private uint _currPageSize;

    public LinearReadCache()
        : this(DefaultPageSize)
    {
    }

    public LinearReadCache(uint pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfZero(pageSize);
        _pageSize = pageSize;
        _page = new byte[pageSize];
    }

    public void ReadBuffer(ulong address, Span<byte> destination, RawReadDelegate fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        // Requests larger than a page can't fit in the cache buffer - read directly.
        if ((uint)destination.Length >= _pageSize)
        {
            fallback(address, destination);
            return;
        }

        // Refresh the cached page if the request misses it.
        if (address < _currPageStart || address - _currPageStart >= _currPageSize)
        {
            ulong pageStart = address - (address % _pageSize);
            try
            {
                fallback(pageStart, _page.AsSpan(0, (int)_pageSize));
                _currPageStart = pageStart;
                _currPageSize = _pageSize;
            }
            catch
            {
                // The page is unreadable (e.g. unmapped). Drop any cached state and let the
                // direct read surface the failure.
                _currPageStart = 0;
                _currPageSize = 0;
                fallback(address, destination);
                return;
            }
        }

        ulong offset = address - _currPageStart;
        if (offset + (ulong)destination.Length > _currPageSize)
        {
            // Request straddles the end of the cached page.
            fallback(address, destination);
            return;
        }

        _page.AsSpan((int)offset, destination.Length).CopyTo(destination);
    }

    public void Invalidate()
    {
        _currPageStart = 0;
        _currPageSize = 0;
    }

    public void Dispose()
    {
        // No unmanaged resources; Invalidate handles state cleanup.
    }
}
