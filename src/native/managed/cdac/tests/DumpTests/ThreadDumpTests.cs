// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Thread contract.
/// Uses the BasicThreads debuggee dump, which spawns 5 named threads then crashes.
/// </summary>
public class ThreadDumpTests : DumpTestBase
{
    private const int SpawnedThreadCount = 5;

    protected override string DebuggeeName => "BasicThreads";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void ThreadStoreData_HasExpectedThreadCount(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;
        Assert.NotNull(threadContract);

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.True(storeData.ThreadCount >= SpawnedThreadCount + 1,
            $"Expected at least {SpawnedThreadCount + 1} threads, got {storeData.ThreadCount}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void EnumerateThreads_CanWalkThreadList(TestConfiguration config)
    {
        InitializeDumpTest(config);
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void ThreadStoreData_HasFinalizerThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.NotEqual(TargetPointer.Null, storeData.FinalizerThread);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void ThreadStoreData_HasGCThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        // GC thread may or may not be set depending on runtime state at crash time,
        // but the field should be readable without throwing.
        _ = storeData.GCThread;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void Threads_HaveValidIds(TestConfiguration config)
    {
        InitializeDumpTest(config);
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void ThreadCounts_AreNonNegative(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;
        ThreadStoreCounts counts = threadContract.GetThreadCounts();

        Assert.True(counts.UnstartedThreadCount >= 0, $"UnstartedThreadCount should be non-negative, got {counts.UnstartedThreadCount}");
        Assert.True(counts.BackgroundThreadCount >= 0, $"BackgroundThreadCount should be non-negative, got {counts.BackgroundThreadCount}");
        Assert.True(counts.PendingThreadCount >= 0, $"PendingThreadCount should be non-negative, got {counts.PendingThreadCount}");
        Assert.True(counts.DeadThreadCount >= 0, $"DeadThreadCount should be non-negative, got {counts.DeadThreadCount}");
    }
}
