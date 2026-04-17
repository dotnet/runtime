// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class DebuggerTests
{
    private static TargetTestHelpers.LayoutResult GetDebuggerLayout(TargetTestHelpers helpers)
    {
        return helpers.LayoutFields(
        [
            new(nameof(Data.Debugger.LeftSideInitialized), DataType.int32),
            new(nameof(Data.Debugger.Defines), DataType.uint32),
            new(nameof(Data.Debugger.MDStructuresVersion), DataType.uint32),
        ]);
    }

    private static TestPlaceholderTarget BuildTarget(
        MockTarget.Architecture arch,
        int leftSideInitialized,
        uint defines,
        uint mdStructuresVersion,
        int? attachStateFlags = null,
        byte? metadataUpdatesApplied = null)
    {
        TargetTestHelpers helpers = new(arch);
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.Builder memBuilder = builder.MemoryBuilder;
        MockMemorySpace.BumpAllocator allocator = memBuilder.CreateAllocator(0x1_0000, 0x2_0000);

        TargetTestHelpers.LayoutResult debuggerLayout = GetDebuggerLayout(helpers);
        builder.AddTypes(new() { [DataType.Debugger] = new Target.TypeInfo() { Fields = debuggerLayout.Fields, Size = debuggerLayout.Stride } });

        // Allocate and populate the Debugger struct
        MockMemorySpace.HeapFragment debuggerFrag = allocator.Allocate(debuggerLayout.Stride, "Debugger");
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.LeftSideInitialized)].Offset, sizeof(int)), leftSideInitialized);
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.Defines)].Offset, sizeof(uint)), defines);
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.MDStructuresVersion)].Offset, sizeof(uint)), mdStructuresVersion);

        // g_pDebugger is a pointer-to-Debugger. The global stores the address of g_pDebugger,
        // so ReadGlobalPointer returns the location, and ReadPointer dereferences it.
        MockMemorySpace.HeapFragment debuggerPtrFrag = allocator.Allocate((ulong)helpers.PointerSize, "g_pDebugger");
        helpers.WritePointer(debuggerPtrFrag.Data, debuggerFrag.Address);
        builder.AddGlobals((Constants.Globals.Debugger, debuggerPtrFrag.Address));

        if (attachStateFlags.HasValue)
        {
            MockMemorySpace.HeapFragment attachFrag = allocator.Allocate(sizeof(uint), "CLRJitAttachState");
            helpers.Write(attachFrag.Data.AsSpan(0, sizeof(uint)), (uint)attachStateFlags.Value);
            builder.AddGlobals((Constants.Globals.CLRJitAttachState, attachFrag.Address));
        }

        if (metadataUpdatesApplied.HasValue)
        {
            MockMemorySpace.HeapFragment metadataFrag = allocator.Allocate(1, "MetadataUpdatesApplied");
            helpers.Write(metadataFrag.Data.AsSpan(0, 1), metadataUpdatesApplied.Value);
            builder.AddGlobals((Constants.Globals.MetadataUpdatesApplied, metadataFrag.Address));
        }

        builder.AddContract<IDebugger>(version: 1);

        return builder.Build();
    }

    private static TestPlaceholderTarget BuildNullDebuggerTarget(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.Builder memBuilder = builder.MemoryBuilder;
        MockMemorySpace.BumpAllocator allocator = memBuilder.CreateAllocator(0x1_0000, 0x2_0000);

        // g_pDebugger is a pointer-to-Debugger that contains null.
        MockMemorySpace.HeapFragment debuggerPtrFrag = allocator.Allocate((ulong)helpers.PointerSize, "g_pDebugger");
        helpers.WritePointer(debuggerPtrFrag.Data, 0);
        builder.AddGlobals((Constants.Globals.Debugger, debuggerPtrFrag.Address));
        builder.AddContract<IDebugger>(version: 1);

        return builder.Build();
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetDebuggerData_ReturnsTrue_WhenInitialized(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0xDEADBEEF, mdStructuresVersion: 42);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.True(debugger.TryGetDebuggerData(out DebuggerData data));
        Assert.Equal(0xDEADBEEFu, data.DefinesBitField);
        Assert.Equal(42u, data.MDStructuresVersion);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetDebuggerData_ReturnsFalse_WhenNotInitialized(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 0, defines: 0, mdStructuresVersion: 0);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.False(debugger.TryGetDebuggerData(out _));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetDebuggerData_ReturnsFalse_WhenDebuggerNull(MockTarget.Architecture arch)
    {
        Target target = BuildNullDebuggerTarget(arch);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.False(debugger.TryGetDebuggerData(out _));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAttachStateFlags_ReturnsValue(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, attachStateFlags: 0x42);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.Equal(0x42, debugger.GetAttachStateFlags());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAttachStateFlags_ReturnsZero_WhenValueIsZero(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, attachStateFlags: 0);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.Equal(0, debugger.GetAttachStateFlags());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MetadataUpdatesApplied_ReturnsTrue_WhenSet(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, metadataUpdatesApplied: 1);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.True(debugger.MetadataUpdatesApplied());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MetadataUpdatesApplied_ReturnsFalse_WhenNotSet(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, metadataUpdatesApplied: 0);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.False(debugger.MetadataUpdatesApplied());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MetadataUpdatesApplied_ReturnsFalse_WhenGlobalMissing(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.False(debugger.MetadataUpdatesApplied());
    }
}
