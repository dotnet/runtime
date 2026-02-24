// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using MockLoader = MockDescriptors.Loader;

public unsafe class LoaderTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPath(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockLoader loader = new(builder);

        string expected = $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}TestModule.dll";

        // Add the modules
        TargetPointer moduleAddr = loader.AddModule(path: expected);
        TargetPointer moduleAddrEmptyPath = loader.AddModule();

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, loader.Types);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)));

        // Validate the expected module data
        ILoader contract = target.Contracts.Loader;
        Assert.NotNull(contract);
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
            string actual = contract.GetPath(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddrEmptyPath);
            string actual = contract.GetFileName(handle);
            Assert.Equal(string.Empty, actual);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFileName(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockLoader loader = new(builder);

        string expected = $"TestModule.dll";

        // Add the modules
        TargetPointer moduleAddr = loader.AddModule(fileName: expected);
        TargetPointer moduleAddrEmptyName = loader.AddModule();

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, loader.Types);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)));

        // Validate the expected module data
        Contracts.ILoader contract = target.Contracts.Loader;
        Assert.NotNull(contract);
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
            string actual = contract.GetFileName(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddrEmptyName);
            string actual = contract.GetFileName(handle);
            Assert.Equal(string.Empty, actual);
        }
    }

    private static readonly Dictionary<string, TargetPointer> MockHeapDictionary = new()
    {
        ["LowFrequencyHeap"] = new(0x1000),
        ["HighFrequencyHeap"] = new(0x2000),
        ["StaticsHeap"] = new(0x3000),
        ["StubHeap"] = new(0x4000),
        ["ExecutableHeap"] = new(0x5000),
        ["FixupPrecodeHeap"] = new(0x6000),
        ["NewStubPrecodeHeap"] = new(0x7000),
        ["IndcellHeap"] = new(0x8000),
        ["CacheEntryHeap"] = new(0x9000),
    };

    private static SOSDacImpl CreateSOSDacImplForHeapTests(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockLoader loader = new(builder);
        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, loader.Types);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Loader == Mock.Of<ILoader>(
                l => l.GetLoaderAllocatorHeaps(It.IsAny<TargetPointer>()) == (IReadOnlyDictionary<string, TargetPointer>)MockHeapDictionary
                && l.GetGlobalLoaderAllocator() == new TargetPointer(0x100))));
        return new SOSDacImpl(target, null);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_GetCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        int needed;
        int hr = impl.GetLoaderAllocatorHeapNames(0, null, &needed);

        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_GetNames(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        int needed;
        int hr = impl.GetLoaderAllocatorHeapNames(0, null, &needed);
        Assert.Equal(MockHeapDictionary.Count, needed);

        char** names = stackalloc char*[needed];
        hr = impl.GetLoaderAllocatorHeapNames(needed, names, &needed);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
        HashSet<string> expectedNames = new(MockHeapDictionary.Keys);
        for (int i = 0; i < needed; i++)
        {
            string actual = Marshal.PtrToStringAnsi((nint)names[i])!;
            Assert.Contains(actual, expectedNames);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_InsufficientBuffer(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        int needed;
        char** names = stackalloc char*[2];
        int hr = impl.GetLoaderAllocatorHeapNames(2, names, &needed);

        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
        HashSet<string> expectedNames = new(MockHeapDictionary.Keys);
        for (int i = 0; i < 2; i++)
        {
            string actual = Marshal.PtrToStringAnsi((nint)names[i])!;
            Assert.Contains(actual, expectedNames);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_NullPNeeded(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        int hr = impl.GetLoaderAllocatorHeapNames(0, null, null);
        Assert.Equal(HResults.S_FALSE, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_GetCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        int needed;
        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0x100), 0, null, null, &needed);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_GetHeaps(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        int needed;
        impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0x100), 0, null, null, &needed);

        ClrDataAddress* heaps = stackalloc ClrDataAddress[needed];
        int* kinds = stackalloc int[needed];
        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0x100), needed, heaps, kinds, &needed);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
        HashSet<ulong> expectedAddresses = new(MockHeapDictionary.Values.Select(p => (ulong)p));
        for (int i = 0; i < needed; i++)
        {
            Assert.Contains((ulong)heaps[i], expectedAddresses);
            Assert.Equal(0, kinds[i]); // LoaderHeapKindNormal
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_InsufficientBuffer(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        ClrDataAddress* heaps = stackalloc ClrDataAddress[2];
        int* kinds = stackalloc int[2];
        int needed;
        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0x100), 2, heaps, kinds, &needed);

        Assert.Equal(HResults.E_INVALIDARG, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_NullAddress(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacImplForHeapTests(arch);

        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0), 0, null, null, null);

        Assert.Equal(HResults.E_INVALIDARG, hr);
    }
}
