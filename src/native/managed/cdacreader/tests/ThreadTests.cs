// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

using MockThread = MockDescriptors.Thread;

public unsafe class ThreadTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadStoreData(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockThread thread = new(builder);
        builder = builder
            .SetContracts([nameof(Contracts.Thread)])
            .SetTypes(thread.Types)
            .SetGlobals(thread.Globals);

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

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);

        // Validate the expected thread counts
        Contracts.IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        Contracts.ThreadStoreCounts counts = contract.GetThreadCounts();
        Assert.Equal(unstartedCount, counts.UnstartedThreadCount);
        Assert.Equal(backgroundCount, counts.BackgroundThreadCount);
        Assert.Equal(pendingCount, counts.PendingThreadCount);
        Assert.Equal(deadCount, counts.DeadThreadCount);

        Contracts.ThreadStoreData data = contract.GetThreadStoreData();
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
        MockThread thread = new(builder);
        builder = builder
            .SetContracts([nameof(Contracts.Thread)])
            .SetTypes(thread.Types)
            .SetGlobals(thread.Globals);

        uint id = 1;
        TargetNUInt osId = new TargetNUInt(1234);

        // Add thread
        TargetPointer addr = thread.AddThread(id, osId);

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);

        // Validate the expected thread counts
        Contracts.IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        Contracts.ThreadData data= contract.GetThreadData(addr);
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
        MockThread thread = new(builder);
        builder = builder
            .SetContracts([nameof(Contracts.Thread)])
            .SetTypes(thread.Types)
            .SetGlobals(thread.Globals);

        // Add threads
        uint expectedCount = 10;
        uint osIdStart = 1000;
        for (uint i = 1; i <= expectedCount; i++)
        {
            thread.AddThread(i, new TargetNUInt(i + osIdStart));
        }

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);

        // Validate the expected thread counts
        Contracts.IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        TargetPointer currentThread = contract.GetThreadStoreData().FirstThread;
        uint count = 0;
        while (currentThread != TargetPointer.Null)
        {
            count++;
            Contracts.ThreadData threadData = contract.GetThreadData(currentThread);
            Assert.Equal(count, threadData.Id);
            Assert.Equal(count + osIdStart, threadData.OSId.Value);
            currentThread = threadData.NextThread;
        }

        Assert.Equal(expectedCount, count);
    }
}
