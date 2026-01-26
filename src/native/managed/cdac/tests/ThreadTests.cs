// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ThreadTests
{
    private static Target CreateTarget(MockDescriptors.Thread thread)
    {
        MockTarget.Architecture arch = thread.Builder.TargetTestHelpers.Arch;
        var target = new TestPlaceholderTarget(arch, thread.Builder.GetMemoryContext().ReadFromTarget, thread.Types, thread.Globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Thread == ((IContractFactory<IThread>)new ThreadFactory()).CreateContract(target, 1)));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadStoreData(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        int threadCount = 15;
        int unstartedCount = 1;
        int backgroundCount = 2;
        int pendingCount = 3;
        int deadCount = 4;

        // Set thread store data
        thread.SetThreadCounts(
            threadCount,
            unstartedCount,
            backgroundCount,
            pendingCount,
            deadCount);

        Target target = CreateTarget(thread);

        // Validate the expected thread counts
        IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        ThreadStoreCounts counts = contract.GetThreadCounts();
        Assert.Equal(unstartedCount, counts.UnstartedThreadCount);
        Assert.Equal(backgroundCount, counts.BackgroundThreadCount);
        Assert.Equal(pendingCount, counts.PendingThreadCount);
        Assert.Equal(deadCount, counts.DeadThreadCount);

        ThreadStoreData data = contract.GetThreadStoreData();
        Assert.Equal(threadCount, data.ThreadCount);
        Assert.Equal(thread.FinalizerThreadAddress, data.FinalizerThread);
        Assert.Equal(thread.GCThreadAddress, data.GCThread);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadData(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        uint id = 1;
        TargetNUInt osId = new TargetNUInt(1234);

        // Add thread
        TargetPointer addr = thread.AddThread(id, osId);

        Target target = CreateTarget(thread);

        // Validate the expected thread counts
        IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        ThreadData data= contract.GetThreadData(addr);
        Assert.Equal(id, data.Id);
        Assert.Equal(osId, data.OSId);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IterateThreads(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        // Add threads
        uint expectedCount = 10;
        uint osIdStart = 1000;
        for (uint i = 1; i <= expectedCount; i++)
        {
            thread.AddThread(i, new TargetNUInt(i + osIdStart));
        }

        Target target = CreateTarget(thread);

        // Validate the expected thread counts
        IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        TargetPointer currentThread = contract.GetThreadStoreData().FirstThread;
        uint count = 0;
        while (currentThread != TargetPointer.Null)
        {
            count++;
            ThreadData threadData = contract.GetThreadData(currentThread);
            Assert.Equal(count, threadData.Id);
            Assert.Equal(count + osIdStart, threadData.OSId.Value);
            currentThread = threadData.NextThread;
        }

        Assert.Equal(expectedCount, count);
    }
}
