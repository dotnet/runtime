// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests that validate the cDAC reader against a <c>Mini</c> (normal)
/// minidump (<c>MiniDumpNormal</c>, <c>DOTNET_DbgMiniDumpType=1</c>).
///
/// A mini dump only contains the memory the runtime reports while walking each thread's
/// stack — it does NOT include the GC heap. Per the runtime's dump-generation design
/// (<c>EnumMemoryRegionsWorkerSkinny</c>) and the documented SOS behavior for triage/normal
/// dumps, the supported scenarios are limited to:
///   * stack traces for all threads  — <c>clrstack</c>  (see <see cref="MiniDumpStackWalkTests"/>)
///   * managed thread enumeration     — <c>clrthreads</c> (see <see cref="MiniDumpThreadTests"/>)
///   * current exception viewing       — <c>!pe</c>
///   * partial module info             — <c>lm</c> / <c>!eeheap -loader</c>
/// Heap-dependent scenarios (<c>!dumpheap</c>, <c>!dumpobj</c>, <c>!gcroot</c>, locals) are
/// intentionally NOT covered because the backing memory is absent from a mini dump.
/// </summary>
public class MiniDumpStackWalkTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "mini";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void CanWalkCrashingThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        List<IStackDataFrameHandle> frames = DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread).ToList();

        Assert.True(frames.Count > 0, "Expected at least one stack frame on the crashing thread");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void HasMultipleFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        List<IStackDataFrameHandle> frames = DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread).ToList();

        // The debuggee has Main → MethodA → MethodB → MethodC → FailFast,
        // plus runtime helper and native transition frames.
        Assert.True(frames.Count >= 5,
            $"Expected multiple stack frames from the crashing thread, got {frames.Count}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ContainsExpectedFrames(TestConfiguration config)
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ManagedFramesHaveValidMethodDescs(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle frame in DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread))
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
    public void FramesHaveRawContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IStackDataFrameHandle? firstFrame = DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread).FirstOrDefault();
        Assert.NotNull(firstFrame);

        byte[] context = stackWalk.GetRawContext(firstFrame);
        Assert.NotNull(context);
        Assert.True(context.Length > 0, "Expected non-empty raw context for stack frame");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_ReturnsNonEmptyContext(TestConfiguration config)
    {
        InitializeDumpTest(config);

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        uint allFlags = Contracts.StackWalkHelpers.IPlatformAgnosticContext.GetContextForPlatform(Target).AllContextFlags;
        byte[] context = Target.Contracts.StackWalk.GetContext(crashingThread, ThreadContextSource.None, allFlags);

        Assert.NotNull(context);
        Assert.True(context.Length > 0, "Expected non-empty context");

        var ctx = Contracts.StackWalkHelpers.IPlatformAgnosticContext.GetContextForPlatform(Target);
        ctx.FillFromBuffer(context);
        Assert.NotEqual(TargetCodePointer.Null, ctx.InstructionPointer);
    }
}

/// <summary>
/// Mini-tier coverage for the <c>clrthreads</c> scenario. A normal minidump iterates the
/// thread store while enumerating each thread's stack (<c>EnumMemDumpAllThreadsStack</c>),
/// so the thread store and every <see cref="ThreadData"/> record it touches are captured.
/// This validates that the Thread contract can enumerate the thread list from a mini dump
/// even though the GC heap is absent.
/// </summary>
public class MiniDumpThreadTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "mini";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ThreadStoreData_IsReadable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;
        Assert.NotNull(threadContract);

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.True(storeData.ThreadCount > 0, $"Expected at least one thread, got {storeData.ThreadCount}");
        Assert.NotEqual(TargetPointer.Null, storeData.FirstThread);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void EnumerateThreads_CanWalkThreadList(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        int count = 0;
        HashSet<uint> seenIds = new();
        TargetPointer currentThread = storeData.FirstThread;
        while (currentThread != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThread);
            count++;
            Assert.True(seenIds.Add(threadData.Id), $"Duplicate thread ID: {threadData.Id}");
            currentThread = threadData.NextThread;
        }

        Assert.Equal(storeData.ThreadCount, count);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ThreadStoreData_HasFinalizerThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.NotEqual(TargetPointer.Null, storeData.FinalizerThread);
    }
}
