// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.InteropServices;
using Xunit;
using System.Runtime.CompilerServices;

public delegate void MyCallback();

public class ForeignThreadCleanupTest
{
    [DllImport("ForeignThreadCleanupNative")]
    public static extern void InvokeCallbackOnNewThread(MyCallback callback, [MarshalAs(UnmanagedType.U1)] bool joinBeforeReturn);

    private sealed class WaitForMutexInFinalizer(AutoResetEvent startEvent, Mutex mutex)
    {
        public static bool TestPassed = false;
        ~WaitForMutexInFinalizer()
        {
            // Wait for the foreign thread to have exited.
            startEvent.WaitOne();
            // Now try to acquire the mutex that the foreign thread is holding.
            try
            {
                mutex.WaitOne();
            }
            catch (AbandonedMutexException)
            {
                // Expected
                TestPassed = true;
            }
        }
    }

    [Fact]
    [SkipOnMono("Mono cleans up threads on the finalizer thread always.")]
    public static void AbandonMutexOnForeignThread()
    {
        using Mutex mutex = new();
        using AutoResetEvent startEvent = new(false);
        (WeakReference shortRef, WeakReference longRef) = ConstructMutexWaiter(startEvent, mutex);
        GC.Collect();
        Assert.False(shortRef.IsAlive, "GC should have collected the joiner object.");

        bool mutexAcquired = false;

        MyCallback cb = () =>
        {
            mutexAcquired = mutex.WaitOne(0);
        };

        // Start a foreign thread that acquires the mutex and then exits without releasing it.
        InvokeCallbackOnNewThread(cb, joinBeforeReturn: true);

        GC.KeepAlive(cb);

        Assert.True(mutexAcquired, "Foreign thread should have acquired the mutex.");

        // Allow the waiter object to try to wait on the mutex in its finalizer.
        startEvent.Set();

        while (longRef.IsAlive)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.True(WaitForMutexInFinalizer.TestPassed, "Finalizer should have detected abandoned mutex.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        (WeakReference, WeakReference) ConstructMutexWaiter(AutoResetEvent startEvent, Mutex mutex)
        {
            WaitForMutexInFinalizer waiter = new(startEvent, mutex);
            return (new WeakReference(waiter), new WeakReference(waiter, trackResurrection: true));
        }
    }

    private sealed class JoinThreadInFinalizer(AutoResetEvent startTestEvent, AutoResetEvent exitForeignThreadEvent)
    {
        public static Thread ForeignThread;
        public static bool TestPassed = false;
        ~JoinThreadInFinalizer()
        {
            startTestEvent.WaitOne();
            // Signal the foreign thread to exit.
            exitForeignThreadEvent.Set();
            // Now join the foreign thread.
            ForeignThread.Join();
            TestPassed = true;
        }
    }

    [Fact]
    [SkipOnMono("Mono cleans up threads on the finalizer thread always.")]
    public static void JoinForeignThreadOnFinalizerThread()
    {
        using AutoResetEvent startTestEvent = new(false);
        using AutoResetEvent exitForeignThreadEvent = new(false);

        // Start a foreign thread that waits for a signal to exit.
        MyCallback cb = () =>
        {
            JoinThreadInFinalizer.ForeignThread = Thread.CurrentThread;
            startTestEvent.Set();
            exitForeignThreadEvent.WaitOne();
        };
        InvokeCallbackOnNewThread(cb, joinBeforeReturn: false);

        (WeakReference shortRef, WeakReference longRef) = ConstructThreadJoiner(startTestEvent, exitForeignThreadEvent);
        GC.Collect();
        Assert.False(shortRef.IsAlive, "GC should have collected the joiner object.");

        // Wait until the joiner object is finalized.
        while (longRef.IsAlive)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.True(JoinThreadInFinalizer.TestPassed, "Finalizer should have been able to join the foreign thread.");

        GC.KeepAlive(cb);

        [MethodImpl(MethodImplOptions.NoInlining)]
        (WeakReference, WeakReference) ConstructThreadJoiner(AutoResetEvent startTestEvent, AutoResetEvent exitForeignThreadEvent)
        {
            JoinThreadInFinalizer joiner = new(startTestEvent, exitForeignThreadEvent);
            return (new WeakReference(joiner), new WeakReference(joiner, trackResurrection: true));
        }
    }
}
