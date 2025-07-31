// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

public unsafe partial class TargetTests
{
    const uint SubDescriptorAddr = 0x12345678;
    const uint SubDescriptorJsonAddr = 0x12445678;
    const uint SubDescriptorPointerAddr = 0x12545678;

    private static readonly Dictionary<DataType, Target.TypeInfo> SubDescriptorTypes = new()
    {
        // Size and fields
        [DataType.AppDomain] = new()
        {
            Size = 56,
            Fields = new Dictionary<string, Target.FieldInfo> {
                { "Field1", new(){ Offset = 8, Type = DataType.uint16, TypeName = DataType.uint16.ToString() }},
                { "Field2", new(){ Offset = 16, Type = DataType.GCHandle, TypeName = DataType.GCHandle.ToString() }},
                { "Field3", new(){ Offset = 32 }}
            }
        },
        // Fields only
        [DataType.SystemDomain] = new()
        {
            Fields = new Dictionary<string, Target.FieldInfo> {
                { "Field1", new(){ Offset = 0, TypeName = "FieldType" }},
                { "Field2", new(){ Offset = 8 }}
            }
        },
        // Size only
        [DataType.ArrayClass] = new()
        {
            Size = 8
        }
    };

    private static readonly (string Name, ulong Value, string? Type)[] SubDescriptorGlobals =
    [
        ("subValue", (ulong)sbyte.MaxValue, null),
        ("subInt8Value", 0x13, "int8"),
        ("subUInt8Value", 0x13, "uint8"),
        ("subInt16Value", 0x1235, "int16"),
        ("subUInt16Value", 0x1235, "uint16"),
        ("subInt32Value", 0x12345679, "int32"),
        ("subUInt32Value", 0x12345679, "uint32"),
        ("subInt64Value", 0x123456789abcdef1, "int64"),
        ("subUInt64Value", 0x123456789abcdef1, "uint64"),
        ("subNintValue", 0xabcdef1, "nint"),
        ("subNuintValue", 0xabcdef1, "nuint"),
        ("subPointerValue", 0xabcdef1, "pointer"),
    ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ParseSubDescriptorProperly(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        ContractDescriptorBuilder.DescriptorBuilder subDescriptor = new(builder);

        subDescriptor.SetTypes(SubDescriptorTypes)
            .SetGlobals(SubDescriptorGlobals)
            .SetContracts([]);
        subDescriptor.CreateSubDescriptor(SubDescriptorAddr, SubDescriptorJsonAddr, SubDescriptorPointerAddr);

        uint subDescriptorPointerAddr = 0x12465312;
        byte[] pointerDataBytes = new byte[targetTestHelpers.PointerSize];
        targetTestHelpers.WritePointer(pointerDataBytes, SubDescriptorAddr);
        MockMemorySpace.HeapFragment pointerData = new()
        {
            Address = subDescriptorPointerAddr,
            Data = pointerDataBytes,
            Name = "SubDescriptorPointerData"
        };
        builder.AddHeapFragment(pointerData);

        ContractDescriptorBuilder.DescriptorBuilder primaryDescriptor = new(builder);
        primaryDescriptor.SetTypes(TestTypes)
            .SetGlobals([..TestGlobals.Select(GlobalToIndirectFormat), ("GCDescriptor", null, 1, null, null)], [0, subDescriptorPointerAddr])
            .SetContracts([]);

        bool success = builder.TryCreateTarget(primaryDescriptor, out ContractDescriptorTarget? target);
        Assert.True(success);

        ValidateTypes(target, TestTypes);
        ValidateTypes(target, SubDescriptorTypes);

        ValidateGlobals(target, TestGlobals);
        ValidateGlobals(target, SubDescriptorGlobals);

        static (string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? Type) GlobalToIndirectFormat((string Name, ulong Value, string? Type) global)
        {
            return (global.Name, global.Value, null, null, global.Type);
        }
    }
}
