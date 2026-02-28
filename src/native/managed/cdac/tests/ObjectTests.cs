// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using MockObject = MockDescriptors.Object;

public unsafe class ObjectTests
{
    private static void ObjectContractHelper(MockTarget.Architecture arch, Action<MockObject> configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        MockMemorySpace.Builder builder = new(targetTestHelpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockObject objectBuilder = new(rtsBuilder);

        configure?.Invoke(objectBuilder);

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, objectBuilder.Types, objectBuilder.Globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Object == ((IContractFactory<IObject>)new ObjectFactory()).CreateContract(target, 1)
                && c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)));

        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnmaskMethodTableAddress(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        const ulong TestMethodTableAddress = 0x00000000_10000027;
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                Assert.NotNull(contract);
                TargetPointer mt = contract.GetMethodTableAddress(TestObjectAddress);
                Assert.Equal(TestMethodTableAddress & ~MockObject.TestObjectToMethodTableUnmask, mt.Value);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void StringValue(MockTarget.Architecture arch)
    {
        TargetPointer TestStringAddress = default;
        string expected = "test_string_value";
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                TestStringAddress = objectBuilder.AddStringObject(expected);
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                Assert.NotNull(contract);
                string actual = contract.GetStringValue(TestStringAddress);
                Assert.Equal(expected, actual);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ArrayData(MockTarget.Architecture arch)
    {
        TargetPointer SingleDimensionArrayAddress = default;
        TargetPointer MultiDimensionArrayAddress = default;
        TargetPointer NonZeroLowerBoundArrayAddress = default;

        Array singleDimension = new int[10];
        Array multiDimension = new int[1, 2, 3, 4];
        Array nonZeroLowerBound = Array.CreateInstance(typeof(int), [10], [5]);
        TargetTestHelpers targetTestHelpers = new(arch);
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                SingleDimensionArrayAddress = objectBuilder.AddArrayObject(singleDimension);
                MultiDimensionArrayAddress = objectBuilder.AddArrayObject(multiDimension);
                NonZeroLowerBoundArrayAddress = objectBuilder.AddArrayObject(nonZeroLowerBound);
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                Assert.NotNull(contract);
                {
                    TargetPointer data = contract.GetArrayData(SingleDimensionArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                    Assert.Equal(SingleDimensionArrayAddress + targetTestHelpers.ArrayBaseBaseSize - targetTestHelpers.ObjHeaderSize, data.Value);
                    Assert.Equal((uint)singleDimension.Length, count);
                    Target.TypeInfo arrayType = target.GetTypeInfo(DataType.Array);
                    Assert.Equal(SingleDimensionArrayAddress + (ulong)arrayType.Fields["m_NumComponents"].Offset, boundsStart.Value);
                    Assert.Equal(MockObject.TestArrayBoundsZeroGlobalAddress, lowerBounds.Value);
                }
                {
                    TargetPointer data = contract.GetArrayData(MultiDimensionArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                    Assert.Equal(MultiDimensionArrayAddress + targetTestHelpers.ArrayBaseBaseSize - targetTestHelpers.ObjHeaderSize, data.Value);
                    Assert.Equal((uint)multiDimension.Length, count);
                    Assert.Equal(MultiDimensionArrayAddress + targetTestHelpers.ArrayBaseSize, boundsStart.Value);
                    Assert.Equal(boundsStart.Value + (ulong)(multiDimension.Rank * sizeof(int)), lowerBounds.Value);
                }
                {
                    TargetPointer data = contract.GetArrayData(NonZeroLowerBoundArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                    Assert.Equal(NonZeroLowerBoundArrayAddress + targetTestHelpers.ArrayBaseBaseSize - targetTestHelpers.ObjHeaderSize, data.Value);
                    Assert.Equal((uint)nonZeroLowerBound.Length, count);
                    Assert.Equal(NonZeroLowerBoundArrayAddress + targetTestHelpers.ArrayBaseSize, boundsStart.Value);
                    Assert.Equal(boundsStart.Value + (ulong)(nonZeroLowerBound.Rank * sizeof(int)), lowerBounds.Value);
                }
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ComData(MockTarget.Architecture arch)
    {
        TargetPointer TestComObjectAddress = default;
        TargetPointer TestNonComObjectAddress = default;

        TargetPointer expectedRCW = 0xaaaa;
        TargetPointer expectedCCW = 0xbbbb;

        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                uint syncBlockIndex = 0;
                TestComObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, expectedRCW, expectedCCW);
                TestNonComObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, TargetPointer.Null, TargetPointer.Null);
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                Assert.NotNull(contract);
                {
                    bool res = contract.GetBuiltInComData(TestComObjectAddress, out TargetPointer rcw, out TargetPointer ccw);
                    Assert.True(res);
                    Assert.Equal(expectedRCW.Value, rcw.Value);
                    Assert.Equal(expectedCCW.Value, ccw.Value);
                }
                {
                    bool res = contract.GetBuiltInComData(TestNonComObjectAddress, out TargetPointer rcw, out TargetPointer ccw);
                    Assert.False(res);
                    Assert.Equal(TargetPointer.Null.Value, rcw.Value);
                    Assert.Equal(TargetPointer.Null.Value, ccw.Value);
                }
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ComData_RCWLockBitMasked(MockTarget.Architecture arch)
    {
        // m_pRCW uses bit0 as a lock bit; verify the cDAC masks it off correctly.
        TargetPointer testObjectAddress = default;
        TargetPointer rawRCW = 0xaaab;     // valid pointer | 1 (lock bit set)
        TargetPointer expectedRCW = 0xaaaa; // after masking

        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                uint syncBlockIndex = 0;
                testObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, rawRCW, TargetPointer.Null);
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                bool res = contract.GetBuiltInComData(testObjectAddress, out TargetPointer rcw, out TargetPointer ccw);
                Assert.True(res);
                Assert.Equal(expectedRCW.Value, rcw.Value);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ComData_RCWSentinelIsNull(MockTarget.Architecture arch)
    {
        // m_pRCW == 0x1 (sentinel meaning "was set but now cleared") → RCW is null.
        TargetPointer testObjectAddress = default;
        TargetPointer sentinelRCW = 0x1;

        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                uint syncBlockIndex = 0;
                testObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, sentinelRCW, TargetPointer.Null);
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                bool res = contract.GetBuiltInComData(testObjectAddress, out TargetPointer rcw, out _);
                Assert.False(res);
                Assert.Equal(TargetPointer.Null, rcw);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ComData_CCWSentinelIsNull(MockTarget.Architecture arch)
    {
        // m_pCCW == 0x1 (sentinel meaning "was set but now null") → CCW is null.
        TargetPointer testObjectAddress = default;
        TargetPointer sentinelCCW = 0x1;

        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                uint syncBlockIndex = 0;
                testObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, TargetPointer.Null, sentinelCCW);
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                bool res = contract.GetBuiltInComData(testObjectAddress, out _, out TargetPointer ccw);
                Assert.False(res);
                Assert.Equal(TargetPointer.Null, ccw);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_ZeroAddress(MockTarget.Architecture arch)
    {
        ObjectContractHelper(arch,
            (objectBuilder) => { },
            (target) =>
            {
                ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null);
                char[] buffer = new char[256];
                uint needed;
                int hr;
                fixed (char* ptr = buffer)
                {
                    hr = sosDac.GetObjectClassName(default, (uint)buffer.Length, ptr, &needed);
                }
                Assert.NotEqual(HResults.S_OK, hr);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_UnloadedModule(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        TargetPointer TestMethodTableAddress = default;
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                TargetPointer eeClass = objectBuilder.RTSBuilder.AddEEClass("TestClass", attr: 0, numMethods: 0, numNonVirtualSlots: 0);
                TestMethodTableAddress = objectBuilder.RTSBuilder.AddMethodTable("TestClass",
                    mtflags: default, mtflags2: default, baseSize: objectBuilder.Builder.TargetTestHelpers.ObjectBaseSize,
                    module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: 0);
                objectBuilder.RTSBuilder.SetEEClassAndCanonMTRefs(eeClass, TestMethodTableAddress);
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            },
            (target) =>
            {
                var mockRts = new Mock<IRuntimeTypeSystem>();
                TypeHandle handle = new TypeHandle(TestMethodTableAddress);
                mockRts.Setup(r => r.GetTypeHandle(TestMethodTableAddress)).Returns(handle);
                mockRts.Setup(r => r.GetModule(handle)).Returns(TargetPointer.Null);

                var mockLoader = new Mock<ILoader>();
                mockLoader.Setup(l => l.GetModuleHandleFromModulePtr(It.IsAny<TargetPointer>())).Returns(default(Contracts.ModuleHandle));
                mockLoader.Setup(l => l.TryGetLoadedImageContents(It.IsAny<Contracts.ModuleHandle>(), out It.Ref<TargetPointer>.IsAny, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny)).Returns(false);

                var mockObject = new Mock<IObject>();
                mockObject.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(TestMethodTableAddress);

                ((TestPlaceholderTarget)target).SetContracts(Mock.Of<ContractRegistry>(
                    c => c.Object == mockObject.Object
                        && c.RuntimeTypeSystem == mockRts.Object
                        && c.Loader == mockLoader.Object));

                ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null);
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
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_NullBufferReturnsNeededSize(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        TargetPointer TestMethodTableAddress = default;
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                TargetPointer eeClass = objectBuilder.RTSBuilder.AddEEClass("TestClass", attr: 0, numMethods: 0, numNonVirtualSlots: 0);
                TestMethodTableAddress = objectBuilder.RTSBuilder.AddMethodTable("TestClass",
                    mtflags: default, mtflags2: default, baseSize: objectBuilder.Builder.TargetTestHelpers.ObjectBaseSize,
                    module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: 0);
                objectBuilder.RTSBuilder.SetEEClassAndCanonMTRefs(eeClass, TestMethodTableAddress);
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            },
            (target) =>
            {
                var mockRts = new Mock<IRuntimeTypeSystem>();
                TypeHandle handle = new TypeHandle(TestMethodTableAddress);
                mockRts.Setup(r => r.GetTypeHandle(TestMethodTableAddress)).Returns(handle);
                mockRts.Setup(r => r.GetModule(handle)).Returns(TargetPointer.Null);

                var mockLoader = new Mock<ILoader>();
                mockLoader.Setup(l => l.GetModuleHandleFromModulePtr(It.IsAny<TargetPointer>())).Returns(default(Contracts.ModuleHandle));
                mockLoader.Setup(l => l.TryGetLoadedImageContents(It.IsAny<Contracts.ModuleHandle>(), out It.Ref<TargetPointer>.IsAny, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny)).Returns(false);

                var mockObject = new Mock<IObject>();
                mockObject.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(TestMethodTableAddress);

                ((TestPlaceholderTarget)target).SetContracts(Mock.Of<ContractRegistry>(
                    c => c.Object == mockObject.Object
                        && c.RuntimeTypeSystem == mockRts.Object
                        && c.Loader == mockLoader.Object));

                ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null);
                uint needed;
                int hr = sosDac.GetObjectClassName(new ClrDataAddress(TestObjectAddress.Value), 0, null, &needed);
                Assert.Equal(HResults.S_OK, hr);
                Assert.Equal((uint)"<Unloaded Type>".Length + 1, needed);
            });
    }
}
