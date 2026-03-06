// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class LoaderHeapTests
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

    private static readonly MockDescriptors.TypeFields ExplicitControlLoaderHeapFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.ExplicitControlLoaderHeap,
        Fields =
        [
            new(nameof(Data.ExplicitControlLoaderHeap.FirstBlock), DataType.pointer),
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
        return MockDescriptors.GetTypesForTypeFields(helpers, [LoaderHeapFields, ExplicitControlLoaderHeapFields, LoaderHeapBlockFields]);
    }

    private static Target CreateTarget(MockTarget.Architecture arch, Dictionary<DataType, Target.TypeInfo> types, MockMemorySpace.Builder builder)
    {
        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EmptyLoaderHeap_Normal(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.LoaderHeap];
        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "LoaderHeap");
        // FirstBlock is zero (null) by default
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        ILoader loader = target.Contracts.Loader;

        TargetPointer firstBlock = loader.GetFirstLoaderHeapBlock(heapFragment.Address, LoaderHeapKind.Normal);
        Assert.Equal(TargetPointer.Null, firstBlock);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EmptyLoaderHeap_ExplicitControl(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.ExplicitControlLoaderHeap];
        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "ExplicitControlLoaderHeap");
        // FirstBlock is zero (null) by default
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        ILoader loader = target.Contracts.Loader;

        TargetPointer firstBlock = loader.GetFirstLoaderHeapBlock(heapFragment.Address, LoaderHeapKind.ExplicitControl);
        Assert.Equal(TargetPointer.Null, firstBlock);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnknownKind_ThrowsNotImplemented(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.LoaderHeap];
        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "LoaderHeap");
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        ILoader loader = target.Contracts.Loader;

        Assert.Throws<NotImplementedException>(() => loader.GetFirstLoaderHeapBlock(heapFragment.Address, (LoaderHeapKind)99));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SingleBlockLoaderHeap_Normal(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.LoaderHeap];
        Target.TypeInfo blockType = types[DataType.LoaderHeapBlock];

        ulong virtualAddress = 0x1234_0000UL;
        ulong virtualSize = 0x1000UL;
        MockMemorySpace.HeapFragment blockFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "LoaderHeapBlock");
        helpers.WritePointer(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), TargetPointer.Null.Value);
        helpers.WritePointer(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddress);
        helpers.WriteNUInt(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSize));
        builder.AddHeapFragment(blockFragment);

        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "LoaderHeap");
        helpers.WritePointer(heapFragment.Data.AsSpan().Slice(heapType.Fields[nameof(Data.LoaderHeap.FirstBlock)].Offset, helpers.PointerSize), blockFragment.Address);
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        ILoader loader = target.Contracts.Loader;

        TargetPointer firstBlock = loader.GetFirstLoaderHeapBlock(heapFragment.Address, LoaderHeapKind.Normal);
        Assert.Equal((TargetPointer)blockFragment.Address, firstBlock);

        Assert.Equal(virtualAddress, loader.GetLoaderHeapBlockAddress(firstBlock).Value);
        Assert.Equal(virtualSize, loader.GetLoaderHeapBlockSize(firstBlock).Value);

        TargetPointer nextBlock = loader.GetNextLoaderHeapBlock(firstBlock);
        Assert.Equal(TargetPointer.Null, nextBlock);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SingleBlockLoaderHeap_ExplicitControl(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.ExplicitControlLoaderHeap];
        Target.TypeInfo blockType = types[DataType.LoaderHeapBlock];

        ulong virtualAddress = 0x5678_0000UL;
        ulong virtualSize = 0x2000UL;
        MockMemorySpace.HeapFragment blockFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "LoaderHeapBlock");
        helpers.WritePointer(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), TargetPointer.Null.Value);
        helpers.WritePointer(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddress);
        helpers.WriteNUInt(blockFragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSize));
        builder.AddHeapFragment(blockFragment);

        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "ExplicitControlLoaderHeap");
        helpers.WritePointer(heapFragment.Data.AsSpan().Slice(heapType.Fields[nameof(Data.ExplicitControlLoaderHeap.FirstBlock)].Offset, helpers.PointerSize), blockFragment.Address);
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        ILoader loader = target.Contracts.Loader;

        TargetPointer firstBlock = loader.GetFirstLoaderHeapBlock(heapFragment.Address, LoaderHeapKind.ExplicitControl);
        Assert.Equal((TargetPointer)blockFragment.Address, firstBlock);

        Assert.Equal(virtualAddress, loader.GetLoaderHeapBlockAddress(firstBlock).Value);
        Assert.Equal(virtualSize, loader.GetLoaderHeapBlockSize(firstBlock).Value);

        TargetPointer nextBlock = loader.GetNextLoaderHeapBlock(firstBlock);
        Assert.Equal(TargetPointer.Null, nextBlock);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MultipleBlockLoaderHeap_Normal(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.LoaderHeap];
        Target.TypeInfo blockType = types[DataType.LoaderHeapBlock];

        ulong[] virtualAddresses = [0x1000_0000UL, 0x2000_0000UL];
        ulong[] virtualSizes = [0x8000UL, 0x10000UL];

        MockMemorySpace.HeapFragment block2Fragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "LoaderHeapBlock2");
        helpers.WritePointer(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), TargetPointer.Null.Value);
        helpers.WritePointer(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddresses[1]);
        helpers.WriteNUInt(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSizes[1]));
        builder.AddHeapFragment(block2Fragment);

        MockMemorySpace.HeapFragment block1Fragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "LoaderHeapBlock1");
        helpers.WritePointer(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), block2Fragment.Address);
        helpers.WritePointer(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddresses[0]);
        helpers.WriteNUInt(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSizes[0]));
        builder.AddHeapFragment(block1Fragment);

        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "LoaderHeap");
        helpers.WritePointer(heapFragment.Data.AsSpan().Slice(heapType.Fields[nameof(Data.LoaderHeap.FirstBlock)].Offset, helpers.PointerSize), block1Fragment.Address);
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        ILoader loader = target.Contracts.Loader;

        List<(ulong Address, ulong Size)> blocks = [];
        TargetPointer block = loader.GetFirstLoaderHeapBlock(heapFragment.Address, LoaderHeapKind.Normal);
        while (block != TargetPointer.Null)
        {
            blocks.Add((loader.GetLoaderHeapBlockAddress(block).Value, loader.GetLoaderHeapBlockSize(block).Value));
            block = loader.GetNextLoaderHeapBlock(block);
        }

        Assert.Equal(2, blocks.Count);
        Assert.Equal((virtualAddresses[0], virtualSizes[0]), blocks[0]);
        Assert.Equal((virtualAddresses[1], virtualSizes[1]), blocks[1]);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MultipleBlockLoaderHeap_ExplicitControl(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);
        Dictionary<DataType, Target.TypeInfo> types = GetTypes(helpers);

        Target.TypeInfo heapType = types[DataType.ExplicitControlLoaderHeap];
        Target.TypeInfo blockType = types[DataType.LoaderHeapBlock];

        ulong[] virtualAddresses = [0x3000_0000UL, 0x4000_0000UL];
        ulong[] virtualSizes = [0x4000UL, 0x8000UL];

        MockMemorySpace.HeapFragment block2Fragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "ExplicitBlock2");
        helpers.WritePointer(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), TargetPointer.Null.Value);
        helpers.WritePointer(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddresses[1]);
        helpers.WriteNUInt(block2Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSizes[1]));
        builder.AddHeapFragment(block2Fragment);

        MockMemorySpace.HeapFragment block1Fragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(blockType), "ExplicitBlock1");
        helpers.WritePointer(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.Next)].Offset, helpers.PointerSize), block2Fragment.Address);
        helpers.WritePointer(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualAddress)].Offset, helpers.PointerSize), virtualAddresses[0]);
        helpers.WriteNUInt(block1Fragment.Data.AsSpan().Slice(blockType.Fields[nameof(Data.LoaderHeapBlock.VirtualSize)].Offset, helpers.PointerSize), new TargetNUInt(virtualSizes[0]));
        builder.AddHeapFragment(block1Fragment);

        MockMemorySpace.HeapFragment heapFragment = allocator.Allocate((ulong)helpers.SizeOfTypeInfo(heapType), "ExplicitControlLoaderHeap");
        helpers.WritePointer(heapFragment.Data.AsSpan().Slice(heapType.Fields[nameof(Data.ExplicitControlLoaderHeap.FirstBlock)].Offset, helpers.PointerSize), block1Fragment.Address);
        builder.AddHeapFragment(heapFragment);

        Target target = CreateTarget(arch, types, builder);
        ILoader loader = target.Contracts.Loader;

        List<(ulong Address, ulong Size)> blocks = [];
        TargetPointer block = loader.GetFirstLoaderHeapBlock(heapFragment.Address, LoaderHeapKind.ExplicitControl);
        while (block != TargetPointer.Null)
        {
            blocks.Add((loader.GetLoaderHeapBlockAddress(block).Value, loader.GetLoaderHeapBlockSize(block).Value));
            block = loader.GetNextLoaderHeapBlock(block);
        }

        Assert.Equal(2, blocks.Count);
        Assert.Equal((virtualAddresses[0], virtualSizes[0]), blocks[0]);
        Assert.Equal((virtualAddresses[1], virtualSizes[1]), blocks[1]);
    }
}

