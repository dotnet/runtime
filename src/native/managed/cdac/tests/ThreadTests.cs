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

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadAllocContext(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        long allocBytes = 1024;
        long allocBytesLoh = 4096;

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234), allocBytes, allocBytesLoh);

        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        contract.GetThreadAllocContext(addr, out long resultAllocBytes, out long resultAllocBytesLoh);
        Assert.Equal(allocBytes, resultAllocBytes);
        Assert.Equal(allocBytesLoh, resultAllocBytesLoh);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadAllocContext_ZeroValues(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));

        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        contract.GetThreadAllocContext(addr, out long allocBytes, out long allocBytesLoh);
        Assert.Equal(0, allocBytes);
        Assert.Equal(0, allocBytesLoh);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetStackLimits(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        uint id = 1;
        TargetNUInt osId = new TargetNUInt(1234);
        TargetPointer stackBase = new TargetPointer(0xAA00);
        TargetPointer stackLimit = new TargetPointer(0xA000);

        // Add thread and set stack limits
        TargetPointer addr = thread.AddThread(id, osId);
        thread.SetStackLimits(addr, stackBase, stackLimit);
        Target target = CreateTarget(thread);

        // Validate the expected stack limit values
        IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);
        Target.TypeInfo threadType = target.GetTypeInfo(DataType.Thread);
        TargetPointer expectedFrameAddr = addr + (ulong)threadType.Fields[nameof(Data.Thread.Frame)].Offset;
        TargetPointer outStackBase, outStackLimit, outFrameAddress;

        contract.GetStackLimitData(addr, out outStackBase, out outStackLimit, out outFrameAddress);
        Assert.Equal(stackBase, outStackBase);
        Assert.Equal(stackLimit, outStackLimit);
        Assert.Equal(expectedFrameAddr, outFrameAddress);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPartialUserState_BackgroundThread(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        // TS_Background = 0x200
        thread.SetThreadState(addr, 0x200);
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        int userState = contract.GetPartialUserState(addr);
        // USER_BACKGROUND = 0x4
        Assert.Equal(0x4, userState & 0x4);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPartialUserState_CombinedFlags(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        // TS_Background(0x200) | TS_TPWorkerThread(0x1000000) | TS_Dead(0x800)
        // TSNC_DebuggerSleepWaitJoin(0x04000000)
        thread.SetThreadState(addr, 0x200 | 0x1000000 | 0x800, stateNC: 0x04000000);
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        int userState = contract.GetPartialUserState(addr);
        // USER_BACKGROUND(0x4) | USER_STOPPED(0x10) | USER_WAIT_SLEEP_JOIN(0x20) | USER_THREADPOOL(0x100)
        Assert.Equal(0x4 | 0x10 | 0x20 | 0x100, userState);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPartialUserState_NoFlags(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        int userState = contract.GetPartialUserState(addr);
        Assert.Equal(0, userState);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadHandle(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        TargetPointer expectedHandle = new TargetPointer(0xDEAD);
        thread.SetThreadHandle(addr, expectedHandle);
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        TargetPointer handle = contract.GetThreadHandle(addr);
        Assert.Equal(expectedHandle, handle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetExposedObject(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        TargetPointer expectedGCHandle = new TargetPointer(0xBEEF);
        thread.SetGCHandle(addr, expectedGCHandle);
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        TargetPointer gcHandle = contract.GetExposedObject(addr);
        Assert.Equal(expectedGCHandle, gcHandle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentCustomDebuggerNotification(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        TargetPointer expectedNotification = new TargetPointer(0xCAFE);
        thread.SetCurrNotification(addr, expectedNotification);
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        TargetPointer notification = contract.GetCurrentCustomDebuggerNotification(addr);
        Assert.Equal(expectedNotification, notification);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionHandle_NoException(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        // The default ExceptionTracker points to an ExceptionInfo with null ThrownObjectHandle
        TargetPointer handle = contract.GetCurrentExceptionHandle(addr);
        Assert.Equal(TargetPointer.Null, handle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionHandle_WithException(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.Thread thread = new(builder);

        TargetPointer addr = thread.AddThread(1, new TargetNUInt(1234));
        TargetPointer expectedHandle = new TargetPointer(0xFACE);
        thread.SetExceptionThrownObjectHandle(addr, expectedHandle);
        Target target = CreateTarget(thread);
        IThread contract = target.Contracts.Thread;

        TargetPointer handle = contract.GetCurrentExceptionHandle(addr);
        Assert.Equal(expectedHandle, handle);
    }
}
