// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class LoaderHeapTests
{
    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockLoaderBuilder loader)
        => new()
        {
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(loader.ModuleLayout),
            [DataType.Assembly] = TargetTestHelpers.CreateTypeInfo(loader.AssemblyLayout),
            [DataType.EEConfig] = TargetTestHelpers.CreateTypeInfo(loader.EEConfigLayout),
            [DataType.LoaderHeap] = TargetTestHelpers.CreateTypeInfo(loader.LoaderHeapLayout),
            [DataType.LoaderHeapBlock] = TargetTestHelpers.CreateTypeInfo(loader.LoaderHeapBlockLayout),
        };

    private static ILoader CreateLoaderContract(MockTarget.Architecture arch, Action<MockLoaderBuilder> configure)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockLoaderBuilder loader = new(targetBuilder.MemoryBuilder);

        configure(loader);

        var target = targetBuilder
            .AddTypes(CreateContractTypes(loader))
            .AddContract<ILoader>(version: "c1")
            .Build();
        return target.Contracts.Loader;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EmptyLoaderHeap(MockTarget.Architecture arch)
    {
        TargetPointer heapAddr = TargetPointer.Null;

        ILoader loader = CreateLoaderContract(arch, loaderBuilder =>
        {
            MockLoaderHeap heap = loaderBuilder.AddLoaderHeap(firstBlockAddress: 0);
            heapAddr = new TargetPointer(heap.Address);
        });

        Assert.Empty(loader.EnumerateLoaderHeapBlocks(heapAddr));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SingleBlockLoaderHeap(MockTarget.Architecture arch)
    {
        TargetPointer heapAddr = TargetPointer.Null;
        const ulong virtualAddress = 0x1234_0000UL;
        const ulong virtualSize = 0x1000UL;

        ILoader loader = CreateLoaderContract(arch, loaderBuilder =>
        {
            MockLoaderHeapBlock block = loaderBuilder.AddLoaderHeapBlock(virtualAddress, virtualSize);
            MockLoaderHeap heap = loaderBuilder.AddLoaderHeap(firstBlockAddress: block.Address);
            heapAddr = new TargetPointer(heap.Address);
        });

        LoaderHeapBlock blockData = Assert.Single(loader.EnumerateLoaderHeapBlocks(heapAddr));
        Assert.Equal(virtualAddress, blockData.Address.Value);
        Assert.Equal(virtualSize, blockData.Size.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MultipleBlockLoaderHeap(MockTarget.Architecture arch)
    {
        TargetPointer heapAddr = TargetPointer.Null;
        ulong[] virtualAddresses = [0x1000_0000UL, 0x2000_0000UL];
        ulong[] virtualSizes = [0x8000UL, 0x10000UL];

        ILoader loader = CreateLoaderContract(arch, loaderBuilder =>
        {
            // Build chain: heap -> block1 -> block2 -> null
            MockLoaderHeapBlock block2 = loaderBuilder.AddLoaderHeapBlock(virtualAddresses[1], virtualSizes[1]);
            MockLoaderHeapBlock block1 = loaderBuilder.AddLoaderHeapBlock(virtualAddresses[0], virtualSizes[0], nextBlockAddress: block2.Address);
            MockLoaderHeap heap = loaderBuilder.AddLoaderHeap(firstBlockAddress: block1.Address);
            heapAddr = new TargetPointer(heap.Address);
        });

        List<(ulong Address, ulong Size)> blocks = loader.EnumerateLoaderHeapBlocks(heapAddr)
            .Select(block => (block.Address.Value, block.Size.Value))
            .ToList();

        Assert.Equal(2, blocks.Count);
        Assert.Equal((virtualAddresses[0], virtualSizes[0]), blocks[0]);
        Assert.Equal((virtualAddresses[1], virtualSizes[1]), blocks[1]);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void CyclicLoaderHeapThrows(MockTarget.Architecture arch)
    {
        TargetPointer heapAddr = TargetPointer.Null;

        ILoader loader = CreateLoaderContract(arch, loaderBuilder =>
        {
            MockLoaderHeapBlock block1 = loaderBuilder.AddLoaderHeapBlock(0x1000_0000, 0x8000);
            MockLoaderHeapBlock block2 = loaderBuilder.AddLoaderHeapBlock(0x2000_0000, 0x10000, block1.Address);
            block1.Next = block2.Address;
            MockLoaderHeap heap = loaderBuilder.AddLoaderHeap(firstBlockAddress: block1.Address);
            heapAddr = new TargetPointer(heap.Address);
        });

        Assert.Throws<InvalidOperationException>(() => loader.EnumerateLoaderHeapBlocks(heapAddr).ToList());
    }
}
