// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class BuiltInCOMTests
{
    private const ulong AllocationRangeStart = 0x00000000_20000000;
    private const ulong AllocationRangeEnd   = 0x00000000_30000000;

    private const uint TestRCWInterfaceCacheSize = 8;

    private static readonly MockDescriptors.TypeFields RCWFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.RCW,
        Fields =
        [
            new(nameof(Data.RCW.InterfaceEntries), DataType.pointer),
        ]
    };

    private static readonly MockDescriptors.TypeFields InterfaceEntryFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.InterfaceEntry,
        Fields =
        [
            new(nameof(Data.InterfaceEntry.MethodTable), DataType.pointer),
            new(nameof(Data.InterfaceEntry.Unknown), DataType.pointer),
        ]
    };

    private static void BuiltInCOMContractHelper(
        MockTarget.Architecture arch,
        Action<MockMemorySpace.Builder, TargetTestHelpers, Dictionary<DataType, Target.TypeInfo>> configure,
        Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        MockMemorySpace.Builder builder = new(targetTestHelpers);

        Dictionary<DataType, Target.TypeInfo> types = MockDescriptors.GetTypesForTypeFields(
            targetTestHelpers,
            [RCWFields, InterfaceEntryFields]);

        configure(builder, targetTestHelpers, types);

        (string Name, ulong Value)[] globals =
        [
            (nameof(Constants.Globals.RCWInterfaceCacheSize), TestRCWInterfaceCacheSize),
        ];

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.BuiltInCOM == ((IContractFactory<IBuiltInCOM>)new BuiltInCOMFactory()).CreateContract(target, 1)));

        testCase(target);
    }

    /// <summary>
    /// Allocates an RCW mock with the interface entries embedded inline (matching the real C++ layout
    /// where m_aInterfaceEntries is an inline array within the RCW struct).
    /// Returns the address of the RCW.
    /// </summary>
    private static TargetPointer AddRCWWithInlineEntries(
        MockMemorySpace.Builder builder,
        TargetTestHelpers targetTestHelpers,
        Dictionary<DataType, Target.TypeInfo> types,
        MockMemorySpace.BumpAllocator allocator,
        (TargetPointer MethodTable, TargetPointer Unknown)[] entries)
    {
        Target.TypeInfo rcwTypeInfo = types[DataType.RCW];
        Target.TypeInfo entryTypeInfo = types[DataType.InterfaceEntry];
        uint entrySize = entryTypeInfo.Size!.Value;
        uint entriesOffset = (uint)rcwTypeInfo.Fields[nameof(Data.RCW.InterfaceEntries)].Offset;

        // The RCW block must be large enough to hold the RCW header plus all inline entries
        uint totalSize = entriesOffset + entrySize * TestRCWInterfaceCacheSize;
        MockMemorySpace.HeapFragment fragment = allocator.Allocate(totalSize, "RCW with inline entries");
        Span<byte> data = fragment.Data;

        // Write the inline interface entries starting at entriesOffset
        for (int i = 0; i < entries.Length && i < TestRCWInterfaceCacheSize; i++)
        {
            Span<byte> entryData = data.Slice((int)(entriesOffset + i * entrySize));
            targetTestHelpers.WritePointer(
                entryData.Slice(entryTypeInfo.Fields[nameof(Data.InterfaceEntry.MethodTable)].Offset),
                entries[i].MethodTable);
            targetTestHelpers.WritePointer(
                entryData.Slice(entryTypeInfo.Fields[nameof(Data.InterfaceEntry.Unknown)].Offset),
                entries[i].Unknown);
        }

        builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWInterfaces_ReturnsFilledEntries(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        (TargetPointer MethodTable, TargetPointer Unknown)[] expectedEntries =
        [
            (new TargetPointer(0x1000), new TargetPointer(0x2000)),
            (new TargetPointer(0x3000), new TargetPointer(0x4000)),
        ];

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddRCWWithInlineEntries(builder, targetTestHelpers, types, allocator, expectedEntries);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                Assert.NotNull(contract);

                List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
                    contract.GetRCWInterfaces(rcwAddress).ToList();

                Assert.Equal(expectedEntries.Length, results.Count);
                for (int i = 0; i < expectedEntries.Length; i++)
                {
                    Assert.Equal(expectedEntries[i].MethodTable, results[i].MethodTable);
                    Assert.Equal(expectedEntries[i].Unknown, results[i].Unknown);
                }
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWInterfaces_SkipsEntriesWithNullUnknown(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        // The IsFree() check uses only Unknown == null; entries with Unknown == null are skipped.
        (TargetPointer MethodTable, TargetPointer Unknown)[] entries =
        [
            (new TargetPointer(0x1000), new TargetPointer(0x2000)),
            (TargetPointer.Null, TargetPointer.Null),  // free entry (Unknown == null)
            (new TargetPointer(0x5000), new TargetPointer(0x6000)),
        ];

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddRCWWithInlineEntries(builder, targetTestHelpers, types, allocator, entries);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
                    contract.GetRCWInterfaces(rcwAddress).ToList();

                // Only the 2 entries with non-null Unknown are returned
                Assert.Equal(2, results.Count);
                Assert.Equal(new TargetPointer(0x1000), results[0].MethodTable);
                Assert.Equal(new TargetPointer(0x2000), results[0].Unknown);
                Assert.Equal(new TargetPointer(0x5000), results[1].MethodTable);
                Assert.Equal(new TargetPointer(0x6000), results[1].Unknown);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWInterfaces_EmptyCache_ReturnsEmpty(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddRCWWithInlineEntries(builder, targetTestHelpers, types, allocator, []);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
                    contract.GetRCWInterfaces(rcwAddress).ToList();

                Assert.Empty(results);
            });
    }
}
