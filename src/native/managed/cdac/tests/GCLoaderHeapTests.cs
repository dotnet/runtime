// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GCLoaderHeapTests
{
    private const ulong DefaultAllocationRangeStart = 0x0005_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0006_0000;

    private static readonly MockDescriptors.TypeFields LoaderHeapFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.LoaderHeap,
        Fields =
        [
            new(nameof(Data.LoaderHeap.FirstBlock), DataType.pointer),
        ]
    };

    private static readonly MockDescriptors.TypeFields LoaderHeapBlockFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.LoaderHeapBlock,
        Fields =
        [
            new(nameof(Data.LoaderHeapBlock.Next), DataType.pointer),
            new(nameof(Data.LoaderHeapBlock.VirtualAddress), DataType.pointer),
            new(nameof(Data.LoaderHeapBlock.VirtualSize), DataType.nuint),
        ]
    };

    private static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
    {
        return MockDescriptors.GetTypesForTypeFields(helpers, [LoaderHeapFields, LoaderHeapBlockFields]);
    }

    private static Target CreateTarget(MockTarget.Architecture arch, Dictionary<DataType, Target.TypeInfo> types, MockMemorySpace.Builder builder)
    {
        (string Name, ulong Value)[] globals =
        [
            (nameof(Constants.Globals.HandlesPerBlock), 1),
            (nameof(Constants.Globals.BlockInvalid), 0xFF),
            (nameof(Constants.Globals.DebugDestroyedHandleValue), 0),
            (nameof(Constants.Globals.HandleMaxInternalTypes), 1),
        ];
        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.GC == ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1)));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EmptyLoaderHeap(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        // Allocate a loader heap with no blocks
        Target.TypeInfo heapType = types[DataType.LoaderHeap];
        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "LoaderHeap");
        // FirstBlock is zero (null) by default
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        IGC gc = target.Contracts.GC;

        TargetPointer firstBlock = gc.GetFirstLoaderHeapBlock(heapFragment.Address);
        Assert.Equal(TargetPointer.Null, firstBlock);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SingleBlockLoaderHeap(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.LoaderHeap];
        Target.TypeInfo blockType = types[DataType.LoaderHeapBlock];

        // Allocate a single block
        ulong virtualAddress = 0x1234_0000UL;
        ulong virtualSize = 0x1000UL;
        MockMemorySpace.HeapFragment blockFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "LoaderHeapBlock");
        helpers.WritePointer(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), TargetPointer.Null.Value);
        helpers.WritePointer(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddress);
        helpers.WriteNUInt(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSize));
        builder.AddHeapFragment(blockFragment);

        // Allocate the heap pointing to the single block
        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "LoaderHeap");
        helpers.WritePointer(heapFragment.Data.AsSpan().Slice(heapType.Fields[nameof(Data.LoaderHeap.FirstBlock)].Offset, helpers.PointerSize), blockFragment.Address);
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        IGC gc = target.Contracts.GC;

        TargetPointer firstBlock = gc.GetFirstLoaderHeapBlock(heapFragment.Address);
        Assert.Equal((TargetPointer)blockFragment.Address, firstBlock);

        LoaderHeapBlockData data = gc.GetLoaderHeapBlockData(firstBlock);
        Assert.Equal(virtualAddress, data.VirtualAddress.Value);
        Assert.Equal(virtualSize, data.VirtualSize.Value);

        TargetPointer nextBlock = gc.GetNextLoaderHeapBlock(firstBlock);
        Assert.Equal(TargetPointer.Null, nextBlock);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MultipleBlockLoaderHeap(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.LoaderHeap];
        Target.TypeInfo blockType = types[DataType.LoaderHeapBlock];

        // Create two blocks
        ulong[] virtualAddresses = [0x1000_0000UL, 0x2000_0000UL];
        ulong[] virtualSizes = [0x8000UL, 0x10000UL];

        // Allocate second block (next = null)
        MockMemorySpace.HeapFragment block2Fragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "LoaderHeapBlock2");
        helpers.WritePointer(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), TargetPointer.Null.Value);
        helpers.WritePointer(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddresses[1]);
        helpers.WriteNUInt(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSizes[1]));
        builder.AddHeapFragment(block2Fragment);

        // Allocate first block (next = block2)
        MockMemorySpace.HeapFragment block1Fragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "LoaderHeapBlock1");
        helpers.WritePointer(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), block2Fragment.Address);
        helpers.WritePointer(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddresses[0]);
        helpers.WriteNUInt(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSizes[0]));
        builder.AddHeapFragment(block1Fragment);

        // Allocate the heap pointing to the first block
        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "LoaderHeap");
        helpers.WritePointer(heapFragment.Data.AsSpan().Slice(heapType.Fields[nameof(Data.LoaderHeap.FirstBlock)].Offset, helpers.PointerSize), block1Fragment.Address);
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        IGC gc = target.Contracts.GC;

        // Traverse the heap blocks
        List<(ulong Address, ulong Size)> blocks = [];
        TargetPointer block = gc.GetFirstLoaderHeapBlock(heapFragment.Address);
        while (block != TargetPointer.Null)
        {
            LoaderHeapBlockData data = gc.GetLoaderHeapBlockData(block);
            blocks.Add((data.VirtualAddress.Value, data.VirtualSize.Value));
            block = gc.GetNextLoaderHeapBlock(block);
        }

        Assert.Equal(2, blocks.Count);
        Assert.Equal((virtualAddresses[0], virtualSizes[0]), blocks[0]);
        Assert.Equal((virtualAddresses[1], virtualSizes[1]), blocks[1]);
    }
}
