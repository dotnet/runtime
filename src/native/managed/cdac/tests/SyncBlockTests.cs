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
    public void GetNextSyncBlock_ReturnsNextInChain(MockTarget.Architecture arch)
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

        // Head of list is secondAdded; its next should be firstAdded
        TargetPointer next = contract.GetNextSyncBlock(secondAdded);

        Assert.Equal(firstAdded, next);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNextSyncBlock_LastItemReturnsNull(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        TargetPointer syncBlockAddr = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        TargetPointer next = contract.GetNextSyncBlock(syncBlockAddr);

        Assert.Equal(TargetPointer.Null, next);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetBuiltInComData_NoInteropInfo(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.SyncBlock syncBlockDesc = new(builder);

        TargetPointer syncBlockAddr = syncBlockDesc.AddSyncBlockToCleanupList(
            TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, hasInteropInfo: false);

        Target target = CreateTarget(syncBlockDesc);
        ISyncBlock contract = target.Contracts.SyncBlock;

        bool result = contract.GetBuiltInComData(syncBlockAddr, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);

        Assert.False(result);
        Assert.Equal(TargetPointer.Null, rcw);
        Assert.Equal(TargetPointer.Null, ccw);
        Assert.Equal(TargetPointer.Null, ccf);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetBuiltInComData_WithInteropData(MockTarget.Architecture arch)
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

        bool result = contract.GetBuiltInComData(syncBlockAddr, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);

        Assert.True(result);
        Assert.Equal(expectedRCW, rcw);
        Assert.Equal(expectedCCW, ccw);
        Assert.Equal(expectedCCF, ccf);
    }
}
