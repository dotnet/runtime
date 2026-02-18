// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Shared dump-based integration tests for the Thread contract.
/// Uses the BasicThreads debuggee dump, which spawns 5 named threads then crashes.
/// Subclasses specify which runtime version's dump to test against.
/// </summary>
public abstract class ThreadDumpTestsBase : DumpTestBase
{
    private const int SpawnedThreadCount = 5;

    protected ThreadDumpTestsBase()
    {
        LoadDump();
    }

    protected override string DebuggeeName => "BasicThreads";

    [Fact]
    public void ThreadStoreData_HasExpectedThreadCount()
    {
        IThread threadContract = Target.Contracts.Thread;
        Assert.NotNull(threadContract);

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.True(storeData.ThreadCount >= SpawnedThreadCount + 1,
            $"Expected at least {SpawnedThreadCount + 1} threads, got {storeData.ThreadCount}");
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "Thread.UEWatsonBucketTrackerBuckets field not present in .NET 10 contract descriptor")]
    public void EnumerateThreads_CanWalkThreadList()
    {
        IThread threadContract = Target.Contracts.Thread;
        Assert.NotNull(threadContract);

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        int count = 0;
        TargetPointer currentThread = storeData.FirstThread;
        while (currentThread != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThread);
            count++;
            currentThread = threadData.NextThread;

            Assert.NotEqual(new TargetNUInt(0), threadData.OSId);
        }

        Assert.Equal(storeData.ThreadCount, count);
    }

    [Fact]
    public void ThreadStoreData_HasFinalizerThread()
    {
        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.NotEqual(TargetPointer.Null, storeData.FinalizerThread);
    }

    [Fact]
    public void ThreadStoreData_HasGCThread()
    {
        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        // GC thread may or may not be set depending on runtime state at crash time,
        // but the field should be readable without throwing.
        _ = storeData.GCThread;
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "Thread.UEWatsonBucketTrackerBuckets field not present in .NET 10 contract descriptor")]
    public void Threads_HaveValidIds()
    {
        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        TargetPointer currentThread = storeData.FirstThread;
        HashSet<uint> seenIds = new();

        while (currentThread != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThread);
            Assert.True(seenIds.Add(threadData.Id), $"Duplicate thread ID: {threadData.Id}");
            currentThread = threadData.NextThread;
        }
    }
}

/// <summary>
/// Thread contract tests against a dump from the local (in-repo) runtime build.
/// </summary>
public class ThreadDumpTests_Local : ThreadDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

/// <summary>
/// Thread contract tests against a dump from the .NET 10 release runtime.
/// </summary>
public class ThreadDumpTests_Net10 : ThreadDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
