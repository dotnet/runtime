// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ThreadTests
{
    private static TestPlaceholderTarget CreateTarget(
        MockTarget.Architecture arch,
        Action<MockThreadBuilder> configure,
        bool hasProfilingSupport = true)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockThreadBuilder threadBuilder = new(targetBuilder.MemoryBuilder, hasProfilingSupport: hasProfilingSupport);
        configure(threadBuilder);

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(CreateContractTypes(threadBuilder))
            .AddGlobals(
                (nameof(Constants.Globals.ThreadStore), threadBuilder.ThreadStoreGlobalAddress),
                (nameof(Constants.Globals.FinalizerThread), threadBuilder.FinalizerThreadGlobalAddress),
                (nameof(Constants.Globals.GCThread), threadBuilder.GCThreadGlobalAddress))
            .AddContract<IThread>(version: "c1")
            .Build();

        return target;
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockThreadBuilder threadBuilder)
        => new()
        {
            [DataType.ExceptionInfo] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ExceptionInfoLayout),
            [DataType.Thread] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadLayout),
            [DataType.ThreadStore] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadStoreLayout),
            [DataType.GCAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.GCAllocContextLayout),
            [DataType.EEAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.EEAllocContextLayout),
            [DataType.RuntimeThreadLocals] = TargetTestHelpers.CreateTypeInfo(threadBuilder.RuntimeThreadLocalsLayout),
        };

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadStoreData(MockTarget.Architecture arch)
    {
        int threadCount = 15;
        int unstartedCount = 1;
        int backgroundCount = 2;
        int pendingCount = 3;
        int deadCount = 4;

        MockThreadBuilder? configuredBuilder = null;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                configuredBuilder = threadBuilder;
                threadBuilder.ThreadStore.ThreadCount = threadCount;
                threadBuilder.ThreadStore.UnstartedCount = unstartedCount;
                threadBuilder.ThreadStore.BackgroundCount = backgroundCount;
                threadBuilder.ThreadStore.PendingCount = pendingCount;
                threadBuilder.ThreadStore.DeadCount = deadCount;
            });

        IThread contract = target.Contracts.Thread;
        ThreadStoreCounts counts = contract.GetThreadCounts();
        Assert.Equal(unstartedCount, counts.UnstartedThreadCount);
        Assert.Equal(backgroundCount, counts.BackgroundThreadCount);
        Assert.Equal(pendingCount, counts.PendingThreadCount);
        Assert.Equal(deadCount, counts.DeadThreadCount);

        ThreadStoreData data = contract.GetThreadStoreData();
        Assert.Equal(threadCount, data.ThreadCount);
        Assert.Equal(new TargetPointer(configuredBuilder!.FinalizerThreadAddress), data.FinalizerThread);
        Assert.Equal(new TargetPointer(configuredBuilder.GCThreadAddress), data.GCThread);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadData(MockTarget.Architecture arch)
    {
        const uint id = 1;
        const ulong osId = 1234;
        MockThread? thread = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => thread = threadBuilder.AddThread(id, osId));

        IThread contract = target.Contracts.Thread;
        ThreadData data = contract.GetThreadData(new TargetPointer(thread!.Address));
        Assert.Equal(id, data.Id);
        Assert.Equal(new TargetNUInt(osId), data.OSId);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IterateThreads(MockTarget.Architecture arch)
    {
        const uint expectedCount = 10;
        const uint osIdStart = 1000;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                for (uint i = 1; i <= expectedCount; i++)
                {
                    threadBuilder.AddThread(i, i + osIdStart);
                }
            });

        IThread contract = target.Contracts.Thread;
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
        const long allocBytes = 1024;
        const long allocBytesLoh = 4096;
        MockThread? thread = null;
        MockRuntimeThreadLocals? runtimeThreadLocals = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                thread = threadBuilder.AddThread(1, 1234, allocBytes, allocBytesLoh);
                runtimeThreadLocals = threadBuilder.GetRuntimeThreadLocals(thread);
            });

        IThread contract = target.Contracts.Thread;
        contract.GetThreadAllocContext(new TargetPointer(thread!.Address), out long resultAllocBytes, out long resultAllocBytesLoh);
        Assert.Equal(allocBytes, resultAllocBytes);
        Assert.Equal(allocBytesLoh, resultAllocBytesLoh);
        Assert.Equal(allocBytes, runtimeThreadLocals!.GCAllocContext.AllocBytes);
        Assert.Equal(allocBytesLoh, runtimeThreadLocals.GCAllocContext.AllocBytesLoh);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadAllocContext_ZeroValues(MockTarget.Architecture arch)
    {
        MockThread? thread = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234));

        IThread contract = target.Contracts.Thread;
        contract.GetThreadAllocContext(new TargetPointer(thread!.Address), out long allocBytes, out long allocBytesLoh);
        Assert.Equal(0, allocBytes);
        Assert.Equal(0, allocBytesLoh);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetStackLimits(MockTarget.Architecture arch)
    {
        TargetPointer stackBase = new(0xAA00);
        TargetPointer stackLimit = new(0xA000);
        MockThread? thread = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                thread = threadBuilder.AddThread(1, 1234);
                thread.CachedStackBase = stackBase.Value;
                thread.CachedStackLimit = stackLimit.Value;
            });

        IThread contract = target.Contracts.Thread;
        contract.GetStackLimitData(new TargetPointer(thread!.Address), out TargetPointer outStackBase, out TargetPointer outStackLimit, out TargetPointer outFrameAddress);
        Assert.Equal(stackBase, outStackBase);
        Assert.Equal(stackLimit, outStackLimit);
        Assert.Equal(new TargetPointer(thread!.FrameAddress), outFrameAddress);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionHandle_NoException(MockTarget.Architecture arch)
    {
        MockThread? thread = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234));

        IThread contract = target.Contracts.Thread;
        TargetPointer thrownObjectHandle = contract.GetCurrentExceptionHandle(new TargetPointer(thread!.Address));
        Assert.Equal(TargetPointer.Null, thrownObjectHandle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionHandle_WithException(MockTarget.Architecture arch)
    {
        MockThread? thread = null;
        MockExceptionInfo? exceptionInfo = null;
        TargetPointer expectedObject = new(0xA001);

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                thread = threadBuilder.AddThread(1, 1234);
                exceptionInfo = threadBuilder.GetExceptionInfo(thread);
                TargetTestHelpers helpers = threadBuilder.Builder.TargetTestHelpers;
                MockMemorySpace.BumpAllocator allocator = threadBuilder.Builder.CreateAllocator(0x1_0000, 0x2_0000);
                MockMemorySpace.HeapFragment handleFragment = allocator.Allocate((ulong)helpers.PointerSize, "ThrownObjectHandle");
                helpers.WritePointer(handleFragment.Data, expectedObject);
                exceptionInfo!.ThrownObjectHandle = handleFragment.Address;
            });

        IThread contract = target.Contracts.Thread;
        TargetPointer thrownObjectHandle = contract.GetCurrentExceptionHandle(new TargetPointer(thread!.Address));
        Assert.Equal(new TargetPointer(exceptionInfo!.ThrownObjectHandle), thrownObjectHandle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionHandle_NullExceptionTracker(MockTarget.Architecture arch)
    {
        MockThread? thread = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                thread = threadBuilder.AddThread(1, 1234);
                thread.ExceptionTracker = 0;
            });

        IThread contract = target.Contracts.Thread;
        TargetPointer thrownObjectHandle = contract.GetCurrentExceptionHandle(new TargetPointer(thread!.Address));
        Assert.Equal(TargetPointer.Null, thrownObjectHandle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionHandle_HandlePointsToNull(MockTarget.Architecture arch)
    {
        MockThread? thread = null;
        MockExceptionInfo? exceptionInfo = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                thread = threadBuilder.AddThread(1, 1234);
                exceptionInfo = threadBuilder.GetExceptionInfo(thread);
                TargetTestHelpers helpers = threadBuilder.Builder.TargetTestHelpers;
                MockMemorySpace.BumpAllocator allocator = threadBuilder.Builder.CreateAllocator(0x1_0000, 0x2_0000);
                MockMemorySpace.HeapFragment handleFragment = allocator.Allocate((ulong)helpers.PointerSize, "ThrownObjectHandle");
                helpers.WritePointer(handleFragment.Data, TargetPointer.Null);
                exceptionInfo!.ThrownObjectHandle = handleFragment.Address;
            });

        IThread contract = target.Contracts.Thread;
        TargetPointer thrownObjectHandle = contract.GetCurrentExceptionHandle(new TargetPointer(thread!.Address));
        Assert.Equal(TargetPointer.Null, thrownObjectHandle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetThreadData_NoProfilerFilterContext(MockTarget.Architecture arch)
    {
        const uint id = 1;
        const ulong osId = 1234;
        MockThread? thread = null;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => thread = threadBuilder.AddThread(id, osId),
            hasProfilingSupport: false);

        IThread contract = target.Contracts.Thread;
        Assert.NotNull(contract);

        ThreadData data = contract.GetThreadData(new TargetPointer(thread!.Address));
        Assert.Equal(id, data.Id);
        Assert.Equal(new TargetNUInt(osId), data.OSId);
    }
}
