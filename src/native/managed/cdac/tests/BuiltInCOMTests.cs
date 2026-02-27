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
            c => c.BuiltInCOM == ((IContractFactory<IBuiltInCOM>)new BuiltInCOMFactory()).CreateContract(target, 2)));

        testCase(target);
    }

    private static TargetPointer AddRCW(
        MockMemorySpace.Builder builder,
        TargetTestHelpers targetTestHelpers,
        Dictionary<DataType, Target.TypeInfo> types,
        MockMemorySpace.BumpAllocator allocator,
        TargetPointer interfaceEntriesAddress)
    {
        Target.TypeInfo rcwTypeInfo = types[DataType.RCW];
        MockMemorySpace.HeapFragment fragment = allocator.Allocate(rcwTypeInfo.Size!.Value, "RCW");
        Span<byte> data = fragment.Data;
        targetTestHelpers.WritePointer(
            data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.InterfaceEntries)].Offset),
            interfaceEntriesAddress);
        builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    private static TargetPointer AddInterfaceEntries(
        MockMemorySpace.Builder builder,
        TargetTestHelpers targetTestHelpers,
        Dictionary<DataType, Target.TypeInfo> types,
        MockMemorySpace.BumpAllocator allocator,
        (TargetPointer MethodTable, TargetPointer Unknown)[] entries)
    {
        Target.TypeInfo entryTypeInfo = types[DataType.InterfaceEntry];
        uint entrySize = entryTypeInfo.Size!.Value;
        uint totalSize = entrySize * TestRCWInterfaceCacheSize;
        MockMemorySpace.HeapFragment fragment = allocator.Allocate(totalSize, "InterfaceEntries");
        Span<byte> data = fragment.Data;

        for (int i = 0; i < entries.Length && i < TestRCWInterfaceCacheSize; i++)
        {
            Span<byte> entryData = data.Slice((int)(i * entrySize));
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

                TargetPointer entriesAddress = AddInterfaceEntries(builder, targetTestHelpers, types, allocator, expectedEntries);
                rcwAddress = AddRCW(builder, targetTestHelpers, types, allocator, entriesAddress);
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
    public void GetRCWInterfaces_SkipsEmptyEntries(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        // Only some entries are filled; the rest have null MT/Unknown
        (TargetPointer MethodTable, TargetPointer Unknown)[] entries =
        [
            (new TargetPointer(0x1000), new TargetPointer(0x2000)),
            (TargetPointer.Null, TargetPointer.Null),  // empty
            (new TargetPointer(0x5000), new TargetPointer(0x6000)),
        ];

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                TargetPointer entriesAddress = AddInterfaceEntries(builder, targetTestHelpers, types, allocator, entries);
                rcwAddress = AddRCW(builder, targetTestHelpers, types, allocator, entriesAddress);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
                    contract.GetRCWInterfaces(rcwAddress).ToList();

                // Only the 2 non-null entries should be returned
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
                TargetPointer entriesAddress = AddInterfaceEntries(builder, targetTestHelpers, types, allocator, []);
                rcwAddress = AddRCW(builder, targetTestHelpers, types, allocator, entriesAddress);
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
