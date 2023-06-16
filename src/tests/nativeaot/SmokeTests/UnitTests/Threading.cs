// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

// TODO: Move these tests to CoreFX once they can be run on CoreRT

internal static class Threading
{
    private const int Pass = 100;

    public static int Run()
    {
        Console.WriteLine("    WaitSubsystemTests.DoubleSetOnEventWithTimedOutWaiterShouldNotStayInWaitersList");
        WaitSubsystemTests.DoubleSetOnEventWithTimedOutWaiterShouldNotStayInWaitersList();

        Console.WriteLine("    WaitSubsystemTests.ManualResetEventTest");
        WaitSubsystemTests.ManualResetEventTest();

        Console.WriteLine("    WaitSubsystemTests.AutoResetEventTest");
        WaitSubsystemTests.AutoResetEventTest();

        Console.WriteLine("    WaitSubsystemTests.SemaphoreTest");
        WaitSubsystemTests.SemaphoreTest();

        Console.WriteLine("    WaitSubsystemTests.MutexTest");
        WaitSubsystemTests.MutexTest();

        Console.WriteLine("    WaitSubsystemTests.WaitDurationTest");
        WaitSubsystemTests.WaitDurationTest();

        // This test takes a long time to run in release and especially in debug builds. Enable for manual testing.
        //Console.WriteLine("    WaitSubsystemTests.MutexMaximumReacquireCountTest");
        //WaitSubsystemTests.MutexMaximumReacquireCountTest();

        Console.WriteLine("    TimersCreatedConcurrentlyOnDifferentThreadsAllFire");
        TimerTests.TimersCreatedConcurrentlyOnDifferentThreadsAllFire();

        Console.WriteLine("    ThreadPoolTests.RunProcessorCountItemsInParallel");
        ThreadPoolTests.RunProcessorCountItemsInParallel();

        Console.WriteLine("    ThreadPoolTests.RunMoreThanMaxJobsMakesOneJobWaitForStarvationDetection");
        ThreadPoolTests.RunMoreThanMaxJobsMakesOneJobWaitForStarvationDetection();

        Console.WriteLine("    ThreadPoolTests.ThreadPoolCanPickUpOneJobWhenThreadIsAvailable");
        ThreadPoolTests.ThreadPoolCanPickUpOneJobWhenThreadIsAvailable();

        Console.WriteLine("    ThreadPoolTests.ThreadPoolCanPickUpMultipleJobsWhenThreadsAreAvailable");
        ThreadPoolTests.ThreadPoolCanPickUpMultipleJobsWhenThreadsAreAvailable();

        Console.WriteLine("    ThreadPoolTests.ThreadPoolCanProcessManyWorkItemsInParallelWithoutDeadlocking");
        ThreadPoolTests.ThreadPoolCanProcessManyWorkItemsInParallelWithoutDeadlocking();

        // This test takes a long time to run (min 42 seconds sleeping). Enable for manual testing.
        // Console.WriteLine("    ThreadPoolTests.RunJobsAfterThreadTimeout");
        // ThreadPoolTests.RunJobsAfterThreadTimeout();

        Console.WriteLine("    ThreadPoolTests.WorkQueueDepletionTest");
        ThreadPoolTests.WorkQueueDepletionTest();

        Console.WriteLine("    ThreadPoolTests.WorkerThreadStateReset");
        ThreadPoolTests.WorkerThreadStateReset();

        // This test is not applicable (and will not pass) on Windows since it uses the Windows OS-provided thread pool.
        // Console.WriteLine("    ThreadPoolTests.SettingMinThreadsWillCreateThreadsUpToMinimum");
        // ThreadPoolTests.SettingMinThreadsWillCreateThreadsUpToMinimum();

        Console.WriteLine("    WaitThreadTests.SignalingRegisteredHandleCallsCalback");
        WaitThreadTests.SignalingRegisteredHandleCallsCalback();

        Console.WriteLine("    WaitThreadTests.TimingOutRegisteredHandleCallsCallback");
        WaitThreadTests.TimingOutRegisteredHandleCallsCallback();

        Console.WriteLine("    WaitThreadTests.UnregisteringBeforeSignalingDoesNotCallCallback");
        WaitThreadTests.UnregisteringBeforeSignalingDoesNotCallCallback();

        Console.WriteLine("    WaitThreadTests.RepeatingWaitFiresUntilUnregistered");
        WaitThreadTests.RepeatingWaitFiresUntilUnregistered();

        Console.WriteLine("    WaitThreadTests.UnregisterEventSignaledWhenUnregistered");
        WaitThreadTests.UnregisterEventSignaledWhenUnregistered();

        Console.WriteLine("    WaitThreadTests.CanRegisterMoreThan64Waits");
        WaitThreadTests.CanRegisterMoreThan64Waits();

        Console.WriteLine("    WaitThreadTests.StateIsPasssedThroughToCallback");
        WaitThreadTests.StateIsPasssedThroughToCallback();


        // This test takes a long time to run. Enable for manual testing.
        // Console.WriteLine("    WaitThreadTests.WaitWithLongerTimeoutThanWaitThreadCanStillTimeout");
        // WaitThreadTests.WaitWithLongerTimeoutThanWaitThreadCanStillTimeout();

        Console.WriteLine("    WaitThreadTests.UnregisterCallbackIsNotCalledAfterCallbackFinishesIfAnotherCallbackOnSameWaitRunning");
        WaitThreadTests.UnregisterCallbackIsNotCalledAfterCallbackFinishesIfAnotherCallbackOnSameWaitRunning();

        Console.WriteLine("    WaitThreadTests.CallingUnregisterOnAutomaticallyUnregisteredHandleReturnsTrue");
        WaitThreadTests.CallingUnregisterOnAutomaticallyUnregisteredHandleReturnsTrue();

        Console.WriteLine("    WaitThreadTests.EventSetAfterUnregisterNotObservedOnWaitThread");
        WaitThreadTests.EventSetAfterUnregisterNotObservedOnWaitThread();

        Console.WriteLine("    WaitThreadTests.BlockingUnregister");
        WaitThreadTests.BlockingUnregister();

        Console.WriteLine("    WaitThreadTests.CanDisposeEventAfterUnblockingUnregister");
        WaitThreadTests.CanDisposeEventAfterUnblockingUnregister();

        Console.WriteLine("    WaitThreadTests.UnregisterEventSignaledWhenUnregisteredEvenIfAutoUnregistered");
        WaitThreadTests.UnregisterEventSignaledWhenUnregisteredEvenIfAutoUnregistered();


        Console.WriteLine("    WaitThreadTests.BlockingUnregisterBlocksEvenIfCallbackExecuting");
        WaitThreadTests.BlockingUnregisterBlocksEvenIfCallbackExecuting();

        return Pass;
    }
}

