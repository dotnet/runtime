// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class DebuggerTests
{
    private const uint DebuggerControlFlagPendingAttach = 0x0100;
    private const uint DebuggerControlFlagAttached = 0x0200;

    /// <summary>
    /// Provides all standard architectures paired with each bool value,
    /// for testing write APIs that take a bool.
    /// </summary>
    public static IEnumerable<object[]> StdArchWithBool()
    {
        foreach (object[] archArgs in new MockTarget.StdArch())
        {
            yield return [archArgs[0], true];
            yield return [archArgs[0], false];
        }
    }

    private static TargetTestHelpers.LayoutResult GetDebuggerLayout(TargetTestHelpers helpers)
    {
        return helpers.LayoutFields(
        [
            new(nameof(Data.Debugger.LeftSideInitialized), DataType.int32),
            new(nameof(Data.Debugger.Defines), DataType.uint32),
            new(nameof(Data.Debugger.MDStructuresVersion), DataType.uint32),
            new(nameof(Data.Debugger.RCThread), DataType.pointer),
            new(nameof(Data.Debugger.RSRequestedSync), DataType.int32),
            new(nameof(Data.Debugger.SendExceptionsOutsideOfJMC), DataType.int32),
            new(nameof(Data.Debugger.GCNotificationEventsEnabled), DataType.int32),
        ]);
    }

    private static TargetTestHelpers.LayoutResult GetDebuggerRCThreadLayout(TargetTestHelpers helpers)
    {
        return helpers.LayoutFields(
        [
            new(nameof(Data.DebuggerRCThread.DCB), DataType.pointer),
        ]);
    }

    private static TestPlaceholderTarget BuildTarget(
        MockTarget.Architecture arch,
        int leftSideInitialized,
        uint defines,
        uint mdStructuresVersion,
        int? attachStateFlags = null,
        uint? debuggerControlFlags = null,
        byte? metadataUpdatesApplied = null,
        ulong? debuggerControlBlockAddress = null)
    {
        TargetTestHelpers helpers = new(arch);
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.Builder memBuilder = builder.MemoryBuilder;
        MockMemorySpace.BumpAllocator allocator = memBuilder.CreateAllocator(0x1_0000, 0x2_0000);

        TargetTestHelpers.LayoutResult debuggerLayout = GetDebuggerLayout(helpers);
        TargetTestHelpers.LayoutResult debuggerRcThreadLayout = GetDebuggerRCThreadLayout(helpers);
        builder.AddTypes(new()
        {
            [DataType.Debugger] = new Target.TypeInfo() { Fields = debuggerLayout.Fields, Size = debuggerLayout.Stride },
            [DataType.DebuggerRCThread] = new Target.TypeInfo() { Fields = debuggerRcThreadLayout.Fields, Size = debuggerRcThreadLayout.Stride },
        });

        ulong debuggerRcThreadAddress = 0;
        if (debuggerControlBlockAddress.HasValue)
        {
            MockMemorySpace.HeapFragment debuggerRCThreadFrag = allocator.Allocate(debuggerRcThreadLayout.Stride, "DebuggerRCThread");
            helpers.WritePointer(debuggerRCThreadFrag.Data.AsSpan(debuggerRcThreadLayout.Fields[nameof(Data.DebuggerRCThread.DCB)].Offset, helpers.PointerSize), debuggerControlBlockAddress.Value);
            debuggerRcThreadAddress = debuggerRCThreadFrag.Address;
        }

        // Allocate and populate the Debugger struct
        MockMemorySpace.HeapFragment debuggerFrag = allocator.Allocate(debuggerLayout.Stride, "Debugger");
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.LeftSideInitialized)].Offset, sizeof(int)), leftSideInitialized);
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.Defines)].Offset, sizeof(uint)), defines);
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.MDStructuresVersion)].Offset, sizeof(uint)), mdStructuresVersion);
        helpers.WritePointer(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.RCThread)].Offset, helpers.PointerSize), debuggerRcThreadAddress);
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.RSRequestedSync)].Offset, sizeof(int)), 0);
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.SendExceptionsOutsideOfJMC)].Offset, sizeof(int)), 0);
        helpers.Write(debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.GCNotificationEventsEnabled)].Offset, sizeof(int)), 0);

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

        if (debuggerControlFlags.HasValue)
        {
            MockMemorySpace.HeapFragment debuggerControlFlagsFrag = allocator.Allocate(sizeof(uint), "g_CORDebuggerControlFlags");
            helpers.Write(debuggerControlFlagsFrag.Data.AsSpan(0, sizeof(uint)), debuggerControlFlags.Value);
            builder.AddGlobals((Constants.Globals.CORDebuggerControlFlags, debuggerControlFlagsFrag.Address));
        }

        if (metadataUpdatesApplied.HasValue)
        {
            MockMemorySpace.HeapFragment metadataFrag = allocator.Allocate(1, "MetadataUpdatesApplied");
            helpers.Write(metadataFrag.Data.AsSpan(0, 1), metadataUpdatesApplied.Value);
            builder.AddGlobals((Constants.Globals.MetadataUpdatesApplied, metadataFrag.Address));
        }

        builder.AddContract<IDebugger>(version: "c1");

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
        builder.AddContract<IDebugger>(version: "c1");

        return builder.Build();
    }

    private static uint GetDebuggerControlFlags(Target target)
    {
        TargetPointer addr = target.ReadGlobalPointer(Constants.Globals.CORDebuggerControlFlags);
        return target.Read<uint>(addr.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetDebuggerData_ReturnsTrue_WhenInitialized(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0xDEADBEEF, mdStructuresVersion: 42);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.True(debugger.TryGetDebuggerData(out DebuggerData data));
        Assert.True(data.IsLeftSideInitialized);
        Assert.Equal(0xDEADBEEFu, data.DefinesBitField);
        Assert.Equal(42u, data.MDStructuresVersion);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetDebuggerData_ReturnsTrue_WhenNotInitialized(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 0, defines: 0xCAFE, mdStructuresVersion: 7);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.True(debugger.TryGetDebuggerData(out DebuggerData data));
        Assert.False(data.IsLeftSideInitialized);
        Assert.Equal(0xCAFEu, data.DefinesBitField);
        Assert.Equal(7u, data.MDStructuresVersion);
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
    public void MarkDebuggerAttachPending_SetsPendingAttachFlag(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, attachStateFlags: 0, debuggerControlFlags: 0x42);
        IDebugger debugger = target.Contracts.Debugger;

        debugger.MarkDebuggerAttachPending();

        Assert.Equal(0x42u | DebuggerControlFlagPendingAttach, GetDebuggerControlFlags(target));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MarkDebuggerAttached_SetsAttachedFlag_WhenTrue(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, attachStateFlags: 0, debuggerControlFlags: DebuggerControlFlagPendingAttach);
        IDebugger debugger = target.Contracts.Debugger;

        debugger.MarkDebuggerAttached(true);

        Assert.Equal(DebuggerControlFlagPendingAttach | DebuggerControlFlagAttached, GetDebuggerControlFlags(target));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MarkDebuggerAttached_ClearsAttachedAndPending_WhenFalse(MockTarget.Architecture arch)
    {
        const uint originalFlags = 0x0042u | DebuggerControlFlagPendingAttach | DebuggerControlFlagAttached;
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, attachStateFlags: 0, debuggerControlFlags: originalFlags);
        IDebugger debugger = target.Contracts.Debugger;

        debugger.MarkDebuggerAttached(false);

        Assert.Equal(0x42u, GetDebuggerControlFlags(target));
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

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void RequestSyncAtEvent_DoesNothing_WhenDebuggerNull(MockTarget.Architecture arch)
    {
        Target target = BuildNullDebuggerTarget(arch);
        IDebugger debugger = target.Contracts.Debugger;

        debugger.RequestSyncAtEvent();
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetSendExceptionsOutsideOfJMC_DoesNothing_WhenDebuggerNull(MockTarget.Architecture arch)
    {
        Target target = BuildNullDebuggerTarget(arch);
        IDebugger debugger = target.Contracts.Debugger;

        debugger.SetSendExceptionsOutsideOfJMC(true);
        debugger.SetSendExceptionsOutsideOfJMC(false);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDebuggerControlBlockAddress_ReturnsAddress(MockTarget.Architecture arch)
    {
        const ulong expectedAddress = 0x1234_5678;
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0, debuggerControlBlockAddress: expectedAddress);
        IDebugger debugger = target.Contracts.Debugger;

        TargetPointer result = debugger.GetDebuggerControlBlockAddress();

        Assert.Equal(expectedAddress, result.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDebuggerControlBlockAddress_ReturnsNull_WhenDebuggerNull(MockTarget.Architecture arch)
    {
        Target target = BuildNullDebuggerTarget(arch);
        IDebugger debugger = target.Contracts.Debugger;

        TargetPointer result = debugger.GetDebuggerControlBlockAddress();

        Assert.Equal(TargetPointer.Null, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDebuggerControlBlockAddress_ReturnsNull_WhenRCThreadNull(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0);
        IDebugger debugger = target.Contracts.Debugger;

        TargetPointer result = debugger.GetDebuggerControlBlockAddress();

        Assert.Equal(TargetPointer.Null, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnableGCNotificationEvents_DoesNothing_WhenDebuggerNull(MockTarget.Architecture arch)
    {
        Target target = BuildNullDebuggerTarget(arch);
        IDebugger debugger = target.Contracts.Debugger;

        // Should not throw; null g_pDebugger is silently ignored
        debugger.EnableGCNotificationEvents(true);
        debugger.EnableGCNotificationEvents(false);
    }

    // -----------------------------------------------------------------------
    // Helpers shared by write-verification tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the address of the live <c>Debugger</c> struct by following the
    /// same two-pointer indirection that <see cref="Debugger_1"/> uses:
    /// <c>*ReadGlobalPointer("Debugger")</c>.
    /// </summary>
    private static TargetPointer GetDebuggerAddress(TestPlaceholderTarget target)
    {
        TargetPointer debuggerPtrPtr = target.ReadGlobalPointer(Constants.Globals.Debugger);
        return target.ReadPointer(debuggerPtrPtr.Value);
    }

    // -----------------------------------------------------------------------
    // Write-verification tests
    // -----------------------------------------------------------------------

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void RequestSyncAtEvent_WritesSyncFlag(MockTarget.Architecture arch)
    {
        TestPlaceholderTarget target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0);
        IDebugger debugger = target.Contracts.Debugger;

        TargetPointer debuggerAddress = GetDebuggerAddress(target);
        int fieldOffset = target.GetTypeInfo(DataType.Debugger).Fields[nameof(Data.Debugger.RSRequestedSync)].Offset;

        Assert.Equal(0, target.Read<int>(debuggerAddress.Value + (ulong)fieldOffset));

        debugger.RequestSyncAtEvent();

        Assert.Equal(1, target.Read<int>(debuggerAddress.Value + (ulong)fieldOffset));
    }

    [Theory]
    [MemberData(nameof(StdArchWithBool))]
    public void SetSendExceptionsOutsideOfJMC_WritesFlag(MockTarget.Architecture arch, bool value)
    {
        TestPlaceholderTarget target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0);
        IDebugger debugger = target.Contracts.Debugger;

        TargetPointer debuggerAddress = GetDebuggerAddress(target);
        int fieldOffset = target.GetTypeInfo(DataType.Debugger).Fields[nameof(Data.Debugger.SendExceptionsOutsideOfJMC)].Offset;

        debugger.SetSendExceptionsOutsideOfJMC(value);

        Assert.Equal(value ? 1 : 0, target.Read<int>(debuggerAddress.Value + (ulong)fieldOffset));
    }

    [Theory]
    [MemberData(nameof(StdArchWithBool))]
    public void EnableGCNotificationEvents_WritesFlag(MockTarget.Architecture arch, bool value)
    {
        TestPlaceholderTarget target = BuildTarget(arch, leftSideInitialized: 1, defines: 0, mdStructuresVersion: 0);
        IDebugger debugger = target.Contracts.Debugger;

        TargetPointer debuggerAddress = GetDebuggerAddress(target);
        int fieldOffset = target.GetTypeInfo(DataType.Debugger).Fields[nameof(Data.Debugger.GCNotificationEventsEnabled)].Offset;

        debugger.EnableGCNotificationEvents(value);

        Assert.Equal(value ? 1 : 0, target.Read<int>(debuggerAddress.Value + (ulong)fieldOffset));
    }
}
