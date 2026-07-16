// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataProcessTaskTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };

    [Fact]
    public void EnumTasks_ReturnsAllThreadsAndAdvancesCursor()
    {
        TargetPointer first = new(0x1000);
        TargetPointer second = new(0x2000);
        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadStoreData()).Returns(new ThreadStoreData(2, first, TargetPointer.Null, TargetPointer.Null));
        thread.Setup(t => t.GetThreadData(first)).Returns(
            ClrDataTaskTests.CreateThreadData(first, id: 10, osId: 100, nextThread: second, state: ThreadState.Unstarted));
        thread.Setup(t => t.GetThreadData(second)).Returns(
            ClrDataTaskTests.CreateThreadData(second, id: 20, osId: 200, state: ThreadState.Detached));

        IXCLRDataProcess process = CreateProcess(thread);
        ulong handle;
        Assert.Equal(HResults.S_OK, process.StartEnumTasks(&handle));
        Assert.NotEqual(0u, handle);

        DacComNullableByRef<IXCLRDataTask> taskOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.EnumTask(&handle, taskOut));
        Assert.Equal(10u, GetTaskId(Assert.IsAssignableFrom<IXCLRDataTask>(taskOut.Interface)));
        Assert.NotEqual(0u, handle);

        taskOut = new DacComNullableByRef<IXCLRDataTask>(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.EnumTask(&handle, taskOut));
        Assert.Equal(20u, GetTaskId(Assert.IsAssignableFrom<IXCLRDataTask>(taskOut.Interface)));
        Assert.Equal(0u, handle);

        taskOut = new DacComNullableByRef<IXCLRDataTask>(isNullRef: false);
        Assert.Equal(HResults.S_FALSE, process.EnumTask(&handle, taskOut));
        Assert.Null(taskOut.Interface);
        Assert.Equal(
            HResults.S_FALSE,
            process.EnumTask(&handle, new DacComNullableByRef<IXCLRDataTask>(isNullRef: true)));
        Assert.Equal(HResults.S_OK, process.EndEnumTasks(handle));
    }

    [Fact]
    public void StartEnumTasks_NoThreadsReturnsSFalse()
    {
        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadStoreData()).Returns(new ThreadStoreData(0, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null));

        IXCLRDataProcess process = CreateProcess(thread);
        ulong handle = ulong.MaxValue;
        Assert.Equal(HResults.S_FALSE, process.StartEnumTasks(&handle));
        Assert.Equal(0u, handle);
    }

    [Fact]
    public void EndEnumTasks_CanEndBeforeExhaustion()
    {
        TargetPointer first = new(0x1000);
        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadStoreData()).Returns(new ThreadStoreData(1, first, TargetPointer.Null, TargetPointer.Null));

        IXCLRDataProcess process = CreateProcess(thread);
        ulong handle;
        Assert.Equal(HResults.S_OK, process.StartEnumTasks(&handle));
        Assert.Equal(HResults.S_OK, process.EndEnumTasks(handle));
    }

    [Fact]
    public void GetTaskByUniqueID_ScansThreadStoreByManagedId()
    {
        TargetPointer first = new(0x1000);
        TargetPointer second = new(0x2000);
        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadStoreData()).Returns(new ThreadStoreData(2, first, TargetPointer.Null, TargetPointer.Null));
        thread.Setup(t => t.GetThreadData(first)).Returns(
            ClrDataTaskTests.CreateThreadData(first, id: 10, osId: 100, nextThread: second));
        thread.Setup(t => t.GetThreadData(second)).Returns(
            ClrDataTaskTests.CreateThreadData(second, id: 20, osId: 200));

        IXCLRDataProcess process = CreateProcess(thread);

        // Found via a linear scan of the thread store by managed thread id.
        DacComNullableByRef<IXCLRDataTask> taskOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.GetTaskByUniqueID(20, taskOut));
        Assert.Equal(20u, GetTaskId(Assert.IsAssignableFrom<IXCLRDataTask>(taskOut.Interface)));

        // Native casts the id to a 32-bit DWORD before comparing.
        taskOut = new DacComNullableByRef<IXCLRDataTask>(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.GetTaskByUniqueID((1ul << 32) | 10, taskOut));
        Assert.Equal(10u, GetTaskId(Assert.IsAssignableFrom<IXCLRDataTask>(taskOut.Interface)));

        // An id matching no live thread (e.g. a recycled dispenser slot value) returns E_INVALIDARG.
        taskOut = new DacComNullableByRef<IXCLRDataTask>(isNullRef: false);
        Assert.Equal(HResults.E_INVALIDARG, process.GetTaskByUniqueID(999, taskOut));
        Assert.Null(taskOut.Interface);
        Assert.Equal(
            HResults.E_INVALIDARG,
            process.GetTaskByUniqueID(999, new DacComNullableByRef<IXCLRDataTask>(isNullRef: true)));

        // The dispenser lookup (IThread.IdToThread) must not be consulted: recycled slots
        // can contain free-list integers rather than Thread pointers.
        thread.Verify(t => t.IdToThread(It.IsAny<uint>()), Times.Never);
    }

    private static IXCLRDataProcess CreateProcess(Mock<IThread> thread)
    {
        var builder = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1);
        builder.AddMockContract(thread);
        return new SOSDacImpl(builder.Build(), legacyObj: null);
    }

    private static ulong GetTaskId(IXCLRDataTask task)
    {
        ulong id;
        Assert.Equal(HResults.S_OK, task.GetUniqueID(&id));
        return id;
    }
}