internal static class WaitSubsystemTests
{
    private const int AcceptableEarlyWaitTerminationDiffMilliseconds = -100;
    private const int AcceptableLateWaitTerminationDiffMilliseconds = 300;

    [Fact]
    public static void ManualResetEventTest()
    {
        // Constructor and dispose

        var e = new ManualResetEvent(false);
        Assert.False(e.WaitOne(0));
        e.Dispose();
        Assert.Throws<ObjectDisposedException>(() => e.Reset());
        Assert.Throws<ObjectDisposedException>(() => e.Set());
        Assert.Throws<ObjectDisposedException>(() => e.WaitOne(0));

        e = new ManualResetEvent(true);
        Assert.True(e.WaitOne(0));

        // Set and reset

        e = new ManualResetEvent(true);
        e.Reset();
        Assert.False(e.WaitOne(0));
        Assert.False(e.WaitOne(0));
        e.Reset();
        Assert.False(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne(0));
        Assert.True(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne(0));

        // Wait

        e.Set();
        Assert.True(e.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.True(e.WaitOne());

        e.Reset();
        Assert.False(e.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));

        e = null;

        // Multi-wait with all indexes set
        var es =
            new ManualResetEvent[]
            {
                new ManualResetEvent(true),
                new ManualResetEvent(true),
                new ManualResetEvent(true),
                new ManualResetEvent(true)
            };
        Assert.Equal(0, WaitHandle.WaitAny(es, 0));
        Assert.Equal(0, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.Equal(0, WaitHandle.WaitAny(es));
        Assert.True(WaitHandle.WaitAll(es, 0));
        Assert.True(WaitHandle.WaitAll(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.True(WaitHandle.WaitAll(es));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.True(es[i].WaitOne(0));
        }

        // Multi-wait with indexes 1 and 2 set
        es[0].Reset();
        es[3].Reset();
        Assert.Equal(1, WaitHandle.WaitAny(es, 0));
        Assert.Equal(1, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 1 || i == 2, es[i].WaitOne(0));
        }

        // Multi-wait with all indexes reset
        es[1].Reset();
        es[2].Reset();
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, 0));
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
        }
    }

    [Fact]
    public static void AutoResetEventTest()
    {
        // Constructor and dispose

        var e = new AutoResetEvent(false);
        Assert.False(e.WaitOne(0));
        e.Dispose();
        Assert.Throws<ObjectDisposedException>(() => e.Reset());
        Assert.Throws<ObjectDisposedException>(() => e.Set());
        Assert.Throws<ObjectDisposedException>(() => e.WaitOne(0));

        e = new AutoResetEvent(true);
        Assert.True(e.WaitOne(0));

        // Set and reset

        e = new AutoResetEvent(true);
        e.Reset();
        Assert.False(e.WaitOne(0));
        Assert.False(e.WaitOne(0));
        e.Reset();
        Assert.False(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne(0));
        Assert.False(e.WaitOne(0));
        e.Set();
        e.Set();
        Assert.True(e.WaitOne(0));

        // Wait

        e.Set();
        Assert.True(e.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.False(e.WaitOne(0));
        e.Set();
        Assert.True(e.WaitOne());
        Assert.False(e.WaitOne(0));

        e.Reset();
        Assert.False(e.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));

        e = null;

        // Multi-wait with all indexes set
        var es =
            new AutoResetEvent[]
            {
                new AutoResetEvent(true),
                new AutoResetEvent(true),
                new AutoResetEvent(true),
                new AutoResetEvent(true)
            };
        Assert.Equal(0, WaitHandle.WaitAny(es, 0));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i > 0, es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.Equal(0, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i > 0, es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.Equal(0, WaitHandle.WaitAny(es));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i > 0, es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.True(WaitHandle.WaitAll(es, 0));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.True(WaitHandle.WaitAll(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
            es[i].Set();
        }
        Assert.True(WaitHandle.WaitAll(es));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
        }

        // Multi-wait with indexes 1 and 2 set
        es[1].Set();
        es[2].Set();
        Assert.Equal(1, WaitHandle.WaitAny(es, 0));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 2, es[i].WaitOne(0));
        }
        es[1].Set();
        es[2].Set();
        Assert.Equal(1, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 2, es[i].WaitOne(0));
        }
        es[1].Set();
        es[2].Set();
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.Equal(i == 1 || i == 2, es[i].WaitOne(0));
        }

        // Multi-wait with all indexes reset
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, 0));
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(es, 0));
        Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < es.Length; ++i)
        {
            Assert.False(es[i].WaitOne(0));
        }
    }

    [Fact]
    public static void SemaphoreTest()
    {
        // Constructor and dispose

        Assert.Throws<ArgumentOutOfRangeException>(() => new Semaphore(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Semaphore(0, 0));
        Assert.Throws<ArgumentException>(() => new Semaphore(2, 1));

        var s = new Semaphore(0, 1);
        Assert.False(s.WaitOne(0));
        s.Dispose();
        Assert.Throws<ObjectDisposedException>(() => s.Release());
        Assert.Throws<ObjectDisposedException>(() => s.WaitOne(0));

        s = new Semaphore(1, 1);
        Assert.True(s.WaitOne(0));

        // Signal and unsignal

        Assert.Throws<ArgumentOutOfRangeException>(() => s.Release(0));

        s = new Semaphore(1, 1);
        Assert.True(s.WaitOne(0));
        Assert.False(s.WaitOne(0));
        Assert.False(s.WaitOne(0));
        Assert.Equal(0, s.Release());
        Assert.True(s.WaitOne(0));
        Assert.Throws<SemaphoreFullException>(() => s.Release(2));
        Assert.Equal(0, s.Release());
        Assert.Throws<SemaphoreFullException>(() => s.Release());

        s = new Semaphore(1, 2);
        Assert.Throws<SemaphoreFullException>(() => s.Release(2));
        Assert.Equal(1, s.Release(1));
        Assert.True(s.WaitOne(0));
        Assert.True(s.WaitOne(0));
        Assert.Throws<SemaphoreFullException>(() => s.Release(3));
        Assert.Equal(0, s.Release(2));
        Assert.Throws<SemaphoreFullException>(() => s.Release());

        // Wait

        s = new Semaphore(1, 2);
        Assert.True(s.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.False(s.WaitOne(0));
        s.Release();
        Assert.True(s.WaitOne());
        Assert.False(s.WaitOne(0));
        s.Release(2);
        Assert.True(s.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        s.Release();
        Assert.True(s.WaitOne());

        s = new Semaphore(0, 2);
        Assert.False(s.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));

        s = null;

        // Multi-wait with all indexes signaled
        var ss =
            new Semaphore[]
            {
                new Semaphore(1, 1),
                new Semaphore(1, 1),
                new Semaphore(1, 1),
                new Semaphore(1, 1)
            };
        Assert.Equal(0, WaitHandle.WaitAny(ss, 0));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i > 0, ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.Equal(0, WaitHandle.WaitAny(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i > 0, ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.Equal(0, WaitHandle.WaitAny(ss));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i > 0, ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.True(WaitHandle.WaitAll(ss, 0));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.True(WaitHandle.WaitAll(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
            ss[i].Release();
        }
        Assert.True(WaitHandle.WaitAll(ss));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
        }

        // Multi-wait with indexes 1 and 2 signaled
        ss[1].Release();
        ss[2].Release();
        Assert.Equal(1, WaitHandle.WaitAny(ss, 0));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i == 2, ss[i].WaitOne(0));
        }
        ss[1].Release();
        ss[2].Release();
        Assert.Equal(1, WaitHandle.WaitAny(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i == 2, ss[i].WaitOne(0));
        }
        ss[1].Release();
        ss[2].Release();
        Assert.False(WaitHandle.WaitAll(ss, 0));
        Assert.False(WaitHandle.WaitAll(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.Equal(i == 1 || i == 2, ss[i].WaitOne(0));
        }

        // Multi-wait with all indexes unsignaled
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(ss, 0));
        Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        Assert.False(WaitHandle.WaitAll(ss, 0));
        Assert.False(WaitHandle.WaitAll(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        for (int i = 0; i < ss.Length; ++i)
        {
            Assert.False(ss[i].WaitOne(0));
        }
    }

    // There is a race condition between a timed out WaitOne and a Set call not clearing the waiters list
    // in the wait subsystem (Unix only). More information can be found at
    // https://github.com/dotnet/corert/issues/3616 and https://github.com/dotnet/corert/pull/3782.
    [Fact]
    public static void DoubleSetOnEventWithTimedOutWaiterShouldNotStayInWaitersList()
    {
        AutoResetEvent threadStartedEvent = new AutoResetEvent(false);
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        Thread thread = new Thread(() => {
            threadStartedEvent.Set();
            Thread.Sleep(50);
            resetEvent.Set();
            resetEvent.Set();
        });

        thread.IsBackground = true;
        thread.Start();
        threadStartedEvent.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
        resetEvent.WaitOne(50);
        thread.Join(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
    }

    [Fact]
    public static void MutexTest()
    {
        // Constructor and dispose

        var m = new Mutex();
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        m.Dispose();
        Assert.Throws<ObjectDisposedException>(() => m.ReleaseMutex());
        Assert.Throws<ObjectDisposedException>(() => m.WaitOne(0));

        m = new Mutex(false);
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();

        m = new Mutex(true);
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        m.ReleaseMutex();

        m = new Mutex(true);
        Assert.True(m.WaitOne(0));
        m.Dispose();
        Assert.Throws<ObjectDisposedException>(() => m.ReleaseMutex());
        Assert.Throws<ObjectDisposedException>(() => m.WaitOne(0));

        // Acquire and release

        m = new Mutex();
        VerifyThrowsApplicationException(() => m.ReleaseMutex());
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        VerifyThrowsApplicationException(() => m.ReleaseMutex());
        Assert.True(m.WaitOne(0));
        Assert.True(m.WaitOne(0));
        Assert.True(m.WaitOne(0));
        m.ReleaseMutex();
        m.ReleaseMutex();
        m.ReleaseMutex();
        VerifyThrowsApplicationException(() => m.ReleaseMutex());

        // Wait

        Assert.True(m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.True(m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        m.ReleaseMutex();
        m.ReleaseMutex();
        Assert.True(m.WaitOne());
        Assert.True(m.WaitOne());
        m.ReleaseMutex();
        m.ReleaseMutex();

        m = null;

        // Multi-wait with all indexes unlocked
        var ms =
            new Mutex[]
            {
                new Mutex(),
                new Mutex(),
                new Mutex(),
                new Mutex()
            };
        Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
        ms[0].ReleaseMutex();
        for (int i = 1; i < ms.Length; ++i)
        {
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
        Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        ms[0].ReleaseMutex();
        for (int i = 1; i < ms.Length; ++i)
        {
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
        Assert.Equal(0, WaitHandle.WaitAny(ms));
        ms[0].ReleaseMutex();
        for (int i = 1; i < ms.Length; ++i)
        {
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
        Assert.True(WaitHandle.WaitAll(ms, 0));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }

        // Multi-wait with indexes 0 and 3 locked
        ms[0].WaitOne(0);
        ms[3].WaitOne(0);
        Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        Assert.Equal(0, WaitHandle.WaitAny(ms));
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        Assert.True(WaitHandle.WaitAll(ms, 0));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        ms[0].ReleaseMutex();
        VerifyThrowsApplicationException(() => ms[1].ReleaseMutex());
        VerifyThrowsApplicationException(() => ms[2].ReleaseMutex());
        ms[3].ReleaseMutex();

        // Multi-wait with all indexes locked
        for (int i = 0; i < ms.Length; ++i)
        {
            Assert.True(ms[i].WaitOne(0));
        }
        Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
        ms[0].ReleaseMutex();
        Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        ms[0].ReleaseMutex();
        Assert.Equal(0, WaitHandle.WaitAny(ms));
        ms[0].ReleaseMutex();
        Assert.True(WaitHandle.WaitAll(ms, 0));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        Assert.True(WaitHandle.WaitAll(ms));
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
        }
        for (int i = 0; i < ms.Length; ++i)
        {
            ms[i].ReleaseMutex();
            VerifyThrowsApplicationException(() => ms[i].ReleaseMutex());
        }
    }

    private static void VerifyThrowsApplicationException(Action action)
    {
        // TODO: netstandard2.0 - After switching to ns2.0 contracts, use Assert.Throws<T> instead of this function
        // TODO: Enable this verification. There currently seem to be some reliability issues surrounding exceptions on Unix.
        //try
        //{
        //    action();
        //}
        //catch (Exception ex)
        //{
        //    if (ex.HResult == unchecked((int)0x80131600))
        //        return;
        //    Console.WriteLine(
        //        "VerifyThrowsApplicationException: Assertion failure - Assert.Throws<ApplicationException>: got {1}",
        //        ex.GetType());
        //    throw new AssertionFailureException(ex);
        //}
        //Console.WriteLine(
        //    "VerifyThrowsApplicationException: Assertion failure - Assert.Throws<ApplicationException>: got no exception");
        //throw new AssertionFailureException();
    }

    [Fact]
    [OuterLoop]
    public static void WaitDurationTest()
    {
        VerifyWaitDuration(
            new ManualResetEvent(false),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds);
        VerifyWaitDuration(
            new ManualResetEvent(true),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);

        VerifyWaitDuration(
            new AutoResetEvent(false),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds);
        VerifyWaitDuration(
            new AutoResetEvent(true),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);

        VerifyWaitDuration(
            new Semaphore(0, 1),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds);
        VerifyWaitDuration(
            new Semaphore(1, 1),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);

        VerifyWaitDuration(
            new Mutex(true),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);
        VerifyWaitDuration(
            new Mutex(false),
            waitTimeoutMilliseconds: ThreadTestHelpers.ExpectedMeasurableTimeoutMilliseconds,
            expectedWaitTerminationAfterMilliseconds: 0);
    }

    private static void VerifyWaitDuration(
        WaitHandle wh,
        int waitTimeoutMilliseconds,
        int expectedWaitTerminationAfterMilliseconds)
    {
        Assert.True(waitTimeoutMilliseconds > 0);
        Assert.True(expectedWaitTerminationAfterMilliseconds >= 0);

        var sw = new Stopwatch();
        sw.Restart();
        Assert.Equal(expectedWaitTerminationAfterMilliseconds == 0, wh.WaitOne(waitTimeoutMilliseconds));
        sw.Stop();
        int waitDurationDiffMilliseconds = (int)sw.ElapsedMilliseconds - expectedWaitTerminationAfterMilliseconds;
        Assert.True(waitDurationDiffMilliseconds >= AcceptableEarlyWaitTerminationDiffMilliseconds);
        Assert.True(waitDurationDiffMilliseconds <= AcceptableLateWaitTerminationDiffMilliseconds);
    }

    //[Fact] // This test takes a long time to run in release and especially in debug builds. Enable for manual testing.
    public static void MutexMaximumReacquireCountTest()
    {
        // Create a mutex with the maximum reacquire count
        var m = new Mutex();
        int tenPercent = int.MaxValue / 10;
        int progressPercent = 0;
        Console.Write("        0%");
        for (int i = 0; i >= 0; ++i)
        {
            if (i >= (progressPercent + 1) * tenPercent)
            {
                ++progressPercent;
                if (progressPercent < 10)
                {
                    Console.Write(" {0}%", progressPercent * 10);
                }
            }
            Assert.True(m.WaitOne(0));
        }
        Assert.Throws<OverflowException>(
            () =>
            {
                // Windows allows a slightly higher maximum reacquire count than this implementation
                Assert.True(m.WaitOne(0));
                Assert.True(m.WaitOne(0));
            });
        Console.WriteLine(" 100%");

        // Single wait fails
        Assert.Throws<OverflowException>(() => m.WaitOne(0));
        Assert.Throws<OverflowException>(() => m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        Assert.Throws<OverflowException>(() => m.WaitOne());

        var e0 = new AutoResetEvent(false);
        var s0 = new Semaphore(0, 1);
        var e1 = new AutoResetEvent(false);
        var s1 = new Semaphore(0, 1);
        var h = new WaitHandle[] { e0, s0, m, e1, s1 };
        Action<bool, bool, bool, bool> init =
            (signale0, signals0, signale1, signals1) =>
            {
                if (signale0)
                    e0.Set();
                else
                    e0.Reset();
                s0.WaitOne(0);
                if (signals0)
                    s0.Release();
                if (signale1)
                    e1.Set();
                else
                    e1.Reset();
                s1.WaitOne(0);
                if (signals1)
                    s1.Release();
            };
        Action<bool, bool, bool, bool> verify =
            (e0signaled, s0signaled, e1signaled, s1signaled) =>
            {
                Assert.Equal(e0signaled, e0.WaitOne(0));
                Assert.Equal(s0signaled, s0.WaitOne(0));
                Assert.Throws<OverflowException>(() => m.WaitOne(0));
                Assert.Equal(e1signaled, e1.WaitOne(0));
                Assert.Equal(s1signaled, s1.WaitOne(0));
                init(e0signaled, s0signaled, e1signaled, s1signaled);
            };

        // WaitAny succeeds when a signaled object is before the mutex
        init(true, true, true, true);
        Assert.Equal(0, WaitHandle.WaitAny(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(false, true, true, true);
        Assert.Equal(1, WaitHandle.WaitAny(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(false, false, true, true);

        // WaitAny fails when all objects before the mutex are unsignaled
        init(false, false, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAny(h, 0));
        verify(false, false, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAny(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(false, false, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAny(h));
        verify(false, false, true, true);

        // WaitAll fails and does not unsignal any signaled object
        init(true, true, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAll(h, 0));
        verify(true, true, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAll(h, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
        verify(true, true, true, true);
        Assert.Throws<OverflowException>(() => WaitHandle.WaitAll(h));
        verify(true, true, true, true);

        m.Dispose();
    }
}

internal static class TimerTests
{
    [Fact]
    public static void TimersCreatedConcurrentlyOnDifferentThreadsAllFire()
    {
        int processorCount = Environment.ProcessorCount;

        int timerTickCount = 0;
        TimerCallback timerCallback = data => Interlocked.Increment(ref timerTickCount);

        var threadStarted = new AutoResetEvent(false);
        var createTimers = new ManualResetEvent(false);
        var timers = new Timer[processorCount];
        Action<object> createTimerThreadStart = data =>
        {
            int i = (int)data;
            var sw = new Stopwatch();
            threadStarted.Set();
            createTimers.WaitOne();

            // Use the CPU a bit around creating the timer to try to have some of these threads run concurrently
            sw.Restart();
            do
            {
                Thread.SpinWait(1000);
            } while (sw.ElapsedMilliseconds < 10);

            timers[i] = new Timer(timerCallback, null, 1, Timeout.Infinite);

            // Use the CPU a bit around creating the timer to try to have some of these threads run concurrently
            sw.Restart();
            do
            {
                Thread.SpinWait(1000);
            } while (sw.ElapsedMilliseconds < 10);
        };

        var waitsForThread = new Action[timers.Length];
        for (int i = 0; i < timers.Length; ++i)
        {
            var t = ThreadTestHelpers.CreateGuardedThread(out waitsForThread[i], createTimerThreadStart);
            t.IsBackground = true;
            t.Start(i);
            threadStarted.CheckedWait();
        }

        createTimers.Set();
        ThreadTestHelpers.WaitForCondition(() => timerTickCount == timers.Length);

        foreach (var waitForThread in waitsForThread)
        {
            waitForThread();
        }
    }
}

internal static class ThreadPoolTests
{
    [Fact]
    public static void RunProcessorCountItemsInParallel()
    {
        int count = 0;
        AutoResetEvent e0 = new AutoResetEvent(false);
        for(int i = 0; i < Environment.ProcessorCount; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount)
                {
                    e0.Set();
                }
            });
        }
        e0.CheckedWait();
        // Run the test again to make sure we can reuse the threads.
        count = 0;
        for(int i = 0; i < Environment.ProcessorCount; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount)
                {
                    e0.Set();
                }
            });
        }
        e0.CheckedWait();
    }

    [Fact]
    public static void RunMoreThanMaxJobsMakesOneJobWaitForStarvationDetection()
    {
        ManualResetEvent e0 = new ManualResetEvent(false);
        AutoResetEvent jobsQueued = new AutoResetEvent(false);
        int count = 0;
        AutoResetEvent e1 = new AutoResetEvent(false);
        for(int i = 0; i < Environment.ProcessorCount; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount)
                {
                    jobsQueued.Set();
                }
                e0.CheckedWait();
            });
        }
        jobsQueued.CheckedWait();
        ThreadPool.QueueUserWorkItem( _ => e1.Set());
        Thread.Sleep(500); // Sleep for the gate thread delay to wait for starvation
        e1.CheckedWait();
        e0.Set();
    }

    [Fact]
    public static void ThreadPoolCanPickUpOneJobWhenThreadIsAvailable()
    {
        ManualResetEvent e0 = new ManualResetEvent(false);
        AutoResetEvent jobsQueued = new AutoResetEvent(false);
        AutoResetEvent testJobCompleted = new AutoResetEvent(false);
        int count = 0;

        for(int i = 0; i < Environment.ProcessorCount - 1; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount - 1)
                {
                    jobsQueued.Set();
                }
                e0.CheckedWait();
            });
        }
        jobsQueued.CheckedWait();
        ThreadPool.QueueUserWorkItem( _ => testJobCompleted.Set());
        testJobCompleted.CheckedWait();
        e0.Set();
    }

    [Fact]
    public static void ThreadPoolCanPickUpMultipleJobsWhenThreadsAreAvailable()
    {
        ManualResetEvent e0 = new ManualResetEvent(false);
        AutoResetEvent jobsQueued = new AutoResetEvent(false);
        AutoResetEvent testJobCompleted = new AutoResetEvent(false);
        int count = 0;

        for(int i = 0; i < Environment.ProcessorCount - 1; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount - 1)
                {
                    jobsQueued.Set();
                }
                e0.CheckedWait();
            });
        }
        jobsQueued.CheckedWait();
        int testJobsCount = 0;
        int maxCount = 5;
        void Job(object _)
        {
            if(Interlocked.Increment(ref testJobsCount) != maxCount)
            {
                ThreadPool.QueueUserWorkItem(Job);
            }
            else
            {
                testJobCompleted.Set();
            }
        }
        ThreadPool.QueueUserWorkItem(Job);
        testJobCompleted.CheckedWait();
        e0.Set();
    }

    // See https://github.com/dotnet/corert/issues/6780
    [Fact]
    public static void ThreadPoolCanProcessManyWorkItemsInParallelWithoutDeadlocking()
    {
        int iterationCount = 100_000;
        var done = new ManualResetEvent(false);

        WaitCallback wc = null;
        wc = data =>
        {
            int n = Interlocked.Decrement(ref iterationCount);
            if (n == 0)
            {
                done.Set();
            }
            else if (n > 0)
            {
                ThreadPool.QueueUserWorkItem(wc);
            }
        };

        for (int i = 0, n = Environment.ProcessorCount; i < n; ++i)
        {
            ThreadPool.QueueUserWorkItem(wc);
        }
        done.WaitOne();
    }

    private static WaitCallback CreateRecursiveJob(int jobCount, int targetJobCount, AutoResetEvent testJobCompleted)
    {
        return _ =>
        {
            if (jobCount == targetJobCount)
            {
                testJobCompleted.Set();
            }
            else
            {
                ThreadPool.QueueUserWorkItem(CreateRecursiveJob(jobCount + 1, targetJobCount, testJobCompleted));
            }
        };
    }

    [Fact]
    [OuterLoop]
    public static void RunJobsAfterThreadTimeout()
    {
        ManualResetEvent e0 = new ManualResetEvent(false);
        AutoResetEvent jobsQueued = new AutoResetEvent(false);
        AutoResetEvent testJobCompleted = new AutoResetEvent(false);
        int count = 0;

        for(int i = 0; i < Environment.ProcessorCount - 1; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount - 1)
                {
                    jobsQueued.Set();
                }
                e0.CheckedWait();
            });
        }
        jobsQueued.CheckedWait();
        ThreadPool.QueueUserWorkItem( _ => testJobCompleted.Set());
        testJobCompleted.CheckedWait();
        Console.Write("Sleeping to time out thread\n");
        Thread.Sleep(21000);
        ThreadPool.QueueUserWorkItem( _ => testJobCompleted.Set());
        testJobCompleted.CheckedWait();
        e0.Set();
        Console.Write("Sleeping to time out all threads\n");
        Thread.Sleep(21000);
        ThreadPool.QueueUserWorkItem( _ => testJobCompleted.Set());
        testJobCompleted.CheckedWait();
    }

    [Fact]
    public static void WorkQueueDepletionTest()
    {
        ManualResetEvent e0 = new ManualResetEvent(false);
        int numLocalScheduled = 1;
        int numGlobalScheduled = 1;
        int numToSchedule = Environment.ProcessorCount * 64;
        int numCompleted = 0;
        object syncRoot = new object();
        void ThreadLocalJob()
        {
            if(Interlocked.Increment(ref numLocalScheduled) <= numToSchedule)
            {
                Task.Factory.StartNew(ThreadLocalJob);
            }
            if(Interlocked.Increment(ref numLocalScheduled) <= numToSchedule)
            {
                Task.Factory.StartNew(ThreadLocalJob);
            }
            if (Interlocked.Increment(ref numCompleted) == numToSchedule * 2)
            {
                e0.Set();
            }
        }
        void GlobalJob(object _)
        {
            if(Interlocked.Increment(ref numGlobalScheduled) <= numToSchedule)
            {
                ThreadPool.QueueUserWorkItem(GlobalJob);
            }
            if(Interlocked.Increment(ref numGlobalScheduled) <= numToSchedule)
            {
                ThreadPool.QueueUserWorkItem(GlobalJob);
            }
            if (Interlocked.Increment(ref numCompleted) == numToSchedule * 2)
            {
                e0.Set();
            }
        }
        Task.Factory.StartNew(ThreadLocalJob);
        ThreadPool.QueueUserWorkItem(GlobalJob);
        e0.CheckedWait();
    }

    [Fact]
    public static void WorkerThreadStateReset()
    {
        var cultureInfo = new CultureInfo("pt-BR");
        var expectedCultureInfo = CultureInfo.CurrentCulture;
        var expectedUICultureInfo = CultureInfo.CurrentUICulture;
        int count = 0;
        AutoResetEvent e0 = new AutoResetEvent(false);
        for(int i = 0; i < Environment.ProcessorCount; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount)
                {
                    e0.Set();
                }
            });
        }
        e0.CheckedWait();
        // Run the test again to make sure we can reuse the threads.
        count = 0;
        for(int i = 0; i < Environment.ProcessorCount; ++i)
        {
            ThreadPool.QueueUserWorkItem( _ => {
                Assert.Equal(expectedCultureInfo, CultureInfo.CurrentCulture);
                Assert.Equal(expectedUICultureInfo, CultureInfo.CurrentUICulture);
                Assert.Equal(ThreadPriority.Normal, Thread.CurrentThread.Priority);
                if(Interlocked.Increment(ref count) == Environment.ProcessorCount)
                {
                    e0.Set();
                }
            });
        }
        e0.CheckedWait();
    }

    [Fact]
    public static void SettingMinThreadsWillCreateThreadsUpToMinimum()
    {
        ThreadPool.GetMinThreads(out int minThreads, out int unusedMin);
        ThreadPool.GetMaxThreads(out int maxThreads, out int unusedMax);
        try
        {
            ManualResetEvent e0 = new ManualResetEvent(false);
            AutoResetEvent jobsQueued = new AutoResetEvent(false);
            int count = 0;
            ThreadPool.SetMaxThreads(minThreads, unusedMax);
            for(int i = 0; i < minThreads + 1; ++i)
            {
                ThreadPool.QueueUserWorkItem( _ => {
                    if(Interlocked.Increment(ref count) == minThreads + 1)
                    {
                        jobsQueued.Set();
                    }
                    e0.CheckedWait();
                });
            }
            Assert.False(jobsQueued.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            Assert.True(ThreadPool.SetMaxThreads(minThreads + 1, unusedMax));
            Assert.True(ThreadPool.SetMinThreads(minThreads + 1, unusedMin));

            jobsQueued.CheckedWait();

            e0.Set();
        }
        finally
        {
            ThreadPool.SetMinThreads(minThreads, unusedMin);
            ThreadPool.SetMaxThreads(maxThreads, unusedMax);
        }
    }
}

