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
    struct TargetConfig
    {
        public bool IsLittleEndian { get; init; }
        public bool Is64Bit { get; init; }
    }
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

    private static readonly (DataType Type, Target.TypeInfo Info)[] MetadataTypes =
       [
        (DataType.MethodTable, MethodTableTypeInfo),
        (DataType.EEClass, new(){
            Fields = {
                { nameof (Data.EEClass.MethodTable), new () { Offset = 8, Type = DataType.pointer}},
                { nameof (Data.EEClass.AttrClass), new () { Offset = 16, Type = DataType.uint32}},
                { nameof (Data.EEClass.NumMethods), new () { Offset = 20, Type = DataType.uint16}},
            }}),
        ];


    private static readonly (string Name, ulong Value, string? Type)[] MetadataGlobals =
    [
        (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress, null),
    ];

    private static TargetTestHelpers.HeapFragment[] FreeObjectMethodTableHeapFragments(TargetConfig targetConfig)
    {
        var globalAddr = new TargetTestHelpers.HeapFragment { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetConfig.Is64Bit ? 8 : 4] };
        TargetTestHelpers.WritePointer(globalAddr.Data, TestFreeObjectMethodTableAddress, targetConfig.IsLittleEndian, targetConfig.Is64Bit ? 8 : 4);
        return [
            globalAddr,
            new TargetTestHelpers.HeapFragment { Name = "Free Object Method Table", Address = TestFreeObjectMethodTableAddress, Data = new byte[TargetTestHelpers.SizeOfTypeInfo(targetConfig.Is64Bit, MethodTableTypeInfo)] }
        ];
    }

    private static void MetadataContractHelper(TargetConfig targetConfig, Action<TargetConfig, Target> testCase)
    {
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
        Span<byte> descriptor = stackalloc byte[TargetTestHelpers.ContractDescriptor.Size(targetConfig.Is64Bit)];
        TargetTestHelpers.ContractDescriptor.Fill(descriptor, targetConfig.IsLittleEndian, targetConfig.Is64Bit, json.Length, MetadataGlobals.Length);

        int pointerSize = targetConfig.Is64Bit ? sizeof(ulong) : sizeof(uint);
        Span<byte> pointerData = stackalloc byte[MetadataGlobals.Length * pointerSize];
        for (int i = 0; i < MetadataGlobals.Length; i++)
        {
            var (_, value, _) = MetadataGlobals[i];
            TargetTestHelpers.WritePointer(pointerData.Slice(i * pointerSize), value, targetConfig.IsLittleEndian, pointerSize);
        }

        fixed (byte* jsonPtr = json)
        {
            TargetTestHelpers.ContextBuilder builder = new();

            builder = builder.SetDescriptor(descriptor)
                    .SetJson(json)
                    .SetPointerData(pointerData)
                    .AddHeapFragments(FreeObjectMethodTableHeapFragments(targetConfig));

            using TargetTestHelpers.ReadContext context = builder.Create();

            bool success = TargetTestHelpers.TryCreateTarget(&context, out Target? target);
            Assert.True(success);

            testCase(targetConfig, target);
        }
        GC.KeepAlive(json);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void HasMetadataContract(bool isLittleEndian, bool is64Bit)
    {
        TargetConfig targetConfig = new TargetConfig { IsLittleEndian = isLittleEndian, Is64Bit = is64Bit };
        MetadataContractHelper(targetConfig, static (arch, target) =>
        {
            Contracts.IMetadata metadataContract = target.Contracts.Metadata;
            Assert.NotNull(metadataContract);
            Contracts.MethodTableHandle handle = metadataContract.GetMethodTableHandle(TestFreeObjectMethodTableAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(metadataContract.IsFreeObjectMethodTable(handle));
        });
    }
}
