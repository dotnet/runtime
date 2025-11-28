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
    const uint SubDescriptorPointerDataAddr = 0x12545678;

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
    public void SubDescriptor_TypesAndGlobals(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        ContractDescriptorBuilder.DescriptorBuilder subDescriptor = new(builder);

        subDescriptor.SetTypes(SubDescriptorTypes)
            .SetGlobals(SubDescriptorGlobals)
            .SetContracts([]);
        subDescriptor.CreateSubDescriptor(SubDescriptorAddr, SubDescriptorJsonAddr, SubDescriptorPointerDataAddr);

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
            .SetGlobals([.. TestGlobals.Select(GlobalToIndirectFormat)])
            .SetSubDescriptors([("GC", 1u)])
            .SetIndirectValues([0, subDescriptorPointerAddr])
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

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SubDescriptor_Multiple_Nested(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        uint subDescriptorAddr = 0x4004_0000;
        uint subDescriptorJsonAddr = 0x4104_0000;
        uint subDescriptorPointerDataAddr = 0x4204_0000;
        uint subDescriptorPointerAddr = 0x4304_0000;

        const int START_DEPTH = 4;

        Dictionary<string, string> expectedGlobals = [];

        for (int depth = START_DEPTH; depth >= 0; depth--)
        {
            ContractDescriptorBuilder.DescriptorBuilder subDescriptor = new(builder);

            if (depth != START_DEPTH)
            {
                subDescriptor
                    .SetSubDescriptors([($"SubDescriptorDepth{depth + 1}", 1u)])
                    .SetIndirectValues([0, subDescriptorPointerAddr]);

                subDescriptorAddr += 0x1000;
                subDescriptorJsonAddr += 0x1000;
                subDescriptorPointerDataAddr += 0x1000;
                subDescriptorPointerAddr += 0x1000;
            }

            string globalName = $"SubDescriptorDepth{depth}";
            expectedGlobals.Add(globalName, globalName);
            subDescriptor
                .SetGlobals([(globalName, null, globalName, null)])
                .CreateSubDescriptor(subDescriptorAddr, subDescriptorJsonAddr, subDescriptorPointerDataAddr);

            byte[] pointerDataBytes = new byte[targetTestHelpers.PointerSize];
            targetTestHelpers.WritePointer(pointerDataBytes, subDescriptorAddr);
            MockMemorySpace.HeapFragment pointerData = new()
            {
                Address = subDescriptorPointerAddr,
                Data = pointerDataBytes,
                Name = $"SubDescriptorPointerData_Depth{depth}"
            };
            builder.AddHeapFragment(pointerData);
        }


        ContractDescriptorBuilder.DescriptorBuilder primaryDescriptor = new(builder);
        primaryDescriptor.SetTypes(TestTypes)
            .SetSubDescriptors([("SubDescriptorDepth0", 1u)])
            .SetIndirectValues([0, subDescriptorPointerAddr]);

        bool success = builder.TryCreateTarget(primaryDescriptor, out ContractDescriptorTarget? target);
        Assert.True(success);

        foreach ((string globalName, string expectedValue) in expectedGlobals)
        {
            Assert.True(target.TryReadGlobalString(globalName, out string? globalStringValue));
            Assert.Equal(expectedValue, globalStringValue);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SubDescriptor_Multiple_Breadth(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        uint subDescriptorAddr = 0x4004_0000;
        uint subDescriptorJsonAddr = 0x4104_0000;
        uint subDescriptorPointerDataAddr = 0x4204_0000;
        uint subDescriptorPointerAddr = 0x4304_0000;

        Dictionary<string, string> expectedGlobals = [];

        List<(string Name, uint IndirectIndex)> subDescriptors = [];
        List<ulong> indirectValues = [0];

        for (int i = 1; i < 5; i++)
        {
            ContractDescriptorBuilder.DescriptorBuilder subDescriptor = new(builder);

            string globalName = $"SubDescriptor_Global_{i}";
            expectedGlobals.Add(globalName, globalName);
            subDescriptor
                .SetGlobals([(globalName, null, globalName, null)])
                .CreateSubDescriptor(subDescriptorAddr, subDescriptorJsonAddr, subDescriptorPointerDataAddr);

            byte[] pointerDataBytes = new byte[targetTestHelpers.PointerSize];
            targetTestHelpers.WritePointer(pointerDataBytes, subDescriptorAddr);
            MockMemorySpace.HeapFragment pointerData = new()
            {
                Address = subDescriptorPointerAddr,
                Data = pointerDataBytes,
                Name = $"SubDescriptorPointerData_{i}"
            };
            builder.AddHeapFragment(pointerData);

            subDescriptors.Add(($"SubDescriptor{i}", (uint)indirectValues.Count));
            indirectValues.Add(subDescriptorPointerAddr);

            subDescriptorAddr += 0x1000;
            subDescriptorJsonAddr += 0x1000;
            subDescriptorPointerDataAddr += 0x1000;
            subDescriptorPointerAddr += 0x1000;
        }


        ContractDescriptorBuilder.DescriptorBuilder primaryDescriptor = new(builder);
        primaryDescriptor.SetTypes(TestTypes)
            .SetSubDescriptors(subDescriptors)
            .SetIndirectValues(indirectValues);

        bool success = builder.TryCreateTarget(primaryDescriptor, out ContractDescriptorTarget? target);
        Assert.True(success);

        foreach ((string globalName, string expectedValue) in expectedGlobals)
        {
            Assert.True(target.TryReadGlobalString(globalName, out string? globalStringValue));
            Assert.Equal(expectedValue, globalStringValue);
        }
    }
}
