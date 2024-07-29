// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public unsafe class ObjectTests
{
    const ulong TestStringMethodTableGlobalAddress = 0x00000000_100000a0;
    const ulong TestStringMethodTableAddress = 0x00000000_100000a8;

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

    private static readonly (DataType Type, Target.TypeInfo Info)[] ObjectTypes =
    [
        (DataType.Object, ObjectTypeInfo),
        (DataType.String, StringTypeInfo),
    ];

    const ulong TestObjectToMethodTableUnmask = 0x7;
    private static (string Name, ulong Value, string? Type)[] ObjectGlobals =
    [
        (nameof(Constants.Globals.ObjectToMethodTableUnmask), TestObjectToMethodTableUnmask, "uint8"),
        (nameof(Constants.Globals.StringMethodTable), TestStringMethodTableGlobalAddress, null),
    ];

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
        string typesJson = TargetTestHelpers.MakeTypesJson(ObjectTypes);
        string globalsJson = TargetTestHelpers.MakeGlobalsJson(ObjectGlobals);
        byte[] json = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {
                "{{nameof(Contracts.Object)}}": 1
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
}
