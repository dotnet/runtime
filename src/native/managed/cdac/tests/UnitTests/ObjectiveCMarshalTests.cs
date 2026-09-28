// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ObjectiveCMarshalTests
{
    private const uint SyncBlockIsHashOrSyncBlockIndex = 0x08000000;
    private const uint SyncBlockIsHashCode = 0x04000000;
    private const uint SyncBlockIndexMask = (1u << 26) - 1;

    private const string ObjectiveCMarshalTypeName = "System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal";
    private const string ObjcTrackingInformationTypeName = "System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal+ObjcTrackingInformation";

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

    private static TestPlaceholderTarget CreateObjectiveCMarshalTargetWithCWT(
        MockTarget.Architecture arch,
        Mock<IConditionalWeakTable> mockCwt,
        Action<MockDescriptors.MockObjectBuilder, MockMemorySpace.Builder> configure)
    {
        const ulong globalSlotAddress = 0x7000;
        const ulong cwtAddress = 0x8000;

        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(targetBuilder.MemoryBuilder);
        MockDescriptors.MockObjectBuilder objectBuilder = new(rtsBuilder);

        configure(objectBuilder, targetBuilder.MemoryBuilder);

        var helpers = new TargetTestHelpers(arch);

        // Allocate a heap fragment for the s_objects global slot and write the CWT address into it.
        byte[] slotData = new byte[helpers.PointerSize];
        helpers.WritePointer(slotData, cwtAddress);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = globalSlotAddress,
            Data = slotData,
            Name = "ObjectiveCMarshal.s_objects slot"
        });

        var globals = new List<(string Name, ulong Value)>(CreateContractGlobals(objectBuilder))
        {
            (ObjectiveCMarshalTypeName + ".s_objects", globalSlotAddress)
        };

        // Register ObjcTrackingInformation type with a single pointer-sized _memory field at offset 0.
        // The managed _memory field is an IntPtr, so the contract descriptor reports its type as "nint";
        // model that here so the pointer read path is validated against the real descriptor type.
        var trackingInfoType = new Target.TypeInfo
        {
            Size = (uint)helpers.PointerSize,
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["_memory"] = new Target.FieldInfo { Offset = 0, TypeName = "nint" }
            }
        };

        var types = CreateContractTypes(objectBuilder);
        var stringTypes = new Dictionary<string, Target.TypeInfo>
        {
            [ObjcTrackingInformationTypeName] = trackingInfoType
        };

        targetBuilder
            .AddTypes(types)
            .AddTypes(stringTypes)
            .AddGlobals(globals.ToArray())
            .AddContract<IObjectiveCMarshal>(version: "c1")
            .AddContract<IObject>(version: "c1")
            .AddContract<ISyncBlock>(version: "c1")
            .AddMockContract(mockCwt);

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
    public void GetTaggedMemory_NoTrackingTable_ReturnsNull(MockTarget.Architecture arch)
    {
        ulong testObjectAddress = 0;
        TestPlaceholderTarget target = CreateObjectiveCMarshalTarget(arch,
            objectBuilder =>
            {
                testObjectAddress = objectBuilder.AddObject(0, prefixSize: (uint)objectBuilder.ObjectHeaderLayout.Size);
            });

        IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
        TargetPointer result = contract.GetTaggedMemory(testObjectAddress, out TargetNUInt size);

        Assert.Equal(TargetPointer.Null, result);
        Assert.Equal(default, size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaggedMemory_ObjectNotInTable_ReturnsNull(MockTarget.Architecture arch)
    {
        ulong testObjectAddress = 0;
        var mockCwt = new Mock<IConditionalWeakTable>();
        mockCwt
            .Setup(c => c.TryGetValue(It.IsAny<TargetPointer>(), It.IsAny<TargetPointer>(), out It.Ref<TargetPointer>.IsAny))
            .Returns(false);

        TestPlaceholderTarget target = CreateObjectiveCMarshalTargetWithCWT(arch, mockCwt,
            (objectBuilder, _) =>
            {
                testObjectAddress = objectBuilder.AddObject(0, prefixSize: (uint)objectBuilder.ObjectHeaderLayout.Size);
            });

        IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
        TargetPointer result = contract.GetTaggedMemory(testObjectAddress, out TargetNUInt size);

        Assert.Equal(TargetPointer.Null, result);
        Assert.Equal(default, size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaggedMemory_NullMemory_ReturnsNull(MockTarget.Architecture arch)
    {
        ulong testObjectAddress = 0;
        var helpers = new TargetTestHelpers(arch);

        const ulong trackingInfoAddress = 0xA000;

        var mockCwt = new Mock<IConditionalWeakTable>();

        TestPlaceholderTarget target = CreateObjectiveCMarshalTargetWithCWT(arch, mockCwt,
            (objectBuilder, memBuilder) =>
            {
                testObjectAddress = objectBuilder.AddObject(0, prefixSize: (uint)objectBuilder.ObjectHeaderLayout.Size);

                // Write a zero pointer at the tracking info address (_memory field = 0)
                byte[] trackingData = new byte[helpers.PointerSize];
                helpers.WritePointer(trackingData, 0);
                memBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
                {
                    Address = trackingInfoAddress,
                    Data = trackingData,
                    Name = "ObjcTrackingInformation (null memory)"
                });
            });

        // Setup CWT mock to return the tracking info address for our object
        TargetPointer trackingInfoOut = new TargetPointer(trackingInfoAddress);
        mockCwt
            .Setup(c => c.TryGetValue(It.IsAny<TargetPointer>(), new TargetPointer(testObjectAddress), out trackingInfoOut))
            .Returns(true);

        IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
        TargetPointer result = contract.GetTaggedMemory(testObjectAddress, out TargetNUInt size);

        Assert.Equal(TargetPointer.Null, result);
        Assert.Equal(default, size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaggedMemory_HasMemory_ReturnsPointerAndSize(MockTarget.Architecture arch)
    {
        ulong testObjectAddress = 0;
        var helpers = new TargetTestHelpers(arch);

        const ulong trackingInfoAddress = 0xA000;
        const ulong expectedTaggedMemory = 0x5000;

        var mockCwt = new Mock<IConditionalWeakTable>();

        TestPlaceholderTarget target = CreateObjectiveCMarshalTargetWithCWT(arch, mockCwt,
            (objectBuilder, memBuilder) =>
            {
                testObjectAddress = objectBuilder.AddObject(0, prefixSize: (uint)objectBuilder.ObjectHeaderLayout.Size);

                // Write the tagged memory pointer at the tracking info address (_memory field)
                byte[] trackingData = new byte[helpers.PointerSize];
                helpers.WritePointer(trackingData, expectedTaggedMemory);
                memBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
                {
                    Address = trackingInfoAddress,
                    Data = trackingData,
                    Name = "ObjcTrackingInformation (with memory)"
                });
            });

        // Setup CWT mock to return the tracking info address for our object
        TargetPointer trackingInfoOut = new TargetPointer(trackingInfoAddress);
        mockCwt
            .Setup(c => c.TryGetValue(It.IsAny<TargetPointer>(), new TargetPointer(testObjectAddress), out trackingInfoOut))
            .Returns(true);

        IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
        TargetPointer result = contract.GetTaggedMemory(testObjectAddress, out TargetNUInt size);

        Assert.Equal(expectedTaggedMemory, result.Value);
        Assert.Equal(2ul * (ulong)helpers.PointerSize, size.Value);
    }
}
