// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.Wasm;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class WasmR2RInfoTests
{
    // WASM is a 32-bit little-endian target.
    private static readonly MockTarget.Architecture WasmArch = new() { IsLittleEndian = true, Is64Bit = false };

    private const uint MinFunctionTableIndex = 5;
    private const uint FunctionTableIndex = 5; // localIndex 0
    private const ulong MinVirtualIP = 0x0005_0000;
    private const ulong LoadedImageBase = 0x0090_0000;
    private const ulong GCInfoBase = 0x00a0_0000;
    private const ulong RuntimeFunctionAddress = 0x0010_0000;
    private const uint FunctionBeginAddress = 0x100;
    private const uint FunctionUnwindData = 0x40;
    private const ulong PortableEntryPointAddress = 0x007f_0000;

    private static uint TableFrameIdentity(uint functionTableIndex)
        => checked(functionTableIndex * 2 + 1);

    private static uint CoreLibFrameIdentity(uint runtimeFunctionIndex)
        => ~(runtimeFunctionIndex << 1);

    private sealed class FakeExecutionManager : IExecutionManager
    {
        public TargetCodePointer ControlPC { get; set; }
        public TargetPointer RuntimeFunction { get; set; }
        public TargetPointer UnwindInfoBaseAddress { get; set; }

        public CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip)
            => ip == ControlPC ? new CodeBlockHandle(ip.AsTargetPointer) : null;

        public TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle) => RuntimeFunction;

        public TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle) => UnwindInfoBaseAddress;
    }

    // Builds a target whose FunctionTableIndexRangeList global points at a *slot* (pointer-to-pointer),
    // matching the CDAC_GLOBAL_POINTER contract. WasmR2RInfo must dereference the slot to reach the
    // list head; walking from the slot address directly reads garbage and finds nothing.
    private static TestPlaceholderTarget CreateTarget(
        bool emptyList = false,
        ulong minVirtualIP = MinVirtualIP,
        MockWasmRuntimeFunctions.Entry[]? functions = null,
        byte virtualIPLength = 3)
    {
        TargetTestHelpers helpers = new(WasmArch);
        var targetBuilder = new TestPlaceholderTarget.Builder(WasmArch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        var allocator = builder.CreateAllocator(0x0010_0000, 0x0080_0000);

        int hashMapStride = MockHashMap.CreateLayout(WasmArch).Size;
        var moduleLayout = MockLoaderModule.CreateLayout(WasmArch);
        var r2rInfoLayout = MockReadyToRunInfo.CreateLayout(WasmArch, hashMapStride, isWasm: true);
        var rangeSectionLayout = helpers.LayoutFields([
            new("MinFunctionTableIndex", DataType.uint32),
            new("NumRuntimeFunctions", DataType.uint32),
            new("R2RModule", DataType.pointer),
            new("Next", DataType.pointer),
        ]);

        functions ??= [new(FunctionBeginAddress, FunctionUnwindData)];
        (ulong runtimeFunctions, uint runtimeFunctionCount) = MockWasmRuntimeFunctions.Add(builder, allocator, functions);

        MockReadyToRunInfo r2rInfo = r2rInfoLayout.Create(allocator.Allocate((ulong)r2rInfoLayout.Size, "ReadyToRunInfo"));
        r2rInfo.CompositeInfo = r2rInfo.Address;
        r2rInfo.NumRuntimeFunctions = runtimeFunctionCount;
        r2rInfo.RuntimeFunctions = runtimeFunctions;
        r2rInfo.LoadedImageBase = LoadedImageBase;
        r2rInfo.GCInfoBase = GCInfoBase;
        r2rInfo.MinVirtualIP = minVirtualIP;

        MockLoaderModule module = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));
        module.ReadyToRunInfo = r2rInfo.Address;

        var sectionFrag = allocator.Allocate(rangeSectionLayout.Stride, "FunctionTableIndexRangeSection");
        var secFields = rangeSectionLayout.Fields;
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["MinFunctionTableIndex"].Offset, sizeof(uint)), MinFunctionTableIndex);
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["NumRuntimeFunctions"].Offset, sizeof(uint)), runtimeFunctionCount);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["R2RModule"].Offset, helpers.PointerSize), module.Address);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["Next"].Offset, helpers.PointerSize), 0ul);

        // The slot holds the pointer to the list head. The global points at the slot, not the head.
        var slotFrag = allocator.Allocate((uint)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(slotFrag.Data.AsSpan().Slice(0, helpers.PointerSize), emptyList ? 0ul : sectionFrag.Address);

        TargetTestHelpers.LayoutResult portableEntryPointLayout = helpers.LayoutFields([
            new(nameof(Data.PortableEntryPoint.ActualCode), DataType.pointer),
            new(nameof(Data.PortableEntryPoint.MethodDesc), DataType.pointer),
            new(nameof(Data.PortableEntryPoint.Flags), DataType.int32),
        ]);
        byte[] portableEntryPointData = new byte[portableEntryPointLayout.Stride];
        helpers.WritePointer(
            portableEntryPointData.AsSpan(
                portableEntryPointLayout.Fields[nameof(Data.PortableEntryPoint.ActualCode)].Offset,
                helpers.PointerSize),
            FunctionTableIndex);
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = PortableEntryPointAddress,
            Data = portableEntryPointData,
            Name = "PortableEntryPoint",
        });
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GCInfoBase + FunctionUnwindData,
            Data = [16, virtualIPLength],
            Name = "UnwindData",
        });

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(MockWasmRuntimeFunctions.CreateRuntimeFunctionLayout(WasmArch)),
            [DataType.WasmRuntimeFunctionGroup] = TargetTestHelpers.CreateTypeInfo(MockWasmRuntimeFunctions.CreateGroupLayout(WasmArch)),
            [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(r2rInfoLayout),
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(moduleLayout),
            [DataType.FunctionTableIndexRangeSection] = new() { Fields = rangeSectionLayout.Fields, Size = rangeSectionLayout.Stride },
            [DataType.PortableEntryPoint] = new() { Fields = portableEntryPointLayout.Fields, Size = portableEntryPointLayout.Stride },
        };

        return targetBuilder
            .AddTypes(types)
            .AddGlobals(("FunctionTableIndexRangeList", slotFrag.Address))
            .Build();
    }

    [Fact]
    public void TryGetVirtualIPBase_ResolvesThroughDereferencedGlobal()
    {
        WasmR2RInfo info = new(CreateTarget(), new FakeExecutionManager());

        Assert.True(info.TryGetVirtualIPBase(TableFrameIdentity(FunctionTableIndex), out ulong baseVirtualIP));
        // MinVirtualIP + RuntimeFunction.BeginAddress (non-funclet).
        Assert.Equal(MinVirtualIP + FunctionBeginAddress, baseVirtualIP);
    }

    [Fact]
    public void TryGetVirtualIPBase_PortableEntryPointIdentity_ResolvesActualCode()
    {
        WasmR2RInfo info = new(CreateTarget(), new FakeExecutionManager());

        Assert.True(info.TryGetVirtualIPBase((uint)PortableEntryPointAddress, out ulong baseVirtualIP));
        Assert.Equal(MinVirtualIP + FunctionBeginAddress, baseVirtualIP);
    }

    [Fact]
    public void TryGetVirtualIPBase_CoreLibIdentity_ResolvesRuntimeFunctionOrdinal()
    {
        WasmR2RInfo info = new(CreateTarget(minVirtualIP: SyntheticMinVirtualIP), new FakeExecutionManager());

        Assert.True(info.TryGetVirtualIPBase(CoreLibFrameIdentity(0), out ulong baseVirtualIP));
        Assert.Equal(SyntheticMinVirtualIP + FunctionBeginAddress, baseVirtualIP);
        Assert.False(info.TryGetVirtualIPBase(CoreLibFrameIdentity(1), out _));
    }

    [Theory]
    [InlineData((uint)PortableEntryPointAddress, MinVirtualIP)]
    [InlineData(FunctionTableIndex * 2 + 1, MinVirtualIP)]
    [InlineData(uint.MaxValue, 0x8000_0001ul)]
    public void SingleVirtualIP_FrameIdentitySuppliesUnwindExtent(uint frameIdentity, ulong minVirtualIP)
    {
        TestPlaceholderTarget target = CreateTarget(minVirtualIP: minVirtualIP, virtualIPLength: 2);

        Assert.True(new WasmR2RInfo(target, new FakeExecutionManager()).TryGetVirtualIPBase(
            frameIdentity, out ulong baseVirtualIP, out TargetPointer unwindData));
        Assert.Equal(minVirtualIP + FunctionBeginAddress, baseVirtualIP);
        Assert.Equal(GCInfoBase + FunctionUnwindData, unwindData.Value);
        Assert.Equal(2, target.Read<byte>(unwindData + 1));
    }

    [Fact]
    public void FunctionTableIndex_GroupPaddingIsSkippedForFuncletsAndRejectedAsAnIdentity()
    {
        MockWasmRuntimeFunctions.Entry[] functions = new MockWasmRuntimeFunctions.Entry[34];
        Array.Fill(functions, new MockWasmRuntimeFunctions.Entry(0, IsSkip: true));
        functions[0] = new(0x100, 0x40);
        functions[32] = new(0x120, 0x80, IsFunclet: true);
        functions[33] = new(0x180, 0x90);
        TestPlaceholderTarget target = CreateTarget(minVirtualIP: SyntheticMinVirtualIP, functions: functions);
        FakeExecutionManager executionManager = new()
        {
            ControlPC = new TargetCodePointer(SyntheticMinVirtualIP + 0x120),
            RuntimeFunction = new TargetPointer(RuntimeFunctionAddress + 32 * MockWasmRuntimeFunctions.EntrySize),
            UnwindInfoBaseAddress = new TargetPointer(GCInfoBase),
        };
        WasmR2RInfo info = new(target, executionManager);

        Assert.True(info.TryGetVirtualIPBase(TableFrameIdentity(MinFunctionTableIndex + 32), out ulong funcletBase));
        Assert.Equal(SyntheticMinVirtualIP + 0x100, funcletBase);
        Assert.True(info.TryGetVirtualIPBase(TableFrameIdentity(MinFunctionTableIndex + 33), out ulong nextBase));
        Assert.Equal(SyntheticMinVirtualIP + 0x180, nextBase);
        Assert.False(info.TryGetVirtualIPBase(TableFrameIdentity(MinFunctionTableIndex + 31), out _));
        Assert.False(info.TryGetVirtualIPBase(CoreLibFrameIdentity(32), out _));
        Assert.True(info.TryGetUnwindData(executionManager.ControlPC, out TargetPointer unwind));
        Assert.Equal(new TargetPointer(GCInfoBase + 0x80), unwind);
        executionManager.RuntimeFunction = new TargetPointer(RuntimeFunctionAddress + 31 * MockWasmRuntimeFunctions.EntrySize);
        Assert.False(info.TryGetUnwindData(executionManager.ControlPC, out _));
    }

    [Fact]
    public void TryGetUnwindData_ReturnsGCInfoBasePlusUnwindData()
    {
        TestPlaceholderTarget target = CreateTarget();
        FakeExecutionManager executionManager = new()
        {
            ControlPC = new TargetCodePointer(MinVirtualIP + FunctionBeginAddress),
            RuntimeFunction = new TargetPointer(RuntimeFunctionAddress),
            UnwindInfoBaseAddress = new TargetPointer(GCInfoBase),
        };
        WasmR2RInfo info = new(target, executionManager);

        Assert.True(info.TryGetUnwindData(
            executionManager.ControlPC,
            out TargetPointer unwindData));
        Assert.Equal(GCInfoBase + FunctionUnwindData, unwindData.Value);
    }

    [Fact]
    public void TryGetUnwindData_WithoutGCInfoBase_ReturnsFalse()
    {
        FakeExecutionManager executionManager = new()
        {
            ControlPC = new TargetCodePointer(MinVirtualIP + FunctionBeginAddress),
            RuntimeFunction = new TargetPointer(RuntimeFunctionAddress),
        };
        WasmR2RInfo info = new(CreateTarget(), executionManager);

        Assert.False(info.TryGetUnwindData(executionManager.ControlPC, out _));
    }

    [Theory]
    [InlineData(0x102u, 0x40u)]
    [InlineData(0x122u, 0x80u)]
    public void TryGetUnwindData_ExecutionManager_ResolvesRootAndFunclet(uint offset, uint expectedUnwindData)
    {
        const ulong MethodDescAddress = 0x0101_aaa0;
        const ulong JitManagerAddress = 0x000b_ff00;
        MockExecutionManagerBuilder emBuilder = new(
            "c1", WasmArch, MockExecutionManagerBuilder.DefaultAllocationRange, isWasm: true);
        var virtualIPRange = emBuilder.AllocateJittedCodeRange(SyntheticMinVirtualIP, 0x400);
        MockReadyToRunInfo r2rInfo = emBuilder.AddReadyToRunInfo([0x100, 0x8000_0120, 0x180], []);
        r2rInfo.MinVirtualIP = SyntheticMinVirtualIP;
        r2rInfo.LoadedImageBase = LoadedImageBase;
        emBuilder.SetRuntimeFunctionUnwindData(r2rInfo, 0, 0x40);
        emBuilder.SetRuntimeFunctionUnwindData(r2rInfo, 1, 0x80);
        new MockHashMapBuilder(emBuilder.Builder).PopulatePtrMap(
            r2rInfo.EntryPointToMethodDescMapAddress,
            [(SyntheticMinVirtualIP + FunctionBeginAddress, MethodDescAddress)]);
        MockLoaderModule module = emBuilder.AddReadyToRunModule(r2rInfo.Address);
        emBuilder.AddVirtualIPRangeSection(virtualIPRange, JitManagerAddress, module.Address);
        Target target = ExecutionManager.ExecutionManagerTests.CreateTarget(
            emBuilder, targetArchitecture: RuntimeInfoArchitecture.Wasm);
        WasmR2RInfo info = new(target);

        Assert.True(info.TryGetUnwindData(
            new TargetCodePointer(SyntheticMinVirtualIP + offset), out TargetPointer unwindData));
        Assert.Equal(new TargetPointer(GCInfoBase + expectedUnwindData), unwindData);
    }

    [Fact]
    public void TryGetVirtualIPBase_IndexNotInAnySection_ReturnsFalse()
    {
        WasmR2RInfo info = new(CreateTarget(), new FakeExecutionManager());

        Assert.False(info.TryGetVirtualIPBase(TableFrameIdentity(MinFunctionTableIndex + 100), out _));
    }

    [Fact]
    public void EmptyRangeList_ResolvesToNoSection()
    {
        WasmR2RInfo info = new(CreateTarget(emptyList: true), new FakeExecutionManager());

        Assert.False(info.TryGetVirtualIPBase(TableFrameIdentity(FunctionTableIndex), out _));
        Assert.False(info.TryGetUnwindData(new TargetCodePointer(MinVirtualIP + FunctionBeginAddress), out _));
    }

    // Source: linked merged WASI corerun-composite.wasm (MD5 a978074b91cd722574b308f52c9ca994).
    // The MVID-matched System.Private.CoreLib component's MethodDefEntryPoints uniquely maps
    // RuntimeFunction[1040] to parameterless System.Exception::.ctor() (token 0x060004AB).
    // The old flat-table capture established BeginAddress 0x11cc and the frame identity. This
    // fixture re-encodes those values into a synthetic, padded grouped table; it is not a capture
    // of the new binary format. MinVirtualIP is synthetic.
    private const uint CapturedMinFunctionTableIndex = 6259;
    private const uint CapturedNumRuntimeFunctions = 45283;
    private const uint CapturedExceptionCtorLocalIndex = 1040;
    private const uint CapturedExceptionCtorFunctionTableIndex = CapturedMinFunctionTableIndex + CapturedExceptionCtorLocalIndex;
    private const uint DisassembledFrameVirtualIPHalf = 1;
    private const uint CapturedFunctionBeginAddress = 0x11cc;
    private const ulong SyntheticMinVirtualIP = 0x8000_0001;
    private const uint SyntheticLastFunctionBeginAddress = 0x4000;

    private static (TestPlaceholderTarget Target, ulong FrameAddress) CreateDispatchingCompositeFixture()
    {
        TargetTestHelpers helpers = new(WasmArch);
        var targetBuilder = new TestPlaceholderTarget.Builder(WasmArch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        var allocator = builder.CreateAllocator(0x0010_0000, 0x0080_0000);

        int hashMapStride = MockHashMap.CreateLayout(WasmArch).Size;
        var moduleLayout = MockLoaderModule.CreateLayout(WasmArch);
        var r2rInfoLayout = MockReadyToRunInfo.CreateLayout(WasmArch, hashMapStride, isWasm: true);
        var rangeSectionLayout = helpers.LayoutFields([
            new("MinFunctionTableIndex", DataType.uint32),
            new("NumRuntimeFunctions", DataType.uint32),
            new("R2RModule", DataType.pointer),
            new("Next", DataType.pointer),
        ]);

        MockWasmRuntimeFunctions.Entry[] functions = new MockWasmRuntimeFunctions.Entry[CapturedNumRuntimeFunctions];
        Array.Fill(functions, new MockWasmRuntimeFunctions.Entry(0, IsSkip: true));
        functions[CapturedExceptionCtorLocalIndex] = new(CapturedFunctionBeginAddress);
        functions[CapturedNumRuntimeFunctions - 1] = new(SyntheticLastFunctionBeginAddress);
        (ulong runtimeFunctions, uint runtimeFunctionCount) = MockWasmRuntimeFunctions.Add(builder, allocator, functions);

        MockReadyToRunInfo r2rInfo = r2rInfoLayout.Create(allocator.Allocate((ulong)r2rInfoLayout.Size, "ReadyToRunInfo"));
        r2rInfo.CompositeInfo = r2rInfo.Address;
        r2rInfo.NumRuntimeFunctions = runtimeFunctionCount;
        r2rInfo.RuntimeFunctions = runtimeFunctions;
        r2rInfo.LoadedImageBase = LoadedImageBase;
        r2rInfo.GCInfoBase = GCInfoBase;
        r2rInfo.MinVirtualIP = SyntheticMinVirtualIP;

        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GCInfoBase,
            Data = [16, 2],
            Name = "SyntheticSingleIPUnwindData",
        });

        MockLoaderModule module = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));
        module.ReadyToRunInfo = r2rInfo.Address;

        var sectionFrag = allocator.Allocate(rangeSectionLayout.Stride, "FunctionTableIndexRangeSection");
        var secFields = rangeSectionLayout.Fields;
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["MinFunctionTableIndex"].Offset, sizeof(uint)), CapturedMinFunctionTableIndex);
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["NumRuntimeFunctions"].Offset, sizeof(uint)), runtimeFunctionCount);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["R2RModule"].Offset, helpers.PointerSize), module.Address);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["Next"].Offset, helpers.PointerSize), 0ul);

        var slotFrag = allocator.Allocate((uint)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(slotFrag.Data.AsSpan().Slice(0, helpers.PointerSize), sectionFrag.Address);

        var frameFrag = allocator.Allocate(16, "R2RFrame");
        helpers.Write(
            frameFrag.Data.AsSpan().Slice(0, sizeof(uint)),
            TableFrameIdentity(CapturedExceptionCtorFunctionTableIndex));
        helpers.Write(frameFrag.Data.AsSpan().Slice(4, sizeof(uint)), DisassembledFrameVirtualIPHalf);

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(MockWasmRuntimeFunctions.CreateRuntimeFunctionLayout(WasmArch)),
            [DataType.WasmRuntimeFunctionGroup] = TargetTestHelpers.CreateTypeInfo(MockWasmRuntimeFunctions.CreateGroupLayout(WasmArch)),
            [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(r2rInfoLayout),
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(moduleLayout),
            [DataType.FunctionTableIndexRangeSection] = new() { Fields = rangeSectionLayout.Fields, Size = rangeSectionLayout.Stride },
        };

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(types)
            .AddGlobals(("FunctionTableIndexRangeList", slotFrag.Address))
            .Build();

        return (target, frameFrag.Address);
    }

    [Fact]
    public void DispatchingCompositeFixture_ResolvesCapturedFunctionTableRange()
    {
        (TestPlaceholderTarget target, _) = CreateDispatchingCompositeFixture();
        WasmR2RInfo info = new(target, new FakeExecutionManager());

        Assert.True(info.TryGetVirtualIPBase(
            TableFrameIdentity(CapturedExceptionCtorFunctionTableIndex),
            out ulong baseVirtualIP));
        Assert.Equal(SyntheticMinVirtualIP + CapturedFunctionBeginAddress, baseVirtualIP);

        uint lastFunctionTableIndex = CapturedMinFunctionTableIndex + CapturedNumRuntimeFunctions - 1;
        Assert.True(info.TryGetVirtualIPBase(TableFrameIdentity(lastFunctionTableIndex), out ulong lastBaseVirtualIP));
        Assert.Equal(SyntheticMinVirtualIP + SyntheticLastFunctionBeginAddress, lastBaseVirtualIP);

        Assert.False(info.TryGetVirtualIPBase(TableFrameIdentity(CapturedMinFunctionTableIndex - 1), out _));
        Assert.False(info.TryGetVirtualIPBase(TableFrameIdentity(lastFunctionTableIndex + 1), out _));
        Assert.False(info.TryGetVirtualIPBase(
            TableFrameIdentity(CapturedMinFunctionTableIndex + ((CapturedNumRuntimeFunctions + 31) & ~31u)),
            out _));
    }

    [Fact]
    public void DispatchingCompositeFixture_UnwinderDecodesDisassembledFrameVirtualIP()
    {
        (TestPlaceholderTarget target, ulong frameAddress) = CreateDispatchingCompositeFixture();
        WasmUnwinder unwinder = new(target, new WasmR2RInfo(target, new FakeExecutionManager()));

        TargetCodePointer virtualIP = unwinder.GetVirtualIP(new TargetPointer(frameAddress));
        Assert.Equal(
            SyntheticMinVirtualIP + CapturedFunctionBeginAddress + (DisassembledFrameVirtualIPHalf * 2),
            virtualIP.Value);
    }
}