internal static class WaitThreadTests
{
    private const int WaitThreadTimeoutTimeMs = 20000;

    [Fact]
    public static void SignalingRegisteredHandleCallsCalback()
    {
        var e0 = new AutoResetEvent(false);
        var e1 = new AutoResetEvent(false);
        ThreadPool.RegisterWaitForSingleObject(e0, (_, timedOut) =>
        {
            if(!timedOut)
            {
                e1.Set();
            }
        }, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        e0.Set();
        e1.CheckedWait();
    }

    [Fact]
    public static void TimingOutRegisteredHandleCallsCallback()
    {
        var e0 = new AutoResetEvent(false);
        var e1 = new AutoResetEvent(false);
        ThreadPool.RegisterWaitForSingleObject(e0, (_, timedOut) =>
        {
            if(timedOut)
            {
                e1.Set();
            }
        }, null, ThreadTestHelpers.ExpectedTimeoutMilliseconds, true);
        e1.CheckedWait();
    }

    [Fact]
    public static void UnregisteringBeforeSignalingDoesNotCallCallback()
    {
        var e0 = new AutoResetEvent(false);
        var e1 = new AutoResetEvent(false);
        var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) =>
        {
            e1.Set();
        }, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        registeredWaitHandle.Unregister(null);
        Assert.False(e1.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
    }

    [Fact]
    public static void RepeatingWaitFiresUntilUnregistered()
    {
        var e0 = new AutoResetEvent(false);
        var e1 = new AutoResetEvent(false);
        var registered = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) =>
        {
            e1.Set();
        }, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, false);
        for (int i = 0; i < 4; ++i)
        {
            e0.Set();
            e1.CheckedWait();
        }
        registered.Unregister(null);
        e0.Set();
        Assert.False(e1.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
    }

