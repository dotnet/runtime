// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ObjectiveCMarshalTests
{
    private const uint SyncBlockIsHashOrSyncBlockIndex = 0x08000000;
    private const uint SyncBlockIsHashCode = 0x04000000;
    private const uint SyncBlockIndexMask = (1u << 26) - 1;

    private static TestPlaceholderTarget CreateObjectiveCMarshalTarget(
        MockTarget.Architecture arch,
        Action<MockDescriptors.MockObjectBuilder> configure)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(targetBuilder.MemoryBuilder);
        MockDescriptors.MockObjectBuilder objectBuilder = new(rtsBuilder);

        configure(objectBuilder);

        targetBuilder
            .AddTypes(CreateContractTypes(objectBuilder))
            .AddGlobals(CreateContractGlobals(objectBuilder))
            .AddContract<IObjectiveCMarshal>(version: "c1")
            .AddContract<IObject>(version: "c1")
            .AddContract<ISyncBlock>(version: "c1");

        return targetBuilder.Build();
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockDescriptors.MockObjectBuilder objectBuilder)
        => new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.Object] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ObjectLayout),
            [DataType.ObjectHeader] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ObjectHeaderLayout),
            [DataType.SyncTableEntry] = TargetTestHelpers.CreateTypeInfo(objectBuilder.SyncTableEntryLayout),
            [DataType.SyncBlock] = TargetTestHelpers.CreateTypeInfo(objectBuilder.SyncBlockLayout),
            [DataType.InteropSyncBlockInfo] = TargetTestHelpers.CreateTypeInfo(objectBuilder.InteropSyncBlockInfoLayout),
        };

    private static (string Name, ulong Value)[] CreateContractGlobals(MockDescriptors.MockObjectBuilder objectBuilder)
        =>
        [
            (nameof(Constants.Globals.ObjectToMethodTableUnmask), MockDescriptors.MockObjectBuilder.TestObjectToMethodTableUnmask),
            (nameof(Constants.Globals.StringMethodTable), MockDescriptors.MockObjectBuilder.TestStringMethodTableGlobalAddress),
            (nameof(Constants.Globals.SyncTableEntries), MockDescriptors.MockObjectBuilder.TestSyncTableEntriesGlobalAddress),
            (nameof(Constants.Globals.SyncBlockValueToObjectOffset), MockDescriptors.MockObjectBuilder.TestSyncBlockValueToObjectOffset),
            (nameof(Constants.Globals.SyncBlockIsHashOrSyncBlockIndex), SyncBlockIsHashOrSyncBlockIndex),
            (nameof(Constants.Globals.SyncBlockIsHashCode), SyncBlockIsHashCode),
            (nameof(Constants.Globals.SyncBlockIndexMask), SyncBlockIndexMask),
            (nameof(Constants.Globals.SyncBlockHashCodeMask), SyncBlockIndexMask),
        ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaggedMemory_NoSyncBlockIndex_ReturnsNull(MockTarget.Architecture arch)
    {
        ulong testObjectAddress = 0;
        TestPlaceholderTarget target = CreateObjectiveCMarshalTarget(arch,
            objectBuilder =>
            {
                // AddObject with only the ObjectHeader prefix but no sync block index
                testObjectAddress = objectBuilder.AddObject(0, prefixSize: (uint)objectBuilder.ObjectHeaderLayout.Size);
            });

        IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
        TargetPointer result = contract.GetTaggedMemory(testObjectAddress, out TargetNUInt size);

        Assert.Equal(TargetPointer.Null, result);
        Assert.Equal(default, size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaggedMemory_NullTaggedMemory_ReturnsNull(MockTarget.Architecture arch)
    {
        ulong testObjectAddress = 0;
        TestPlaceholderTarget target = CreateObjectiveCMarshalTarget(arch,
            objectBuilder =>
            {
                testObjectAddress = objectBuilder.AddObjectWithSyncBlock(
                    methodTable: 0,
                    syncBlockIndex: 1,
                    rcw: 0,
                    ccw: 0,
                    ccf: 0,
                    taggedMemory: 0);
            });

        IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
        TargetPointer result = contract.GetTaggedMemory(testObjectAddress, out TargetNUInt size);

        Assert.Equal(TargetPointer.Null, result);
        Assert.Equal(default, size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaggedMemory_HasTaggedMemory_ReturnsPointerAndSize(MockTarget.Architecture arch)
    {
        ulong testObjectAddress = 0;
        const ulong expectedTaggedMemory = 0x5000;
        TestPlaceholderTarget target = CreateObjectiveCMarshalTarget(arch,
            objectBuilder =>
            {
                testObjectAddress = objectBuilder.AddObjectWithSyncBlock(
                    methodTable: 0,
                    syncBlockIndex: 1,
                    rcw: 0,
                    ccw: 0,
                    ccf: 0,
                    taggedMemory: expectedTaggedMemory);
            });

        IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
        TargetPointer result = contract.GetTaggedMemory(testObjectAddress, out TargetNUInt size);

        Assert.Equal(expectedTaggedMemory, result.Value);
        Assert.Equal(2ul * (ulong)target.PointerSize, size.Value);
    }
}
