// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class SyncBlockTests
{
    private static ISyncBlock CreateSyncBlockContract(MockTarget.Architecture arch, Action<MockSyncBlockBuilder> configure)
    {
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.BumpAllocator allocator = builder.MemoryBuilder.CreateAllocator(0x0001_0000, 0x0002_0000);
        MockSyncBlockBuilder syncBlock = new(builder.MemoryBuilder, allocator);

        configure(syncBlock);

        var target = builder
            .AddTypes(CreateContractTypes(syncBlock))
            .AddGlobals(CreateContractGlobals(syncBlock))
            .AddContract<ISyncBlock>(version: "c1")
            .Build();
        return target.Contracts.SyncBlock;
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockSyncBlockBuilder syncBlock)
        => new()
        {
            [DataType.SyncBlockCache] = TargetTestHelpers.CreateTypeInfo(syncBlock.SyncBlockCacheLayout),
            [DataType.SyncBlock] = TargetTestHelpers.CreateTypeInfo(syncBlock.SyncBlockLayout),
            [DataType.InteropSyncBlockInfo] = TargetTestHelpers.CreateTypeInfo(syncBlock.InteropSyncBlockInfoLayout),
        };

    private static (string Name, ulong Value)[] CreateContractGlobals(MockSyncBlockBuilder syncBlock)
        =>
        [
            (nameof(Constants.Globals.SyncBlockCache), syncBlock.SyncBlockCacheGlobalAddress),
            (nameof(Constants.Globals.SyncTableEntries), syncBlock.SyncTableEntriesGlobalAddress),
        ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlock_RefreshesSyncTableEntriesAfterFlush(MockTarget.Architecture arch)
    {
        const ulong FirstSyncBlock = 0x1000;
        const ulong SecondSyncBlock = 0x2000;

        TargetTestHelpers helpers = new(arch);
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.BumpAllocator allocator = builder.MemoryBuilder.CreateAllocator(0x0001_0000, 0x0002_0000);
        Layout<MockSyncTableEntry> layout = MockSyncTableEntry.CreateLayout(arch);

        MockSyncTableEntry firstEntry = layout.Create(allocator.Allocate((ulong)layout.Size, "First SyncTableEntry"));
        firstEntry.SyncBlock = FirstSyncBlock;

        MockSyncTableEntry secondEntry = layout.Create(allocator.Allocate((ulong)layout.Size, "Second SyncTableEntry"));
        secondEntry.SyncBlock = SecondSyncBlock;

        MockMemorySpace.HeapFragment global = allocator.Allocate((ulong)helpers.PointerSize, "SyncTableEntries");
        helpers.WritePointer(global.Data, firstEntry.Address);

        TestPlaceholderTarget target = builder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.SyncTableEntry] = TargetTestHelpers.CreateTypeInfo(layout),
            })
            .AddGlobals((Constants.Globals.SyncTableEntries, global.Address))
            .AddContract<ISyncBlock>("c1")
            .Build();

        ISyncBlock contract = target.Contracts.SyncBlock;
        Assert.Equal(new TargetPointer(FirstSyncBlock), contract.GetSyncBlock(0));

        target.WritePointer(global.Address, new TargetPointer(secondEntry.Address));
        Assert.Equal(new TargetPointer(FirstSyncBlock), contract.GetSyncBlock(0));

        target.Flush(FlushScope.All);

        Assert.Equal(new TargetPointer(SecondSyncBlock), contract.GetSyncBlock(0));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockFromCleanupList_SingleItem(MockTarget.Architecture arch)
    {
        TargetPointer syncBlockAddress = TargetPointer.Null;
        ISyncBlock contract = CreateSyncBlockContract(arch, syncBlock =>
        {
            syncBlockAddress = syncBlock.AddSyncBlockToCleanupList(
                TargetPointer.Null,
                TargetPointer.Null,
                TargetPointer.Null).Address;
        });

        TargetPointer result = contract.GetSyncBlockFromCleanupList();

        Assert.Equal(syncBlockAddress, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockFromCleanupList_MultipleItems_ReturnsFirst(MockTarget.Architecture arch)
    {
        TargetPointer addedSecond = TargetPointer.Null;
        ISyncBlock contract = CreateSyncBlockContract(arch, syncBlock =>
        {
            syncBlock.AddSyncBlockToCleanupList(TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);
            addedSecond = syncBlock.AddSyncBlockToCleanupList(
                TargetPointer.Null,
                TargetPointer.Null,
                TargetPointer.Null).Address;
        });

        TargetPointer result = contract.GetSyncBlockFromCleanupList();

        Assert.Equal(addedSecond, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNextSyncBlock_ReturnsNextInChain(MockTarget.Architecture arch)
    {
        TargetPointer firstAdded = TargetPointer.Null;
        TargetPointer secondAdded = TargetPointer.Null;
        ISyncBlock contract = CreateSyncBlockContract(arch, syncBlock =>
        {
            firstAdded = syncBlock.AddSyncBlockToCleanupList(
                TargetPointer.Null,
                TargetPointer.Null,
                TargetPointer.Null).Address;
            secondAdded = syncBlock.AddSyncBlockToCleanupList(
                TargetPointer.Null,
                TargetPointer.Null,
                TargetPointer.Null).Address;
        });

        TargetPointer next = contract.GetNextSyncBlock(secondAdded);

        Assert.Equal(firstAdded, next);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNextSyncBlock_LastItemReturnsNull(MockTarget.Architecture arch)
    {
        TargetPointer syncBlockAddress = TargetPointer.Null;
        ISyncBlock contract = CreateSyncBlockContract(arch, syncBlock =>
        {
            syncBlockAddress = syncBlock.AddSyncBlockToCleanupList(
                TargetPointer.Null,
                TargetPointer.Null,
                TargetPointer.Null).Address;
        });

        TargetPointer next = contract.GetNextSyncBlock(syncBlockAddress);

        Assert.Equal(TargetPointer.Null, next);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetBuiltInComData_NoInteropInfo(MockTarget.Architecture arch)
    {
        TargetPointer syncBlockAddress = TargetPointer.Null;
        ISyncBlock contract = CreateSyncBlockContract(arch, syncBlock =>
        {
            syncBlockAddress = syncBlock.AddSyncBlockToCleanupList(
                TargetPointer.Null,
                TargetPointer.Null,
                TargetPointer.Null,
                hasInteropInfo: false).Address;
        });

        bool result = contract.GetBuiltInComData(syncBlockAddress, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);

        Assert.False(result);
        Assert.Equal(TargetPointer.Null, rcw);
        Assert.Equal(TargetPointer.Null, ccw);
        Assert.Equal(TargetPointer.Null, ccf);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetBuiltInComData_WithInteropData(MockTarget.Architecture arch)
    {
        TargetPointer expectedRCW = new TargetPointer(0x1000);
        TargetPointer expectedCCW = new TargetPointer(0x2000);
        TargetPointer expectedCCF = new TargetPointer(0x3000);
        TargetPointer syncBlockAddress = TargetPointer.Null;
        ISyncBlock contract = CreateSyncBlockContract(arch, syncBlock =>
        {
            syncBlockAddress = syncBlock.AddSyncBlockToCleanupList(expectedRCW, expectedCCW, expectedCCF).Address;
        });

        bool result = contract.GetBuiltInComData(syncBlockAddress, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);

        Assert.True(result);
        Assert.Equal(expectedRCW, rcw);
        Assert.Equal(expectedCCW, ccw);
        Assert.Equal(expectedCCF, ccf);
    }
}
