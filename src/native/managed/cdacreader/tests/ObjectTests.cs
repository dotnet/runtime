// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

using MockObject = MockDescriptors.Object;

public unsafe class ObjectTests
{

    private static void ObjectContractHelper(MockTarget.Architecture arch, Action<MockObject> configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        MockMemorySpace.Builder builder = new(targetTestHelpers);
        Dictionary<DataType, Target.TypeInfo> types = new();
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(types, builder) {
            // arbtrary address range
            TypeSystemAllocator = builder.CreateAllocator(start: 0x00000000_4a000000, end: 0x00000000_4b000000),
        };
        MockObject objectBuilder = new(rtsBuilder);
        MockObject.AddTypes(types, targetTestHelpers);
        builder = builder
            .SetContracts([ nameof (Contracts.Object), nameof (Contracts.RuntimeTypeSystem) ])
            .SetGlobals(MockObject.Globals(targetTestHelpers))
            .SetTypes(types);

        objectBuilder.AddGlobalPointers();

        configure?.Invoke(objectBuilder);

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);
        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnmaskMethodTableAddress(MockTarget.Architecture arch)
    {
        const ulong TestObjectAddress = 0x00000000_10000010;
        const ulong TestMethodTableAddress = 0x00000000_10000027;
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                objectBuilder.AddObject(TestObjectAddress, TestMethodTableAddress);
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
        const ulong TestStringAddress = 0x00000000_10000010;
        string expected = "test_string_value";
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                objectBuilder.AddStringObject(TestStringAddress, expected);
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
        const ulong SingleDimensionArrayAddress = 0x00000000_20000010;
        const ulong MultiDimensionArrayAddress = 0x00000000_30000010;
        const ulong NonZeroLowerBoundArrayAddress = 0x00000000_40000010;

        Array singleDimension = new int[10];
        Array multiDimension = new int[1, 2, 3, 4];
        Array nonZeroLowerBound = Array.CreateInstance(typeof(int), [10], [5]);
        TargetTestHelpers targetTestHelpers = new(arch);
        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                objectBuilder.AddArrayObject(SingleDimensionArrayAddress, singleDimension);
                objectBuilder.AddArrayObject(MultiDimensionArrayAddress, multiDimension);
                objectBuilder.AddArrayObject(NonZeroLowerBoundArrayAddress, nonZeroLowerBound);
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
        const ulong TestComObjectAddress = 0x00000000_10000010;
        const ulong TestNonComObjectAddress = 0x00000000_10000020;

        TargetPointer expectedRCW = 0xaaaa;
        TargetPointer expectedCCW = 0xbbbb;

        ObjectContractHelper(arch,
            (objectBuilder) =>
            {
                uint syncBlockIndex = 0;
                objectBuilder.AddObjectWithSyncBlock(TestComObjectAddress, 0, syncBlockIndex++, expectedRCW, expectedCCW);
                objectBuilder.AddObjectWithSyncBlock(TestNonComObjectAddress, 0, syncBlockIndex++, TargetPointer.Null, TargetPointer.Null);
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
