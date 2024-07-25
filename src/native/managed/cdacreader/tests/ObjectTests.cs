// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public unsafe class ObjectTests
{
    const ulong TestStringMethodTableGlobalAddress = 0x00000000_100000a0;
    const ulong TestStringMethodTableAddress = 0x00000000_100000a8;
    const ulong TestArrayBoundsZeroGlobalAddress = 0x00000000_100000b0;

    private static readonly Target.TypeInfo ObjectTypeInfo = new()
    {
        Fields = {
            { "m_pMethTab", new() { Offset = 0, Type = DataType.pointer} },
        }
    };

    private static readonly Target.TypeInfo StringTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { "m_StringLength", new() { Offset = 0x8, Type = DataType.uint32} },
            { "m_FirstChar", new() { Offset = 0xc, Type = DataType.uint16} },
        }
    };

    private static readonly Target.TypeInfo ArrayTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { "m_NumComponents", new() { Offset = 0x8, Type = DataType.uint32} },
        },
    };

    private static readonly (DataType Type, Target.TypeInfo Info)[] ObjectTypes = MethodTableTests.RTSTypes.Concat(
    [
        (DataType.Object, ObjectTypeInfo),
        (DataType.String, StringTypeInfo),
    ]).ToArray();

    const ulong TestObjectToMethodTableUnmask = 0x7;
    private static (string Name, ulong Value, string? Type)[] ObjectGlobals = MethodTableTests.RTSGlobals.Concat(
    [
        (nameof(Constants.Globals.ObjectToMethodTableUnmask), TestObjectToMethodTableUnmask, "uint8"),
        (nameof(Constants.Globals.StringMethodTable), TestStringMethodTableGlobalAddress, null),
        (nameof(Constants.Globals.ArrayBoundsZero), TestArrayBoundsZeroGlobalAddress, null),
    ]).ToArray();

    private static MockMemorySpace.Builder AddStringMethodTablePointer(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
    {
        MockMemorySpace.HeapFragment fragment = new() { Name = "Address of String Method Table", Address = TestStringMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
        targetTestHelpers.WritePointer(fragment.Data, TestStringMethodTableAddress);
        return builder.AddHeapFragments([
            fragment,
            new () { Name = "String Method Table", Address = TestStringMethodTableAddress, Data = new byte[targetTestHelpers.PointerSize] }
        ]);
    }

    private delegate MockMemorySpace.Builder ConfigureContextBuilder(MockMemorySpace.Builder builder);

    private static void ObjectContractHelper(MockTarget.Architecture arch, ConfigureContextBuilder configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        List<(DataType Type, Target.TypeInfo Info)> typesLocal = new (ObjectTypes);
        typesLocal.Add((DataType.Array, ArrayTypeInfo with { Size = targetTestHelpers.ArrayBaseSize }));

        List<(string Name, ulong Value, string? Type)> globalsLocal = new(ObjectGlobals);
        globalsLocal.Add((nameof(Constants.Globals.ObjectHeaderSize), targetTestHelpers.ObjHeaderSize, "uint32"));

        string typesJson = TargetTestHelpers.MakeTypesJson(typesLocal);
        string globalsJson = TargetTestHelpers.MakeGlobalsJson(globalsLocal);
        byte[] json = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {
                "{{nameof(Contracts.Object)}}": 1,
                "{{nameof(Contracts.RuntimeTypeSystem)}}": 1
            },
            "types": { {{typesJson}} },
            "globals": { {{globalsJson}} }
        }
        """);
        Span<byte> descriptor = stackalloc byte[targetTestHelpers.ContractDescriptorSize];
        targetTestHelpers.ContractDescriptorFill(descriptor, json.Length, ObjectGlobals.Length);

        int pointerSize = targetTestHelpers.PointerSize;
        Span<byte> pointerData = stackalloc byte[ObjectGlobals.Length * pointerSize];
        for (int i = 0; i < ObjectGlobals.Length; i++)
        {
            var (_, value, _) = ObjectGlobals[i];
            targetTestHelpers.WritePointer(pointerData.Slice(i * pointerSize), value);
        }

        fixed (byte* jsonPtr = json)
        {
            MockMemorySpace.Builder builder = new();
            builder = builder.SetDescriptor(descriptor)
                    .SetJson(json)
                    .SetPointerData(pointerData);

            builder = MethodTableTests.AddFreeObjectMethodTable(targetTestHelpers, builder);
            builder = AddStringMethodTablePointer(targetTestHelpers, builder);

            if (configure != null)
            {
                builder = configure(builder);
            }

            using MockMemorySpace.ReadContext context = builder.Create();

            bool success = MockMemorySpace.TryCreateTarget(&context, out Target? target);
            Assert.True(success);
            testCase(target);
        }

        GC.KeepAlive(json);
    }

    private static MockMemorySpace.Builder AddObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer address, TargetPointer methodTable)
    {
        MockMemorySpace.HeapFragment fragment = new() { Name = $"Object : MT = '{methodTable}'", Address = address, Data = new byte[targetTestHelpers.SizeOfTypeInfo(ObjectTypeInfo)] };
        Span<byte> dest = fragment.Data;
        targetTestHelpers.WritePointer(dest.Slice(ObjectTypeInfo.Fields["m_pMethTab"].Offset), methodTable);
        return builder.AddHeapFragment(fragment);
    }

    private static MockMemorySpace.Builder AddStringObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer address, string value)
    {
        int size = targetTestHelpers.SizeOfTypeInfo(ObjectTypeInfo) + targetTestHelpers.SizeOfTypeInfo(StringTypeInfo) + value.Length * sizeof(char);
        MockMemorySpace.HeapFragment fragment = new() { Name = $"String = '{value}'", Address = address, Data = new byte[size] };
        Span<byte> dest = fragment.Data;
        targetTestHelpers.WritePointer(dest.Slice(ObjectTypeInfo.Fields["m_pMethTab"].Offset), TestStringMethodTableAddress);
        targetTestHelpers.Write(dest.Slice(StringTypeInfo.Fields["m_StringLength"].Offset), (uint)value.Length);
        MemoryMarshal.Cast<char, byte>(value).CopyTo(dest.Slice(StringTypeInfo.Fields["m_FirstChar"].Offset));
        return builder.AddHeapFragment(fragment);
    }

    private static MockMemorySpace.Builder AddArrayObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer address, Array array)
    {
        bool isSingleDimensionZeroLowerBound = array.Rank == 1 && array.GetLowerBound(0) == 0;

        // Bounds are part of the array object for non-single dimension or non-zero lower bound arrays
        //   << fields that are part of the array type info >>
        //   int32_t bounds[rank];
        //   int32_t lowerBounds[rank];
        int size = targetTestHelpers.SizeOfTypeInfo(ObjectTypeInfo) + targetTestHelpers.SizeOfTypeInfo(ArrayTypeInfo);
        if (!isSingleDimensionZeroLowerBound)
            size += array.Rank * sizeof(int) * 2;

        ulong methodTableAddress = (address.Value + (ulong)size + (TestObjectToMethodTableUnmask - 1)) & ~(TestObjectToMethodTableUnmask - 1);
        ulong arrayClassAddress = methodTableAddress + (ulong)targetTestHelpers.SizeOfTypeInfo(MethodTableTests.RTSTypes.First(t => t.Type == DataType.MethodTable).Info);

        uint flags = (uint)(RuntimeTypeSystem_1.WFLAGS_HIGH.HasComponentSize | RuntimeTypeSystem_1.WFLAGS_HIGH.Category_Array) | (uint)array.Length;
        if (isSingleDimensionZeroLowerBound)
            flags |= (uint)RuntimeTypeSystem_1.WFLAGS_HIGH.Category_IfArrayThenSzArray;

        string name = string.Join(',', array);

        builder = MethodTableTests.AddArrayClass(targetTestHelpers, builder, arrayClassAddress, name, methodTableAddress,
            attr: 0, numMethods: 0, numNonVirtualSlots: 0, rank: (byte)array.Rank);
        builder = MethodTableTests.AddMethodTable(targetTestHelpers, builder, methodTableAddress, name, arrayClassAddress,
            mtflags: flags, mtflags2: default, baseSize: targetTestHelpers.ArrayBaseBaseSize,
            module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: 0);

        MockMemorySpace.HeapFragment fragment = new() { Name = $"Array = '{string.Join(',', array)}'", Address = address, Data = new byte[size] };
        Span<byte> dest = fragment.Data;
        targetTestHelpers.WritePointer(dest.Slice(ObjectTypeInfo.Fields["m_pMethTab"].Offset), methodTableAddress);
        targetTestHelpers.Write(dest.Slice(ArrayTypeInfo.Fields["m_NumComponents"].Offset), (uint)array.Length);
        return builder.AddHeapFragment(fragment);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnmaskMethodTableAddress(MockTarget.Architecture arch)
    {
        const ulong TestObjectAddress = 0x00000000_10000010;
        const ulong TestMethodTableAddress = 0x00000000_10000027;
        TargetTestHelpers targetTestHelpers = new(arch);
        ObjectContractHelper(arch,
            (builder) =>
            {
                builder = AddObject(targetTestHelpers, builder, TestObjectAddress, TestMethodTableAddress);
                return builder;
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                Assert.NotNull(contract);
                TargetPointer mt = contract.GetMethodTableAddress(TestObjectAddress);
                Assert.Equal(TestMethodTableAddress & ~TestObjectToMethodTableUnmask, mt.Value);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void StringValue(MockTarget.Architecture arch)
    {
        const ulong TestStringAddress = 0x00000000_10000010;
        string expected = "test_string_value";
        TargetTestHelpers targetTestHelpers = new(arch);
        ObjectContractHelper(arch,
            (builder) =>
            {
                builder = AddStringObject(targetTestHelpers, builder, TestStringAddress, expected);
                return builder;
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
            (builder) =>
            {
                builder = AddArrayObject(targetTestHelpers, builder, SingleDimensionArrayAddress, singleDimension);
                builder = AddArrayObject(targetTestHelpers, builder, MultiDimensionArrayAddress, multiDimension);
                builder = AddArrayObject(targetTestHelpers, builder, NonZeroLowerBoundArrayAddress, nonZeroLowerBound);
                return builder;
            },
            (target) =>
            {
                Contracts.IObject contract = target.Contracts.Object;
                Assert.NotNull(contract);
                {
                    TargetPointer data = contract.GetArrayData(SingleDimensionArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                    Assert.Equal(SingleDimensionArrayAddress + targetTestHelpers.ArrayBaseBaseSize - targetTestHelpers.ObjHeaderSize, data.Value);
                    Assert.Equal((uint)singleDimension.Length, count);
                    Assert.Equal(SingleDimensionArrayAddress + (ulong)ArrayTypeInfo.Fields["m_NumComponents"].Offset, boundsStart.Value);
                    Assert.Equal(TestArrayBoundsZeroGlobalAddress, lowerBounds.Value);
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
}
