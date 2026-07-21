// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    private const uint FunctionBeginAddress = 0x100;
    private const uint FunctionUnwindData = 0x40;

    // Builds a target whose FunctionTableIndexRangeList global points at a *slot* (pointer-to-pointer),
    // matching the CDAC_GLOBAL_POINTER contract. WasmR2RInfo must dereference the slot to reach the
    // list head; walking from the slot address directly reads garbage and finds nothing.
    private static TestPlaceholderTarget CreateTarget(bool emptyList = false)
    {
        TargetTestHelpers helpers = new(WasmArch);
        var targetBuilder = new TestPlaceholderTarget.Builder(WasmArch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        var allocator = builder.CreateAllocator(0x0010_0000, 0x0080_0000);

        int hashMapStride = MockHashMap.CreateLayout(WasmArch).Size;
        var moduleLayout = MockLoaderModule.CreateLayout(WasmArch);
        var r2rInfoLayout = MockReadyToRunInfo.CreateLayout(WasmArch, hashMapStride);
        var runtimeFunctionLayout = helpers.LayoutFields([
            new("BeginAddress", DataType.uint32),
            new("UnwindData", DataType.uint32),
        ]);
        var rangeSectionLayout = helpers.LayoutFields([
            new("MinFunctionTableIndex", DataType.uint32),
            new("NumRuntimeFunctions", DataType.uint32),
            new("R2RModule", DataType.pointer),
            new("Next", DataType.pointer),
        ]);

        var runtimeFuncFrag = allocator.Allocate(runtimeFunctionLayout.Stride, "RuntimeFunction");
        helpers.Write(runtimeFuncFrag.Data.AsSpan().Slice(runtimeFunctionLayout.Fields["BeginAddress"].Offset, sizeof(uint)), FunctionBeginAddress);
        helpers.Write(runtimeFuncFrag.Data.AsSpan().Slice(runtimeFunctionLayout.Fields["UnwindData"].Offset, sizeof(uint)), FunctionUnwindData);

        MockReadyToRunInfo r2rInfo = r2rInfoLayout.Create(allocator.Allocate((ulong)r2rInfoLayout.Size, "ReadyToRunInfo"));
        r2rInfo.CompositeInfo = r2rInfo.Address;
        r2rInfo.NumRuntimeFunctions = 1;
        r2rInfo.RuntimeFunctions = runtimeFuncFrag.Address;
        r2rInfo.LoadedImageBase = LoadedImageBase;
        r2rInfo.MinVirtualIP = MinVirtualIP;

        MockLoaderModule module = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));
        module.ReadyToRunInfo = r2rInfo.Address;

        var sectionFrag = allocator.Allocate(rangeSectionLayout.Stride, "FunctionTableIndexRangeSection");
        var secFields = rangeSectionLayout.Fields;
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["MinFunctionTableIndex"].Offset, sizeof(uint)), MinFunctionTableIndex);
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["NumRuntimeFunctions"].Offset, sizeof(uint)), 1u);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["R2RModule"].Offset, helpers.PointerSize), module.Address);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["Next"].Offset, helpers.PointerSize), 0ul);

        // The slot holds the pointer to the list head. The global points at the slot, not the head.
        // An empty list (null head) is the real runtime state before any virtual-IP ranges register.
        var slotFrag = allocator.Allocate((uint)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(slotFrag.Data.AsSpan().Slice(0, helpers.PointerSize), emptyList ? 0ul : sectionFrag.Address);

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RuntimeFunction] = new() { Fields = runtimeFunctionLayout.Fields, Size = runtimeFunctionLayout.Stride },
            [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(r2rInfoLayout),
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(moduleLayout),
            [DataType.FunctionTableIndexRangeSection] = new() { Fields = rangeSectionLayout.Fields, Size = rangeSectionLayout.Stride },
        };

        return targetBuilder
            .AddTypes(types)
            .AddGlobals(("FunctionTableIndexRangeList", slotFrag.Address))
            .Build();
    }

    [Fact]
    public void TryGetVirtualIPBase_ResolvesThroughDereferencedGlobal()
    {
        WasmR2RInfo info = new(CreateTarget());

        Assert.True(info.TryGetVirtualIPBase(FunctionTableIndex, out ulong baseVirtualIP));
        // MinVirtualIP + RuntimeFunction.BeginAddress (non-funclet).
        Assert.Equal(MinVirtualIP + FunctionBeginAddress, baseVirtualIP);
    }

    [Fact]
    public void TryGetUnwindData_ReturnsImageBasePlusUnwindData()
    {
        WasmR2RInfo info = new(CreateTarget());

        Assert.True(info.TryGetUnwindData(FunctionTableIndex, out TargetPointer unwindData));
        Assert.Equal(LoadedImageBase + FunctionUnwindData, unwindData.Value);
    }

    [Fact]
    public void TryGetVirtualIPBase_IndexNotInAnySection_ReturnsFalse()
    {
        WasmR2RInfo info = new(CreateTarget());

        Assert.False(info.TryGetVirtualIPBase(MinFunctionTableIndex + 100, out _));
    }

    [Fact]
    public void EmptyRangeList_ResolvesToNoSection()
    {
        // The runtime registers virtual-IP ranges lazily, so the FunctionTableIndexRangeList head is
        // null until the first managed stack walk. FindSection must treat an empty list as a clean
        // "no R2R sections" answer rather than dereferencing the null head.
        WasmR2RInfo info = new(CreateTarget(emptyList: true));

        Assert.False(info.TryGetVirtualIPBase(FunctionTableIndex, out _));
        Assert.False(info.TryGetUnwindData(FunctionTableIndex, out _));
    }

    // Values captured from a live dispatching composite ReadyToRun WASM build (a merged CoreLib R2R
    // image). The FunctionTableIndexRangeSection node read at runtime was
    // { MinFunctionTableIndex = 6259, NumRuntimeFunctions = 45283, pNext = 0 } with the module's
    // ReadyToRunInfo.MinVirtualIP = 0, and the injected function-table base (== MinFunctionTableIndex)
    // was 6259. A dispatched System.Exception..ctor frame stored its function-table index (6259 + 1040)
    // at frame+0 and its function-local virtual IP / 2 at frame+4. These constants anchor the reader's
    // math to real runtime data.
    private const uint RealMinFunctionTableIndex = 6259;
    private const uint RealNumRuntimeFunctions = 45283;
    private const ulong RealMinVirtualIP = 0;
    private const uint RealLocalIndex = 1040; // System.Exception..ctor, localIndex within the composite
    private const uint RealFunctionTableIndex = RealMinFunctionTableIndex + RealLocalIndex; // 7299
    private const uint RealFrameVirtualIPHalf = 1; // frame+4 captured at method entry; * 2 == 2
    private const uint RealFunctionBeginAddress = 0x2610; // synthetic: the live BeginAddress was not captured

    // Builds a target that mirrors the live dispatching build's single-node range list, with a runtime
    // function table large enough to index RealLocalIndex, plus an R2R frame at frameAddress whose
    // first word is RealFunctionTableIndex and whose virtual-IP field is RealFrameVirtualIPHalf.
    private static (TestPlaceholderTarget Target, ulong FrameAddress) CreateRealDispatchingR2RTarget()
    {
        TargetTestHelpers helpers = new(WasmArch);
        var targetBuilder = new TestPlaceholderTarget.Builder(WasmArch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        var allocator = builder.CreateAllocator(0x0010_0000, 0x0080_0000);

        int hashMapStride = MockHashMap.CreateLayout(WasmArch).Size;
        var moduleLayout = MockLoaderModule.CreateLayout(WasmArch);
        var r2rInfoLayout = MockReadyToRunInfo.CreateLayout(WasmArch, hashMapStride);
        var runtimeFunctionLayout = helpers.LayoutFields([
            new("BeginAddress", DataType.uint32),
            new("UnwindData", DataType.uint32),
        ]);
        var rangeSectionLayout = helpers.LayoutFields([
            new("MinFunctionTableIndex", DataType.uint32),
            new("NumRuntimeFunctions", DataType.uint32),
            new("R2RModule", DataType.pointer),
            new("Next", DataType.pointer),
        ]);

        // A contiguous runtime function table covering local indices [0, RealLocalIndex]. Only the
        // entry at RealLocalIndex carries a non-zero (non-funclet) BeginAddress; the rest read as zero.
        uint runtimeFunctionStride = runtimeFunctionLayout.Stride;
        uint tableEntries = RealLocalIndex + 1;
        var runtimeFuncTableFrag = allocator.Allocate((ulong)(tableEntries * runtimeFunctionStride), "RuntimeFunctions");
        int beginOffset = (int)(RealLocalIndex * runtimeFunctionStride) + runtimeFunctionLayout.Fields["BeginAddress"].Offset;
        helpers.Write(runtimeFuncTableFrag.Data.AsSpan().Slice(beginOffset, sizeof(uint)), RealFunctionBeginAddress);

        MockReadyToRunInfo r2rInfo = r2rInfoLayout.Create(allocator.Allocate((ulong)r2rInfoLayout.Size, "ReadyToRunInfo"));
        r2rInfo.CompositeInfo = r2rInfo.Address;
        r2rInfo.NumRuntimeFunctions = RealNumRuntimeFunctions;
        r2rInfo.RuntimeFunctions = runtimeFuncTableFrag.Address;
        r2rInfo.LoadedImageBase = LoadedImageBase;
        r2rInfo.MinVirtualIP = RealMinVirtualIP;

        MockLoaderModule module = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));
        module.ReadyToRunInfo = r2rInfo.Address;

        var sectionFrag = allocator.Allocate(rangeSectionLayout.Stride, "FunctionTableIndexRangeSection");
        var secFields = rangeSectionLayout.Fields;
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["MinFunctionTableIndex"].Offset, sizeof(uint)), RealMinFunctionTableIndex);
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["NumRuntimeFunctions"].Offset, sizeof(uint)), RealNumRuntimeFunctions);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["R2RModule"].Offset, helpers.PointerSize), module.Address);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["Next"].Offset, helpers.PointerSize), 0ul);

        var slotFrag = allocator.Allocate((uint)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(slotFrag.Data.AsSpan().Slice(0, helpers.PointerSize), sectionFrag.Address);

        // An R2R frame: [0] = function-table index, [4] = function-local virtual IP / 2.
        var frameFrag = allocator.Allocate(16, "R2RFrame");
        helpers.Write(frameFrag.Data.AsSpan().Slice(0, sizeof(uint)), RealFunctionTableIndex);
        helpers.Write(frameFrag.Data.AsSpan().Slice(4, sizeof(uint)), RealFrameVirtualIPHalf);

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RuntimeFunction] = new() { Fields = runtimeFunctionLayout.Fields, Size = runtimeFunctionLayout.Stride },
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
    public void RealDispatchingBuild_ResolvesFunctionTableIndexInRange()
    {
        (TestPlaceholderTarget target, _) = CreateRealDispatchingR2RTarget();
        WasmR2RInfo info = new(target);

        // A real dispatched function-table index resolves through the single range node; the base
        // virtual IP is MinVirtualIP (0 for this single-image composite) + the entry's BeginAddress.
        Assert.True(info.TryGetVirtualIPBase(RealFunctionTableIndex, out ulong baseVirtualIP));
        Assert.Equal(RealMinVirtualIP + RealFunctionBeginAddress, baseVirtualIP);

        // Boundaries of [MinFunctionTableIndex, MinFunctionTableIndex + NumRuntimeFunctions).
        Assert.False(info.TryGetVirtualIPBase(RealMinFunctionTableIndex - 1, out _));
        Assert.False(info.TryGetVirtualIPBase(RealMinFunctionTableIndex + RealNumRuntimeFunctions, out _));
    }

    [Fact]
    public void RealDispatchingBuild_UnwinderDecodesRealFrameVirtualIP()
    {
        (TestPlaceholderTarget target, ulong frameAddress) = CreateRealDispatchingR2RTarget();
        WasmUnwinder unwinder = new(target, new WasmR2RInfo(target));

        // End-to-end: read the real frame's index (frame+0) and virtual-IP field (frame+4), resolve the
        // base through the real range node, and apply the * 2 encoding on the frame field.
        // control PC == MinVirtualIP(0) + BeginAddress + (RealFrameVirtualIPHalf * 2).
        TargetCodePointer virtualIP = unwinder.GetVirtualIP(new TargetPointer(frameAddress));
        Assert.Equal(RealMinVirtualIP + RealFunctionBeginAddress + (RealFrameVirtualIPHalf * 2), virtualIP.Value);
    }
}
