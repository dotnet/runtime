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
    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockDescriptors.MockMethodDescriptorsBuilder methodDescBuilder)
        => new Dictionary<DataType, Target.TypeInfo>()
        {
            [DataType.MethodDesc] = TargetTestHelpers.CreateTypeInfo(methodDescBuilder.MethodDescLayout),
            [DataType.MethodDescChunk] = TargetTestHelpers.CreateTypeInfo(methodDescBuilder.MethodDescChunkLayout),
            [DataType.InstantiatedMethodDesc] = TargetTestHelpers.CreateTypeInfo(methodDescBuilder.InstantiatedMethodDescLayout),
            [DataType.StoredSigMethodDesc] = TargetTestHelpers.CreateTypeInfo(methodDescBuilder.StoredSigMethodDescLayout),
            [DataType.DynamicMethodDesc] = TargetTestHelpers.CreateTypeInfo(methodDescBuilder.DynamicMethodDescLayout),
            [DataType.NonVtableSlot] = new Target.TypeInfo { Size = methodDescBuilder.NonVtableSlotSize },
            [DataType.MethodImpl] = new Target.TypeInfo { Size = methodDescBuilder.MethodImplSize },
            [DataType.NativeCodeSlot] = new Target.TypeInfo { Size = methodDescBuilder.NativeCodeSlotSize },
            [DataType.AsyncMethodData] = new Target.TypeInfo { Size = methodDescBuilder.AsyncMethodDataSize },
            [DataType.ArrayMethodDesc] = new Target.TypeInfo { Size = methodDescBuilder.ArrayMethodDescSize },
            [DataType.FCallMethodDesc] = new Target.TypeInfo { Size = methodDescBuilder.FCallMethodDescSize },
            [DataType.PInvokeMethodDesc] = new Target.TypeInfo { Size = methodDescBuilder.PInvokeMethodDescSize },
            [DataType.EEImplMethodDesc] = new Target.TypeInfo { Size = methodDescBuilder.EEImplMethodDescSize },
            [DataType.CLRToCOMCallMethodDesc] = new Target.TypeInfo { Size = methodDescBuilder.CLRToCOMCallMethodDescSize },
        }
        .Concat(MethodTableTests.CreateContractTypes(methodDescBuilder.RTSBuilder))
        .Concat(LoaderTests.CreateContractTypes(methodDescBuilder.LoaderBuilder))
        .ToDictionary();

    private static (string Name, ulong Value)[] CreateContractGlobals(MockDescriptors.MockMethodDescriptorsBuilder methodDescBuilder)
        => MethodTableTests.CreateContractGlobals(methodDescBuilder.RTSBuilder).Concat(
        [
            (nameof(Constants.Globals.MethodDescTokenRemainderBitCount), methodDescBuilder.MethodDescTokenRemainderBitCount),
        ]).ToArray();

    private static uint GetMethodDescBaseSize(MockDescriptors.MockMethodDescriptorsBuilder methodDescBuilder, DataType methodDescType)
        => methodDescType switch
        {
            DataType.MethodDesc => (uint)methodDescBuilder.MethodDescLayout.Size,
            DataType.FCallMethodDesc => methodDescBuilder.FCallMethodDescSize,
            DataType.PInvokeMethodDesc => methodDescBuilder.PInvokeMethodDescSize,
            DataType.EEImplMethodDesc => methodDescBuilder.EEImplMethodDescSize,
            DataType.ArrayMethodDesc => methodDescBuilder.ArrayMethodDescSize,
            DataType.InstantiatedMethodDesc => (uint)methodDescBuilder.InstantiatedMethodDescLayout.Size,
            DataType.CLRToCOMCallMethodDesc => methodDescBuilder.CLRToCOMCallMethodDescSize,
            DataType.DynamicMethodDesc => (uint)methodDescBuilder.DynamicMethodDescLayout.Size,
            _ => throw new ArgumentOutOfRangeException(nameof(methodDescType))
        };

    private static IRuntimeTypeSystem CreateRuntimeTypeSystemContract(
        MockTarget.Architecture arch,
        Action<MockDescriptors.MockMethodDescriptorsBuilder> configure,
        Mock<IExecutionManager>? mockExecutionManager = null)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockLoaderBuilder loaderBuilder = new(builder);
        MockDescriptors.MockMethodDescriptorsBuilder methodDescBuilder = new(rtsBuilder, loaderBuilder);

        configure(methodDescBuilder);

        var target = new TestPlaceholderTarget(
            builder.TargetTestHelpers.Arch,
            builder.GetMemoryContext().ReadFromTarget,
            CreateContractTypes(methodDescBuilder),
            CreateContractGlobals(methodDescBuilder));

        mockExecutionManager ??= new Mock<IExecutionManager>();
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)
                && c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)
                && c.PlatformMetadata == new Mock<IPlatformMetadata>().Object
                && c.ExecutionManager == mockExecutionManager.Object));
        return target.Contracts.RuntimeTypeSystem;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetMethodDescHandle_ILMethod_GetBasicData(MockTarget.Architecture arch)
    {
        const int MethodDefToken = 0x06 << 24;
        const ushort expectedRidRangeStart = 0x2000;
        Assert.True(expectedRidRangeStart > (1 << MockDescriptors.MockMethodDescriptorsBuilder.TokenRemainderBitCount));
        const ushort expectedRidRemainder = 0x10;
        const uint expectedRid = expectedRidRangeStart | expectedRidRemainder;
        uint expectedToken = MethodDefToken | expectedRid;
        ushort expectedSlotNum = 0x0002;

        TargetPointer testMethodDescAddress = TargetPointer.Null;
        TargetPointer objectMethodTable = TargetPointer.Null;

        IRuntimeTypeSystem rts = CreateRuntimeTypeSystemContract(arch, methodDescBuilder =>
        {
            MockMethodTable objectMethodTableView = methodDescBuilder.RTSBuilder.SystemObjectMethodTable;
            objectMethodTable = objectMethodTableView.Address;
            TargetPointer module = methodDescBuilder.LoaderBuilder.AddModule(simpleName: "testModule").Address;
            MockMethodTableAuxiliaryData auxData = methodDescBuilder.RTSBuilder.AddMethodTableAuxiliaryData();
            objectMethodTableView.AuxiliaryData = auxData.Address;
            auxData.LoaderModule = module;

            const byte count = 10;
            byte methodDescSize = (byte)(methodDescBuilder.MethodDescLayout.Size / methodDescBuilder.MethodDescAlignment);
            byte chunkSize = (byte)(count * methodDescSize);
            MockMethodDescChunk chunk = methodDescBuilder.AddMethodDescChunk("testMethod", chunkSize);
            chunk.MethodTable = objectMethodTable.Value;
            chunk.Size = chunkSize;
            chunk.Count = count;
            chunk.FlagsAndTokenRange = (ushort)(expectedRidRangeStart >> (int)methodDescBuilder.MethodDescTokenRemainderBitCount);

            byte methodDescNum = 3;
            byte methodDescIndex = (byte)(methodDescNum * methodDescSize);
            MockMethodDesc methodDesc = chunk.GetMethodDescAtChunkIndex(methodDescIndex, methodDescBuilder.MethodDescLayout);
            methodDesc.ChunkIndex = methodDescIndex;
            methodDesc.Flags3AndTokenRemainder = expectedRidRemainder;
            methodDesc.Slot = expectedSlotNum;
            testMethodDescAddress = new TargetPointer(methodDesc.Address);
        });

        MethodDescHandle handle = rts.GetMethodDescHandle(testMethodDescAddress);
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
        const ushort numVirtuals = 1;
        const byte count = 5;
        TargetPointer[] arrayMethods = new TargetPointer[count];

        IRuntimeTypeSystem rts = CreateRuntimeTypeSystemContract(arch, methodDescBuilder =>
        {
            TargetPointer methodTable = AddMethodTable(methodDescBuilder.RTSBuilder, numVirtuals);
            uint methodDescSize = methodDescBuilder.ArrayMethodDescSize;
            uint methodDescSizeByAlignment = methodDescSize / methodDescBuilder.MethodDescAlignment;
            byte chunkSize = (byte)(count * methodDescSizeByAlignment);
            MockMethodDescChunk chunk = methodDescBuilder.AddMethodDescChunk(string.Empty, chunkSize);
            chunk.MethodTable = methodTable.Value;
            chunk.Size = chunkSize;
            chunk.Count = count;

            for (byte i = 0; i < count; i++)
            {
                byte index = (byte)(i * methodDescSizeByAlignment);
                ushort slotNum = (ushort)(numVirtuals + i);
                ushort flags = (ushort)MethodClassification.Array | (ushort)MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot;
                MockMethodDesc methodDesc = chunk.GetMethodDescAtChunkIndex(index, methodDescBuilder.MethodDescLayout);
                methodDesc.ChunkIndex = index;
                methodDesc.Flags = flags;
                methodDesc.Slot = slotNum;
                arrayMethods[i] = new TargetPointer(methodDesc.Address);
            }
        });

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
        TargetPointer dynamicMethod = TargetPointer.Null;
        TargetPointer ilStubMethod = TargetPointer.Null;

        IRuntimeTypeSystem rts = CreateRuntimeTypeSystemContract(arch, methodDescBuilder =>
        {
            TargetPointer methodTable = AddMethodTable(methodDescBuilder.RTSBuilder);

            const byte count = 2;
            uint methodDescSize = (uint)methodDescBuilder.DynamicMethodDescLayout.Size;
            uint methodDescSizeByAlignment = methodDescSize / methodDescBuilder.MethodDescAlignment;
            byte chunkSize = (byte)(count * methodDescSizeByAlignment);
            MockMethodDescChunk chunk = methodDescBuilder.AddMethodDescChunk(string.Empty, chunkSize);
            chunk.MethodTable = methodTable.Value;
            chunk.Size = chunkSize;
            chunk.Count = count;

            ushort flags = (ushort)MethodClassification.Dynamic;
            MockDynamicMethodDesc dynamicMethodDesc = chunk.GetMethodDescAtChunkIndex(0, methodDescBuilder.DynamicMethodDescLayout);
            dynamicMethodDesc.Flags = flags;
            dynamicMethod = new TargetPointer(dynamicMethodDesc.Address);
            dynamicMethodDesc.ExtendedFlags = (uint)RuntimeTypeSystem_1.DynamicMethodDescExtendedFlags.IsLCGMethod;
            MockDynamicMethodDesc ilStubMethodDesc = chunk.GetMethodDescAtChunkIndex((int)methodDescSizeByAlignment, methodDescBuilder.DynamicMethodDescLayout);
            ilStubMethodDesc.ChunkIndex = (byte)methodDescSizeByAlignment;
            ilStubMethodDesc.Flags = flags;
            ilStubMethodDesc.Slot = 1;
            ilStubMethod = new TargetPointer(ilStubMethodDesc.Address);
            ilStubMethodDesc.ExtendedFlags = (uint)RuntimeTypeSystem_1.DynamicMethodDescExtendedFlags.IsILStub;
        });

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
        TargetPointer[] typeArgsRawAddrs = [0x1000, 0x2000, 0x3000];
        ulong[] typeArgsHandles = typeArgsRawAddrs.Select(a => GetTypeDescHandlePointer(a).Value).ToArray();
        TargetPointer genericMethodDef = TargetPointer.Null;
        TargetPointer genericWithInst = TargetPointer.Null;

        IRuntimeTypeSystem rts = CreateRuntimeTypeSystemContract(arch, methodDescBuilder =>
        {
            TargetPointer methodTable = AddMethodTable(methodDescBuilder.RTSBuilder);

            const byte count = 2;
            uint methodDescSize = (uint)methodDescBuilder.InstantiatedMethodDescLayout.Size;
            uint methodDescSizeByAlignment = methodDescSize / methodDescBuilder.MethodDescAlignment;
            byte chunkSize = (byte)(count * methodDescSizeByAlignment);
            MockMethodDescChunk chunk = methodDescBuilder.AddMethodDescChunk(string.Empty, chunkSize);
            chunk.MethodTable = methodTable.Value;
            chunk.Size = chunkSize;
            chunk.Count = count;

            ushort flags = (ushort)MethodClassification.Instantiated;
            MockInstantiatedMethodDesc genericMethodDefDesc = chunk.GetMethodDescAtChunkIndex(0, methodDescBuilder.InstantiatedMethodDescLayout);
            genericMethodDefDesc.Flags = flags;
            genericMethodDefDesc.Flags2 = (ushort)RuntimeTypeSystem_1.InstantiatedMethodDescFlags2.GenericMethodDefinition;
            genericMethodDef = new TargetPointer(genericMethodDefDesc.Address);

            MockInstantiatedMethodDesc genericWithInstDesc = chunk.GetMethodDescAtChunkIndex((int)methodDescSizeByAlignment, methodDescBuilder.InstantiatedMethodDescLayout);
            genericWithInstDesc.ChunkIndex = (byte)methodDescSizeByAlignment;
            genericWithInstDesc.Flags = flags;
            genericWithInstDesc.Slot = 1;
            genericWithInstDesc.Flags2 = (ushort)RuntimeTypeSystem_1.InstantiatedMethodDescFlags2.GenericMethodDefinition;
            genericWithInstDesc.NumGenericArgs = (ushort)typeArgsHandles.Length;
            genericWithInstDesc.PerInstInfo = methodDescBuilder.AddPerInstInfo(typeArgsHandles).Address;
            genericWithInst = new TargetPointer(genericWithInstDesc.Address);
        });

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
                Assert.Equal(new TargetPointer(typeArgsHandles[i]), instantiation[i].Address);
                Assert.Equal(typeArgsRawAddrs[i], instantiation[i].TypeDescAddress());
            }
        }
    }

    public static IEnumerable<object[]> StdArchOptionalSlotsData()
    {
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            yield return [arch, 0];
            yield return [arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot];
            yield return [arch, MethodDescFlags_1.MethodDescFlags.HasMethodImpl];
            yield return [arch, MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot];
            yield return [arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot | MethodDescFlags_1.MethodDescFlags.HasMethodImpl];
            yield return [arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot | MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot];
            yield return [arch, MethodDescFlags_1.MethodDescFlags.HasMethodImpl | MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot];
            yield return [arch, MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot | MethodDescFlags_1.MethodDescFlags.HasMethodImpl | MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot];
        }
    }

    [Theory]
    [MemberData(nameof(StdArchOptionalSlotsData))]
    public void GetAddressOfNativeCodeSlot_OptionalSlots(MockTarget.Architecture arch, ushort flagsValue)
    {
        MethodDescFlags_1.MethodDescFlags flags = (MethodDescFlags_1.MethodDescFlags)flagsValue;
        TargetPointer methodDescAddress = TargetPointer.Null;
        uint methodDescSize = 0;
        TargetTestHelpers helpers = new(arch);

        IRuntimeTypeSystem rts = CreateRuntimeTypeSystemContract(arch, methodDescBuilder =>
        {
            TargetPointer methodTable = AddMethodTable(methodDescBuilder.RTSBuilder);

            methodDescSize = (uint)methodDescBuilder.MethodDescLayout.Size;
            if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot))
                methodDescSize += methodDescBuilder.NonVtableSlotSize;

            if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasMethodImpl))
                methodDescSize += methodDescBuilder.MethodImplSize;

            if (flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot))
                methodDescSize += methodDescBuilder.NativeCodeSlotSize;

            byte chunkSize = (byte)(methodDescSize / methodDescBuilder.MethodDescAlignment);
            MockMethodDescChunk chunk = methodDescBuilder.AddMethodDescChunk(string.Empty, chunkSize);
            chunk.MethodTable = methodTable.Value;
            chunk.Size = chunkSize;
            chunk.Count = 1;
            MockMethodDesc methodDesc = chunk.GetMethodDescAtChunkIndex(0, methodDescBuilder.MethodDescLayout);
            methodDesc.Flags = (ushort)flags;
            methodDescAddress = new TargetPointer(methodDesc.Address);
        });

        MethodDescHandle handle = rts.GetMethodDescHandle(methodDescAddress);
        Assert.NotEqual(TargetPointer.Null, handle.Address);

        bool hasNativeCodeSlot = rts.HasNativeCodeSlot(handle);
        Assert.Equal(flags.HasFlag(MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot), hasNativeCodeSlot);
        if (hasNativeCodeSlot)
        {
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
            yield return [arch, DataType.MethodDesc];
            yield return [arch, DataType.FCallMethodDesc];
            yield return [arch, DataType.PInvokeMethodDesc];
            yield return [arch, DataType.EEImplMethodDesc];
            yield return [arch, DataType.ArrayMethodDesc];
            yield return [arch, DataType.InstantiatedMethodDesc];
            yield return [arch, DataType.CLRToCOMCallMethodDesc];
            yield return [arch, DataType.DynamicMethodDesc];
        }
    }

    [Theory]
    [MemberData(nameof(StdArchMethodDescTypeData))]
    public void GetNativeCode_StableEntryPoint_NonVtableSlot(MockTarget.Architecture arch, DataType methodDescType)
    {
        TargetPointer methodDescAddress = TargetPointer.Null;
        TargetCodePointer nativeCode = new TargetCodePointer(0x0789_abc0);
        Mock<IExecutionManager> mockExecutionManager = new();

        IRuntimeTypeSystem rts = CreateRuntimeTypeSystemContract(arch, methodDescBuilder =>
        {
            TargetTestHelpers helpers = methodDescBuilder.Builder.TargetTestHelpers;
            TargetPointer methodTable = AddMethodTable(methodDescBuilder.RTSBuilder);
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

            uint methodDescBaseSize = GetMethodDescBaseSize(methodDescBuilder, methodDescType);
            uint methodDescSize = methodDescBaseSize + methodDescBuilder.NonVtableSlotSize;
            byte chunkSize = (byte)(methodDescSize / methodDescBuilder.MethodDescAlignment);
            MockMethodDescChunk chunk = methodDescBuilder.AddMethodDescChunk(string.Empty, chunkSize);
            chunk.MethodTable = methodTable.Value;
            chunk.Size = chunkSize;
            chunk.Count = 1;

            ushort flags = (ushort)((ushort)classification | (ushort)MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot);
            MockMethodDesc methodDesc = methodDescType switch
            {
                DataType.InstantiatedMethodDesc => chunk.GetMethodDescAtChunkIndex(0, methodDescBuilder.InstantiatedMethodDescLayout),
                DataType.DynamicMethodDesc => chunk.GetMethodDescAtChunkIndex(0, methodDescBuilder.DynamicMethodDescLayout),
                DataType.EEImplMethodDesc or DataType.ArrayMethodDesc => chunk.GetMethodDescAtChunkIndex(0, methodDescBuilder.StoredSigMethodDescLayout),
                _ => chunk.GetMethodDescAtChunkIndex(0, methodDescBuilder.MethodDescLayout),
            };
            methodDesc.Flags = flags;
            methodDesc.Flags3AndTokenRemainder = (ushort)MethodDescFlags_1.MethodDescFlags3.HasStableEntryPoint;
            methodDescAddress = new TargetPointer(methodDesc.Address);
            helpers.WritePointer(
                methodDescBuilder.Builder.BorrowAddressRange(methodDescAddress + methodDescBaseSize, helpers.PointerSize),
                nativeCode);
        }, mockExecutionManager);

        CodeBlockHandle codeBlock = new(methodDescAddress);
        mockExecutionManager.Setup(em => em.GetCodeBlockHandle(nativeCode)).Returns(codeBlock);
        mockExecutionManager.Setup(em => em.GetMethodDesc(codeBlock)).Returns(methodDescAddress);

        MethodDescHandle handle = rts.GetMethodDescHandle(methodDescAddress);
        Assert.NotEqual(TargetPointer.Null, handle.Address);

        TargetCodePointer actualNativeCode = rts.GetNativeCode(handle);
        Assert.Equal(nativeCode, actualNativeCode);
    }

    private static TargetPointer AddMethodTable(MockDescriptors.RuntimeTypeSystem rtsBuilder, ushort numVirtuals = 5)
    {
        MockEEClass eeClass = rtsBuilder.AddEEClass(string.Empty);
        eeClass.NumMethods = 2;
        eeClass.NumNonVirtualSlots = 1;

        MockMethodTable methodTable = rtsBuilder.AddMethodTable(string.Empty);
        methodTable.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
        methodTable.NumVirtuals = numVirtuals;

        eeClass.MethodTable = methodTable.Address;
        methodTable.EEClassOrCanonMT = eeClass.Address;
        return methodTable.Address;
    }

    private static TargetPointer GetTypeDescHandlePointer(TargetPointer addr)
        => addr | (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
}
