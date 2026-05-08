// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

internal static class Program
{
    private static readonly ManualResetEventSlim OwnerHasLock = new(initialState: false);
    private static readonly ManualResetEventSlim ReleaseOwner = new(initialState: false);
    private static readonly CountdownEvent WaitersStarted = new(initialCount: 2);

    public static void Main()
    {
        object gate = new object();

        Thread owner = new(() =>
        {
            lock (gate)
            {
                lock (gate)
                {
                    OwnerHasLock.Set();
                    ReleaseOwner.Wait();
                }
            }
        })
        {
            IsBackground = true,
            Name = "Owner",
        };

        Thread waiter1 = new(() => Waiter(gate))
        {
            IsBackground = true,
            Name = "Waiter-1",
        };

        Thread waiter2 = new(() => Waiter(gate))
        {
            IsBackground = true,
            Name = "Waiter-2",
        };

        owner.Start();
        waiter1.Start();
        waiter2.Start();

        WaitersStarted.Wait();
        _ = SpinWait.SpinUntil(
            () => IsThreadWaiting(waiter1) && IsThreadWaiting(waiter2),
            TimeSpan.FromMilliseconds(500));

        Environment.FailFast("Intentional crash to dump threads with blocked waiters.");
    }

    private static bool IsThreadWaiting(Thread thread)
    {
        return (thread.ThreadState & ThreadState.WaitSleepJoin) != 0;
    }

    private static void Waiter(object gate)
    {
        OwnerHasLock.Wait();
        WaitersStarted.Signal();
        lock (gate)
        {
        }
    }
}
