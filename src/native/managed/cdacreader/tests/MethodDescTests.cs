// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class MethodDescTests
{
    private static Target CreateTarget(MockDescriptors.MethodDescriptors methodDescBuilder)
    {
        MockMemorySpace.Builder builder = methodDescBuilder.Builder;
        var target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetReadContext().ReadFromTarget, methodDescBuilder.Types, methodDescBuilder.Globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)
                && c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)
                && c.PlatformMetadata == new Mock<Contracts.IPlatformMetadata>().Object));
        return target;
    }
    private static void MethodDescHelper(MockTarget.Architecture arch, Action<MockDescriptors.MethodDescriptors> configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        MockMemorySpace.Builder builder = new(targetTestHelpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.Loader loaderBuilder = new(builder);
        MockDescriptors.MethodDescriptors methodDescBuilder = new(rtsBuilder, loaderBuilder);

        configure?.Invoke(methodDescBuilder);

        var target = CreateTarget(methodDescBuilder);

        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MethodDescGetMethodDescTokenOk(MockTarget.Architecture arch)
    {
        TargetPointer testMethodDescAddress = default;
        TargetPointer objectMethodTable = default;
        const int MethodDefToken = 0x06 << 24;
        const ushort expectedRidRangeStart = 0x2000; // arbitrary (larger than  1<< TokenRemainderBitCount)
        const ushort expectedRidRemainder = 0x10; // arbitrary
        const uint expectedRid = expectedRidRangeStart | expectedRidRemainder; // arbitrary
        uint expectedToken = MethodDefToken | expectedRid;
        ushort expectedSlotNum = 0x0002; // arbitrary, but must be less than number of vtable slots in the method table
        MethodDescHelper(arch,
        (builder) =>
        {
            objectMethodTable = MethodTableTests.AddSystemObjectMethodTable(builder.RTSBuilder).MethodTable;
            // add a loader module so that we can do the "IsCollectible" check
            TargetPointer module = builder.LoaderBuilder.AddModule("testModule");
            builder.RTSBuilder.SetMethodTableAuxData(objectMethodTable, loaderModule: module);

            byte count = 10; // arbitrary
            byte methodDescSize = (byte)(builder.Types[DataType.MethodDesc].Size.Value / builder.MethodDescAlignment);
            byte chunkSize = (byte)(count * methodDescSize);
            var chunk = builder.AddMethodDescChunk(objectMethodTable, "testMethod", count, chunkSize, tokenRange: expectedRidRangeStart);

            byte methodDescNum = 3; // abitrary, less than "count"
            byte methodDescIndex = (byte)(methodDescNum * methodDescSize);
            testMethodDescAddress = builder.SetMethodDesc(chunk, methodDescIndex, slotNum: expectedSlotNum, flags: 0, tokenRemainder: expectedRidRemainder);
        },
        (target) =>
        {
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

            var handle = rts.GetMethodDescHandle(testMethodDescAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);

            uint token = rts.GetMethodToken(handle);
            Assert.Equal(expectedToken, token);
            ushort slotNum = rts.GetSlotNumber(handle);
            Assert.Equal(expectedSlotNum, slotNum);
            TargetPointer mt = rts.GetMethodTable(handle);
            Assert.Equal(objectMethodTable, mt);
            bool isCollectible = rts.IsCollectibleMethod(handle);
            Assert.False(isCollectible);
            TargetPointer versioning = rts.GetMethodDescVersioningState(handle);
            Assert.Equal(TargetPointer.Null, versioning);
        });
    }

    public static IEnumerable<object[]> StdArchOptionalSlotsData()
    {
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            yield return new object[] { arch, 0 };
            yield return new object[] { arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot };
            yield return new object[] { arch, MethodDescFlags_1.MethodDescFlags.HasMethodImpl };
            yield return new object[] { arch, MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot };
            yield return new object[] { arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot | MethodDescFlags_1.MethodDescFlags.HasMethodImpl };
            yield return new object[] { arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot | MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot };
            yield return new object[] { arch, MethodDescFlags_1.MethodDescFlags.HasMethodImpl | MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot };
            yield return new object[] { arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot | MethodDescFlags_1.MethodDescFlags.HasMethodImpl | MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot };
        }
    }

    [Theory]
    [MemberData(nameof(StdArchOptionalSlotsData))]
    public void GetAddressOfNativeCodeSlot_OptionalSlots(MockTarget.Architecture arch, ushort flagsValue)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.Loader loaderBuilder = new(builder);
        MockDescriptors.MethodDescriptors methodDescBuilder = new(rtsBuilder, loaderBuilder);

        MethodDescFlags_1.MethodDescFlags flags = (MethodDescFlags_1.MethodDescFlags)flagsValue;
        ushort numVirtuals = 1;
        TargetPointer eeClass = rtsBuilder.AddEEClass(string.Empty, 0, 2, 1);
        TargetPointer methodTable = rtsBuilder.AddMethodTable(string.Empty,
            mtflags: default, mtflags2: default, baseSize: helpers.ObjectBaseSize,
            module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: numVirtuals);
        rtsBuilder.SetEEClassAndCanonMTRefs(eeClass, methodTable);

        uint methodDescSize = methodDescBuilder.Types[DataType.MethodDesc].Size.Value;
        if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot))
            methodDescSize += (uint)helpers.PointerSize;

        if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasMethodImpl))
            methodDescSize += methodDescBuilder.Types[DataType.MethodImpl].Size!.Value;

        if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot))
            methodDescSize += (uint)helpers.PointerSize;

        byte chunkSize = (byte)(methodDescSize / methodDescBuilder.MethodDescAlignment);
        TargetPointer chunk = methodDescBuilder.AddMethodDescChunk(methodTable, string.Empty, count: 1, chunkSize, tokenRange: 0);
        TargetPointer methodDescAddress = methodDescBuilder.SetMethodDesc(chunk, index: 0, slotNum: 0, flags: (ushort)flags, tokenRemainder: 0);

        Target target = CreateTarget(methodDescBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        var handle = rts.GetMethodDescHandle(methodDescAddress);
        Assert.NotEqual(TargetPointer.Null, handle.Address);

        bool hasNativeCodeSlot = rts.HasNativeCodeSlot(handle);
        Assert.Equal(flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot), hasNativeCodeSlot);
        if (hasNativeCodeSlot)
        {
            // Native code slot is last optional slot
            TargetPointer expectedCodeSlotAddr = methodDescAddress + methodDescSize - (uint)helpers.PointerSize;
            TargetPointer actualNativeCodeSlotAddr = rts.GetAddressOfNativeCodeSlot(handle);
            Assert.Equal(expectedCodeSlotAddr, actualNativeCodeSlotAddr);
        }
    }
}
