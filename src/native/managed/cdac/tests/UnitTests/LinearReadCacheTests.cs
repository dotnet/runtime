// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class LinearReadCacheTests
{
    private sealed class ReadCounter
    {
        public List<(ulong Address, int Length)> Reads { get; } = new();
        public Func<ulong, int, byte[]>? Source { get; init; }
        public Action<ulong, int>? OnRead { get; init; }

        public void Read(ulong address, Span<byte> destination)
        {
            Reads.Add((address, destination.Length));
            OnRead?.Invoke(address, destination.Length);
            if (Source is { } src)
                src(address, destination.Length).AsSpan().CopyTo(destination);
        }
    }

    [Fact]
    public void ReadWithinPage_PopulatesPageOnce()
    {
        var counter = new ReadCounter
        {
            Source = (addr, len) =>
            {
                byte[] b = new byte[len];
                for (int i = 0; i < len; i++)
                    b[i] = (byte)((addr + (ulong)i) & 0xFF);
                return b;
            }
        };
        using var cache = new LinearReadCache(pageSize: 0x100);

        Span<byte> first = stackalloc byte[4];
        Span<byte> second = stackalloc byte[8];
        cache.ReadBuffer(0x1010, first, counter.Read);
        cache.ReadBuffer(0x1020, second, counter.Read);

        Assert.Single(counter.Reads);
        Assert.Equal((ulong)0x1000, counter.Reads[0].Address);
        Assert.Equal(0x100, counter.Reads[0].Length);
        Assert.Equal(new byte[] { 0x10, 0x11, 0x12, 0x13 }, first.ToArray());
        Assert.Equal(new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27 }, second.ToArray());
    }

    [Fact]
    public void ReadCrossingPageBoundary_FallsBackToDirectRead()
    {
        var counter = new ReadCounter
        {
            Source = (addr, len) => new byte[len]
        };
        using var cache = new LinearReadCache(pageSize: 0x100);

        Span<byte> straddling = stackalloc byte[16];
        cache.ReadBuffer(0x10F8, straddling, counter.Read);

        // Expect a page load at 0x1000 followed by a direct read at 0x10F8 (request straddles end).
        Assert.Equal(2, counter.Reads.Count);
        Assert.Equal((ulong)0x1000, counter.Reads[0].Address);
        Assert.Equal(0x100, counter.Reads[0].Length);
        Assert.Equal((ulong)0x10F8, counter.Reads[1].Address);
        Assert.Equal(16, counter.Reads[1].Length);
    }

    [Fact]
    public void ReadLargerThanPage_BypassesCache()
    {
        var counter = new ReadCounter { Source = (a, l) => new byte[l] };
        using var cache = new LinearReadCache(pageSize: 0x100);

        Span<byte> big = new byte[0x200];
        cache.ReadBuffer(0x2000, big, counter.Read);

        Assert.Single(counter.Reads);
        Assert.Equal((ulong)0x2000, counter.Reads[0].Address);
        Assert.Equal(0x200, counter.Reads[0].Length);
    }

    [Fact]
    public void ReadAcrossPages_TriggersNewPageLoad()
    {
        var counter = new ReadCounter { Source = (a, l) => new byte[l] };
        using var cache = new LinearReadCache(pageSize: 0x100);

        Span<byte> a = stackalloc byte[4];
        Span<byte> b = stackalloc byte[4];
        cache.ReadBuffer(0x1000, a, counter.Read);
        cache.ReadBuffer(0x1200, b, counter.Read);

        Assert.Equal(2, counter.Reads.Count);
        Assert.Equal((ulong)0x1000, counter.Reads[0].Address);
        Assert.Equal((ulong)0x1200, counter.Reads[1].Address);
    }

    [Fact]
    public void Invalidate_ForcesPageReload()
    {
        var counter = new ReadCounter { Source = (a, l) => new byte[l] };
        using var cache = new LinearReadCache(pageSize: 0x100);

        Span<byte> a = stackalloc byte[4];
        cache.ReadBuffer(0x1000, a, counter.Read);
        cache.Invalidate();
        cache.ReadBuffer(0x1000, a, counter.Read);

        Assert.Equal(2, counter.Reads.Count);
        Assert.All(counter.Reads, r => Assert.Equal((ulong)0x1000, r.Address));
    }

    [Fact]
    public void PageLoadFailure_FallsBackAndKeepsCacheEmpty()
    {
        int callIndex = 0;
        var counter = new ReadCounter
        {
            Source = (a, l) => new byte[l],
            OnRead = (a, l) =>
            {
                // First call (the page load) throws; subsequent calls (the fallback direct read,
                // then a follow-up cached read) succeed.
                if (callIndex++ == 0)
                    throw new VirtualReadException("simulated page fault");
            }
        };
        using var cache = new LinearReadCache(pageSize: 0x100);

        Span<byte> a = stackalloc byte[4];
        cache.ReadBuffer(0x1010, a, counter.Read);

        // Page load + direct read.
        Assert.Equal(2, counter.Reads.Count);
        Assert.Equal((ulong)0x1000, counter.Reads[0].Address);
        Assert.Equal((ulong)0x1010, counter.Reads[1].Address);

        // Second access should retry the page load because the first one was discarded.
        cache.ReadBuffer(0x1010, a, counter.Read);
        Assert.Equal(3, counter.Reads.Count);
        Assert.Equal((ulong)0x1000, counter.Reads[2].Address);
    }

    [Fact]
    public void Constructor_RejectsZeroPageSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearReadCache(pageSize: 0));
    }

    [Fact]
    public void ReadBuffer_NullFallback_Throws()
    {
        using var cache = new LinearReadCache(pageSize: 0x100);
        byte[] buf = new byte[4];
        Assert.Throws<ArgumentNullException>(() => cache.ReadBuffer(0x1000, buf, null!));
    }
}
