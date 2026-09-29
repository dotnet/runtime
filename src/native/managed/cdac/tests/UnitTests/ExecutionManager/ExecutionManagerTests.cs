// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class ExecutionManagerTests
{
    internal static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockExecutionManagerBuilder emBuilder)
    {
        TargetTestHelpers helpers = emBuilder.Builder.TargetTestHelpers;
        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RangeSectionMap] = TargetTestHelpers.CreateTypeInfo(emBuilder.RangeSectionMapLayout),
            [DataType.RangeSectionFragment] = TargetTestHelpers.CreateTypeInfo(emBuilder.RangeSectionFragmentLayout),
            [DataType.RangeSection] = TargetTestHelpers.CreateTypeInfo(emBuilder.RangeSectionLayout),
            [DataType.VirtualIPRangeSection] = TargetTestHelpers.CreateTypeInfo(emBuilder.VirtualIPRangeSectionLayout),
            [DataType.CodeHeapListNode] = TargetTestHelpers.CreateTypeInfo(emBuilder.CodeHeapListNodeLayout),
            [DataType.CodeHeap] = TargetTestHelpers.CreateTypeInfo(emBuilder.CodeHeapLayout),
            [DataType.LoaderCodeHeap] = TargetTestHelpers.CreateTypeInfo(emBuilder.LoaderCodeHeapLayout),
            [DataType.HostCodeHeap] = TargetTestHelpers.CreateTypeInfo(emBuilder.HostCodeHeapLayout),
            [DataType.RealCodeHeader] = TargetTestHelpers.CreateTypeInfo(emBuilder.RealCodeHeaderLayout),
            [DataType.InterpreterRealCodeHeader] = TargetTestHelpers.CreateTypeInfo(emBuilder.InterpreterRealCodeHeaderLayout),
            [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(emBuilder.ReadyToRunInfoLayout),
            [DataType.ReadyToRunHeader] = TargetTestHelpers.CreateTypeInfo(emBuilder.ReadyToRunHeaderLayout),
            [DataType.EEJitManager] = TargetTestHelpers.CreateTypeInfo(emBuilder.EEJitManagerLayout),
            [DataType.DynamicFunctionTable] = TargetTestHelpers.CreateTypeInfo(emBuilder.DynamicFunctionTableLayout),
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(emBuilder.ModuleLayout),
            [DataType.CodeRangeMapRangeList] = TargetTestHelpers.CreateTypeInfo(emBuilder.CodeRangeMapRangeListLayout),
            [DataType.ImageDataDirectory] = TargetTestHelpers.CreateTypeInfo(emBuilder.ImageDataDirectoryLayout),
            [DataType.HashMap] = TargetTestHelpers.CreateTypeInfo(MockHashMap.CreateLayout(helpers.Arch)),
            [DataType.Bucket] = TargetTestHelpers.CreateTypeInfo(MockHashMapBucket.CreateLayout(helpers.Arch)),
        };

        types[DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(emBuilder.RuntimeFunctionLayout);
        types[DataType.UnwindInfo] = TargetTestHelpers.CreateTypeInfo(emBuilder.UnwindInfoLayout);

        return types;
    }

    internal static Target CreateTarget(
        MockExecutionManagerBuilder emBuilder,
        params (string Name, ulong Value)[] additionalGlobals)
        => CreateTarget(
            emBuilder,
            RuntimeInfoOperatingSystem.Windows,
            targetArchitecture: null,
            additionalGlobals);

    internal static Target CreateTarget(
        MockExecutionManagerBuilder emBuilder,
        RuntimeInfoOperatingSystem operatingSystem = RuntimeInfoOperatingSystem.Windows,
        RuntimeInfoArchitecture? targetArchitecture = null,
        Action<TestPlaceholderTarget.Builder>? configureTarget = null)
        => CreateTarget(
            emBuilder,
            operatingSystem,
            targetArchitecture,
            [],
            configureTarget);

    private static Target CreateTarget(
        MockExecutionManagerBuilder emBuilder,
        RuntimeInfoOperatingSystem operatingSystem,
        RuntimeInfoArchitecture? targetArchitecture,
        (string Name, ulong Value)[] additionalGlobals,
        Action<TestPlaceholderTarget.Builder>? configureTarget = null)
    {
        var arch = emBuilder.Builder.TargetTestHelpers.Arch;
        RuntimeInfoArchitecture architecture = targetArchitecture ?? (arch.Is64Bit
            ? RuntimeInfoArchitecture.X64
            : RuntimeInfoArchitecture.Arm);
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetOperatingSystem()).Returns(operatingSystem);
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(architecture);

        var targetBuilder = new TestPlaceholderTarget.Builder(arch)
            .UseReader(emBuilder.Builder.GetMemoryContext().ReadFromTarget)
            .AddTypes(CreateContractTypes(emBuilder))
            .AddGlobals(emBuilder.Globals)
            .AddGlobals(additionalGlobals)
            .AddContract<IExecutionManager>(version: emBuilder.Version)
            .AddMockContract<IPlatformMetadata>(Mock.Of<IPlatformMetadata>())
            .AddMockContract(runtimeInfo);
        configureTarget?.Invoke(targetBuilder);
        return targetBuilder.Build();
    }

    private static IExecutionManager CreateExecutionManagerContract(
        string version,
        MockTarget.Architecture arch,
        Action<MockExecutionManagerBuilder>? configure = null,
        ulong allCodeHeaps = 0,
        RuntimeInfoArchitecture? targetArchitecture = null)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange, allCodeHeaps,
            isWasm: targetArchitecture == RuntimeInfoArchitecture.Wasm);
        configure?.Invoke(emBuilder);
        Target target = CreateTarget(emBuilder, RuntimeInfoOperatingSystem.Windows, targetArchitecture);
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
    public void GetCodeBlockHandle_Null(string version, MockTarget.Architecture arch)
    {
        IExecutionManager em = CreateExecutionManagerContract(version, arch);
        var eeInfo = em.GetCodeBlockHandle(TargetCodePointer.Null);
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_NoRangeSections(string version, MockTarget.Architecture arch)
    {
        IExecutionManager em = CreateExecutionManagerContract(version, arch);
        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(0x0a0a_0000));
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_OneRangeOneMethod(string version, MockTarget.Architecture arch)
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
    public void GetCodeBlockHandle_OneRangeZeroMethod(string version, MockTarget.Architecture arch)
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
    public void GetUnwindInfoBaseAddress_OneRangeOneMethod(string version, MockTarget.Architecture arch)
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
    public void GetCodeBlockHandle_R2R_NoRuntimeFunctionMatch(string version, MockTarget.Architecture arch)
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
    public void GetMethodDesc_R2R_OneRuntimeFunction(string version, MockTarget.Architecture arch)
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

    [Fact]
    public void GetMethodDesc_R2R_WasmVirtualIPRangeList_ResolvesCapturedShape()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong VirtualIPRangeStart = 0x8001_0001;
        const uint VirtualIPRangeSize = 0x400;
        const uint FunctionBeginAddress = 0x108;
        const ulong CapturedVirtualIP = 0x8001_0109;
        const ulong LoadedImageBase = 0x0090_0000;
        const ulong JitManagerAddress = 0x000b_ff00;
        const ulong ExpectedMethodDescAddress = 0x0101_aaa0;
        ulong moduleAddress = 0;
        ulong runtimeFunctionsAddress = 0;
        int runtimeFunctionSize = 0;

        IExecutionManager em = CreateExecutionManagerContract(
            "c1",
            wasmArch,
            emBuilder =>
            {
                MockExecutionManagerBuilder.JittedCodeRange virtualIPRange =
                    emBuilder.AllocateJittedCodeRange(VirtualIPRangeStart, VirtualIPRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([0x80, FunctionBeginAddress, 0x200], []);
                r2rInfo.MinVirtualIP = VirtualIPRangeStart;
                r2rInfo.LoadedImageBase = LoadedImageBase;
                runtimeFunctionsAddress = r2rInfo.RuntimeFunctions;
                runtimeFunctionSize = emBuilder.RuntimeFunctionLayout.Size;
                Assert.Equal(8, runtimeFunctionSize);
                Assert.DoesNotContain(emBuilder.RuntimeFunctionLayout.Fields, field => field.Name == "EndAddress");
                Assert.DoesNotContain(emBuilder.ReadyToRunInfoLayout.Fields,
                    field => field.Name is "NumHotColdMap" or "HotColdMap" or "DelayLoadMethodCallThunks");

                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(
                    r2rInfo.EntryPointToMethodDescMapAddress,
                    [(CapturedVirtualIP, ExpectedMethodDescAddress)]);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                moduleAddress = r2rModule.Address;
                _ = emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address);
            },
            targetArchitecture: RuntimeInfoArchitecture.Wasm);

        TargetCodePointer virtualIP = new(CapturedVirtualIP);
        CodeBlockHandle? handle = em.GetCodeBlockHandle(virtualIP);

        Assert.NotNull(handle);
        Assert.Equal(new TargetPointer(ExpectedMethodDescAddress), em.GetMethodDesc(handle.Value));
        Assert.Equal(new TargetPointer(CapturedVirtualIP), em.GetStartAddress(handle.Value));
        Assert.Equal(new TargetPointer(LoadedImageBase), em.GetUnwindInfoBaseAddress(handle.Value));
        Assert.Equal(new TargetPointer(runtimeFunctionsAddress + (ulong)runtimeFunctionSize), em.GetUnwindInfo(handle.Value));
        Assert.Equal(new TargetPointer(moduleAddress), em.FindReadyToRunModule(new TargetPointer(CapturedVirtualIP)));
        Assert.Equal(CodeKind.ReadyToRun, em.GetCodeKind(virtualIP));
    }

    [Fact]
    public void ReadyToRunInfo_WasmMissingOptionalFields_EnsureAllFieldsRead()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        MockExecutionManagerBuilder emBuilder = new(
            "c1",
            wasmArch,
            MockExecutionManagerBuilder.DefaultAllocationRange,
            isWasm: true);
        MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([0x100], []);
        r2rInfo.MinVirtualIP = 0x8001_0001;
        r2rInfo.LoadedImageBase = 0x0090_0000;
        Target target = CreateTarget(
            emBuilder,
            RuntimeInfoOperatingSystem.Windows,
            RuntimeInfoArchitecture.Wasm);

        Data.ReadyToRunInfo data =
            target.ProcessedData.GetOrAdd<Data.ReadyToRunInfo>(r2rInfo.Address);
        ((Data.IReadableData)data).EnsureAllFieldsRead();

        Assert.Null(data.NumHotColdMap);
        Assert.Null(data.DelayLoadMethodCallThunks);
    }

    [Fact]
    public void GetGCInfo_R2R_WasmVirtualIPUsesLoadedImageBase()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong VirtualIPRangeStart = 0x8001_0001;
        const uint FunctionBeginAddress = 0x108;
        const ulong LoadedImageBase = 0x0090_0000;
        const uint UnwindDataRva = 0x80;
        const ulong MethodDescAddress = 0x0101_aaa0;
        const ulong JitManagerAddress = 0x000b_ff00;
        const byte ExpectedGCInfoByte = 0xa5;

        MockExecutionManagerBuilder emBuilder = new(
            "c1",
            wasmArch,
            MockExecutionManagerBuilder.DefaultAllocationRange,
            isWasm: true);
        MockExecutionManagerBuilder.JittedCodeRange virtualIPRange =
            emBuilder.AllocateJittedCodeRange(VirtualIPRangeStart, 0x400);
        MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([FunctionBeginAddress], []);
        r2rInfo.MinVirtualIP = VirtualIPRangeStart;
        r2rInfo.LoadedImageBase = LoadedImageBase;
        r2rInfo.ReadyToRunHeader = emBuilder.AddReadyToRunHeader(majorVersion: 21).Address;
        emBuilder.SetRuntimeFunctionUnwindData(r2rInfo, index: 0, UnwindDataRva);
        emBuilder.Builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = LoadedImageBase + UnwindDataRva,
            // Frame size 128, function length 32, followed by the GC info.
            Data = [0x80, 0x01, 0x20, ExpectedGCInfoByte],
            Name = "WASM unwind and GC info",
        });

        MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
        hashMapBuilder.PopulatePtrMap(
            r2rInfo.EntryPointToMethodDescMapAddress,
            [(VirtualIPRangeStart + FunctionBeginAddress, MethodDescAddress)]);
        MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
        _ = emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address);

        IGCInfoHandle gcInfoHandle = Mock.Of<IGCInfoHandle>();
        Mock<IGCInfo> gcInfoContract = new(MockBehavior.Strict);
        gcInfoContract.Setup(c => c.DecodePlatformSpecificGCInfo(new TargetPointer(LoadedImageBase + UnwindDataRva + 3), 5))
            .Returns(gcInfoHandle);
        gcInfoContract.Setup(c => c.GetCodeLength(gcInfoHandle)).Returns(32u);
        Target target = CreateTarget(
            emBuilder,
            RuntimeInfoOperatingSystem.Windows,
            RuntimeInfoArchitecture.Wasm,
            targetBuilder => targetBuilder.AddMockContract(gcInfoContract));
        IExecutionManager em = target.Contracts.ExecutionManager;
        CodeBlockHandle? handle =
            em.GetCodeBlockHandle(new TargetCodePointer(VirtualIPRangeStart + FunctionBeginAddress));

        Assert.NotNull(handle);
        em.GetGCInfo(handle.Value, out TargetPointer gcInfo, out uint gcVersion);
        Assert.Equal(new TargetPointer(LoadedImageBase + UnwindDataRva + 3), gcInfo);
        Assert.Equal(5u, gcVersion);
        Assert.Equal(ExpectedGCInfoByte, target.Read<byte>(gcInfo));
        em.GetMethodRegionInfo(handle.Value, out uint hotSize, out TargetPointer coldStart, out uint coldSize);
        Assert.Equal(32u, hotSize);
        Assert.Equal(TargetPointer.Null, coldStart);
        Assert.Equal(0u, coldSize);
    }

    [Fact]
    public void GetCodeKind_R2R_WasmVirtualIPRangeList_RejectsBoundariesAndKeepsAdjacentRangesSeparate()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong FirstRangeStart = 0x8001_0001;
        const ulong SecondRangeStart = 0x8001_0201;
        const uint RangeSize = 0x200;
        const ulong JitManagerAddress = 0x000b_ff00;
        ulong firstModuleAddress = 0;
        ulong secondModuleAddress = 0;

        IExecutionManager em = CreateExecutionManagerContract(
            "c1",
            wasmArch,
            emBuilder =>
            {
                MockExecutionManagerBuilder.JittedCodeRange firstRange =
                    emBuilder.AllocateJittedCodeRange(FirstRangeStart, RangeSize);
                MockReadyToRunInfo firstInfo = emBuilder.AddReadyToRunInfo([0], []);
                firstInfo.MinVirtualIP = FirstRangeStart;
                firstInfo.LoadedImageBase = 0x0090_0000;
                MockLoaderModule firstModule = emBuilder.AddReadyToRunModule(firstInfo.Address);
                firstModuleAddress = firstModule.Address;
                MockVirtualIPRangeSection firstNode =
                    emBuilder.AddVirtualIPRangeSection(firstRange, JitManagerAddress, firstModule.Address, registerAsHead: false);

                MockExecutionManagerBuilder.JittedCodeRange secondRange =
                    emBuilder.AllocateJittedCodeRange(SecondRangeStart, RangeSize);
                MockReadyToRunInfo secondInfo = emBuilder.AddReadyToRunInfo([0], []);
                secondInfo.MinVirtualIP = SecondRangeStart;
                secondInfo.LoadedImageBase = 0x00a0_0000;
                MockLoaderModule secondModule = emBuilder.AddReadyToRunModule(secondInfo.Address);
                secondModuleAddress = secondModule.Address;
                _ = emBuilder.AddVirtualIPRangeSection(secondRange, JitManagerAddress, secondModule.Address, firstNode.Address);
            },
            targetArchitecture: RuntimeInfoArchitecture.Wasm);

        Assert.Equal(CodeKind.Unknown, em.GetCodeKind(new TargetCodePointer(FirstRangeStart - 2)));
        Assert.Equal(new TargetPointer(firstModuleAddress), em.FindReadyToRunModule(new TargetPointer(FirstRangeStart)));
        Assert.Equal(new TargetPointer(secondModuleAddress), em.FindReadyToRunModule(new TargetPointer(SecondRangeStart)));
        Assert.Equal(CodeKind.Unknown, em.GetCodeKind(new TargetCodePointer(SecondRangeStart + RangeSize)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void GetDebugInfoAndExceptionClauses_R2R_WasmUsesLoadedImageBase(bool funclet, bool filter)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const ulong VirtualIPRangeStart = 0x8001_0001;
        const uint RootBeginAddress = 0x100;
        const uint FuncletBeginAddress = 0x120;
        const uint LoadedImageBase = 0x0090_0000;
        const uint DebugInfoRva = 0x80;
        const ulong MethodDescAddress = 0x0101_aaa0;
        const ulong JitManagerAddress = 0x000b_ff00;
        const byte ExpectedDebugInfoByte = 0xa5;

        MockExecutionManagerBuilder emBuilder = new(
            "c1", wasmArch, MockExecutionManagerBuilder.DefaultAllocationRange, isWasm: true);
        var range = emBuilder.AllocateJittedCodeRange(VirtualIPRangeStart, 0x400);
        MockReadyToRunInfo info = emBuilder.AddReadyToRunInfo([0x80, RootBeginAddress, 0x8000_0000 | FuncletBeginAddress], []);
        info.MinVirtualIP = VirtualIPRangeStart;
        info.LoadedImageBase = LoadedImageBase;
        new MockHashMapBuilder(emBuilder.Builder).PopulatePtrMap(
            info.EntryPointToMethodDescMapAddress, [(VirtualIPRangeStart + RootBeginAddress, MethodDescAddress)]);
        MockLoaderModule module = emBuilder.AddReadyToRunModule(info.Address);
        emBuilder.AddVirtualIPRangeSection(range, JitManagerAddress, module.Address);
        emBuilder.SetDebugInfoSection(info, DebugInfoRva, 5);
        emBuilder.Builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = LoadedImageBase + DebugInfoRva,
            // NativeArray: three entries, one-byte block offset, leaf for index 1, no lookback.
            Data = [0x18, 0x01, 0x08, 0x00, ExpectedDebugInfoByte],
            Name = "WASM debug info",
        });
        Dictionary<DataType, Target.TypeInfo> exceptionTypes =
            AddWasmExceptionInfo(
                emBuilder,
                info,
                LoadedImageBase,
                RootBeginAddress,
                filter,
                FuncletBeginAddress - RootBeginAddress);

        MethodDescHandle methodHandle = new(new TargetPointer(MethodDescAddress));
        TargetPointer methodTable = new(0x0102_0000);
        ITypeHandle typeHandle = new TargetTypeHandle(methodTable);
        Mock<IRuntimeTypeSystem> rts = new(MockBehavior.Strict);
        rts.Setup(r => r.GetMethodDescHandle(new TargetPointer(MethodDescAddress))).Returns(methodHandle);
        rts.Setup(r => r.GetMethodTable(methodHandle)).Returns(methodTable);
        rts.Setup(r => r.GetTypeHandle(methodTable)).Returns(typeHandle);
        rts.Setup(r => r.GetModule(typeHandle)).Returns(new TargetPointer(module.Address));
        Target target = CreateTarget(emBuilder, RuntimeInfoOperatingSystem.Windows, RuntimeInfoArchitecture.Wasm,
            targetBuilder => targetBuilder.AddTypes(exceptionTypes).AddMockContract(rts));
        IExecutionManager em = target.Contracts.ExecutionManager;

        CodeBlockHandle? handle = em.GetCodeBlockHandle(
            new TargetCodePointer(VirtualIPRangeStart + (funclet ? FuncletBeginAddress : RootBeginAddress)));
        Assert.NotNull(handle);
        TargetPointer debugInfo = em.GetDebugInfo(handle.Value, out bool hasFlagByte);
        Assert.False(hasFlagByte);
        Assert.Equal(new TargetPointer(LoadedImageBase + DebugInfoRva + 4), debugInfo);
        Assert.Equal(ExpectedDebugInfoByte, target.Read<byte>(debugInfo));
        ExceptionClauseInfo clause = Assert.Single(em.GetExceptionClauses(handle.Value));
        Assert.Equal(
            filter ? ExceptionClauseInfo.ExceptionClauseFlags.Filter : ExceptionClauseInfo.ExceptionClauseFlags.Finally,
            clause.ClauseType);
        Assert.Equal(2u, clause.TryStartPC);
        Assert.Equal(16u, clause.TryEndPC);
        Assert.Equal(filter ? 48u : FuncletBeginAddress - RootBeginAddress, clause.HandlerStartPC);
        Assert.Equal(filter ? 64u : 48u, clause.HandlerEndPC);
        Assert.Null(clause.ClassToken);
        Assert.Equal(filter ? FuncletBeginAddress - RootBeginAddress + 2 : null, clause.FilterOffset);
        Assert.Equal(filter, em.IsFilterFunclet(handle.Value));
    }

    private static Dictionary<DataType, Target.TypeInfo> AddWasmExceptionInfo(
        MockExecutionManagerBuilder emBuilder,
        MockReadyToRunInfo info,
        uint imageBase,
        uint methodRva,
        bool filter,
        uint funcletOffset)
    {
        const uint CoreInfoRva = 0x200;
        const uint CoreHeaderRva = 0x210;
        const uint ExceptionTableRva = 0x300;
        const uint ClauseRva = 0x340;
        const uint ClauseSize = 24;
        Dictionary<DataType, Target.TypeInfo> types = [];
        info.Composite = imageBase + CoreInfoRva;
        AddWords(imageBase + CoreInfoRva, DataType.ReadyToRunCoreInfo,
            [new("Header", DataType.pointer)], [imageBase + CoreHeaderRva]);
        AddWords(imageBase + CoreHeaderRva, DataType.ReadyToRunCoreHeader,
            [new("NumberOfSections", DataType.uint32)], [1]);
        AddWords(imageBase + CoreHeaderRva + 4, DataType.ReadyToRunSection,
            [new("Type", DataType.uint32), new("Section", DataType.ImageDataDirectory, 8)],
            [104, ExceptionTableRva, 16]);
        AddWords(imageBase + ExceptionTableRva, DataType.ExceptionLookupTableEntry,
            [new("MethodStartRVA", DataType.uint32), new("ExceptionInfoRVA", DataType.uint32)],
            [methodRva, ClauseRva, uint.MaxValue, ClauseRva + ClauseSize]);
        AddWords(imageBase + ClauseRva, DataType.R2RExceptionClause,
            [new("Flags", DataType.uint32), new("TryStartPC", DataType.uint32), new("TryEndPC", DataType.uint32),
             new("HandlerStartPC", DataType.uint32), new("HandlerEndPC", DataType.uint32), new("ClassToken", DataType.uint32)],
            filter
                ? [1, 2, 16, 48, 64, funcletOffset + 2]
                : [2, 2, 16, funcletOffset, 48, 0]);
        return types;

        void AddWords(uint address, DataType type, TargetTestHelpers.Field[] fields, uint[] words)
        {
            TargetTestHelpers helpers = emBuilder.Builder.TargetTestHelpers;
            var layout = helpers.LayoutFields(TargetTestHelpers.FieldLayout.Packed, fields);
            types[type] = new Target.TypeInfo { Fields = layout.Fields, Size = layout.Stride };
            byte[] data = new byte[words.Length * sizeof(uint)];
            for (int i = 0; i < words.Length; i++)
                helpers.Write(data.AsSpan(i * sizeof(uint), sizeof(uint)), words[i]);
            emBuilder.Builder.AddHeapFragment(new MockMemorySpace.HeapFragment
            {
                Address = address,
                Data = data,
                Name = type.ToString(),
            });
        }
    }

    [Fact]
    public void GetCodeKind_R2R_WasmVirtualIPMissingFromList_DoesNotUseRangeSectionMap()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong VirtualIPRangeStart = 0x8001_0001;
        const uint VirtualIPRangeSize = 0x400;
        const ulong JitManagerAddress = 0x000b_ff00;

        IExecutionManager em = CreateExecutionManagerContract(
            "c1",
            wasmArch,
            emBuilder =>
            {
                MockExecutionManagerBuilder.JittedCodeRange falseMappedRange =
                    emBuilder.AllocateJittedCodeRange(VirtualIPRangeStart, VirtualIPRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([0x100], []);
                r2rInfo.MinVirtualIP = VirtualIPRangeStart;
                r2rInfo.LoadedImageBase = 0x0090_0000;
                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection =
                    emBuilder.AddReadyToRunRangeSection(falseMappedRange, JitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(falseMappedRange, rangeSection.Address);
            },
            targetArchitecture: RuntimeInfoArchitecture.Wasm);

        TargetCodePointer virtualIP = new(VirtualIPRangeStart + 0x100);
        Assert.Null(em.GetCodeBlockHandle(virtualIP));
        Assert.Equal(CodeKind.Unknown, em.GetCodeKind(virtualIP));
        Assert.Equal(TargetPointer.Null, em.FindReadyToRunModule(virtualIP.AsTargetPointer));
    }

    [Theory]
    [InlineData("self-cycle")]
    [InlineData("two-node-cycle")]
    [InlineData("inverted-range")]
    [InlineData("null-module")]
    [InlineData("overlap")]
    public void GetCodeKind_R2R_WasmVirtualIPCorruptList_FailsClosed(string corruption)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong VirtualIPRangeStart = 0x8001_0001;
        const uint VirtualIPRangeSize = 0x200;
        const ulong ProbeVirtualIP = VirtualIPRangeStart + 0x10;
        const ulong JitManagerAddress = 0x000b_ff00;

        IExecutionManager em = CreateExecutionManagerContract(
            "c1",
            wasmArch,
            emBuilder =>
            {
                MockExecutionManagerBuilder.JittedCodeRange virtualIPRange =
                    emBuilder.AllocateJittedCodeRange(VirtualIPRangeStart, VirtualIPRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([0], []);
                r2rInfo.MinVirtualIP = VirtualIPRangeStart;
                r2rInfo.LoadedImageBase = 0x0090_0000;
                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);

                switch (corruption)
                {
                    case "self-cycle":
                    {
                        MockVirtualIPRangeSection node =
                            emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address);
                        node.Next = node.Address;
                        break;
                    }
                    case "two-node-cycle":
                    {
                        MockVirtualIPRangeSection first =
                            emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address, registerAsHead: false);
                        MockVirtualIPRangeSection second =
                            emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address, first.Address, registerAsHead: false);
                        first.Next = second.Address;
                        emBuilder.SetVirtualIPRangeListHead(first.Address);
                        break;
                    }
                    case "inverted-range":
                    {
                        MockVirtualIPRangeSection node =
                            emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address);
                        MockRangeSection range = emBuilder.GetRangeSection(node);
                        range.RangeBegin = range.RangeEndOpen;
                        break;
                    }
                    case "null-module":
                        _ = emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModuleAddress: 0);
                        break;
                    case "overlap":
                    {
                        MockVirtualIPRangeSection first =
                            emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address, registerAsHead: false);
                        _ = emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address, first.Address);
                        break;
                    }
                    default:
                        throw new InvalidOperationException(corruption);
                }
            },
            targetArchitecture: RuntimeInfoArchitecture.Wasm);

        Assert.Null(em.GetCodeBlockHandle(new TargetCodePointer(ProbeVirtualIP)));
        Assert.Equal(CodeKind.Unknown, em.GetCodeKind(new TargetCodePointer(ProbeVirtualIP)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetMethodDesc_R2R_WasmVirtualIPUnrelatedRegistration(bool registrationComplete)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const ulong InitializedRangeStart = 0x8001_0001;
        const ulong PendingRangeStart = 0x8002_0001;
        const ulong MethodDescAddress = 0x0101_aaa0;
        const ulong JitManagerAddress = 0x000b_ff00;
        ulong initializedModule = 0;

        IExecutionManager em = CreateExecutionManagerContract("c1", wasmArch, emBuilder =>
        {
            var initializedRange = emBuilder.AllocateJittedCodeRange(InitializedRangeStart, 0x200);
            MockReadyToRunInfo initializedInfo = emBuilder.AddReadyToRunInfo([0], []);
            initializedInfo.MinVirtualIP = InitializedRangeStart;
            initializedInfo.LoadedImageBase = 0x0090_0000;
            new MockHashMapBuilder(emBuilder.Builder).PopulatePtrMap(
                initializedInfo.EntryPointToMethodDescMapAddress, [(InitializedRangeStart, MethodDescAddress)]);
            initializedModule = emBuilder.AddReadyToRunModule(initializedInfo.Address).Address;
            MockVirtualIPRangeSection tail = emBuilder.AddVirtualIPRangeSection(
                initializedRange, JitManagerAddress, initializedModule, registerAsHead: false);

            var pendingRange = emBuilder.AllocateJittedCodeRange(PendingRangeStart, 0x200);
            MockReadyToRunInfo pendingInfo = emBuilder.AddReadyToRunInfo([0], []);
            pendingInfo.MinVirtualIP = registrationComplete ? PendingRangeStart : 0;
            pendingInfo.LoadedImageBase = 0x00a0_0000;
            MockLoaderModule pendingModule = emBuilder.AddReadyToRunModule(pendingInfo.Address);
            emBuilder.AddVirtualIPRangeSection(pendingRange, JitManagerAddress, pendingModule.Address, tail.Address);
        }, targetArchitecture: RuntimeInfoArchitecture.Wasm);

        CodeBlockHandle? handle = em.GetCodeBlockHandle(new TargetCodePointer(InitializedRangeStart));
        Assert.NotNull(handle);
        Assert.Equal(new TargetPointer(MethodDescAddress), em.GetMethodDesc(handle.Value));
        Assert.Equal(new TargetPointer(initializedModule), em.FindReadyToRunModule(new TargetPointer(InitializedRangeStart)));
        Assert.Equal(registrationComplete ? CodeKind.ReadyToRun : CodeKind.Unknown,
            em.GetCodeKind(new TargetCodePointer(PendingRangeStart)));
    }

    [Theory]
    [InlineData(1024, false)]
    [InlineData(1025, false)]
    [InlineData(2, true)]
    [InlineData(1025, true)]
    public void FindReadyToRunModule_WasmVirtualIPLongList(int count, bool cycle)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const ulong FirstRangeStart = 0x8001_0001;
        const uint RangeSize = 0x200;
        const ulong JitManagerAddress = 0x000b_ff00;
        ulong firstModule = 0;
        ulong lastModule = 0;

        IExecutionManager em = CreateExecutionManagerContract("c1", wasmArch, emBuilder =>
        {
            ulong head = 0;
            MockVirtualIPRangeSection? tail = null;
            for (int i = 0; i < count; i++)
            {
                ulong rangeStart = FirstRangeStart + (ulong)i * RangeSize;
                var range = emBuilder.AllocateJittedCodeRange(rangeStart, RangeSize);
                MockReadyToRunInfo info = emBuilder.AddReadyToRunInfo([0], []);
                info.MinVirtualIP = rangeStart;
                info.LoadedImageBase = 0x0090_0000 + (ulong)i * RangeSize;
                MockLoaderModule module = emBuilder.AddReadyToRunModule(info.Address);
                MockVirtualIPRangeSection node = emBuilder.AddVirtualIPRangeSection(
                    range, JitManagerAddress, module.Address, head, registerAsHead: false);
                head = node.Address;
                if (i == 0)
                {
                    tail = node;
                    firstModule = module.Address;
                }
                lastModule = module.Address;
            }
            if (cycle)
                tail!.Next = head;
            emBuilder.SetVirtualIPRangeListHead(head);
        }, targetArchitecture: RuntimeInfoArchitecture.Wasm);

        Assert.Equal(new TargetPointer(cycle ? 0 : lastModule),
            em.FindReadyToRunModule(new TargetPointer(FirstRangeStart + (ulong)(count - 1) * RangeSize)));
        Assert.Equal(new TargetPointer(cycle ? 0 : firstModule),
            em.FindReadyToRunModule(new TargetPointer(FirstRangeStart)));
        Assert.Equal(TargetPointer.Null,
            em.FindReadyToRunModule(new TargetPointer(FirstRangeStart + (ulong)count * RangeSize)));
    }

    [Fact]
    public void GetMethodDesc_R2R_WasmFuncletUsesMaskedBeginAddressAndControllingMethodDesc()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong VirtualIPRangeStart = 0x8001_0001;
        const uint RootBeginAddress = 0x100;
        const uint FuncletBeginAddress = 0x120;
        const uint FlaggedFuncletBeginAddress = 0x8000_0120;
        const ulong MethodDescAddress = 0x0101_aaa0;
        const ulong JitManagerAddress = 0x000b_ff00;
        ulong runtimeFunctionsAddress = 0;
        int runtimeFunctionSize = 0;

        IExecutionManager em = CreateExecutionManagerContract(
            "c1",
            wasmArch,
            emBuilder =>
            {
                MockExecutionManagerBuilder.JittedCodeRange virtualIPRange =
                    emBuilder.AllocateJittedCodeRange(VirtualIPRangeStart, 0x400);
                MockReadyToRunInfo r2rInfo =
                    emBuilder.AddReadyToRunInfo([RootBeginAddress, FlaggedFuncletBeginAddress, 0x180], []);
                r2rInfo.MinVirtualIP = VirtualIPRangeStart;
                r2rInfo.LoadedImageBase = 0x0090_0000;
                runtimeFunctionsAddress = r2rInfo.RuntimeFunctions;
                runtimeFunctionSize = emBuilder.RuntimeFunctionLayout.Size;
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(
                    r2rInfo.EntryPointToMethodDescMapAddress,
                    [(VirtualIPRangeStart + RootBeginAddress, MethodDescAddress)]);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                _ = emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, r2rModule.Address);
            },
            targetArchitecture: RuntimeInfoArchitecture.Wasm);

        CodeBlockHandle? root = em.GetCodeBlockHandle(new TargetCodePointer(VirtualIPRangeStart + RootBeginAddress));
        CodeBlockHandle? funclet = em.GetCodeBlockHandle(new TargetCodePointer(VirtualIPRangeStart + FuncletBeginAddress + 2));

        Assert.NotNull(root);
        Assert.NotNull(funclet);
        Assert.Equal(new TargetPointer(MethodDescAddress), em.GetMethodDesc(root.Value));
        Assert.Equal(new TargetPointer(MethodDescAddress), em.GetMethodDesc(funclet.Value));
        Assert.False(em.IsFunclet(root.Value));
        Assert.True(em.IsFunclet(funclet.Value));
        Assert.Equal(em.GetStartAddress(root.Value), em.GetStartAddress(funclet.Value));
        Assert.Equal(new TargetPointer(VirtualIPRangeStart + FuncletBeginAddress), em.GetFuncletStartAddress(funclet.Value));
        Assert.Equal(new TargetPointer(runtimeFunctionsAddress + (ulong)runtimeFunctionSize), em.GetUnwindInfo(funclet.Value));
    }

    [Theory]
    [InlineData(65_536, true)]
    [InlineData(65_537, false)]
    public void FindReadyToRunModule_WasmVirtualIPTraversalBudget(int count, bool withinBudget)
    {
        const int NodeBudget = 65_536;
        const ulong FirstRangeStart = 0x8001_0001;
        const uint RangeSize = 0x200;
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        var allocationRange = MockExecutionManagerBuilder.DefaultAllocationRange with
        {
            ExecutionManagerStart = 0x0100_0000,
            ExecutionManagerEnd = 0x0400_0000,
        };
        MockExecutionManagerBuilder emBuilder = new("c1", wasmArch, allocationRange, isWasm: true);
        (ulong nodes, ulong firstModule, ulong lastModule) =
            emBuilder.AddVirtualIPRangeSectionArray(count, FirstRangeStart, RangeSize, 0x000b_ff00);
        int nodeSize = emBuilder.VirtualIPRangeSectionLayout.Size;
        int highestNodeRead = -1;
        MockMemorySpace.MemoryContext memory = emBuilder.Builder.GetMemoryContext();
        Target target = CreateTarget(emBuilder, RuntimeInfoOperatingSystem.Windows, RuntimeInfoArchitecture.Wasm,
            targetBuilder => targetBuilder.UseReader((address, buffer) =>
            {
                if (address >= nodes && address < nodes + (ulong)(count * nodeSize))
                    highestNodeRead = Math.Max(highestNodeRead, (int)((address - nodes) / (ulong)nodeSize));
                return memory.ReadFromTarget(address, buffer);
            }));
        IExecutionManager em = target.Contracts.ExecutionManager;

        TargetPointer firstResult = em.FindReadyToRunModule(new TargetPointer(FirstRangeStart));
        Assert.Equal(NodeBudget - 1, highestNodeRead);
        Assert.Equal(new TargetPointer(withinBudget ? firstModule : 0), firstResult);
        Assert.Equal(new TargetPointer(withinBudget ? lastModule : 0),
            em.FindReadyToRunModule(new TargetPointer(FirstRangeStart + (ulong)(count - 1) * RangeSize)));
        Assert.Equal(NodeBudget - 1, highestNodeRead);
        Assert.Equal(withinBudget ? CodeKind.ReadyToRun : CodeKind.Unknown,
            em.GetCodeKind(new TargetCodePointer(FirstRangeStart)));
        Assert.Equal(NodeBudget - 1, highestNodeRead);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_R2R_MultipleRuntimeFunctions(string version, MockTarget.Architecture arch)
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
    public void GetMethodDesc_R2R_HotColdBlock(string version, MockTarget.Architecture arch)
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
    public void GetUnwindInfoBaseAddress_R2R_ManyRuntimeFunction(string version, MockTarget.Architecture arch)
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
    public void GetMethodDesc_CollectibleFragmentNext(string version, MockTarget.Architecture arch)
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
    public void GetJitManagerInfo_EE_ReturnsManagerAddress(string version, MockTarget.Architecture arch)
    {
        ulong expectedManagerAddress = 0;
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder => expectedManagerAddress = emBuilder.EEJitManagerAddress);
        JitManagerInfo info = em.GetJitManagerInfo(JitManagerKind.EE)!.Value;
        Assert.Equal(new TargetPointer(expectedManagerAddress), info.ManagerAddress);
        Assert.Equal(0u, info.CodeType);
        Assert.Equal(TargetPointer.Null, info.HeapListAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetJitManagerInfo_EE_WithCodeHeaps(string version, MockTarget.Architecture arch)
    {
        const ulong expectedHeapList = 0x0099_aa00;
        ulong expectedManagerAddress = 0;
        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder => expectedManagerAddress = emBuilder.EEJitManagerAddress,
            allCodeHeaps: expectedHeapList);
        JitManagerInfo info = em.GetJitManagerInfo(JitManagerKind.EE)!.Value;
        Assert.Equal(new TargetPointer(expectedManagerAddress), info.ManagerAddress);
        Assert.Equal(0u, info.CodeType);
        Assert.Equal(new TargetPointer(expectedHeapList), info.HeapListAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_LoaderCodeHeap(string version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        MockLoaderCodeHeap heap = emBuilder.AddLoaderCodeHeap();
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        ICodeHeapInfo info = em.GetCodeHeapInfos(JitManagerKind.EE).Single();
        Assert.IsType<LoaderCodeHeapInfo>(info);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_HostCodeHeap(string version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        TargetPointer baseAddr    = new(0x0001_0000);
        TargetPointer currentAddr = new(0x0001_8000);
        MockHostCodeHeap heap = emBuilder.AddHostCodeHeap(baseAddr.Value, currentAddr.Value);
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        ICodeHeapInfo info = em.GetCodeHeapInfos(JitManagerKind.EE).Single();
        Assert.IsType<HostCodeHeapInfo>(info);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_LoaderCodeHeap_ReturnsLoaderHeapAddress(string version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        MockLoaderCodeHeap heap = emBuilder.AddLoaderCodeHeap();
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        LoaderCodeHeapInfo loader = Assert.IsType<LoaderCodeHeapInfo>(em.GetCodeHeapInfos(JitManagerKind.EE).Single());
        Target.TypeInfo loaderCodeHeapType = TargetTestHelpers.CreateTypeInfo(emBuilder.LoaderCodeHeapLayout);
        ulong loaderHeapFieldOffset = (ulong)loaderCodeHeapType.Fields[nameof(Data.LoaderCodeHeap.LoaderHeap)].Offset;
        Assert.Equal(new TargetPointer(heap.Address + loaderHeapFieldOffset), loader.LoaderHeapAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfo_HostCodeHeap_ReturnsAddresses(string version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        TargetPointer expectedBase    = new(0x0002_0000);
        TargetPointer expectedCurrent = new(0x0002_4000);
        MockHostCodeHeap heap = emBuilder.AddHostCodeHeap(expectedBase.Value, expectedCurrent.Value);
        LinkHeapIntoAllCodeHeaps(emBuilder, heap.Address);
        var target = CreateTarget(emBuilder);

        var em = target.Contracts.ExecutionManager;
        HostCodeHeapInfo host = Assert.IsType<HostCodeHeapInfo>(em.GetCodeHeapInfos(JitManagerKind.EE).Single());
        Assert.Equal(expectedBase, host.BaseAddress);
        Assert.Equal(expectedCurrent, host.CurrentAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapList_SingleNode(string version, MockTarget.Architecture arch)
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

        List<ICodeHeapInfo> heapInfos = em.GetCodeHeapInfos(JitManagerKind.EE).ToList();
        Assert.Single(heapInfos);
        Assert.IsType<LoaderCodeHeapInfo>(heapInfos[0]);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapList_LinkedList_TwoNodes(string version, MockTarget.Architecture arch)
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

        List<ICodeHeapInfo> heapInfos = em.GetCodeHeapInfos(JitManagerKind.EE).ToList();
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

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetJitManagerInfo_Interpreter(string version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        const ulong HeapListAddress = 0x0012_3400;
        emBuilder.SetInterpreterCodeHeaps(HeapListAddress);

        IExecutionManager executionManager = CreateTarget(emBuilder).Contracts.ExecutionManager;
        JitManagerInfo info = Assert.IsType<JitManagerInfo>(
            executionManager.GetJitManagerInfo(JitManagerKind.Interpreter));

        Assert.Equal(new TargetPointer(emBuilder.InterpreterJitManagerAddress), info.ManagerAddress);
        Assert.Equal(2u, info.CodeType);
        Assert.Equal(new TargetPointer(HeapListAddress), info.HeapListAddress);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetJitManagerInfo_NoInterpreter(string version, MockTarget.Architecture arch)
    {
        IExecutionManager executionManager = CreateExecutionManagerContract(version, arch);

        Assert.Null(executionManager.GetJitManagerInfo(JitManagerKind.Interpreter));
        Assert.Empty(executionManager.GetCodeHeapInfos(JitManagerKind.Interpreter));
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeHeapInfos_SelectsJitManager(string version, MockTarget.Architecture arch)
    {
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        MockLoaderCodeHeap eeHeap = emBuilder.AddLoaderCodeHeap();
        MockHostCodeHeap interpreterHeap = emBuilder.AddHostCodeHeap(0x0005_0000, 0x0005_8000);

        MockCodeHeapListNode eeNode = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: 0x1000_0000,
            endAddress: 0x1000_1000,
            mapBase: 0,
            headerMap: 0,
            heap: eeHeap.Address);
        MockCodeHeapListNode interpreterNode = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: 0x2000_0000,
            endAddress: 0x2000_1000,
            mapBase: 0,
            headerMap: 0,
            heap: interpreterHeap.Address);
        emBuilder.SetAllCodeHeaps(eeNode.Address);
        emBuilder.SetInterpreterCodeHeaps(interpreterNode.Address);

        IExecutionManager executionManager = CreateTarget(emBuilder).Contracts.ExecutionManager;

        LoaderCodeHeapInfo eeHeapInfo = Assert.IsType<LoaderCodeHeapInfo>(
            executionManager.GetCodeHeapInfos(JitManagerKind.EE).Single());
        HostCodeHeapInfo interpreterHeapInfo = Assert.IsType<HostCodeHeapInfo>(
            executionManager.GetCodeHeapInfos(JitManagerKind.Interpreter).Single());

        Assert.Equal(eeHeap.Address, eeHeapInfo.HeapAddress.Value);
        Assert.Equal(interpreterHeap.Address, interpreterHeapInfo.HeapAddress.Value);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetStubKind_NoRangeSection(string version, MockTarget.Architecture arch)
    {
        IExecutionManager em = CreateExecutionManagerContract(version, arch);

        Assert.Equal(CodeKind.Unknown, em.GetCodeKind(new TargetCodePointer(0x00aa_9000)));
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetStubKind_GlobalStub(string version, MockTarget.Architecture arch)
    {
        (string Name, ulong Address, CodeKind Kind)[] stubs =
        [
            (Constants.Globals.ThePreStub, 0x00aa_1000, CodeKind.ThePreStub),
        ];
        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        Target target = CreateTarget(
            emBuilder,
            stubs.Select(stub => (stub.Name, emBuilder.AddPointerGlobal(stub.Address, stub.Name))).ToArray());

        foreach ((_, ulong address, CodeKind kind) in stubs)
        {
            Assert.Equal(kind, target.Contracts.ExecutionManager.GetCodeKind(new TargetCodePointer(address)));
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetStubKind_RangeListStubs(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0x4000u;
        const ulong jitManagerAddress = 0x000b_ff00;
        const int stubCodeBlockKindPrecode = 3; // STUB_CODE_BLOCK_STUBPRECODE

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockRangeSection rangeSection = emBuilder.AddRangeListRangeSection(jittedCode, jitManagerAddress, stubCodeBlockKindPrecode);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        CodeKind kind = em.GetCodeKind(new TargetCodePointer(codeRangeStart + 0x100));
        Assert.Equal(CodeKind.StubPrecode, kind);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetStubKind_CodeHeapStubs(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint stubSize = 0x20;
        const ulong jitManagerAddress = 0x000b_ff00;
        const int stubCodeBlockKindJumpStub = 1; // STUB_CODE_BLOCK_JUMPSTUB
        const int stubCodeBlockKindWrapperStub = 10; // STUB_CODE_BLOCK_WRAPPER_STUB
        const int stubCodeBlockKindShuffleThunk = 11; // STUB_CODE_BLOCK_SHUFFLE_THUNK
        (int StubCodeBlockKind, CodeKind CodeKind, ulong CodeAddress)[] stubs =
        [
            (stubCodeBlockKindJumpStub, CodeKind.JumpStub, 0),
            (stubCodeBlockKindWrapperStub, CodeKind.WrapperStub, 0),
            (stubCodeBlockKindShuffleThunk, CodeKind.ShuffleThunk, 0),
        ];

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

                NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
                for (int i = 0; i < stubs.Length; i++)
                {
                    MockJittedMethod stub = emBuilder.AddStubCodeBlock(jittedCode, stubSize, stubs[i].StubCodeBlockKind);
                    stubs[i].CodeAddress = stub.CodeAddress;
                    nibBuilder.AllocateCodeChunk(new TargetCodePointer(stub.CodeAddress), stubSize);
                }

                MockCodeHeapListNode codeHeapListNode = emBuilder.AddCodeHeapListNode(0, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
                MockRangeSection rangeSection = emBuilder.AddRangeSection(jittedCode, jitManagerAddress, codeHeapListNode.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        foreach ((_, CodeKind expected, ulong codeAddress) in stubs)
        {
            CodeKind kind = em.GetCodeKind(new TargetCodePointer(codeAddress));
            Assert.Equal(expected, kind);
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetStubKind_ManagedCode(string version, MockTarget.Architecture arch)
    {
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
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        CodeKind kind = em.GetCodeKind(new TargetCodePointer(methodStart));
        Assert.Equal(CodeKind.Jitted, kind);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetStubKind_R2R_MethodCallThunk(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const ulong jitManagerAddress = 0x000b_ff00;
        const uint thunkRva = 0x2000;
        const uint thunkSize = 0x100;

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([], []);
                emBuilder.SetDelayLoadMethodCallThunks(r2rInfo, thunkRva, thunkSize);
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(r2rInfo.EntryPointToMethodDescMapAddress, []);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        CodeKind kind = em.GetCodeKind(new TargetCodePointer(codeRangeStart + thunkRva + 0x10));
        Assert.Equal(CodeKind.MethodCallThunk, kind);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetStubKind_R2R_OutsideThunkRange(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const ulong jitManagerAddress = 0x000b_ff00;
        const uint thunkRva = 0x2000;
        const uint thunkSize = 0x100;

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
                MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([], []);
                emBuilder.SetDelayLoadMethodCallThunks(r2rInfo, thunkRva, thunkSize);
                MockHashMapBuilder hashMapBuilder = new(emBuilder.Builder);
                hashMapBuilder.PopulatePtrMap(r2rInfo.EntryPointToMethodDescMapAddress, []);

                MockLoaderModule r2rModule = emBuilder.AddReadyToRunModule(r2rInfo.Address);
                MockRangeSection rangeSection = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule.Address);
                _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSection.Address);
            });

        CodeKind kind = em.GetCodeKind(new TargetCodePointer(codeRangeStart + thunkRva + thunkSize + 0x10));
        Assert.Equal(CodeKind.ReadyToRun, kind);
    }

    public static IEnumerable<object[]> StdArchAllVersions()
    {
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            yield return new object[] { "c1", arch };
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_InterpreterOneMethod(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x200;

        const ulong jitManagerAddress = 0x000b_ff00;
        const ulong expectedMethodDescAddress = 0x0101_bbb0;

        ulong methodStart = 0;

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var interpCodeRange = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

                methodStart = emBuilder.AddInterpretedMethod(interpCodeRange, methodSize, expectedMethodDescAddress).CodeAddress;

                NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
                nibBuilder.AllocateCodeChunk(new TargetCodePointer(methodStart), methodSize);

                MockCodeHeapListNode codeHeapListNode = emBuilder.AddCodeHeapListNode(0, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
                MockRangeSection rangeSection = emBuilder.AddInterpreterRangeSection(interpCodeRange, jitManagerAddress, codeHeapListNode.Address);
                _ = emBuilder.AddRangeSectionFragment(interpCodeRange, rangeSection.Address);
            });

        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(methodStart));
        Assert.NotNull(eeInfo);
        TargetPointer actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);
        Assert.Equal(CodeKind.Interpreter, em.GetCodeKind(new TargetCodePointer(methodStart)));

        eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(methodStart + methodSize / 2));
        Assert.NotNull(eeInfo);
        actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(new TargetPointer(expectedMethodDescAddress), actualMethodDesc);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_InterpreterPrecode_ReturnsNull(string version, MockTarget.Architecture arch)
    {
        const ulong precodeRangeStart = 0x0b0b_0000u;
        const uint precodeRangeSize = 0x1000u;
        const ulong jitManagerAddress = 0x000b_ff00;
        const int stubCodeBlockKindPrecode = 4; // STUB_CODE_BLOCK_STUBPRECODE

        IExecutionManager em = CreateExecutionManagerContract(
            version,
            arch,
            emBuilder =>
            {
                var precodeRange = emBuilder.AllocateJittedCodeRange(precodeRangeStart, precodeRangeSize);
                MockRangeSection precodeRangeSection = emBuilder.AddRangeListRangeSection(precodeRange, jitManagerAddress, stubCodeBlockKindPrecode);
                _ = emBuilder.AddRangeSectionFragment(precodeRange, precodeRangeSection.Address);
            });

        // GetCodeBlockHandle should return null for a precode address.
        // Callers are responsible for resolving interpreter precodes via
        // PrecodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent before calling GetCodeBlockHandle.
        TargetCodePointer precodeAddress = new(precodeRangeStart + 0x100);
        var eeInfo = em.GetCodeBlockHandle(precodeAddress);
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetDynamicFunctionTableEntries_MultipleMethods(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x100;
        const ulong personalityRoutine = 0x00cc_0000u;

        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);

        MockExecutionManagerBuilder.JittedCodeRange jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);

        // Three methods with varying unwind info counts, allocated in ascending address order.
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m0 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_0000, [0x10]);
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m1 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_1000, [0x20, 0x40]);
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m2 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_2000, [0x80]);

        foreach (MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m in new[] { m0, m1, m2 })
            nibBuilder.AllocateCodeChunk(new TargetCodePointer(m.CodeAddress), methodSize);

        ulong endAddress = m2.CodeAddress + methodSize;
        ulong moduleBase = arch.Is64Bit ? personalityRoutine : codeRangeStart;

        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart,
            endAddress: endAddress,
            mapBase: codeRangeStart,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            clrPersonalityRoutine: personalityRoutine);
        emBuilder.SetAllCodeHeaps(node.Address);

        // Context carries flags in its low bits; the contract must strip them to find the JIT manager.
        MockDynamicFunctionTable table = emBuilder.AddDynamicFunctionTable(moduleBase, emBuilder.EEJitManagerAddress | 2);

        Target target = CreateTarget(emBuilder);
        IExecutionManager em = target.Contracts.ExecutionManager;

        uint rfSize = (uint)emBuilder.RuntimeFunctionLayout.Size;
        IReadOnlyList<TargetPointer> entries = em.GetDynamicFunctionTableEntries(new TargetPointer(table.Address));

        // Entries are ordered by descending method start address, ascending within each method.
        List<TargetPointer> expected = [];
        foreach (MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m in new[] { m2, m1, m0 })
        {
            for (uint i = 0; i < m.NumUnwindInfos; i++)
                expected.Add(new TargetPointer(m.UnwindInfosAddress + i * rfSize));
        }

        Assert.Equal(expected, entries);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetDynamicFunctionTableEntries_SkipsStubCodeBlocks(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x100;
        const ulong personalityRoutine = 0x00cc_0000u;
        const int stubCodeBlockKind = 4; // STUB_CODE_BLOCK_STUBPRECODE (<= StubCodeBlockLast)

        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);

        MockExecutionManagerBuilder.JittedCodeRange jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);

        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m0 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_0000, [0x10]);
        MockJittedMethod stub = emBuilder.AddStubCodeBlock(jittedCode, methodSize, stubCodeBlockKind);
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m1 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_1000, [0x20]);

        nibBuilder.AllocateCodeChunk(new TargetCodePointer(m0.CodeAddress), methodSize);
        nibBuilder.AllocateCodeChunk(new TargetCodePointer(stub.CodeAddress), methodSize);
        nibBuilder.AllocateCodeChunk(new TargetCodePointer(m1.CodeAddress), methodSize);

        ulong endAddress = m1.CodeAddress + methodSize;
        ulong moduleBase = arch.Is64Bit ? personalityRoutine : codeRangeStart;

        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart,
            endAddress: endAddress,
            mapBase: codeRangeStart,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            clrPersonalityRoutine: personalityRoutine);
        emBuilder.SetAllCodeHeaps(node.Address);

        MockDynamicFunctionTable table = emBuilder.AddDynamicFunctionTable(moduleBase, emBuilder.EEJitManagerAddress);

        Target target = CreateTarget(emBuilder);
        IExecutionManager em = target.Contracts.ExecutionManager;

        uint rfSize = (uint)emBuilder.RuntimeFunctionLayout.Size;
        IReadOnlyList<TargetPointer> entries = em.GetDynamicFunctionTableEntries(new TargetPointer(table.Address));

        // The stub in the middle contributes no entries; only the two real methods do.
        List<TargetPointer> expected =
        [
            new TargetPointer(m1.UnwindInfosAddress),
            new TargetPointer(m0.UnwindInfosAddress),
        ];
        Assert.Equal(expected, entries);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetDynamicFunctionTableEntries_NoMatchingHeap_ReturnsEmpty(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x100;
        const ulong personalityRoutine = 0x00cc_0000u;

        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);

        MockExecutionManagerBuilder.JittedCodeRange jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m0 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_0000, [0x10]);
        nibBuilder.AllocateCodeChunk(new TargetCodePointer(m0.CodeAddress), methodSize);

        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart,
            endAddress: m0.CodeAddress + methodSize,
            mapBase: codeRangeStart,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            clrPersonalityRoutine: personalityRoutine);
        emBuilder.SetAllCodeHeaps(node.Address);

        // MinimumAddress matches no code heap's module base.
        MockDynamicFunctionTable table = emBuilder.AddDynamicFunctionTable(0xdead_0000, emBuilder.EEJitManagerAddress);

        Target target = CreateTarget(emBuilder);
        IExecutionManager em = target.Contracts.ExecutionManager;

        IReadOnlyList<TargetPointer> entries = em.GetDynamicFunctionTableEntries(new TargetPointer(table.Address));
        Assert.Empty(entries);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetDynamicFunctionTableEntries_NullPersonalityRoutine_FallsBackToMapBase(string version, MockTarget.Architecture arch)
    {
        // When the personality routine is null (e.g. an interpreter code heap on 64-bit, or any
        // 32-bit heap where the field is absent), the module base falls back to the map base -
        // matching HeapList::GetModuleBase and the value used to register the function table.
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x100;

        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);

        MockExecutionManagerBuilder.JittedCodeRange jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m0 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_0000, [0x10]);
        nibBuilder.AllocateCodeChunk(new TargetCodePointer(m0.CodeAddress), methodSize);

        // Explicit zero personality routine; module base must resolve to MapBase (codeRangeStart).
        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart,
            endAddress: m0.CodeAddress + methodSize,
            mapBase: codeRangeStart,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            clrPersonalityRoutine: 0);
        emBuilder.SetAllCodeHeaps(node.Address);

        MockDynamicFunctionTable table = emBuilder.AddDynamicFunctionTable(codeRangeStart, emBuilder.EEJitManagerAddress);

        Target target = CreateTarget(emBuilder);
        IExecutionManager em = target.Contracts.ExecutionManager;

        IReadOnlyList<TargetPointer> entries = em.GetDynamicFunctionTableEntries(new TargetPointer(table.Address));
        Assert.Equal([new TargetPointer(m0.UnwindInfosAddress)], entries);
    }

    [Theory]
    [InlineData(RuntimeInfoOperatingSystem.Unix, RuntimeInfoArchitecture.X64)]
    [InlineData(RuntimeInfoOperatingSystem.Windows, RuntimeInfoArchitecture.X86)]
    public void GetDynamicFunctionTableEntries_UnsupportedPlatform_ReturnsEmpty(
        RuntimeInfoOperatingSystem operatingSystem,
        RuntimeInfoArchitecture architecture)
    {
        MockTarget.Architecture targetArchitecture = new() { IsLittleEndian = true, Is64Bit = true };
        MockExecutionManagerBuilder emBuilder = new("c1", targetArchitecture, MockExecutionManagerBuilder.DefaultAllocationRange);
        Target target = CreateTarget(emBuilder, operatingSystem, architecture);

        IReadOnlyList<TargetPointer> entries =
            target.Contracts.ExecutionManager.GetDynamicFunctionTableEntries(new TargetPointer(0xdead_beef));

        Assert.Empty(entries);
    }
}