    [Fact]
    public static void UnregisterEventSignaledWhenUnregistered()
    {
        var e0 = new AutoResetEvent(false);
        var e1 = new AutoResetEvent(false);
        var registered = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) => {}, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        registered.Unregister(e1);
        e1.CheckedWait();
    }

    [Fact]
    public static void CanRegisterMoreThan64Waits()
    {
        RegisteredWaitHandle[] handles = new RegisteredWaitHandle[65];
        for(int i = 0; i < 65; ++i) {
            handles[i] = ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(false), (_, __) => {}, null, -1, true);
        }
        for(int i = 0; i < 65; ++i) {
            handles[i].Unregister(null);
        }
    }

    [Fact]
    public static void StateIsPasssedThroughToCallback()
    {
        object state = new object();
        AutoResetEvent e0 = new AutoResetEvent(false);
        ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(true), (callbackState, _) =>
        {
            if(state == callbackState)
            {
                e0.Set();
            }
        }, state, 0, true);
        e0.CheckedWait();
    }

    [Fact]
    [OuterLoop]
    public static void WaitWithLongerTimeoutThanWaitThreadCanStillTimeout()
    {
        AutoResetEvent e0 = new AutoResetEvent(false);
        ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(false), (_, __) => e0.Set(), null, WaitThreadTimeoutTimeMs + 1000, true);
        Thread.Sleep(WaitThreadTimeoutTimeMs);
        e0.CheckedWait();
    }

    [Fact]
    public static void UnregisterCallbackIsNotCalledAfterCallbackFinishesIfAnotherCallbackOnSameWaitRunning()
    {
        AutoResetEvent e0 = new AutoResetEvent(false);
        AutoResetEvent e1 = new AutoResetEvent(false);
        AutoResetEvent e2 = new AutoResetEvent(false);
        RegisteredWaitHandle handle = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) =>
        {
            e2.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
        }, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, false);
        e0.Set();
        Thread.Sleep(50);
        e0.Set();
        Thread.Sleep(50);
        handle.Unregister(e1);
        Assert.False(e1.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        e2.Set();
    }

    [Fact]
    public static void CallingUnregisterOnAutomaticallyUnregisteredHandleReturnsTrue()
    {
        AutoResetEvent e0 = new AutoResetEvent(false);
        RegisteredWaitHandle handle = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) => {}, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        e0.Set();
        Thread.Sleep(ThreadTestHelpers.ExpectedTimeoutMilliseconds);
        Assert.True(handle.Unregister(null));
    }

    [Fact]
    public static void EventSetAfterUnregisterNotObservedOnWaitThread()
    {
        AutoResetEvent e0 = new AutoResetEvent(false);
        RegisteredWaitHandle handle = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) => {}, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        handle.Unregister(null);
        e0.Set();
        e0.CheckedWait();
    }

    [Fact]
    public static void BlockingUnregister()
    {
        RegisteredWaitHandle handle = ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(false), (_, __) => {}, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        handle.Unregister(new InvalidWaitHandle());
    }

    [Fact]
    public static void CanDisposeEventAfterUnblockingUnregister()
    {
        using(var e0 = new AutoResetEvent(false))
        {
            RegisteredWaitHandle handle = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) => {}, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
            handle.Unregister(null);
        }
    }

    [Fact]
    public static void UnregisterEventSignaledWhenUnregisteredEvenIfAutoUnregistered()
    {
        var e0 = new AutoResetEvent(false);
        RegisteredWaitHandle handle = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) => {}, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        e0.Set();
        Thread.Sleep(50); // Ensure the callback has happened
        var e1 = new AutoResetEvent(false);
        handle.Unregister(e1);
        e1.CheckedWait();
    }

    [Fact]
    public static void BlockingUnregisterBlocksEvenIfCallbackExecuting()
    {
        bool callbackComplete = false;
        var e0 = new AutoResetEvent(false);
        RegisteredWaitHandle handle = ThreadPool.RegisterWaitForSingleObject(e0, (_, __) =>
        {
            Thread.Sleep(300);
            callbackComplete = true;
        }, null, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, true);
        e0.Set();
        Thread.Sleep(100); // Give the wait thread time to process removals.
        handle.Unregister(new InvalidWaitHandle());
        Assert.True(callbackComplete);
    }
}

