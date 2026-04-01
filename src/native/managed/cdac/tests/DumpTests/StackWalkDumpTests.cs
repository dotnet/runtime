// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the StackWalk contract and related stack frame APIs.
/// Tests cover multiple debuggees: StackWalk, PInvokeStub, and VarargPInvoke.
/// </summary>
public class StackWalkDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    // ========== StackWalk debuggee ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void StackWalk_CanWalkCrashingThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        List<IStackDataFrameHandle> frameList = frames.ToList();

        Assert.True(frameList.Count > 0, "Expected at least one stack frame on the crashing thread");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void StackWalk_HasMultipleFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        List<IStackDataFrameHandle> frameList = frames.ToList();

        // The debuggee has Main → MethodA → MethodB → MethodC → FailFast,
        // but the stack walk may include runtime helper frames and native transitions.
        // We just assert there are multiple frames visible.
        Assert.True(frameList.Count >= 5,
            $"Expected multiple stack frames from the crashing thread, got {frameList.Count}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void StackWalk_ManagedFramesHaveValidMethodDescs(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);

        foreach (IStackDataFrameHandle frame in frames)
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            if (methodDescPtr == TargetPointer.Null)
                continue;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            uint token = rts.GetMethodToken(mdHandle);
            Assert.Equal(0x06000000u, token & 0xFF000000u);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void StackWalk_FramesHaveRawContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        IStackDataFrameHandle? firstFrame = frames.FirstOrDefault();
        Assert.NotNull(firstFrame);

        byte[] context = stackWalk.GetRawContext(firstFrame);
        Assert.NotNull(context);
        Assert.True(context.Length > 0, "Expected non-empty raw context for stack frame");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void StackWalk_ContainsExpectedFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        DumpTestStackWalker.Walk(Target, crashingThread)
            .ExpectFrame("MethodC")
            .ExpectAdjacentFrame("MethodB")
            .ExpectAdjacentFrame("MethodA")
            .ExpectAdjacentFrame("Main")
            .Verify();
    }

    // ========== PInvokeStub debuggee ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "PInvokeStub debuggee uses msvcrt.dll (Windows only)")]
    public void PInvokeStub_CanWalkCrashingThread(TestConfiguration config)
    {
        InitializeDumpTest(config, "PInvokeStub", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");
        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        List<IStackDataFrameHandle> frameList = frames.ToList();

        Assert.True(frameList.Count > 0, "Expected at least one stack frame on the crashing thread");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "PInvokeStub debuggee uses msvcrt.dll (Windows only)")]
    public void PInvokeStub_ContainsExpectedFrames(TestConfiguration config)
    {
        InitializeDumpTest(config, "PInvokeStub", "full");

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        DumpTestStackWalker.Walk(Target, crashingThread)
            .ExpectFrame("memcpy")
            .ExpectAdjacentFrame("CrashInILStubPInvoke")
            .ExpectAdjacentFrame("Main")
            .Verify();
    }

    // ========== VarargPInvoke debuggee ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public void VarargPInvoke_CanWalkCrashingThread(TestConfiguration config)
    {
        InitializeDumpTest(config, "VarargPInvoke", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");
        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        List<IStackDataFrameHandle> frameList = frames.ToList();

        Assert.True(frameList.Count > 0, "Expected at least one stack frame on the crashing thread");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public void VarargPInvoke_ContainsExpectedFrames(TestConfiguration config)
    {
        InitializeDumpTest(config, "VarargPInvoke", "full");
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        DumpTestStackWalker.Walk(Target, crashingThread)
            .ExpectFrameWhere(
                f => f.MethodDescPtr != TargetPointer.Null && rts.IsILStub(rts.GetMethodDescHandle(f.MethodDescPtr)),
                "ILStub frame")
            .ExpectAdjacentFrame("CrashInVarargPInvoke")
            .ExpectAdjacentFrame("Main")
            .Verify();
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public void VarargPInvoke_HasILStubFrame(TestConfiguration config)
    {
        InitializeDumpTest(config, "VarargPInvoke", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");
        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);

        bool foundILStub = false;
        foreach (IStackDataFrameHandle frame in frames)
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            if (methodDescPtr == TargetPointer.Null)
                continue;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            if (rts.IsILStub(mdHandle))
            {
                foundILStub = true;
                break;
            }
        }

        Assert.True(foundILStub, "Expected to find an ILStub MethodDesc on the crashing thread stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void VarargPInvoke_GetMethodTableDataForILStubFrame(TestConfiguration config)
    {
        InitializeDumpTest(config, "VarargPInvoke", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");
        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);

        foreach (IStackDataFrameHandle frame in frames)
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            if (methodDescPtr == TargetPointer.Null)
                continue;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            if (!rts.IsILStub(mdHandle))
                continue;

            TargetPointer mt = rts.GetMethodTable(mdHandle);
            DacpMethodTableData mtData;
            int hr = sosDac.GetMethodTableData(new ClrDataAddress(mt), &mtData);
            AssertHResult(HResults.S_OK, hr);

            return;
        }

        Assert.Fail("Expected to find an ILStub MethodDesc on the crashing thread stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void VarargPInvoke_GetILAddressMapForILStub_ReturnsFailure(TestConfiguration config)
    {
        InitializeDumpTest(config, "VarargPInvoke", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");
        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);

        foreach (IStackDataFrameHandle frame in frames)
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            if (methodDescPtr == TargetPointer.Null)
                continue;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            if (!rts.IsILStub(mdHandle))
                continue;

            IXCLRDataMethodInstance methodInstance = new ClrDataMethodInstance(
                Target, mdHandle, TargetPointer.Null, legacyImpl: null);
            uint mapNeeded;
            int hr = methodInstance.GetILAddressMap(0, &mapNeeded, null);
            Assert.True(hr < 0, $"Expected failure HRESULT for ILStub GetILAddressMap, got 0x{hr:X8}");

            return;
        }

        Assert.Fail("Expected to find an ILStub MethodDesc on the crashing thread stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void VarargPInvoke_GetCodeHeaderDataWithInvalidPrecodeAddress(TestConfiguration config)
    {
        InitializeDumpTest(config, "VarargPInvoke", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");
        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);

        foreach (IStackDataFrameHandle frame in frames)
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            if (methodDescPtr == TargetPointer.Null)
                continue;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            TargetCodePointer entryPoint = rts.GetMethodEntryPointIfExists(mdHandle);
            if (entryPoint == TargetCodePointer.Null)
                continue;

            DacpCodeHeaderData codeHeaderData;
            int hr = sosDac.GetCodeHeaderData(new ClrDataAddress(entryPoint.Value), &codeHeaderData);
            if (hr != 0)
                continue;

            DacpCodeHeaderData invalidCodeHeaderData;
            hr = sosDac.GetCodeHeaderData(new ClrDataAddress(entryPoint.Value + 1), &invalidCodeHeaderData);
            Assert.True(hr < 0, $"Expected failure HRESULT for invalid precode address, got 0x{hr:X8}");

            return;
        }

        Assert.Fail("Expected to find a frame with a valid entry point");
    }
}
