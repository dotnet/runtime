// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class MethodDescTests
{
    private static Target CreateTarget(MockDescriptors.MethodDescriptors methodDescBuilder, Mock<IExecutionManager> mockExecutionManager = null)
    {
        MockMemorySpace.Builder builder = methodDescBuilder.Builder;
        var target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetMemoryContext().ReadFromTarget, methodDescBuilder.Types, methodDescBuilder.Globals);

        mockExecutionManager ??= new Mock<IExecutionManager>();
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)
                && c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)
                && c.PlatformMetadata == new Mock<IPlatformMetadata>().Object
                && c.ExecutionManager == mockExecutionManager.Object));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetMethodDescHandle_ILMethod_GetBasicData(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.Loader loaderBuilder = new(builder);
        MockDescriptors.MethodDescriptors methodDescBuilder = new(rtsBuilder, loaderBuilder);

        const int MethodDefToken = 0x06 << 24;
        const ushort expectedRidRangeStart = 0x2000; // arbitrary (larger than  1<< TokenRemainderBitCount)
        Assert.True(expectedRidRangeStart > (1 << MockDescriptors.MethodDescriptors.TokenRemainderBitCount));
        const ushort expectedRidRemainder = 0x10; // arbitrary
        const uint expectedRid = expectedRidRangeStart | expectedRidRemainder; // arbitrary
        uint expectedToken = MethodDefToken | expectedRid;
        ushort expectedSlotNum = 0x0002; // arbitrary, but must be less than number of vtable slots in the method table
        TargetPointer objectMethodTable = MethodTableTests.AddSystemObjectMethodTable(methodDescBuilder.RTSBuilder).MethodTable;
        // add a loader module so that we can do the "IsCollectible" check
        TargetPointer module = methodDescBuilder.LoaderBuilder.AddModule("testModule");
        methodDescBuilder.RTSBuilder.SetMethodTableAuxData(objectMethodTable, loaderModule: module);

        byte count = 10; // arbitrary
        byte methodDescSize = (byte)(methodDescBuilder.Types[DataType.MethodDesc].Size.Value / methodDescBuilder.MethodDescAlignment);
        byte chunkSize = (byte)(count * methodDescSize);
        var chunk = methodDescBuilder.AddMethodDescChunk(objectMethodTable, "testMethod", count, chunkSize, tokenRange: expectedRidRangeStart);

        byte methodDescNum = 3; // abitrary, less than "count"
        byte methodDescIndex = (byte)(methodDescNum * methodDescSize);
        TargetPointer testMethodDescAddress = methodDescBuilder.SetMethodDesc(chunk, methodDescIndex, slotNum: expectedSlotNum, flags: 0, tokenRemainder: expectedRidRemainder);

        Target target = CreateTarget(methodDescBuilder);
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
        TargetPointer gcStressCodeCopy = rts.GetGCStressCodeCopy(handle);
        Assert.Equal(TargetPointer.Null, gcStressCodeCopy);

        // Method classification - IL method
        Assert.False(rts.IsStoredSigMethodDesc(handle, out _));
        Assert.False(rts.IsNoMetadataMethod(handle, out _));
        Assert.False(rts.IsDynamicMethod(handle));
        Assert.False(rts.IsILStub(handle));
        Assert.False(rts.IsArrayMethod(handle, out _));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsArrayMethod(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.Loader loaderBuilder = new(builder);
        MockDescriptors.MethodDescriptors methodDescBuilder = new(rtsBuilder, loaderBuilder);

        ushort numVirtuals = 1;
        TargetPointer methodTable = AddMethodTable(rtsBuilder, numVirtuals);

        byte count = 5;
        uint methodDescSize = methodDescBuilder.Types[DataType.ArrayMethodDesc].Size.Value;
        uint methodDescSizeByAlignment = methodDescSize / methodDescBuilder.MethodDescAlignment;
        byte chunkSize = (byte)(count * methodDescSizeByAlignment);
        TargetPointer chunk = methodDescBuilder.AddMethodDescChunk(methodTable, string.Empty, count, chunkSize, tokenRange: 0);

        TargetPointer[] arrayMethods = new TargetPointer[count];
        for (byte i = 0; i < count; i++)
        {
            // Add the array methods by setting the appropriate slot number
            // Array vtable is:
            //   <base class vtables>
            //   Get
            //   Set
            //   Address
            //   .ctor
            //   [optionally other constructors]
            byte index = (byte)(i * methodDescSizeByAlignment);
            ushort slotNum = (ushort)(numVirtuals + i);
            ushort flags = (ushort)MethodClassification.Array | (ushort)MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot;
            arrayMethods[i] = methodDescBuilder.SetMethodDesc(chunk, index, slotNum, flags, tokenRemainder: 0);
        }

        Target target = CreateTarget(methodDescBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        for (byte i = 0; i < count; i++)
        {
            MethodDescHandle handle = rts.GetMethodDescHandle(arrayMethods[i]);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(rts.IsStoredSigMethodDesc(handle, out _));
            Assert.True(rts.IsArrayMethod(handle, out ArrayFunctionType functionType));

            ArrayFunctionType expectedFunctionType = i <= (byte)ArrayFunctionType.Constructor
                ? (ArrayFunctionType)i
                : ArrayFunctionType.Constructor;
            Assert.Equal(expectedFunctionType, functionType);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsDynamicMethod(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.Loader loaderBuilder = new(builder);
        MockDescriptors.MethodDescriptors methodDescBuilder = new(rtsBuilder, loaderBuilder);

        TargetPointer methodTable = AddMethodTable(rtsBuilder);

        byte count = 2;
        uint methodDescSize = methodDescBuilder.Types[DataType.DynamicMethodDesc].Size.Value;
        uint methodDescSizeByAlignment = methodDescSize / methodDescBuilder.MethodDescAlignment;
        byte chunkSize = (byte)(count * methodDescSizeByAlignment);
        TargetPointer chunk = methodDescBuilder.AddMethodDescChunk(methodTable, string.Empty, count, chunkSize, tokenRange: 0);

        ushort flags = (ushort)MethodClassification.Dynamic;
        TargetPointer dynamicMethod = methodDescBuilder.SetMethodDesc(chunk, index: 0, slotNum: 0, flags, tokenRemainder: 0);
        methodDescBuilder.SetDynamicMethodDesc(dynamicMethod, (uint)RuntimeTypeSystem_1.DynamicMethodDescExtendedFlags.IsLCGMethod);
        TargetPointer ilStubMethod = methodDescBuilder.SetMethodDesc(chunk, index: (byte)methodDescSizeByAlignment, slotNum: 1, flags, tokenRemainder: 0);
        methodDescBuilder.SetDynamicMethodDesc(ilStubMethod, (uint)RuntimeTypeSystem_1.DynamicMethodDescExtendedFlags.IsILStub);

        Target target = CreateTarget(methodDescBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        {
            MethodDescHandle handle = rts.GetMethodDescHandle(dynamicMethod);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(rts.IsStoredSigMethodDesc(handle, out _));
            Assert.True(rts.IsNoMetadataMethod(handle, out _));
            Assert.True(rts.IsDynamicMethod(handle));
            Assert.False(rts.IsILStub(handle));
        }
        {
            MethodDescHandle handle = rts.GetMethodDescHandle(ilStubMethod);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(rts.IsStoredSigMethodDesc(handle, out _));
            Assert.True(rts.IsNoMetadataMethod(handle, out _));
            Assert.False(rts.IsDynamicMethod(handle));
            Assert.True(rts.IsILStub(handle));
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsGenericMethodDefinition(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.Loader loaderBuilder = new(builder);
        MockDescriptors.MethodDescriptors methodDescBuilder = new(rtsBuilder, loaderBuilder);

        TargetPointer methodTable = AddMethodTable(rtsBuilder);

        byte count = 2;
        uint methodDescSize = methodDescBuilder.Types[DataType.InstantiatedMethodDesc].Size.Value;
        uint methodDescSizeByAlignment = methodDescSize / methodDescBuilder.MethodDescAlignment;
        byte chunkSize = (byte)(count * methodDescSizeByAlignment);
        TargetPointer chunk = methodDescBuilder.AddMethodDescChunk(methodTable, string.Empty, count, chunkSize, tokenRange: 0);

        ushort flags = (ushort)MethodClassification.Instantiated;
        TargetPointer genericMethodDef = methodDescBuilder.SetMethodDesc(chunk, index: 0, slotNum: 0, flags, tokenRemainder: 0);
        methodDescBuilder.SetInstantiatedMethodDesc(genericMethodDef, (ushort)RuntimeTypeSystem_1.InstantiatedMethodDescFlags2.GenericMethodDefinition, []);
        TargetPointer[] typeArgsRawAddrs = [0x1000, 0x2000, 0x3000];
        TargetPointer[] typeArgsHandles = typeArgsRawAddrs.Select(a => GetTypeDescHandlePointer(a)).ToArray();

        TargetPointer genericWithInst = methodDescBuilder.SetMethodDesc(chunk, index: (byte)methodDescSizeByAlignment, slotNum: 1, flags, tokenRemainder: 0);
        methodDescBuilder.SetInstantiatedMethodDesc(genericWithInst, (ushort)RuntimeTypeSystem_1.InstantiatedMethodDescFlags2.GenericMethodDefinition, typeArgsHandles);

        Target target = CreateTarget(methodDescBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        {
            MethodDescHandle handle = rts.GetMethodDescHandle(genericMethodDef);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(rts.IsGenericMethodDefinition(handle));
            ReadOnlySpan<TypeHandle> instantiation = rts.GetGenericMethodInstantiation(handle);
            Assert.Equal(0, instantiation.Length);
        }

        {
            MethodDescHandle handle = rts.GetMethodDescHandle(genericWithInst);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(rts.IsGenericMethodDefinition(handle));
            ReadOnlySpan<TypeHandle> instantiation = rts.GetGenericMethodInstantiation(handle);
            Assert.Equal(typeArgsRawAddrs.Length, instantiation.Length);
            for (int i = 0; i < typeArgsRawAddrs.Length; i++)
            {
                Assert.Equal(typeArgsHandles[i], instantiation[i].Address);
                Assert.Equal(typeArgsRawAddrs[i], instantiation[i].TypeDescAddress());
            }
        }
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
        TargetPointer methodTable = AddMethodTable(rtsBuilder);

        uint methodDescSize = methodDescBuilder.Types[DataType.MethodDesc].Size.Value;
        if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot))
            methodDescSize += methodDescBuilder.Types[DataType.NonVtableSlot].Size!.Value;

        if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasMethodImpl))
            methodDescSize += methodDescBuilder.Types[DataType.MethodImpl].Size!.Value;

        if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot))
            methodDescSize += methodDescBuilder.Types[DataType.NativeCodeSlot].Size!.Value;

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

    public static IEnumerable<object[]> StdArchMethodDescTypeData()
    {
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            yield return new object[] { arch, DataType.MethodDesc };
            yield return new object[] { arch, DataType.FCallMethodDesc };
            yield return new object[] { arch, DataType.PInvokeMethodDesc };
            yield return new object[] { arch, DataType.EEImplMethodDesc };
            yield return new object[] { arch, DataType.ArrayMethodDesc };
            yield return new object[] { arch, DataType.InstantiatedMethodDesc };
            yield return new object[] { arch, DataType.CLRToCOMCallMethodDesc };
            yield return new object[] { arch, DataType.DynamicMethodDesc };
        }
    }

    [Theory]
    [MemberData(nameof(StdArchMethodDescTypeData))]
    public void GetNativeCode_StableEntryPoint_NonVtableSlot(MockTarget.Architecture arch, DataType methodDescType)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.Loader loaderBuilder = new(builder);
        MockDescriptors.MethodDescriptors methodDescBuilder = new(rtsBuilder, loaderBuilder);

        TargetPointer methodTable = AddMethodTable(rtsBuilder);
        MethodClassification classification = methodDescType switch
        {
            DataType.MethodDesc => MethodClassification.IL,
            DataType.FCallMethodDesc => MethodClassification.FCall,
            DataType.PInvokeMethodDesc => MethodClassification.PInvoke,
            DataType.EEImplMethodDesc => MethodClassification.EEImpl,
            DataType.ArrayMethodDesc => MethodClassification.Array,
            DataType.InstantiatedMethodDesc => MethodClassification.Instantiated,
            DataType.CLRToCOMCallMethodDesc => MethodClassification.ComInterop,
            DataType.DynamicMethodDesc => MethodClassification.Dynamic,
            _ => throw new ArgumentOutOfRangeException(nameof(methodDescType))
        };
        uint methodDescBaseSize = methodDescBuilder.Types[methodDescType].Size.Value;
        uint methodDescSize = methodDescBaseSize + methodDescBuilder.Types[DataType.NonVtableSlot].Size!.Value;
        byte chunkSize = (byte)(methodDescSize / methodDescBuilder.MethodDescAlignment);
        TargetPointer chunk = methodDescBuilder.AddMethodDescChunk(methodTable, string.Empty, count: 1, chunkSize, tokenRange: 0);

        ushort flags = (ushort)((ushort)classification | (ushort)MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot);
        TargetPointer methodDescAddress = methodDescBuilder.SetMethodDesc(chunk, index: 0, slotNum: 0, flags, tokenRemainder: 0, flags3: (ushort)MethodDescFlags_1.MethodDescFlags3.HasStableEntryPoint);
        TargetCodePointer nativeCode = new TargetCodePointer(0x0789_abc0);
        helpers.WritePointer(
            methodDescBuilder.Builder.BorrowAddressRange(methodDescAddress + methodDescBaseSize, helpers.PointerSize),
            nativeCode);

        Mock<IExecutionManager> mockExecutionManager = new();
        CodeBlockHandle codeBlock = new CodeBlockHandle(methodDescAddress);
        mockExecutionManager.Setup(em => em.GetCodeBlockHandle(nativeCode))
            .Returns(codeBlock);
        mockExecutionManager.Setup(em => em.GetMethodDesc(codeBlock))
            .Returns(methodDescAddress);
        Target target = CreateTarget(methodDescBuilder, mockExecutionManager);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        var handle = rts.GetMethodDescHandle(methodDescAddress);
        Assert.NotEqual(TargetPointer.Null, handle.Address);

        TargetCodePointer actualNativeCode = rts.GetNativeCode(handle);
        Assert.Equal(nativeCode, actualNativeCode);
    }

    private TargetPointer AddMethodTable(MockDescriptors.RuntimeTypeSystem rtsBuilder, ushort numVirtuals = 5)
    {
        TargetPointer eeClass = rtsBuilder.AddEEClass(string.Empty, attr: 0, numMethods: 2, numNonVirtualSlots: 1);
        TargetPointer methodTable = rtsBuilder.AddMethodTable(string.Empty,
            mtflags: default, mtflags2: default, baseSize: rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize,
            module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals);
        rtsBuilder.SetEEClassAndCanonMTRefs(eeClass, methodTable);
        return methodTable;
    }

    private static TargetPointer GetTypeDescHandlePointer(TargetPointer addr)
        => addr | (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
}