internal static class ThreadTestHelpers
{
    public const int ExpectedTimeoutMilliseconds = 50;
    public const int ExpectedMeasurableTimeoutMilliseconds = 500;
    public const int UnexpectedTimeoutMilliseconds = 1000 * 30;

    // Wait longer for a thread to time out, so that an unexpected timeout in the thread is more likely to expire first and
    // provide a better stack trace for the failure
    public const int UnexpectedThreadTimeoutMilliseconds = UnexpectedTimeoutMilliseconds * 2;

    public static Thread CreateGuardedThread(out Action waitForThread, Action<object> start)
    {
        Action checkForThreadErrors;
        return CreateGuardedThread(out checkForThreadErrors, out waitForThread, start);
    }

    public static Thread CreateGuardedThread(out Action checkForThreadErrors, out Action waitForThread, Action<object> start)
    {
        Exception backgroundEx = null;
        var t =
            new Thread(parameter =>
            {
                try
                {
                    start(parameter);
                }
                catch (Exception ex)
                {
                    backgroundEx = ex;
                    Interlocked.MemoryBarrier();
                }
            });
        Action localCheckForThreadErrors = checkForThreadErrors = // cannot use ref or out parameters in lambda
            () =>
            {
                Interlocked.MemoryBarrier();
                if (backgroundEx != null)
                {
                    throw new AggregateException(backgroundEx);
                }
            };
        waitForThread =
            () =>
            {
                Assert.True(t.Join(UnexpectedThreadTimeoutMilliseconds));
                localCheckForThreadErrors();
            };
        return t;
    }

