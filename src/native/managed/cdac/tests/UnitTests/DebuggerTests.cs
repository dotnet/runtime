// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
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
            new(nameof(Data.Debugger.RgHijackFunction), DataType.pointer),
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
        builder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
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

    // -----------------------------------------------------------------------
    // GetHijackKind
    // -----------------------------------------------------------------------

    private static TargetTestHelpers.LayoutResult GetMemoryRangeLayout(TargetTestHelpers helpers)
    {
        return helpers.LayoutFields(
        [
            new(nameof(Data.MemoryRange.StartAddress), DataType.pointer),
            new(nameof(Data.MemoryRange.Size), DataType.nuint),
        ]);
    }

    /// <summary>
    /// Builds a target whose Debugger has an RgHijackFunction array of <paramref name="ranges"/>
    /// MemoryRange entries. Index 0 in the array is the unhandled-exception hijack, matching
    /// Debugger::kUnhandledException == 0 in native debugger.h.
    /// </summary>
    private static TestPlaceholderTarget BuildTargetWithHijackTable(
        MockTarget.Architecture arch,
        (ulong Start, ulong Size)[] ranges)
    {
        TargetTestHelpers helpers = new(arch);
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.BumpAllocator allocator = builder.MemoryBuilder.CreateAllocator(0x1_0000, 0x10_0000);

        TargetTestHelpers.LayoutResult debuggerLayout = GetDebuggerLayout(helpers);
        TargetTestHelpers.LayoutResult memoryRangeLayout = GetMemoryRangeLayout(helpers);
        builder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.Debugger] = new() { Fields = debuggerLayout.Fields, Size = debuggerLayout.Stride },
            [DataType.MemoryRange] = new() { Fields = memoryRangeLayout.Fields, Size = memoryRangeLayout.Stride },
        });

        // Allocate the RgHijackFunction array as a contiguous block of MemoryRange entries.
        ulong rgHijackAddress = 0;
        if (ranges.Length > 0)
        {
            MockMemorySpace.HeapFragment rgFrag = allocator.Allocate(
                (ulong)ranges.Length * memoryRangeLayout.Stride,
                "RgHijackFunction");
            int startOff = memoryRangeLayout.Fields[nameof(Data.MemoryRange.StartAddress)].Offset;
            int sizeOff = memoryRangeLayout.Fields[nameof(Data.MemoryRange.Size)].Offset;
            for (int i = 0; i < ranges.Length; i++)
            {
                int entryBase = i * (int)memoryRangeLayout.Stride;
                helpers.WritePointer(rgFrag.Data.AsSpan(entryBase + startOff, helpers.PointerSize), ranges[i].Start);
                helpers.WriteNUInt(rgFrag.Data.AsSpan(entryBase + sizeOff, helpers.PointerSize), new TargetNUInt(ranges[i].Size));
            }
            rgHijackAddress = rgFrag.Address;
        }

        // Allocate and populate the Debugger struct.
        MockMemorySpace.HeapFragment debuggerFrag = allocator.Allocate(debuggerLayout.Stride, "Debugger");
        helpers.WritePointer(
            debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.RgHijackFunction)].Offset, helpers.PointerSize),
            rgHijackAddress);

        // g_pDebugger -> Debugger
        MockMemorySpace.HeapFragment debuggerPtrFrag = allocator.Allocate((ulong)helpers.PointerSize, "g_pDebugger");
        helpers.WritePointer(debuggerPtrFrag.Data, debuggerFrag.Address);
        builder.AddGlobals(
            (Constants.Globals.Debugger, debuggerPtrFrag.Address),
            (Constants.Globals.MaxHijackFunctions, (ulong)ranges.Length));

        builder.AddContract<IDebugger>(version: "c1");
        return builder.Build();
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHijackKind_DetectsUnhandledExceptionHijack(MockTarget.Architecture arch)
    {
        // Index 0 is the unhandled-exception hijack; index 1 is any other redirect stub.
        (ulong Start, ulong Size)[] ranges =
        [
            (0x10_0000, 0x100),
            (0x20_0000, 0x100),
        ];
        Target target = BuildTargetWithHijackTable(arch, ranges);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.Equal(HijackKind.UnhandledException, debugger.GetHijackKind(new TargetCodePointer(0x10_0080)));
        Assert.Equal(HijackKind.Other, debugger.GetHijackKind(new TargetCodePointer(0x20_0010)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHijackKind_ReturnsNoneForUnmatchedPC(MockTarget.Architecture arch)
    {
        (ulong Start, ulong Size)[] ranges =
        [
            (0x10_0000, 0x100),
        ];
        Target target = BuildTargetWithHijackTable(arch, ranges);
        IDebugger debugger = target.Contracts.Debugger;

        // Just outside the range (end is exclusive).
        Assert.Equal(HijackKind.None, debugger.GetHijackKind(new TargetCodePointer(0x10_0100)));

        // Well outside any range.
        Assert.Equal(HijackKind.None, debugger.GetHijackKind(new TargetCodePointer(0xDEAD_BEEF)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHijackKind_ReturnsNoneWhenTableEmpty(MockTarget.Architecture arch)
    {
        // FEATURE_HIJACK off / uninitialized: MaxHijackFunctions == 0.
        Target target = BuildTargetWithHijackTable(arch, []);
        IDebugger debugger = target.Contracts.Debugger;

        Assert.Equal(HijackKind.None, debugger.GetHijackKind(new TargetCodePointer(0x10_0080)));
    }

    // -----------------------------------------------------------------------
    // PrepareExceptionHijack
    // -----------------------------------------------------------------------

    public static IEnumerable<object[]> HijackArches()
    {
        MockTarget.Architecture le64 = new() { IsLittleEndian = true, Is64Bit = true };
        MockTarget.Architecture le32 = new() { IsLittleEndian = true, Is64Bit = false };
        yield return [le64, "x64", "windows"];
        yield return [le64, "x64", "unix"];
        yield return [le64, "arm64", "unix"];
        yield return [le32, "x86", "windows"];
        yield return [le32, "arm", "unix"];
    }

    private static int ExceptionRecordSize(int ptrSize)
    {
        int unaligned = sizeof(uint) + sizeof(uint) + ptrSize + ptrSize + sizeof(uint);
        int header = (unaligned + (ptrSize - 1)) & ~(ptrSize - 1);
        return header + (15 * ptrSize);
    }

    private static string[] IntegerArgRegisters(string targetArch, bool isWindows) => targetArch switch
    {
        "x86" => [],
        "x64" => isWindows ? ["rcx", "rdx", "r8", "r9"] : ["rdi", "rsi", "rdx", "rcx", "r8", "r9"],
        "arm" => ["r0", "r1", "r2", "r3"],
        "arm64" => ["x0", "x1", "x2", "x3", "x4", "x5", "x6", "x7"],
        "loongarch64" or "riscv64" => ["a0", "a1", "a2", "a3", "a4", "a5", "a6", "a7"],
        _ => throw new NotSupportedException(targetArch),
    };

    private static TestPlaceholderTarget BuildHijackTarget(
        MockTarget.Architecture arch,
        string targetArch,
        string os,
        ulong hijackStart,
        out ulong stackBase,
        out ulong stackSize)
    {
        TargetTestHelpers helpers = new(arch);
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.BumpAllocator allocator = builder.MemoryBuilder.CreateAllocator(0x1_0000, 0x100_0000);

        TargetTestHelpers.LayoutResult debuggerLayout = GetDebuggerLayout(helpers);
        TargetTestHelpers.LayoutResult memoryRangeLayout = GetMemoryRangeLayout(helpers);
        builder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.Debugger] = new() { Fields = debuggerLayout.Fields, Size = debuggerLayout.Stride },
            [DataType.MemoryRange] = new() { Fields = memoryRangeLayout.Fields, Size = memoryRangeLayout.Stride },
        });

        // Single hijack entry at index 0 (the unhandled-exception hijack).
        MockMemorySpace.HeapFragment rgFrag = allocator.Allocate(memoryRangeLayout.Stride, "RgHijackFunction");
        helpers.WritePointer(rgFrag.Data.AsSpan(memoryRangeLayout.Fields[nameof(Data.MemoryRange.StartAddress)].Offset, helpers.PointerSize), hijackStart);
        helpers.WriteNUInt(rgFrag.Data.AsSpan(memoryRangeLayout.Fields[nameof(Data.MemoryRange.Size)].Offset, helpers.PointerSize), new TargetNUInt(0x100));

        MockMemorySpace.HeapFragment debuggerFrag = allocator.Allocate(debuggerLayout.Stride, "Debugger");
        helpers.WritePointer(
            debuggerFrag.Data.AsSpan(debuggerLayout.Fields[nameof(Data.Debugger.RgHijackFunction)].Offset, helpers.PointerSize),
            rgFrag.Address);

        MockMemorySpace.HeapFragment debuggerPtrFrag = allocator.Allocate((ulong)helpers.PointerSize, "g_pDebugger");
        helpers.WritePointer(debuggerPtrFrag.Data, debuggerFrag.Address);

        // A writable region that the hijack setup uses as the target thread's stack.
        stackSize = 0x4000;
        MockMemorySpace.HeapFragment stackFrag = allocator.Allocate(stackSize, "Stack");
        stackBase = stackFrag.Address;

        builder.AddGlobals(
            (Constants.Globals.Debugger, debuggerPtrFrag.Address),
            (Constants.Globals.MaxHijackFunctions, (ulong)1));
        builder.AddGlobalStrings(
            (Constants.Globals.Architecture, targetArch),
            (Constants.Globals.OperatingSystem, os));
        builder.AddContract<IDebugger>(version: "c1");
        builder.AddContract<IRuntimeInfo>(version: "c1");

        return builder.Build();
    }

    [Theory]
    [MemberData(nameof(HijackArches))]
    public void PrepareExceptionHijack_EditsContextAndStack(MockTarget.Architecture arch, string targetArch, string os)
    {
        const ulong HijackStart = 0x55_0000;
        const ulong OriginalIp = 0x1_2340;
        const int Reason = 7;
        TargetPointer userData = new(0xCAFEF00D);

        Target target = BuildHijackTarget(arch, targetArch, os, HijackStart, out ulong stackBase, out ulong stackSize);
        IDebugger debugger = target.Contracts.Debugger;

        int ptrSize = target.PointerSize;
        ulong originalSp = stackBase + stackSize - 0x200;

        IPlatformAgnosticContext seed = IPlatformAgnosticContext.GetContextForPlatform(target);
        uint contextSize = seed.Size;
        seed.StackPointer = new TargetPointer(originalSp);
        seed.InstructionPointer = new TargetCodePointer(OriginalIp);
        byte[] contextBuffer = seed.GetBytes();

        int recordSize = ExceptionRecordSize(ptrSize);
        byte[] recordBytes = new byte[recordSize];
        for (int i = 0; i < recordSize; i++)
            recordBytes[i] = (byte)(0x80 + (i % 0x40));

        TargetPointer espContext = debugger.PrepareExceptionHijack(contextBuffer, TargetPointer.Null, recordBytes, Reason, userData);

        // The saved CONTEXT lands within the stack, below the original SP.
        Assert.True(espContext.Value >= stackBase && espContext.Value < originalSp);

        // The pushed CONTEXT preserves the original IP and SP for the worker to restore.
        byte[] savedBytes = new byte[contextSize];
        target.ReadBuffer(espContext.Value, savedBytes);
        IPlatformAgnosticContext saved = IPlatformAgnosticContext.GetContextForPlatform(target);
        saved.FillFromBuffer(savedBytes);
        Assert.Equal(OriginalIp, saved.InstructionPointer.Value);
        Assert.Equal(originalSp, saved.StackPointer.Value);

        // The committed CONTEXT jumps to the hijack worker with a descended SP.
        IPlatformAgnosticContext final = IPlatformAgnosticContext.GetContextForPlatform(target);
        final.FillFromBuffer(contextBuffer);
        Assert.Equal(HijackStart, final.InstructionPointer.Value);
        Assert.True(final.StackPointer.Value < espContext.Value);

        // Worker arguments are (espContext, espRecord, reason, userData).
        string[] argRegs = IntegerArgRegisters(targetArch, os == "windows");
        TargetPointer espRecord;
        if (argRegs.Length > 0)
        {
            Assert.True(final.TryReadRegister(argRegs[0], out TargetNUInt arg0));
            Assert.True(final.TryReadRegister(argRegs[1], out TargetNUInt arg1));
            Assert.True(final.TryReadRegister(argRegs[2], out TargetNUInt arg2));
            Assert.True(final.TryReadRegister(argRegs[3], out TargetNUInt arg3));
            Assert.Equal(espContext.Value, arg0.Value);
            Assert.Equal((ulong)Reason, arg2.Value);
            Assert.Equal(userData.Value, arg3.Value);
            espRecord = new TargetPointer(arg1.Value);
        }
        else
        {
            ulong sp = final.StackPointer.Value;
            Assert.Equal(espContext.Value, target.ReadPointer(sp).Value);
            espRecord = target.ReadPointer(sp + (ulong)ptrSize);
            Assert.Equal((ulong)Reason, target.ReadPointer(sp + (ulong)(2 * ptrSize)).Value);
            Assert.Equal(userData.Value, target.ReadPointer(sp + (ulong)(3 * ptrSize)).Value);
        }

        // espRecord points at the pushed EXCEPTION_RECORD bytes.
        Assert.True(espRecord.Value >= stackBase && espRecord.Value < espContext.Value);
        byte[] pushedRecord = new byte[recordSize];
        target.ReadBuffer(espRecord.Value, pushedRecord);
        Assert.Equal(recordBytes, pushedRecord);
    }
}
