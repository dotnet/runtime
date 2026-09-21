// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure.ContractDescriptor;
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
                { "Field1", new(){ Offset = 8, TypeName = DataType.uint16.ToString() }},
                { "Field2", new(){ Offset = 16, TypeName = DataType.ObjectHandle.ToString() }},
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
    public void SubDescriptor_UnsupportedDataDescriptorVersion_ThrowsFormatException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        ContractDescriptorBuilder.DescriptorBuilder subDescriptor = new(builder);
        subDescriptor.SetVersion(1);
        subDescriptor.CreateSubDescriptor(SubDescriptorAddr, SubDescriptorJsonAddr, SubDescriptorPointerDataAddr);

        uint subDescriptorPointerAddr = 0x12465312;
        byte[] pointerDataBytes = new byte[targetTestHelpers.PointerSize];
        targetTestHelpers.WritePointer(pointerDataBytes, SubDescriptorAddr);
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = subDescriptorPointerAddr,
            Data = pointerDataBytes,
            Name = "SubDescriptorPointerData"
        });

        ContractDescriptorBuilder.DescriptorBuilder primaryDescriptor = new(builder);
        primaryDescriptor
            .SetSubDescriptors([("GC", 1u)])
            .SetIndirectValues([0, subDescriptorPointerAddr]);

        FormatException ex = Assert.Throws<FormatException>(() => builder.CreateTarget(primaryDescriptor));
        Assert.Equal(CdacHResults.CDAC_E_DESCRIPTOR_MALFORMED, ex.HResult);
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

    // Builds a target whose primary descriptor advertises the given contracts and references a GC
    // sub-descriptor whose pointer slot still reads null. This models early attach: the GC has not
    // yet published its sub-descriptor address (dotnet/runtime#128215), so the slot stays pending and
    // IsSubDescriptorResolved("GC") is false.
    private static ContractDescriptorTarget CreatePendingGCSubDescriptorTarget(
        MockTarget.Architecture arch, IReadOnlyDictionary<string, string> contracts)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        // A pointer slot that reads null models a sub-descriptor the target has not published yet.
        uint pendingPointerAddr = 0x12465312;
        byte[] nullPointerBytes = new byte[targetTestHelpers.PointerSize];
        targetTestHelpers.WritePointer(nullPointerBytes, 0);
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = pendingPointerAddr,
            Data = nullPointerBytes,
            Name = "PendingGCSubDescriptorPointer"
        });

        ContractDescriptorBuilder.DescriptorBuilder primaryDescriptor = new(builder);
        primaryDescriptor
            .SetSubDescriptors([("GC", 1u)])
            .SetIndirectValues([0, pendingPointerAddr])
            .SetContracts(new Dictionary<string, string>(contracts));

        Assert.True(builder.TryCreateTarget(primaryDescriptor, out ContractDescriptorTarget? target));
        Assert.False(target.IsSubDescriptorResolved("GC"));
        return target;
    }

    // The in-box (main-descriptor) contracts required for data access, minus the sub-descriptor-only
    // IGC, each advertised at the version CoreCLRContracts registers.
    private static Dictionary<string, string> RequiredContractsWithoutGC()
        => s_requiredDataAccessContracts
            .Where(static pair => pair.Key != "GC")
            .ToDictionary(static pair => pair.Key, static pair => pair.Value);

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_PendingGCSubDescriptor_MissingGC_DoesNotThrow(MockTarget.Architecture arch)
    {
        // Every in-box contract is present, IGC is not advertised, and its GC sub-descriptor is still
        // pending. IGC's absence is deferred (the GC may publish it after a later Flush), so a target
        // attached this early must not be rejected.
        ContractDescriptorTarget target = CreatePendingGCSubDescriptorTarget(arch, RequiredContractsWithoutGC());

        Contracts.CoreCLRContracts.ValidateForDataAccess(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_PendingGCSubDescriptor_MissingInBoxContract_Throws(MockTarget.Architecture arch)
    {
        // A pending GC sub-descriptor only defers IGC. An in-box contract (Loader) is still required
        // unconditionally, so its absence rejects the target even during early attach.
        Dictionary<string, string> contracts = RequiredContractsWithoutGC();
        contracts.Remove("Loader");
        ContractDescriptorTarget target = CreatePendingGCSubDescriptorTarget(arch, contracts);

        ContractMissingException ex = Assert.Throws<ContractMissingException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal("Loader", ex.ContractName);
    }

    // Builds a target whose GC sub-descriptor is fully published (resolved): the primary descriptor's
    // sub-descriptor pointer slot reads a real address, so BuildDescriptors parses the sub-descriptor
    // and IsSubDescriptorResolved("GC") is true. The main descriptor advertises mainContracts; the GC
    // sub-descriptor advertises gcSubContracts (where IGC lives in a real runtime).
    private static ContractDescriptorTarget CreateResolvedGCSubDescriptorTarget(
        MockTarget.Architecture arch,
        IReadOnlyDictionary<string, string> mainContracts,
        IReadOnlyDictionary<string, string> gcSubContracts)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        uint gcSubDescriptorAddr = 0x12345678;
        uint gcSubDescriptorJsonAddr = 0x12445678;
        uint gcSubDescriptorPointerDataAddr = 0x12545678;
        uint gcSubDescriptorPointerAddr = 0x12465312;

        ContractDescriptorBuilder.DescriptorBuilder gcSubDescriptor = new(builder);
        gcSubDescriptor
            .SetGlobals(SubDescriptorGlobals)
            .SetContracts(new Dictionary<string, string>(gcSubContracts));
        gcSubDescriptor.CreateSubDescriptor(gcSubDescriptorAddr, gcSubDescriptorJsonAddr, gcSubDescriptorPointerDataAddr);

        byte[] pointerDataBytes = new byte[targetTestHelpers.PointerSize];
        targetTestHelpers.WritePointer(pointerDataBytes, gcSubDescriptorAddr);
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = gcSubDescriptorPointerAddr,
            Data = pointerDataBytes,
            Name = "ResolvedGCSubDescriptorPointer"
        });

        ContractDescriptorBuilder.DescriptorBuilder primaryDescriptor = new(builder);
        primaryDescriptor
            .SetSubDescriptors([("GC", 1u)])
            .SetIndirectValues([0, gcSubDescriptorPointerAddr])
            .SetContracts(new Dictionary<string, string>(mainContracts));

        Assert.True(builder.TryCreateTarget(primaryDescriptor, out ContractDescriptorTarget? target));
        Assert.True(target.IsSubDescriptorResolved("GC"));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_ResolvedGCSubDescriptor_ValidGC_DoesNotThrow(MockTarget.Architecture arch)
    {
        // The GC sub-descriptor is published and advertises IGC at a supported version, so the target
        // is fully serviceable.
        ContractDescriptorTarget target = CreateResolvedGCSubDescriptorTarget(
            arch, RequiredContractsWithoutGC(), new Dictionary<string, string> { ["GC"] = "c1" });

        Contracts.CoreCLRContracts.ValidateForDataAccess(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_ResolvedGCSubDescriptor_MissingGC_Throws(MockTarget.Architecture arch)
    {
        // The GC sub-descriptor is published but does not advertise IGC. Deferral no longer applies
        // once the provider has resolved, so the absent IGC now rejects the target.
        ContractDescriptorTarget target = CreateResolvedGCSubDescriptorTarget(
            arch, RequiredContractsWithoutGC(), new Dictionary<string, string>());

        ContractMissingException ex = Assert.Throws<ContractMissingException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal("GC", ex.ContractName);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_ResolvedGCSubDescriptor_UnrecognizedGCVersion_Throws(MockTarget.Architecture arch)
    {
        // The GC sub-descriptor is published and advertises IGC at a version this cDAC cannot service.
        // A version that has actually been read is always a failure.
        ContractDescriptorTarget target = CreateResolvedGCSubDescriptorTarget(
            arch, RequiredContractsWithoutGC(), new Dictionary<string, string> { ["GC"] = "c99" });

        ContractUnrecognizedException ex = Assert.Throws<ContractUnrecognizedException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal("GC", ex.ContractName);
    }
}
