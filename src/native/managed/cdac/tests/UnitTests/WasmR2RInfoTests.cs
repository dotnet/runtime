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
    private static TestPlaceholderTarget CreateTarget()
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
        var slotFrag = allocator.Allocate((uint)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(slotFrag.Data.AsSpan().Slice(0, helpers.PointerSize), sectionFrag.Address);

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
}
