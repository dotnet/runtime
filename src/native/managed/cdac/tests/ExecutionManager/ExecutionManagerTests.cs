// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class ExecutionManagerTests
{
    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockExecutionManagerBuilder emBuilder)
    {
        TargetTestHelpers helpers = emBuilder.Builder.TargetTestHelpers;
        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RangeSectionMap] = TargetTestHelpers.CreateTypeInfo(emBuilder.RangeSectionMapLayout),
            [DataType.RangeSectionFragment] = TargetTestHelpers.CreateTypeInfo(emBuilder.RangeSectionFragmentLayout),
            [DataType.RangeSection] = TargetTestHelpers.CreateTypeInfo(emBuilder.RangeSectionLayout),
            [DataType.CodeHeapListNode] = TargetTestHelpers.CreateTypeInfo(emBuilder.CodeHeapListNodeLayout),
            [DataType.CodeHeap] = TargetTestHelpers.CreateTypeInfo(emBuilder.CodeHeapLayout),
            [DataType.LoaderCodeHeap] = TargetTestHelpers.CreateTypeInfo(emBuilder.LoaderCodeHeapLayout),
            [DataType.HostCodeHeap] = TargetTestHelpers.CreateTypeInfo(emBuilder.HostCodeHeapLayout),
            [DataType.RealCodeHeader] = TargetTestHelpers.CreateTypeInfo(emBuilder.RealCodeHeaderLayout),
            [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(emBuilder.ReadyToRunInfoLayout),
            [DataType.EEJitManager] = TargetTestHelpers.CreateTypeInfo(emBuilder.EEJitManagerLayout),
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(emBuilder.ModuleLayout),
            [DataType.HashMap] = TargetTestHelpers.CreateTypeInfo(MockHashMap.CreateLayout(helpers.Arch)),
            [DataType.Bucket] = TargetTestHelpers.CreateTypeInfo(MockHashMapBucket.CreateLayout(helpers.Arch)),
        };

        types[DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(emBuilder.RuntimeFunctionLayout);
        types[DataType.UnwindInfo] = TargetTestHelpers.CreateTypeInfo(emBuilder.UnwindInfoLayout);

        return types;
    }

    private static Target CreateTarget(MockExecutionManagerBuilder emBuilder)
    {
        var arch = emBuilder.Builder.TargetTestHelpers.Arch;
        return new TestPlaceholderTarget.Builder(arch)
            .UseReader(emBuilder.Builder.GetMemoryContext().ReadFromTarget)
            .AddTypes(CreateContractTypes(emBuilder))
            .AddGlobals(emBuilder.Globals)
            .AddContract<IExecutionManager>(version: emBuilder.Version)
            .AddMockContract<IPlatformMetadata>(Mock.Of<IPlatformMetadata>())
            .Build();
    }

    private static IExecutionManager CreateExecutionManagerContract(
        int version,
        MockTarget.Architecture arch,
        Action<MockExecutionManagerBuilder>? configure = null,
        ulong allCodeHeaps = 0)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange, allCodeHeaps);
        configure?.Invoke(emBuilder);
        Target target = CreateTarget(emBuilder);
        return target.Contracts.ExecutionManager;
    }

    private static void LinkHeapIntoAllCodeHeaps(MockExecutionManagerBuilder emBuilder, ulong heapAddress)
    {
        const ulong CodeRangeStart = 0x1000_0000;
        const uint CodeRangeSize = 0x1000;

        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(CodeRangeStart, CodeRangeSize);
        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: CodeRangeStart,
            endAddress: CodeRangeStart + CodeRangeSize,
            mapBase: CodeRangeStart,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            heap: heapAddress);
        emBuilder.SetAllCodeHeaps(node.Address);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_Null(int version, MockTarget.Architecture arch)
    {
        IExecutionManager em = CreateExecutionManagerContract(version, arch);
        var eeInfo = em.GetCodeBlockHandle(TargetCodePointer.Null);
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_NoRangeSections(int version, MockTarget.Architecture arch)
    {
        IExecutionManager em = CreateExecutionManagerContract(version, arch);
        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(0x0a0a_0000));
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_OneRangeOneMethod(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const uint methodSize = 0x450; // arbitrary
        ulong methodStart = 0;

        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary

        const ulong expectedMethodDescAddress = 0x0101_aaa0;

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                methodStart = emBuilder.AddJittedMethod(jittedCode, methodSize, expectedMethodDescAddress).CodeAddress;

                NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
                nibBuilder.AllocateCodeChunk(new TargetCodePointer(methodStart), methodSize);

                MockCodeHeapListNode codeHeapListNode = emBuilder.AddCodeHeapListNode(0, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
                MockRangeSection rangeSection = emBuilder.AddRangeSection(jittedCode, jitManagerAddress, codeHeapListNode.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        // test at method start
        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(methodStart));
        Assert.NotNull(eeInfo);
        TargetPointer actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);

        // test middle of method
        eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(methodStart + methodSize / 2));
        Assert.NotNull(eeInfo);
        actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);

        // test end of method
        eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(methodStart + methodSize - 1));
        Assert.NotNull(eeInfo);
        actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_OneRangeZeroMethod(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary

        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
                MockCodeHeapListNode codeHeapListNode = emBuilder.AddCodeHeapListNode(0, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
                MockRangeSection rangeSection = emBuilder.AddRangeSection(jittedCode, jitManagerAddress, codeHeapListNode.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        // test at code range start
        var eeInfo = em.GetCodeBlockHandle(codeRangeStart);
        Assert.Null(eeInfo);

        // test middle of code range
        eeInfo = em.GetCodeBlockHandle(codeRangeStart + codeRangeSize / 2);
        Assert.Null(eeInfo);

        // test end of code range
        eeInfo = em.GetCodeBlockHandle(codeRangeStart + codeRangeSize - 1);
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetUnwindInfoBaseAddress_OneRangeOneMethod(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const uint methodSize = 0x450; // arbitrary
        ulong methodStart = 0;

        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                methodStart = emBuilder.AddJittedMethod(jittedCode, methodSize, 0x0101_aaa0).CodeAddress;

                NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
                nibBuilder.AllocateCodeChunk(new TargetCodePointer(methodStart), methodSize);

                MockCodeHeapListNode codeHeapListNode = emBuilder.AddCodeHeapListNode(0, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
                MockRangeSection rangeSection = emBuilder.AddRangeSection(jittedCode, jitManagerAddress, codeHeapListNode.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        // Get CodeBlockHandle
        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(methodStart));
        Assert.NotNull(eeInfo);
        TargetPointer actualBaseAddress = em.GetUnwindInfoBaseAddress(eeInfo.Value);
        Assert.Equal(new TargetPointer(codeRangeStart), actualBaseAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_R2R_NoRuntimeFunctionMatch(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary
        uint runtimeFunction = 0x100;

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([runtimeFunction], []);
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(
                    r2rInfo.EntryPointToMethodDescMapAddress,
                    []);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        // Before any functions
        var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunction - 1);
        Assert.Null(handle);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_R2R_OneRuntimeFunction(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary

        const ulong expectedMethodDescAddress = 0x0101_aaa0;

        uint expectedRuntimeFunction = 0x100;
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([expectedRuntimeFunction], []);
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(
                    r2rInfo.EntryPointToMethodDescMapAddress,
                    [(jittedCode.RangeStart + expectedRuntimeFunction, expectedMethodDescAddress)]);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        {
            // Function start
            var handle = em.GetCodeBlockHandle(codeRangeStart + expectedRuntimeFunction);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);
        }
        {
            // Past function start
            var handle = em.GetCodeBlockHandle(codeRangeStart + expectedRuntimeFunction * 2);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_R2R_MultipleRuntimeFunctions(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary

        TargetPointer[] methodDescAddresses = [0x0101_aaa0, 0x0201_aaa0];

        uint[] runtimeFunctions = [0x100, 0xc00];
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo(runtimeFunctions, []);
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(
                    r2rInfo.EntryPointToMethodDescMapAddress,
                    [
                        (jittedCode.RangeStart + runtimeFunctions[0], methodDescAddresses[0].Value),
                        (jittedCode.RangeStart + runtimeFunctions[1], methodDescAddresses[1].Value),
                    ]);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        {
            // Match first function
            var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[0]);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[0], actualMethodDesc);
        }
        {
            // After first function, before second - match first function
            uint betweenFirstAndSecond = runtimeFunctions[0] + (runtimeFunctions[1] - runtimeFunctions[0]) / 2;
            var handle = em.GetCodeBlockHandle(codeRangeStart + betweenFirstAndSecond);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[0], actualMethodDesc);
        }
        {
            // Match second function
            var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[1]);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[1], actualMethodDesc);
        }
        {
            // After second/last function - match second/last function
            var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[1] * 2);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[1], actualMethodDesc);
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_R2R_HotColdBlock(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary

        TargetPointer[] methodDescAddresses = [0x0101_aaa0, 0x0201_aaa0];

        uint[] runtimeFunctions = [0x100, 0x200, 0x300, 0x400, 0x500];
        uint[] hotColdMap = [3, 0, 4, 1];
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo(runtimeFunctions, hotColdMap);
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(
                    r2rInfo.EntryPointToMethodDescMapAddress,
                    [
                        (jittedCode.RangeStart + runtimeFunctions[hotColdMap[1]], methodDescAddresses[0].Value),
                        (jittedCode.RangeStart + runtimeFunctions[hotColdMap[3]], methodDescAddresses[1].Value),
                    ]);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        // Hot and cold parts should map to the same method desc
        for (int i = 0; i < hotColdMap.Length; i++)
        {
            // Function start
            var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[hotColdMap[i]]);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[i / 2], actualMethodDesc);

            // Past function start
            handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[hotColdMap[i]] + 8);
            Assert.NotNull(handle);
            actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[i / 2], actualMethodDesc);
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetUnwindInfoBaseAddress_R2R_ManyRuntimeFunction(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const ulong jitManagerAddress = 0x000b_ff00; // arbitrary

        uint runtimeFunction = 0x100;
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([runtimeFunction], []);
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(
                    r2rInfo.EntryPointToMethodDescMapAddress,
                    [(jittedCode.RangeStart + runtimeFunction, 0x0101_aaa0)]);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunction);
        Assert.NotNull(handle);
        TargetPointer actualBaseAddress = em.GetUnwindInfoBaseAddress(handle.Value);
        Assert.Equal(new TargetPointer(codeRangeStart), actualBaseAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_CollectibleFragmentNext(int version, MockTarget.Architecture arch)
    {
        // Regression test: RangeSectionFragment.Next uses bit 0 as a collectible flag (see
        // RangeSectionFragmentPointer in codeman.h). If the cDAC fails to strip this bit before
        // following the pointer, it reads from a misaligned address and produces garbage data.
        // This test creates a two-fragment chain where the head fragment (in the map) has an empty
        // range and its Next pointer has the collectible tag bit set. The tail fragment (not in the
        // map) covers the actual code range. The lookup must traverse the chain to find the method.
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x450;

        const ulong jitManagerAddress = 0x000b_ff00;
        const ulong expectedMethodDescAddress = 0x0101_aaa0;

        ulong methodStart = 0;
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                methodStart = emBuilder.AddJittedMethod(jittedCode, methodSize, expectedMethodDescAddress).CodeAddress;

                NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
                nibBuilder.AllocateCodeChunk(new TargetCodePointer(methodStart), methodSize);

                MockCodeHeapListNode codeHeapListNode = emBuilder.AddCodeHeapListNode(0, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
                MockRangeSection rangeSection = emBuilder.AddRangeSection(jittedCode, jitManagerAddress, codeHeapListNode.Address);
                MockRangeSectionFragment tailFragment = emBuilder.AddUnmappedRangeSectionFragment(jittedCode, rangeSection.Address);
                _ = emBuilder.AddRangeSectionFragmentWithCollectibleNext(jittedCode, rangeSection.Address, tailFragment.Address);
            });

        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(methodStart));
        Assert.NotNull(eeInfo);
        TargetPointer actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetEEJitManagerInfo_ReturnsManagerAddress(int version, MockTarget.Architecture arch)
    {
        ulong expectedManagerAddress = 0;
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder => expectedManagerAddress = emBuilder.EEJitManagerAddress);
        JitManagerInfo info = em.GetEEJitManagerInfo();
        Assert.Equal(new TargetPointer(expectedManagerAddress), info.ManagerAddress);
        Assert.Equal(0u, info.CodeType);
        Assert.Equal(TargetPointer.Null, info.HeapListAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetEEJitManagerInfo_WithCodeHeaps(int version, MockTarget.Architecture arch)
    {
        const ulong expectedHeapList = 0x0099_aa00;
        ulong expectedManagerAddress = 0;
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder => expectedManagerAddress = emBuilder.EEJitManagerAddress,
            allCodeHeaps: expectedHeapList);
        JitManagerInfo info = em.GetEEJitManagerInfo();
        Assert.Equal(new TargetPointer(expectedManagerAddress), info.ManagerAddress);
        Assert.Equal(0u, info.CodeType);
        Assert.Equal(new TargetPointer(expectedHeapList), info.HeapListAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_LoaderCodeHeap(int version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        MockLoaderCodeHeap heap = emBuilder.AddLoaderCodeHeap();
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        ICodeHeapInfo info = em.GetCodeHeapInfos().Single();
        Assert.IsType<LoaderCodeHeapInfo>(info);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_HostCodeHeap(int version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        TargetPointer baseAddr    = new(0x0001_0000);
        TargetPointer currentAddr = new(0x0001_8000);
        MockHostCodeHeap heap = emBuilder.AddHostCodeHeap(baseAddr.Value, currentAddr.Value);
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        ICodeHeapInfo info = em.GetCodeHeapInfos().Single();
        Assert.IsType<HostCodeHeapInfo>(info);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_LoaderCodeHeap_ReturnsLoaderHeapAddress(int version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        MockLoaderCodeHeap heap = emBuilder.AddLoaderCodeHeap();
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        LoaderCodeHeapInfo loader = Assert.IsType<LoaderCodeHeapInfo>(em.GetCodeHeapInfos().Single());
        Target.TypeInfo loaderCodeHeapType = TargetTestHelpers.CreateTypeInfo(emBuilder.LoaderCodeHeapLayout);
        ulong loaderHeapFieldOffset = (ulong)loaderCodeHeapType.Fields[nameof(Data.LoaderCodeHeap.LoaderHeap)].Offset;
        Assert.Equal(new TargetPointer(heap.Address + loaderHeapFieldOffset), loader.LoaderHeapAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_HostCodeHeap_ReturnsAddresses(int version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        TargetPointer expectedBase    = new(0x0002_0000);
        TargetPointer expectedCurrent = new(0x0002_4000);
        MockHostCodeHeap heap = emBuilder.AddHostCodeHeap(expectedBase.Value, expectedCurrent.Value);
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        HostCodeHeapInfo host = Assert.IsType<HostCodeHeapInfo>(em.GetCodeHeapInfos().Single());
        Assert.Equal(expectedBase, host.BaseAddress);
        Assert.Equal(expectedCurrent, host.CurrentAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapList_SingleNode(int version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);

        MockLoaderCodeHeap heap = emBuilder.AddLoaderCodeHeap();

        TargetPointer codeRangeStart = new(0x1000_0000);
        uint codeRangeSize = 0x1000;
        var nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart.Value, codeRangeSize);

        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart.Value,
            endAddress: codeRangeStart.Value + codeRangeSize,
            mapBase: codeRangeStart.Value,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            heap: heap.Address);

        emBuilder.SetAllCodeHeaps(node.Address);

        var target = CreateTarget(emBuilder);
        var em = target.Contracts.ExecutionManager;

        List<ICodeHeapInfo> heapInfos = em.GetCodeHeapInfos().ToList();
        Assert.Single(heapInfos);
        Assert.IsType<LoaderCodeHeapInfo>(heapInfos[0]);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapList_LinkedList_TwoNodes(int version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);

        MockLoaderCodeHeap loaderHeap = emBuilder.AddLoaderCodeHeap();

        TargetPointer baseAddr    = new(0x0003_0000);
        TargetPointer currentAddr = new(0x0003_8000);
        MockHostCodeHeap hostHeap = emBuilder.AddHostCodeHeap(baseAddr.Value, currentAddr.Value);

        TargetPointer codeRangeStart1 = new(0x1000_0000);
        TargetPointer codeRangeStart2 = new(0x2000_0000);
        uint codeRangeSize = 0x1000;

        var nib1 = emBuilder.CreateNibbleMap(codeRangeStart1.Value, codeRangeSize);
        var nib2 = emBuilder.CreateNibbleMap(codeRangeStart2.Value, codeRangeSize);

        // Build list: node2 -> null, node1 -> node2
        MockCodeHeapListNode node2 = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart2.Value,
            endAddress: codeRangeStart2.Value + codeRangeSize,
            mapBase: codeRangeStart2.Value,
            headerMap: nib2.NibbleMapFragment.Address,
            heap: hostHeap.Address);

        MockCodeHeapListNode node1 = emBuilder.AddCodeHeapListNode(
            next: node2.Address,
            startAddress: codeRangeStart1.Value,
            endAddress: codeRangeStart1.Value + codeRangeSize,
            mapBase: codeRangeStart1.Value,
            headerMap: nib1.NibbleMapFragment.Address,
            heap: loaderHeap.Address);

        emBuilder.SetAllCodeHeaps(node1.Address);

        var target = CreateTarget(emBuilder);
        var em = target.Contracts.ExecutionManager;

        List<ICodeHeapInfo> heapInfos = em.GetCodeHeapInfos().ToList();
        Assert.Equal(2, heapInfos.Count);

        // First heap (from node1) is a LoaderCodeHeap
        LoaderCodeHeapInfo loaderInfo = Assert.IsType<LoaderCodeHeapInfo>(heapInfos[0]);
        Assert.Equal(loaderHeap.Address, loaderInfo.HeapAddress.Value);
        Target.TypeInfo loaderCodeHeapType = TargetTestHelpers.CreateTypeInfo(emBuilder.LoaderCodeHeapLayout);
        ulong loaderHeapFieldOffset = (ulong)loaderCodeHeapType.Fields[nameof(Data.LoaderCodeHeap.LoaderHeap)].Offset;
        Assert.Equal(new TargetPointer(loaderHeap.Address + loaderHeapFieldOffset), loaderInfo.LoaderHeapAddress);

        // Second heap (from node2) is a HostCodeHeap
        HostCodeHeapInfo hostInfo = Assert.IsType<HostCodeHeapInfo>(heapInfos[1]);
        Assert.Equal(hostHeap.Address, hostInfo.HeapAddress.Value);
        Assert.Equal(baseAddr, hostInfo.BaseAddress);
        Assert.Equal(currentAddr, hostInfo.CurrentAddress);
    }

    public static IEnumerable<object[]> StdArchAllVersions()
    {
        const int highestVersion = 2;
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            for (int version = 1; version <= highestVersion; version++)
            {
                yield return new object[] { version, arch };
            }
        }
    }
}
