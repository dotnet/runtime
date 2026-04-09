// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ObjectTests
{
    private static IObject CreateObjectContract(MockTarget.Architecture arch, Action<MockDescriptors.MockObjectBuilder> configure)
        => CreateObjectTarget(arch, configure).Contracts.Object;

    private static ISOSDacInterface CreateSOSDacInterface(
        MockTarget.Architecture arch,
        Action<MockDescriptors.MockObjectBuilder> configure,
        Func<TestPlaceholderTarget, ContractRegistry>? createContracts = null)
        => new SOSDacImpl(CreateObjectTarget(arch, configure, createContracts), legacyObj: null);

    private static TestPlaceholderTarget CreateObjectTarget(
        MockTarget.Architecture arch,
        Action<MockDescriptors.MockObjectBuilder> configure,
        Func<TestPlaceholderTarget, ContractRegistry>? createContracts = null)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        MockMemorySpace.Builder builder = new(targetTestHelpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockDescriptors.MockObjectBuilder objectBuilder = new(rtsBuilder);

        configure?.Invoke(objectBuilder);

        var target = new TestPlaceholderTarget(
            arch,
            builder.GetMemoryContext().ReadFromTarget,
            CreateContractTypes(objectBuilder),
            CreateContractGlobals(objectBuilder));
        target.SetContracts(createContracts?.Invoke(target) ?? CreateDefaultContracts(target));
        return target;
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockDescriptors.MockObjectBuilder objectBuilder)
        => new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.Object] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ObjectLayout),
            [DataType.ObjectHeader] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ObjectHeaderLayout),
            [DataType.String] = TargetTestHelpers.CreateTypeInfo(objectBuilder.StringLayout),
            [DataType.Array] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ArrayLayout),
            [DataType.SyncTableEntry] = TargetTestHelpers.CreateTypeInfo(objectBuilder.SyncTableEntryLayout),
            [DataType.SyncBlock] = TargetTestHelpers.CreateTypeInfo(objectBuilder.SyncBlockLayout),
            [DataType.InteropSyncBlockInfo] = TargetTestHelpers.CreateTypeInfo(objectBuilder.InteropSyncBlockInfoLayout),
        }.Concat(MethodTableTests.CreateContractTypes(objectBuilder.RTSBuilder)).ToDictionary();

    private static (string Name, ulong Value)[] CreateContractGlobals(MockDescriptors.MockObjectBuilder objectBuilder)
        => MethodTableTests.CreateContractGlobals(objectBuilder.RTSBuilder).Concat(
        [
            (nameof(Constants.Globals.ObjectToMethodTableUnmask), MockDescriptors.MockObjectBuilder.TestObjectToMethodTableUnmask),
            (nameof(Constants.Globals.StringMethodTable), MockDescriptors.MockObjectBuilder.TestStringMethodTableGlobalAddress),
            (nameof(Constants.Globals.ArrayBoundsZero), MockDescriptors.MockObjectBuilder.TestArrayBoundsZeroGlobalAddress),
            (nameof(Constants.Globals.SyncTableEntries), MockDescriptors.MockObjectBuilder.TestSyncTableEntriesGlobalAddress),
            (nameof(Constants.Globals.SyncBlockValueToObjectOffset), MockDescriptors.MockObjectBuilder.TestSyncBlockValueToObjectOffset),
            (nameof(Constants.Globals.SyncBlockIsHashOrSyncBlockIndex), 0x08000000u),
            (nameof(Constants.Globals.SyncBlockIsHashCode), 0x04000000u),
            (nameof(Constants.Globals.SyncBlockIndexMask), (1u << 26) - 1),
            (nameof(Constants.Globals.SyncBlockHashCodeMask), (1u << 26) - 1),
        ]).ToArray();

    private static ContractRegistry CreateDefaultContracts(TestPlaceholderTarget target)
        => Mock.Of<ContractRegistry>(
            c => c.Object == ((IContractFactory<IObject>)new ObjectFactory()).CreateContract(target, 1)
                && c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)
                && c.SyncBlock == ((IContractFactory<ISyncBlock>)new SyncBlockFactory()).CreateContract(target, 1));

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnmaskMethodTableAddress(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        const ulong TestMethodTableAddress = 0x00000000_10000027;
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            });

        TargetPointer mt = contract.GetMethodTableAddress(TestObjectAddress);

        Assert.Equal(TestMethodTableAddress & ~MockDescriptors.MockObjectBuilder.TestObjectToMethodTableUnmask, mt.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void StringValue(MockTarget.Architecture arch)
    {
        TargetPointer TestStringAddress = default;
        string expected = "test_string_value";
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                TestStringAddress = objectBuilder.AddStringObject(expected);
            });

        string actual = contract.GetStringValue(TestStringAddress);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ArrayData(MockTarget.Architecture arch)
    {
        TargetPointer SingleDimensionArrayAddress = default;
        TargetPointer MultiDimensionArrayAddress = default;
        TargetPointer NonZeroLowerBoundArrayAddress = default;
        int numComponentsOffset = 0;

        Array singleDimension = new int[10];
        Array multiDimension = new int[1, 2, 3, 4];
        Array nonZeroLowerBound = Array.CreateInstance(typeof(int), [10], [5]);
        TargetTestHelpers targetTestHelpers = new(arch);
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                numComponentsOffset = objectBuilder.ArrayLayout.GetField("m_NumComponents").Offset;
                SingleDimensionArrayAddress = objectBuilder.AddArrayObject(singleDimension);
                MultiDimensionArrayAddress = objectBuilder.AddArrayObject(multiDimension);
                NonZeroLowerBoundArrayAddress = objectBuilder.AddArrayObject(nonZeroLowerBound);
            });

        {
            TargetPointer data = contract.GetArrayData(SingleDimensionArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
            Assert.Equal(SingleDimensionArrayAddress + targetTestHelpers.ArrayBaseBaseSize - targetTestHelpers.ObjHeaderSize, data.Value);
            Assert.Equal((uint)singleDimension.Length, count);
            Assert.Equal(SingleDimensionArrayAddress + (ulong)numComponentsOffset, boundsStart.Value);
            Assert.Equal(MockDescriptors.MockObjectBuilder.TestArrayBoundsZeroGlobalAddress, lowerBounds.Value);
        }
        {
            TargetPointer data = contract.GetArrayData(MultiDimensionArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
            Assert.Equal(MultiDimensionArrayAddress + targetTestHelpers.ArrayBaseSize + (ulong)(multiDimension.Rank * sizeof(int) * 2), data.Value);
            Assert.Equal((uint)multiDimension.Length, count);
            Assert.Equal(MultiDimensionArrayAddress + targetTestHelpers.ArrayBaseSize, boundsStart.Value);
            Assert.Equal(boundsStart.Value + (ulong)(multiDimension.Rank * sizeof(int)), lowerBounds.Value);
        }
        {
            TargetPointer data = contract.GetArrayData(NonZeroLowerBoundArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
            Assert.Equal(NonZeroLowerBoundArrayAddress + targetTestHelpers.ArrayBaseSize + (ulong)(nonZeroLowerBound.Rank * sizeof(int) * 2), data.Value);
            Assert.Equal((uint)nonZeroLowerBound.Length, count);
            Assert.Equal(NonZeroLowerBoundArrayAddress + targetTestHelpers.ArrayBaseSize, boundsStart.Value);
            Assert.Equal(boundsStart.Value + (ulong)(nonZeroLowerBound.Rank * sizeof(int)), lowerBounds.Value);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ComData(MockTarget.Architecture arch)
    {
        TargetPointer TestComObjectAddress = default;
        TargetPointer TestNonComObjectAddress = default;

        TargetPointer expectedRCW = 0xaaaa;
        TargetPointer expectedCCW = 0xbbbb;
        TargetPointer expectedCCF = 0xcccc;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                uint syncBlockIndex = 0;
                TestComObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, expectedRCW, expectedCCW, expectedCCF);
                TestNonComObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);
            });

        {
            bool res = contract.GetBuiltInComData(TestComObjectAddress, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);
            Assert.True(res);
            Assert.Equal(expectedRCW.Value, rcw.Value);
            Assert.Equal(expectedCCW.Value, ccw.Value);
            Assert.Equal(expectedCCF.Value, ccf.Value);
        }
        {
            bool res = contract.GetBuiltInComData(TestNonComObjectAddress, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);
            Assert.False(res);
            Assert.Equal(TargetPointer.Null.Value, rcw.Value);
            Assert.Equal(TargetPointer.Null.Value, ccw.Value);
            Assert.Equal(TargetPointer.Null.Value, ccf.Value);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_ZeroAddress(MockTarget.Architecture arch)
    {
        ISOSDacInterface sosDac = CreateSOSDacInterface(arch, objectBuilder => { });
        char[] buffer = new char[256];
        uint needed;
        int hr;
        fixed (char* ptr = buffer)
        {
            hr = sosDac.GetObjectClassName(default, (uint)buffer.Length, ptr, &needed);
        }
        Assert.NotEqual(HResults.S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_UnloadedModule(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        TargetPointer TestMethodTableAddress = default;
        ISOSDacInterface sosDac = CreateSOSDacInterface(
            arch,
            objectBuilder =>
            {
                MockEEClass eeClass = objectBuilder.RTSBuilder.AddEEClass("TestClass");
                MockMethodTable methodTable = objectBuilder.RTSBuilder.AddMethodTable("TestClass");
                methodTable.BaseSize = objectBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                TestMethodTableAddress = methodTable.Address;
                eeClass.MethodTable = methodTable.Address;
                methodTable.EEClassOrCanonMT = eeClass.Address;
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            },
            target => {
                var mockRts = new Mock<IRuntimeTypeSystem>();
                TypeHandle handle = new TypeHandle(TestMethodTableAddress);
                mockRts.Setup(r => r.GetTypeHandle(TestMethodTableAddress)).Returns(handle);
                mockRts.Setup(r => r.GetModule(handle)).Returns(TargetPointer.Null);

                var mockLoader = new Mock<ILoader>();
                mockLoader.Setup(l => l.GetModuleHandleFromModulePtr(It.IsAny<TargetPointer>())).Returns(default(Contracts.ModuleHandle));
                mockLoader.Setup(l => l.TryGetLoadedImageContents(It.IsAny<Contracts.ModuleHandle>(), out It.Ref<TargetPointer>.IsAny, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny)).Returns(false);

                var mockObject = new Mock<IObject>();
                mockObject.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(TestMethodTableAddress);

                return Mock.Of<ContractRegistry>(
                    c => c.Object == mockObject.Object
                        && c.RuntimeTypeSystem == mockRts.Object
                        && c.Loader == mockLoader.Object);
            });
        char[] buffer = new char[256];
        uint needed;
        int hr;
        fixed (char* ptr = buffer)
        {
            hr = sosDac.GetObjectClassName(new ClrDataAddress(TestObjectAddress.Value), (uint)buffer.Length, ptr, &needed);
        }
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)"<Unloaded Type>".Length + 1, needed);
        Assert.Equal("<Unloaded Type>", new string(buffer, 0, (int)needed - 1));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_NullBufferReturnsNeededSize(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        TargetPointer TestMethodTableAddress = default;
        ISOSDacInterface sosDac = CreateSOSDacInterface(
            arch,
            objectBuilder =>
            {
                MockEEClass eeClass = objectBuilder.RTSBuilder.AddEEClass("TestClass");
                MockMethodTable methodTable = objectBuilder.RTSBuilder.AddMethodTable("TestClass");
                methodTable.BaseSize = objectBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                TestMethodTableAddress = methodTable.Address;
                eeClass.MethodTable = methodTable.Address;
                methodTable.EEClassOrCanonMT = eeClass.Address;
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            },
            target => {
                var mockRts = new Mock<IRuntimeTypeSystem>();
                TypeHandle handle = new TypeHandle(TestMethodTableAddress);
                mockRts.Setup(r => r.GetTypeHandle(TestMethodTableAddress)).Returns(handle);
                mockRts.Setup(r => r.GetModule(handle)).Returns(TargetPointer.Null);

                var mockLoader = new Mock<ILoader>();
                mockLoader.Setup(l => l.GetModuleHandleFromModulePtr(It.IsAny<TargetPointer>())).Returns(default(Contracts.ModuleHandle));
                mockLoader.Setup(l => l.TryGetLoadedImageContents(It.IsAny<Contracts.ModuleHandle>(), out It.Ref<TargetPointer>.IsAny, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny)).Returns(false);

                var mockObject = new Mock<IObject>();
                mockObject.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(TestMethodTableAddress);

                return Mock.Of<ContractRegistry>(
                    c => c.Object == mockObject.Object
                        && c.RuntimeTypeSystem == mockRts.Object
                        && c.Loader == mockLoader.Object);
            });
        uint needed;
        int hr = sosDac.GetObjectClassName(new ClrDataAddress(TestObjectAddress.Value), 0, null, &needed);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)"<Unloaded Type>".Length + 1, needed);
    }
}
