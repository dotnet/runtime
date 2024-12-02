// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Xunit;

using System;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class HotColdLookupTests
{
    private static readonly TargetPointer HotColdMapAddr = 0x1000; // arbitrary

    private static Target CreateMockTarget((uint Cold, uint Hot)[] entries)
    {
        Mock<Target> target = new();
        target.Setup(t => t.Read<uint>(It.IsAny<ulong>()))
            .Returns((ulong addr) =>
            {
                for (uint i = 0; i < entries.Length; i++)
                {
                    if (addr == HotColdMapAddr + (i * 2) * sizeof(uint))
                        return entries[i].Cold;

                    if (addr == HotColdMapAddr + (i * 2 + 1) * sizeof(uint))
                        return entries[i].Hot;
                }

                throw new NotImplementedException();
            });

        return target.Object;
    }

    [Fact]
    public void GetHotFunctionIndex()
    {
        (uint Cold, uint Hot)[] entries =
        [
            (0x100, 0x10),
            (0x200, 0x20),
            (0x300, 0x30),
            (0x400, 0x40),
        ];

        uint numHotColdMap = (uint)entries.Length * 2;
        Target target = CreateMockTarget(entries);

        var lookup = HotColdLookup.Create(target);
        foreach (var entry in entries)
        {
            // Hot part as input
            uint hotIndex = lookup.GetHotFunctionIndex(numHotColdMap, HotColdMapAddr, entry.Hot);
            Assert.Equal(entry.Hot, hotIndex);

            // Cold part as input
            hotIndex = lookup.GetHotFunctionIndex(numHotColdMap, HotColdMapAddr, entry.Cold);
            Assert.Equal(entry.Hot, hotIndex);
        }
    }

    [Fact]
    public void GetHotFunctionIndex_ColdFunclet()
    {
        (uint Cold, uint Hot)[] entries =
        [
            (0x100, 0x10),
            (0x200, 0x20),
        ];

        uint numHotColdMap = (uint)entries.Length * 2;
        Target target = CreateMockTarget(entries);

        var lookup = HotColdLookup.Create(target);

        // Cold funclet - between two cold blocks' indexes
        uint functionIndex = 0x110;
        uint hotIndex = lookup.GetHotFunctionIndex(numHotColdMap, HotColdMapAddr, functionIndex);
        Assert.Equal(entries[0].Hot, hotIndex);
    }

    [Fact]
    public void GetHotFunctionIndex_EmptyMap()
    {
        Target target = CreateMockTarget([]);

        var lookup = HotColdLookup.Create(target);

        // Function must be hot if there is no map
        uint functionIndex = 0x110;
        uint hotIndex = lookup.GetHotFunctionIndex(0, HotColdMapAddr, functionIndex);
        Assert.Equal(functionIndex, hotIndex);
    }

    [Fact]
    public void GetHotFunctionIndex_NoEntryInMap()
    {
        (uint Cold, uint Hot)[] entries =
        [
            (0x100, 0x10),
            (0x200, 0x20),
        ];

        uint numHotColdMap = (uint)entries.Length * 2;
        Target target = CreateMockTarget(entries);

        var lookup = HotColdLookup.Create(target);

        // Function must be hot if it is not in the map
        uint functionIndex = 0x30;
        uint hotIndex = lookup.GetHotFunctionIndex(numHotColdMap, HotColdMapAddr, functionIndex);
        Assert.Equal(functionIndex, hotIndex);
    }

    [Fact]
    public void TryGetColdFunctionIndex()
    {
        (uint Cold, uint Hot)[] entries =
        [
            (0x100, 0x10),
            (0x200, 0x20),
            (0x300, 0x30),
            (0x400, 0x40),
        ];

        uint numHotColdMap = (uint)entries.Length * 2;
        Target target = CreateMockTarget(entries);

        var lookup = HotColdLookup.Create(target);
        foreach (var entry in entries)
        {
            // Hot part as input
            bool res = lookup.TryGetColdFunctionIndex(numHotColdMap, HotColdMapAddr, entry.Hot, out uint coldFunctionIndex);
            Assert.True(res);
            Assert.Equal(entry.Cold, coldFunctionIndex);

            // Cold part as input
            res = lookup.TryGetColdFunctionIndex(numHotColdMap, HotColdMapAddr, entry.Cold, out coldFunctionIndex);
            Assert.True(res);
            Assert.Equal(entry.Cold, coldFunctionIndex);
        }
    }

    [Fact]
    public void TryGetColdFunctionIndex_ColdFunclet()
    {
        (uint Cold, uint Hot)[] entries =
        [
            (0x100, 0x10),
            (0x200, 0x20),
        ];

        uint numHotColdMap = (uint)entries.Length * 2;
        Target target = CreateMockTarget(entries);

        var lookup = HotColdLookup.Create(target);

        // Cold funclet - between two cold blocks' indexes
        uint functionIndex = 0x110;
        bool res = lookup.TryGetColdFunctionIndex(numHotColdMap, HotColdMapAddr, functionIndex, out uint coldFunctionIndex);
        Assert.True(res);
        Assert.Equal(entries[0].Cold, coldFunctionIndex);
    }

    [Fact]
    public void TryGetColdFunctionIndex_EmptyMap()
    {
        Target target = CreateMockTarget([]);

        var lookup = HotColdLookup.Create(target);

        // Function has no cold part if map is empty
        uint functionIndex = 0x110;
        bool res = lookup.TryGetColdFunctionIndex(0, HotColdMapAddr, functionIndex, out _);
        Assert.False(res);
    }

    [Fact]
    public void TryGetColdFunctionIndex_NoEntryInMap()
    {
        (uint Cold, uint Hot)[] entries =
        [
            (0x100, 0x10),
            (0x200, 0x20),
        ];

        uint numHotColdMap = (uint)entries.Length * 2;
        Target target = CreateMockTarget(entries);

        var lookup = HotColdLookup.Create(target);

        // Function has no cold part if it is not in the map
        uint functionIndex = 0x30;
        bool res = lookup.TryGetColdFunctionIndex(numHotColdMap, HotColdMapAddr, functionIndex, out _);
        Assert.False(res);
    }
}
