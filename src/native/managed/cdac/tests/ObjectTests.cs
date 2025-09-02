// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
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
                && c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)
                && c.SyncBlock == ((IContractFactory<ISyncBlock>)new SyncBlockFactory()).CreateContract(target, 1)));

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
}