    public static void WaitForCondition(Func<bool> condition)
    {
        WaitForConditionWithCustomDelay(condition, () => Thread.Sleep(1));
    }

    public static void WaitForConditionWithCustomDelay(Func<bool> condition, Action delay)
    {
        if (condition())
        {
            return;
        }

        var startTime = Environment.TickCount;
        do
        {
            delay();
            Assert.True(Environment.TickCount - startTime < UnexpectedTimeoutMilliseconds);
        } while (!condition());
    }

    public static void CheckedWait(this WaitHandle wh)
    {
        Assert.True(wh.WaitOne(UnexpectedTimeoutMilliseconds));
    }
}

internal sealed class InvalidWaitHandle : WaitHandle
{
}

internal sealed class Stopwatch
{
    private int _startTimeMs;
    private int _endTimeMs;
    private bool _isStopped;

    public void Restart()
    {
        _isStopped = false;
        _startTimeMs = Environment.TickCount;
    }

    public void Stop()
    {
        _endTimeMs = Environment.TickCount;
        _isStopped = true;
    }

    public long ElapsedMilliseconds => (_isStopped ? _endTimeMs : Environment.TickCount) - _startTimeMs;
}

internal static class Assert
{
    public static void False(bool condition)
    {
        if (!condition)
            return;
        Console.WriteLine("Assertion failure - Assert.False");
        throw new AssertionFailureException();
    }

