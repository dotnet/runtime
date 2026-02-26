// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for P/Invoke stack frames.
/// Uses the PInvokeStub debuggee, which crashes inside a P/Invoke with
/// SetLastError=true (forcing an ILStub DynamicMethodDesc to be generated).
/// </summary>
public class PInvokeStubDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "PInvokeStub";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "PInvokeStub debuggee uses msvcrt.dll (Windows only)")]
    public void PInvokeStub_CanWalkCrashingThread(TestConfiguration config)
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
    [SkipOnOS(IncludeOnly = "windows", Reason = "PInvokeStub debuggee uses msvcrt.dll (Windows only)")]
    public void PInvokeStub_ContainsExpectedFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        // Stack (top -> bottom): ..., memcpy, CrashInILStubPInvoke, Main
        DumpTestStackWalker.Walk(Target, crashingThread)
            .ExpectFrame("memcpy")
            .ExpectAdjacentFrame("CrashInILStubPInvoke")
            .ExpectAdjacentFrame("Main")
            .Verify();
    }
}
