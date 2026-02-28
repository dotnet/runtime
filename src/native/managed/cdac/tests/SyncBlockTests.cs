// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class SyncBlockTests
{
    private static Target CreateTarget(MockDescriptors.SyncBlock syncBlock)
    {
        MockTarget.Architecture arch = syncBlock.Builder.TargetTestHelpers.Arch;
        var target = new TestPlaceholderTarget(arch, syncBlock.Builder.GetMemoryContext().ReadFromTarget, syncBlock.Types, syncBlock.Globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.SyncBlock == ((IContractFactory<ISyncBlock>)new SyncBlockFactory()).CreateContract(target, 1)));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockFromCleanupList_EmptyList(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        TargetPointer result = contract.GetSyncBlockFromCleanupList();

        Assert.Equal(TargetPointer.Null, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockFromCleanupList_SingleItem(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        TargetPointer syncBlockAddr = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        TargetPointer result = contract.GetSyncBlockFromCleanupList();

        Assert.Equal(syncBlockAddr, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockFromCleanupList_MultipleItems_ReturnsFirst(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        // Items are prepended, so addedSecond is at the head
        syncBlockDesc.AddSyncBlockToCleanupList(TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);
        TargetPointer addedSecond = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        TargetPointer result = contract.GetSyncBlockFromCleanupList();

        Assert.Equal(addedSecond, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockCleanupInfo_NoInteropInfo(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        TargetPointer syncBlockAddr = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, hasInteropInfo: false);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        SyncBlockCleanupInfo info = contract.GetSyncBlockCleanupInfo(syncBlockAddr);

        Assert.Equal(TargetPointer.Null, info.NextSyncBlock);
        Assert.Equal(TargetPointer.Null, info.BlockRCW);
        Assert.Equal(TargetPointer.Null, info.BlockClassFactory);
        Assert.Equal(TargetPointer.Null, info.BlockCCW);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockCleanupInfo_InteropData(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        TargetPointer expectedRCW = new TargetPointer(0x1000);
        TargetPointer expectedCCW = new TargetPointer(0x2000);
        TargetPointer expectedCCF = new TargetPointer(0x3000);

        TargetPointer syncBlockAddr = syncBlockDesc.AddSyncBlockToCleanupList(expectedRCW, expectedCCW, expectedCCF);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        SyncBlockCleanupInfo info = contract.GetSyncBlockCleanupInfo(syncBlockAddr);

        Assert.Equal(expectedRCW, info.BlockRCW);
        Assert.Equal(expectedCCW, info.BlockCCW);
        Assert.Equal(expectedCCF, info.BlockClassFactory);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockCleanupInfo_RCWLockBitMasked(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        // RCW with bit 0 set (lock bit) should be masked out
        TargetPointer inputRCW = new TargetPointer(0x1001);
        TargetPointer expectedRCW = new TargetPointer(0x1000);

        TargetPointer syncBlockAddr = syncBlockDesc.AddSyncBlockToCleanupList(
            inputRCW, TargetPointer.Null, TargetPointer.Null);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        SyncBlockCleanupInfo info = contract.GetSyncBlockCleanupInfo(syncBlockAddr);

        Assert.Equal(expectedRCW, info.BlockRCW);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockCleanupInfo_CCWSentinelTreatedAsNull(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        // CCW sentinel value 0x1 means "was CCW but now null"
        TargetPointer ccwSentinel = new TargetPointer(0x1);

        TargetPointer syncBlockAddr = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, ccwSentinel, TargetPointer.Null);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        SyncBlockCleanupInfo info = contract.GetSyncBlockCleanupInfo(syncBlockAddr);

        Assert.Equal(TargetPointer.Null, info.BlockCCW);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSyncBlockCleanupInfo_NextSyncBlockInChain(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        // Add two blocks; the second is prepended (becomes the head)
        TargetPointer firstAdded = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);
        TargetPointer secondAdded = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        // Head of list is secondAdded; its NextSyncBlock should be firstAdded
        SyncBlockCleanupInfo info = contract.GetSyncBlockCleanupInfo(secondAdded);

        Assert.Equal(firstAdded, info.NextSyncBlock);
        Assert.Equal(TargetPointer.Null, info.BlockRCW);
    }
}
