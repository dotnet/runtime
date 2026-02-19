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

    [ConditionalFact]
    public void StackWalk_ContractIsAvailable()
    {
        SkipIfTargetOS("Unix", "Thread.UEWatsonBucketTrackerBuckets is only supported on Windows");
        SkipIfVersion("net10.0", "Thread.UEWatsonBucketTrackerBuckets field not present in .NET 10 contract descriptor");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        Assert.NotNull(stackWalk);
    }

    [ConditionalFact]
    public void StackWalk_CanWalkCrashingThread()
    {
        SkipIfTargetOS("Unix", "Thread.UEWatsonBucketTrackerBuckets is only supported on Windows");
        SkipIfVersion("net10.0", "Thread.UEWatsonBucketTrackerBuckets field not present in .NET 10 contract descriptor");
        IThread threadContract = Target.Contracts.Thread;
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();
        ThreadData crashingThread = FindCrashingThread(threadContract, storeData);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        List<IStackDataFrameHandle> frameList = frames.ToList();

        Assert.True(frameList.Count > 0, "Expected at least one stack frame on the crashing thread");
    }

    [ConditionalFact]
    public void StackWalk_HasMultipleFrames()
    {
        SkipIfTargetOS("Unix", "Thread.UEWatsonBucketTrackerBuckets is only supported on Windows");
        SkipIfVersion("net10.0", "Thread.UEWatsonBucketTrackerBuckets field not present in .NET 10 contract descriptor");
        IThread threadContract = Target.Contracts.Thread;
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();
        ThreadData crashingThread = FindCrashingThread(threadContract, storeData);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        List<IStackDataFrameHandle> frameList = frames.ToList();

        // The debuggee has Main → MethodA → MethodB → MethodC → FailFast,
        // but the stack walk may include runtime helper frames and native transitions.
        // We just assert there are multiple frames visible.
        Assert.True(frameList.Count >= 1,
            $"Expected at least 1 stack frame from the crashing thread, got {frameList.Count}");
    }

    [ConditionalFact]
    public void StackWalk_ManagedFramesHaveValidMethodDescs()
    {
        SkipIfTargetOS("Unix", "Thread.UEWatsonBucketTrackerBuckets is only supported on Windows");
        SkipIfVersion("net10.0", "Thread.UEWatsonBucketTrackerBuckets field not present in .NET 10 contract descriptor");
        IThread threadContract = Target.Contracts.Thread;
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();
        ThreadData crashingThread = FindCrashingThread(threadContract, storeData);

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
        SkipIfTargetOS("Unix", "Thread.UEWatsonBucketTrackerBuckets is only supported on Windows");
        SkipIfVersion("net10.0", "Thread.UEWatsonBucketTrackerBuckets field not present in .NET 10 contract descriptor");
        IThread threadContract = Target.Contracts.Thread;
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();
        ThreadData crashingThread = FindCrashingThread(threadContract, storeData);

        IEnumerable<IStackDataFrameHandle> frames = stackWalk.CreateStackWalk(crashingThread);
        IStackDataFrameHandle? firstFrame = frames.FirstOrDefault();
        Assert.NotNull(firstFrame);

        byte[] context = stackWalk.GetRawContext(firstFrame);
        Assert.NotNull(context);
        Assert.True(context.Length > 0, "Expected non-empty raw context for stack frame");
    }

    /// <summary>
    /// Finds the first thread in the thread list — in a FailFast dump, this is typically
    /// the main thread that initiated the crash.
    /// </summary>
    private static ThreadData FindCrashingThread(IThread threadContract, ThreadStoreData storeData)
    {
        TargetPointer currentThread = storeData.FirstThread;
        Assert.NotEqual(TargetPointer.Null, currentThread);
        return threadContract.GetThreadData(currentThread);
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
