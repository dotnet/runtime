// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test: Verify that Environment.CurrentManagedThreadId is not treated as a
// JIT constant/pure value across async suspension points. In runtime async methods,
// the managed thread ID can change when the continuation resumes on a different thread.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2ManagedThreadId
{
    // Verify that Environment.CurrentManagedThreadId == Thread.CurrentThread.ManagedThreadId
    // both before and after an await that is known to suspend/resume (Task.Delay(1)).
    [Fact]
    public static void TestThreadIdMatchesCurrentThread()
    {
        TestThreadIdMatchesCurrentThreadAsync().GetAwaiter().GetResult();
    }

    private static async Task TestThreadIdMatchesCurrentThreadAsync()
    {
        Assert.Equal(Environment.CurrentManagedThreadId, Thread.CurrentThread.ManagedThreadId);
        await Task.Delay(1);
        Assert.Equal(Environment.CurrentManagedThreadId, Thread.CurrentThread.ManagedThreadId);
    }

    // Verify that after an await that resumes on a specific different thread, the thread ID
    // reflects the actual resumption thread, not the thread from before the await.
    [Fact]
    public static void TestThreadIdReflectsResumptionThread()
    {
        TestThreadIdReflectsResumptionThreadAsync().GetAwaiter().GetResult();
    }

    private static async Task TestThreadIdReflectsResumptionThreadAsync()
    {
        int threadIdBefore = Environment.CurrentManagedThreadId;

        // Switch to a brand-new dedicated thread; the continuation is guaranteed to run there.
        await new SwitchToNewThread();

        // After the await, the thread ID must always match the actual current thread.
        Assert.Equal(Environment.CurrentManagedThreadId, Thread.CurrentThread.ManagedThreadId);

        // A brand-new thread has a different ID than the thread from before the await.
        Assert.NotEqual(threadIdBefore, Environment.CurrentManagedThreadId);
    }

    // Custom awaiter that always resumes the continuation on a brand-new dedicated thread.
    private struct SwitchToNewThread : ICriticalNotifyCompletion
    {
        public SwitchToNewThread GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation) => throw new NotImplementedException();

        public void UnsafeOnCompleted(Action continuation) =>
            new Thread(_ => continuation()) { IsBackground = true }.Start();
    }
}
