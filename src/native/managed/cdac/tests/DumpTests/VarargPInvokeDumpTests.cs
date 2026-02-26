// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for vararg P/Invoke stack frames.
/// Uses the VarargPInvoke debuggee, which crashes inside a vararg
/// P/Invoke (sprintf with __arglist), triggering the VarargPInvokeStub
/// assembly thunk path.
/// </summary>
public class VarargPInvokeDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "VarargPInvoke";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public void VarargPInvoke_CanWalkCrashingThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
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
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        // Stack (top → bottom): sprintf, sprintf, IL_STUB_PInvoke, CrashInVarargPInvoke, Main
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
        InitializeDumpTest(config);
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

        Assert.True(foundILStub, "Expected to find a ILStub MethodDesc on the crashing thread stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void VarargPInvoke_GetMethodTableDataForILStubFrame(TestConfiguration config)
    {
        InitializeDumpTest(config);
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
            Assert.Equal(HResults.S_OK, hr);

            return;
        }

        Assert.Fail("Expected to find an ILStub MethodDesc on the crashing thread stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void VarargPInvoke_GetILAddressMapForILStub_ReturnsEFail(TestConfiguration config)
    {
        InitializeDumpTest(config);
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

            // ILStubs have no debug info (RealCodeHeader.DebugInfo is null).
            // The DAC's GetBoundariesAndVars returns FALSE for this case,
            // causing GetMethodNativeMap to return E_FAIL, which propagates
            // through GetILAddressMap. The cDAC matches by using HasDebugInfo
            // to detect the missing debug info and returning E_FAIL.
            IXCLRDataMethodInstance methodInstance = new ClrDataMethodInstance(
                Target, mdHandle, TargetPointer.Null, legacyImpl: null);
            uint mapNeeded;
            int hr = methodInstance.GetILAddressMap(0, &mapNeeded, null);
            Assert.Equal(HResults.E_FAIL, hr);

            return;
        }

        Assert.Fail("Expected to find an ILStub MethodDesc on the crashing thread stack");
    }
}