    public static void True(bool condition)
    {
        if (condition)
            return;
        Console.WriteLine("Assertion failure - Assert.True");
        throw new AssertionFailureException();
    }

    public static void Same<T>(T expected, T actual) where T : class
    {
        if (expected == actual)
            return;
        Console.WriteLine("Assertion failure - Assert.Same({0}, {1})", expected, actual);
        throw new AssertionFailureException();
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
            return;
        Console.WriteLine("Assertion failure - Assert.Equal({0}, {1})", expected, actual);
        throw new AssertionFailureException();
    }

    public static void NotEqual<T>(T notExpected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(notExpected, actual))
            return;
        Console.WriteLine("Assertion failure - Assert.NotEqual({0}, {1})", notExpected, actual);
        throw new AssertionFailureException();
    }

    public static void Throws<T>(Action action) where T : Exception
    {
        // TODO: Enable Assert.Throws<T> tests. There currently seem to be some reliability issues surrounding exceptions on Unix.
        //try
        //{
        //    action();
        //}
        //catch (T ex)
        //{
        //    if (ex.GetType() == typeof(T))
        //        return;
        //    Console.WriteLine("Assertion failure - Assert.Throws<{0}>: got {1}", typeof(T), ex.GetType());
        //    throw new AssertionFailureException(ex);
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Assertion failure - Assert.Throws<{0}>: got {1}", typeof(T), ex.GetType());
        //    throw new AssertionFailureException(ex);
        //}
        //Console.WriteLine("Assertion failure - Assert.Throws<{0}>: got no exception", typeof(T));
        //throw new AssertionFailureException();
    }

    public static void Null(object value)
    {
        if (value == null)
            return;
        Console.WriteLine("Assertion failure - Assert.Null");
        throw new AssertionFailureException();
    }

    public static void NotNull(object value)
    {
        if (value != null)
            return;
        Console.WriteLine("Assertion failure - Assert.NotNull");
        throw new AssertionFailureException();
    }
}

internal class AssertionFailureException : Exception
{
    public AssertionFailureException()
    {
    }

    public AssertionFailureException(string message) : base(message)
    {
    }

    public AssertionFailureException(Exception innerException) : base(null, innerException)
    {
    }
}

internal class FactAttribute : Attribute
{
}

internal class OuterLoopAttribute : Attribute
{
}
