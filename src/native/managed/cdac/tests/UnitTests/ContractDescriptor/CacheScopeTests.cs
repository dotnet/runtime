// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure.ContractDescriptor;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

public class CacheScopeTests
{
    private static ContractDescriptorTarget CreateTarget(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        ContractDescriptorBuilder builder = new(helpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetGlobals(Array.Empty<(string, ulong, string?)>())
            .SetContracts(Array.Empty<string>());

        // Add a 0x200-byte region with deterministic byte pattern starting at 0x10000.
        const ulong baseAddr = 0x10000;
        byte[] data = new byte[0x200];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)i;
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = baseAddr,
            Data = data,
            Name = "Pattern"
        });

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));
        return target!;
    }

    private sealed class CountingCache : ITargetReadCache
    {
        public int InvalidateCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int ReadCount { get; private set; }
        public int FallbackCount { get; private set; }

        public void ReadBuffer(ulong address, Span<byte> destination, RawReadDelegate fallback)
        {
            ReadCount++;
            FallbackCount++;
            fallback(address, destination);
        }

        public void Invalidate() => InvalidateCount++;
        public void Dispose() => DisposeCount++;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadsInsideScope_RouteThroughCache(MockTarget.Architecture arch)
    {
        ContractDescriptorTarget target = CreateTarget(arch);
        CountingCache cache = new();

        using (target.BeginCacheScope(cache))
        {
            Span<byte> buf = stackalloc byte[8];
            target.ReadBuffer(0x10000, buf);
            target.ReadBuffer(0x10010, buf);
        }

        Assert.Equal(2, cache.ReadCount);
        Assert.Equal(2, cache.FallbackCount);
        Assert.Equal(1, cache.InvalidateCount);
        Assert.Equal(1, cache.DisposeCount);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadsOutsideScope_BypassCache(MockTarget.Architecture arch)
    {
        ContractDescriptorTarget target = CreateTarget(arch);
        CountingCache cache = new();

        using (target.BeginCacheScope(cache))
        {
            Span<byte> buf = stackalloc byte[4];
            target.ReadBuffer(0x10000, buf);
        }

        Assert.Equal(1, cache.ReadCount);

        Span<byte> outsideBuf = stackalloc byte[4];
        target.ReadBuffer(0x10020, outsideBuf);
        Assert.Equal(1, cache.ReadCount); // unchanged: cache was disposed
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TypedReadsInsideScope_RouteThroughCache(MockTarget.Architecture arch)
    {
        ContractDescriptorTarget target = CreateTarget(arch);
        CountingCache cache = new();

        using (target.BeginCacheScope(cache))
        {
            Assert.True(target.TryRead<uint>(0x10000, out _));
            Assert.True(target.TryReadPointer(0x10010, out _));
        }

        // TryRead<uint> and TryReadPointer (which decomposes to TryReadCore) both must funnel
        // through TryReadBuffer and hit the cache.
        Assert.Equal(2, cache.ReadCount);
    }

    [Fact]
    public void NestedScope_Throws()
    {
        ContractDescriptorTarget target = CreateTarget(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true });
        using IDisposable outer = target.BeginCacheScope(new CountingCache());
        Assert.Throws<InvalidOperationException>(() => target.BeginCacheScope(new CountingCache()));
    }

    [Fact]
    public void ScopeDispose_IsIdempotent()
    {
        ContractDescriptorTarget target = CreateTarget(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true });
        CountingCache cache = new();
        IDisposable scope = target.BeginCacheScope(cache);

        scope.Dispose();
        scope.Dispose();

        Assert.Equal(1, cache.InvalidateCount);
        Assert.Equal(1, cache.DisposeCount);
    }

    [Fact]
    public void ScopeDispose_AllowsNewScope()
    {
        ContractDescriptorTarget target = CreateTarget(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true });
        using (target.BeginCacheScope(new CountingCache())) { }
        using IDisposable second = target.BeginCacheScope(new CountingCache());
        // Did not throw.
    }

    [Fact]
    public void BeginCacheScope_NullCache_Throws()
    {
        ContractDescriptorTarget target = CreateTarget(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true });
        Assert.Throws<ArgumentNullException>(() => target.BeginCacheScope(null!));
    }
}
