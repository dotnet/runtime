// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
public abstract class StackWalkDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    [ConditionalFact]
    public void StackWalk_ContractIsAvailable()
    {
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        Assert.NotNull(stackWalk);
    }

    [ConditionalFact]
    public void StackWalk_CanWalkCrashingThread()
    {
        SkipIfVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        List<IStackDataFrameHandle> frameList = frames.ToList();

        Assert.True(frameList.Count > 0, "Expected at least one stack frame on the crashing thread");
    }

    [ConditionalFact]
    public void StackWalk_HasMultipleFrames()
    {
        SkipIfVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0");
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

    [ConditionalFact]
    public void StackWalk_ManagedFramesHaveValidMethodDescs()
    {
        SkipIfVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0");
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

    [ConditionalFact]
    public void StackWalk_FramesHaveRawContext()
    {
        SkipIfVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        IStackDataFrameHandle? firstFrame = frames.FirstOrDefault();
        Assert.NotNull(firstFrame);

        byte[] context = stackWalk.GetRawContext(firstFrame);
        Assert.NotNull(context);
        Assert.True(context.Length > 0, "Expected non-empty raw context for stack frame");
    }
}

public class StackWalkDumpTests_Local : StackWalkDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class StackWalkDumpTests_Net10 : StackWalkDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
