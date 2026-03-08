// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the StackWalk contract.
/// Uses the StackWalk debuggee dump, which has a deterministic call stack:
/// Main → MethodA → MethodB → MethodC → FailFast.
/// </summary>
public class StackWalkDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

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

            // Each managed frame's MethodDesc should resolve to a valid MethodDescHandle
            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            uint token = rts.GetMethodToken(mdHandle);
            // MethodDef tokens have the form 0x06xxxxxx
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
}
