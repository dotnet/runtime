// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Data;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public unsafe class MethodTableTests
{
    const ulong TestFreeObjectMethodTableGlobalAddress = 0x00000000_7a0000a0;
    const ulong TestFreeObjectMethodTableAddress = 0x00000000_7a0000a8;

    private static readonly Target.TypeInfo MethodTableTypeInfo = new()
    {
        Fields = {
            { nameof(Data.MethodTable.MTFlags), new() { Offset = 4, Type = DataType.uint32}},
            { nameof(Data.MethodTable.BaseSize), new() { Offset = 8, Type = DataType.uint32}},
            { nameof(Data.MethodTable.MTFlags2), new() { Offset = 12, Type = DataType.uint32}},
            { nameof(Data.MethodTable.EEClassOrCanonMT), new () { Offset = 16, Type = DataType.nuint}},
            { nameof(Data.MethodTable.Module), new () { Offset = 24, Type = DataType.pointer}},
            { nameof(Data.MethodTable.ParentMethodTable), new () { Offset = 40, Type = DataType.pointer}},
            { nameof(Data.MethodTable.NumInterfaces), new () { Offset = 48, Type = DataType.uint16}},
            { nameof(Data.MethodTable.NumVirtuals), new () { Offset = 50, Type = DataType.uint16}},
        }
    };

    private static readonly Target.TypeInfo EEClassTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof (Data.EEClass.MethodTable), new () { Offset = 8, Type = DataType.pointer}},
            { nameof (Data.EEClass.AttrClass), new () { Offset = 16, Type = DataType.uint32}},
            { nameof (Data.EEClass.NumMethods), new () { Offset = 20, Type = DataType.uint16}},
        }
    };

    private static readonly (DataType Type, Target.TypeInfo Info)[] MetadataTypes =
    [
        (DataType.MethodTable, MethodTableTypeInfo),
        (DataType.EEClass, EEClassTypeInfo),
    ];


    private static readonly (string Name, ulong Value, string? Type)[] MetadataGlobals =
    [
        (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress, null),
    ];

    private static MockMemorySpace.Builder AddFreeObjectMethodTable(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
    {
        MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
        targetTestHelpers.WritePointer(globalAddr.Data, TestFreeObjectMethodTableAddress);
        return builder.AddHeapFragments([
            globalAddr,
            new () { Name = "Free Object Method Table", Address = TestFreeObjectMethodTableAddress, Data = new byte[targetTestHelpers.SizeOfTypeInfo(MethodTableTypeInfo)] }
        ]);
    }

#if false
    private static TargetTestHelpers.ContextBuilder AddEEClass(MockTarget.Architecture arch, TargetTestHelpers.ContextBuilder builder, TargetPointer eeClassPtr, TargetPointer canonMTPtr, uint attr, ushort numMethods)
    {
        TargetTestHelpers.HeapFragment eeClassFragment = new() { Name = "EEClass", Address = eeClassPtr, Data = new byte[TargetTestHelpers.SizeOfTypeInfo(arch.Is64Bit, EEClassTypeInfo)] };
        Span<byte> dest = eeClassFragment.Data;
        TargetTestHelpers.WritePointer(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr, arch.IsLittleEndian, arch.Is64Bit ? 8 : 4);
        return builder.AddHeapFragment(eeClassFragment);
    }
#endif

    // a delegate for adding more heap fragments to the context builder
    private delegate MockMemorySpace.Builder ConfigureContextBuilder(MockTarget.Architecture arch, MockMemorySpace.Builder builder);

    private static void MetadataContractHelper(MockTarget.Architecture arch, ConfigureContextBuilder configure, Action<MockTarget.Architecture, Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        string metadataTypesJson = TargetTestHelpers.MakeTypesJson(MetadataTypes);
        string metadataGlobalsJson = TargetTestHelpers.MakeGlobalsJson(MetadataGlobals);
        byte[] json = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {
                "Metadata": 1
            },
            "types": { {{metadataTypesJson}} },
            "globals": { {{metadataGlobalsJson}} }
        }
        """);
        Span<byte> descriptor = stackalloc byte[targetTestHelpers.ContractDescriptorSize];
        targetTestHelpers.ContractDescriptorFill(descriptor, json.Length, MetadataGlobals.Length);

        int pointerSize = targetTestHelpers.PointerSize;
        Span<byte> pointerData = stackalloc byte[MetadataGlobals.Length * pointerSize];
        for (int i = 0; i < MetadataGlobals.Length; i++)
        {
            var (_, value, _) = MetadataGlobals[i];
            targetTestHelpers.WritePointer(pointerData.Slice(i * pointerSize), value);
        }

        fixed (byte* jsonPtr = json)
        {
            MockMemorySpace.Builder builder = new();

            builder = builder.SetDescriptor(descriptor)
                    .SetJson(json)
                    .SetPointerData(pointerData);

            builder = AddFreeObjectMethodTable(targetTestHelpers, builder);

            if (configure != null)
            {
                builder = configure(arch, builder);
            }

            using MockMemorySpace.ReadContext context = builder.Create();

            bool success = MockMemorySpace.TryCreateTarget(&context, out Target? target);
            Assert.True(success);

            testCase(arch, target);
        }
        GC.KeepAlive(json);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void HasMetadataContract(MockTarget.Architecture arch)
    {
        MetadataContractHelper(arch, default, static (arch, target) =>
        {
            Contracts.IMetadata metadataContract = target.Contracts.Metadata;
            Assert.NotNull(metadataContract);
            Contracts.MethodTableHandle handle = metadataContract.GetMethodTableHandle(TestFreeObjectMethodTableAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(metadataContract.IsFreeObjectMethodTable(handle));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateSystemObjectMethodTable(MockTarget.Architecture arch)
    {
        const ulong SystemObjectMethodTableAddress = 0x00000000_7c000010;
        const ulong SystemObjectEEClassAddress = 0x00000000_7c0000d0;
        TargetPointer systemObjectMethodTablePtr = new TargetPointer(SystemObjectMethodTableAddress);
        TargetPointer systemObjectEEClassPtr = new TargetPointer(SystemObjectEEClassAddress);
        MetadataContractHelper(arch,
        (arch, builder) =>
        {
#if false
            builder = AddEEClass(arch, builder, systemObjectEEClassPtr, systemObjectMethodTablePtr, default, default);
            builder = AddMethodTable(builder, systemObjectMethodTablePtr, default);
#endif
            return builder;
        },
        static (arch, target) =>
        {
            Contracts.IMetadata metadataContract = target.Contracts.Metadata;
            Assert.NotNull(metadataContract);

        });
    }
}
