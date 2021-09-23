// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Collections.Generic;

// disable warnings about various Monitor members being obsolete
#pragma warning disable 0618

class MonEnterTests
{
    int m_failed;

    /// <summary>
    /// Not really negative tests, but testing expected failure modes
    /// </summary>
    void NegTests()
    {
        Console.WriteLine("null object tests");
        ExpectException<ArgumentNullException>(delegate { Monitor.Enter(null); });
        ExpectException<ArgumentNullException>(delegate { Monitor.TryEnter(null); });
        bool tookLock = false;
        ExpectException<ArgumentNullException>(delegate { Monitor.Enter(null, ref tookLock); });
        Assert(!tookLock);
        tookLock = false;
        ExpectException<ArgumentNullException>(delegate { Monitor.TryEnter(null, 0, ref tookLock); });
        Assert(!tookLock);
        tookLock = false;
        ExpectException<ArgumentNullException>(delegate { Monitor.TryEnter(null, TimeSpan.Zero, ref tookLock); });
        Assert(!tookLock);
        ExpectException<ArgumentNullException>(delegate { Monitor.TryEnter(null, 0); });
        ExpectException<ArgumentNullException>(delegate { Monitor.TryEnter(null, TimeSpan.Zero); });

        Console.WriteLine("tookLock == true tests");
        object obj = new object();
        tookLock = true;
        ExpectException<ArgumentException>(delegate { Monitor.Enter(obj, ref tookLock); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, false);
        tookLock = true;
        ExpectException<ArgumentException>(delegate { Monitor.TryEnter(obj, ref tookLock); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, false);
        tookLock = true;
        ExpectException<ArgumentException>(delegate { Monitor.TryEnter(obj, 0, ref tookLock); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, false);
        tookLock = true;
        ExpectException<ArgumentException>(delegate { Monitor.TryEnter(obj, TimeSpan.Zero, ref tookLock); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, false);

        Console.WriteLine("timeout < -1");
        tookLock = false;
        ExpectException<ArgumentOutOfRangeException>(delegate { Monitor.TryEnter(obj, -2, ref tookLock); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, tookLock);
        tookLock = false;
        ExpectException<ArgumentOutOfRangeException>(delegate { Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(-2), ref tookLock); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, tookLock);
        ExpectException<ArgumentOutOfRangeException>(delegate { tookLock = Monitor.TryEnter(obj, -2); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, tookLock);
        ExpectException<ArgumentOutOfRangeException>(delegate { tookLock = Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(-2)); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, tookLock);

        Console.WriteLine("timeout > int.Max");
        tookLock = false;
        ExpectException<ArgumentOutOfRangeException>(delegate { Monitor.TryEnter(obj, TimeSpan.FromMilliseconds((double)int.MaxValue + 1), ref tookLock); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, tookLock);
        ExpectException<ArgumentOutOfRangeException>(delegate { tookLock = Monitor.TryEnter(obj, TimeSpan.FromMilliseconds((double)int.MaxValue + 1)); });
        AssertTookLockAndRelease(LockIsHeld.No, obj, tookLock);
    }


    AutoResetEvent contentionStartEvent = new AutoResetEvent(false);
    volatile bool contentionStarted;
    volatile bool inContention;
    AutoResetEvent contentionDoneEvent = new AutoResetEvent(false);

    // for some reason I had to add these to get this to build in the test environment
    delegate void Action();
    delegate void Action<T>(T arg);
    delegate void Action<T1,T2>(T1 arg1, T2 arg2);

    /// <summary>
    /// Runs a lock acquisition scenario (passed in via <paramref name="run"/>) with contention
    /// on the lock.
    /// </summary>
    /// <param name="obj">The object to lock</param>
    /// <param name="spins">How many times to spin while holding the lock</param>
    /// <param name="run">A lock acquisition scenario to perform under contention (will be passed 
    ///     <paramref name="obj"/> as the object to be locked)</param>
    void RunWithContention(object obj, int spins, Action<object> run)
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            contentionStartEvent.WaitOne();
            Monitor.Enter(obj);
            inContention = true;
            contentionStarted = true;
            Thread.Sleep(0); // yield
            inContention = false;
            Monitor.Exit(obj);
            contentionDoneEvent.Set();
        });
        contentionStarted = false;
        inContention = false;
        Thread.Sleep(1);
        contentionStartEvent.Set();
        int waitCount = 0;
        while (!contentionStarted)
        {
            waitCount++;
            if (waitCount > 30000)
            {
                //Thread.Yield is internal in CoreClr, so change it to Thread.Sleep(0)
                // Thread.Yield();                
                Thread.Sleep(0);
                waitCount = 0;
            }
        }
        run(obj);
        contentionDoneEvent.WaitOne();
    }

    /// <summary>
    /// Runs all tests under varying levels of contention
    /// </summary>
    void ContentionVariants()
    {
        Console.WriteLine("--- no contention ---");
        SyncBlkVariants(LockIsHeld.No, delegate(object o, Action<object> ac) { ac(o); });

        Console.WriteLine("--- a little contention ---");
        SyncBlkVariants(LockIsHeld.Maybe, delegate(object o, Action<object> ac) { RunWithContention(o, 10000, ac); });

        Console.WriteLine("--- lots of contention ---");
        SyncBlkVariants(LockIsHeld.Maybe, delegate(object o, Action<object> ac) { RunWithContention(o, 1000000, ac); });
    }

    /// <summary>
    /// Runs all tests under each SyncBlk variation
    /// </summary>
    /// <param name="lockIsHeld">Whether to expect that the lock is being held (experiencing 
    /// contention) while each lock attempt is performed</param>
    /// <param name="scenario">A lock contention scenario to run under each SyncBlk scenario.  Is 
    /// passed an object that may or may not have a SyncBlk, and a lock acquisition scenario to run 
    /// under that level of contention</param>
    void SyncBlkVariants(LockIsHeld lockIsHeld, Action<object, Action<object>> scenario)
    {
        Console.WriteLine("Positive tests, no SyncBlk");
        MethodVariants(lockIsHeld, delegate(Action<object> innerScenario)
        {
            object obj = new object();
            scenario(obj, delegate(object o) { innerScenario(o); });
        });

        Console.WriteLine("Positive tests, with HashCode");
        MethodVariants(lockIsHeld, delegate(Action<object> innerScenario)
        {
            object obj = new object();
            obj.GetHashCode();
            scenario(obj, delegate(object o) { innerScenario(o); });
        });

        Console.WriteLine("Positive tests, with SyncBlk");
        MethodVariants(lockIsHeld, delegate(Action<object> innerScenario)
        {
            object obj = new object();
            obj.GetHashCode();
            Monitor.Enter(obj);
            Monitor.Exit(obj);
            scenario(obj, delegate(object o) { innerScenario(o); });
        });
    }

    enum LockIsHeld { Yes, No, Maybe }

    LockIsHeld Reverse(LockIsHeld lockIsHeld)
    {
        switch (lockIsHeld)
        {
            case LockIsHeld.Yes:
                return LockIsHeld.No;
            case LockIsHeld.No:
                return LockIsHeld.Yes;
            default:
                return lockIsHeld;
        }
    }

    /// <summary>
    /// Runs all lock acquisition scenarios inside of an outer contention/SyncBlk scenario
    /// </summary>
    /// <param name="lockIsHeld">whether to expect the lock to be held when we try to acquire it</param>
    /// <param name="scenario">The contention/SyncBlk scenario to run each acquisition scenario inside of.</param>
    void MethodVariants(LockIsHeld lockIsHeld, Action<Action<object>> scenario)
    {
        bool tookLock;

        scenario(delegate(object obj)
        {
            Monitor.Enter(obj);
            AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.Enter(obj, ref tookLock);
            AssertTookLockAndRelease(LockIsHeld.Yes, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = Monitor.TryEnter(obj);
            AssertTookLockAndRelease(Reverse(lockIsHeld), obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = Monitor.TryEnter(obj, 0);
            AssertTookLockAndRelease(Reverse(lockIsHeld), obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            DateTime start = DateTime.Now;
            tookLock = Monitor.TryEnter(obj, 10000);
            double elapsed = (DateTime.Now - start).TotalSeconds;
            AssertTookLockAndRelease(elapsed < 5.0 ? LockIsHeld.Yes : LockIsHeld.Maybe, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = Monitor.TryEnter(obj, Timeout.Infinite);
            AssertTookLockAndRelease(LockIsHeld.Yes, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(0));
            AssertTookLockAndRelease(Reverse(lockIsHeld), obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(10000));
            AssertTookLockAndRelease(LockIsHeld.Maybe, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(Timeout.Infinite));
            AssertTookLockAndRelease(LockIsHeld.Yes, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.TryEnter(obj, ref tookLock);
            AssertTookLockAndRelease(Reverse(lockIsHeld), obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.TryEnter(obj, 0, ref tookLock);
            AssertTookLockAndRelease(Reverse(lockIsHeld), obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.TryEnter(obj, 10000, ref tookLock);
            AssertTookLockAndRelease(LockIsHeld.Maybe, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.TryEnter(obj, Timeout.Infinite, ref tookLock);
            AssertTookLockAndRelease(LockIsHeld.Yes, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(0), ref tookLock);
            AssertTookLockAndRelease(Reverse(lockIsHeld), obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(10000), ref tookLock);
            AssertTookLockAndRelease(LockIsHeld.Maybe, obj, tookLock);
        });

        scenario(delegate(object obj)
        {
            tookLock = false;
            Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(Timeout.Infinite), ref tookLock);
            AssertTookLockAndRelease(LockIsHeld.Yes, obj, tookLock);
        });

        if (lockIsHeld == LockIsHeld.No)
        {
            scenario(delegate(object obj)
            {
                Monitor.Enter(obj);
                Monitor.Enter(obj);
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                AssertTookLockAndRelease(LockIsHeld.No, obj, false);
            });

            scenario(delegate(object obj)
            {
                Monitor.Enter(obj);
                tookLock = false;
                Monitor.Enter(obj, ref tookLock);
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, tookLock);
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                AssertTookLockAndRelease(LockIsHeld.No, obj, false);
            });

            scenario(delegate(object obj)
            {
                Monitor.Enter(obj);
                tookLock = false;
                Monitor.TryEnter(obj, ref tookLock);
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, tookLock);
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                AssertTookLockAndRelease(LockIsHeld.No, obj, false);
            });

            scenario(delegate(object obj)
            {
                Monitor.Enter(obj);
                for (int i = 0; i < 70; i++)
                {
                    Monitor.Enter(obj);
                }
                for (int i = 0; i < 70; i++)
                {
                    AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                }
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                AssertTookLockAndRelease(LockIsHeld.No, obj, false);
            });

            scenario(delegate(object obj)
            {
                Monitor.Enter(obj);
                for (int i = 0; i < 70; i++)
                {
                    tookLock = false;
                    Monitor.Enter(obj, ref tookLock);
                    Assert(tookLock);
                }
                for (int i = 0; i < 70; i++)
                {
                    AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                }
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                AssertTookLockAndRelease(LockIsHeld.No, obj, false);
            });

            scenario(delegate(object obj)
            {
                Monitor.Enter(obj);
                for (int i = 0; i < 70; i++)
                {
                    tookLock = false;
                    Monitor.TryEnter(obj, ref tookLock);
                    Assert(tookLock);
                }
                for (int i = 0; i < 70; i++)
                {
                    AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                }
                AssertTookLockAndRelease(LockIsHeld.Yes, obj, true);
                AssertTookLockAndRelease(LockIsHeld.No, obj, false);
            });
        }
    }

    void AssertTookLockAndRelease(LockIsHeld expected, object obj, bool tookLock)
    {
        Assert((expected == LockIsHeld.Yes && tookLock) ||
               (expected == LockIsHeld.No && !tookLock) ||
               expected == LockIsHeld.Maybe);

        if (tookLock) Assert(!inContention);

        bool exitFailed = false;
        try
        {
            Monitor.Exit(obj);
        }
        catch (SynchronizationLockException)
        {
            exitFailed = true;
        }

        Assert(tookLock == !exitFailed);
    }

    void ExpectException<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            if (!(e is T))
            {
                FailNoStack("Unexpected exception:\n{0}", e.ToString());
            }
            return;
        }
        Fail("Expected {0}, but got no exception!", typeof(T).Name);
    }

    void Assert(bool condition, string message)
    {
        if (!condition)
            Fail(message);
    }

    void Assert(bool condition)
    {
        Assert(condition, "Assertion failed");
    }

    void Fail(string message, bool printStack)
    {
        m_failed++;
        Console.WriteLine(message);
    }

    void Fail(string format, params object[] args)
    {
        Fail(string.Format(format, args), true);
    }

    void Fail(string format)
    {
        Fail(format, true);
    }

    void FailNoStack(string format, params object[] args)
    {
        Fail(string.Format(format, args), false);
    }


    void ThreadIdPosTests()
    {
        Console.WriteLine("*** High thread ID tests ***");

        // Create a thread with an ID > 1023 (the largest tid that can be stored in an
        // object header)

        List<Thread> threads = new List<Thread>(1024);
        for (int i = 0; i < 1024; i++)
        {
            threads.Add(new Thread(delegate() { Assert(false, "this thread should never run"); }));
        }

        List<Thread> highThreads = new List<Thread>();
        while (true)
        {
            Thread highThread = new Thread(delegate()
                {
                    Assert(Thread.CurrentThread.ManagedThreadId > 1024, "Managed thread id not high");
                    ContentionVariants();
                });

            // The newly allocated thread can have an id <= 1024 if the thread pool frees threads
            // as just the right time such that the newly allocated thread has a low thread id.
            // This is quite unlikely, but under GC stress it can happen
            if (highThread.ManagedThreadId <= 1024)
            {
                Assert(highThread.ManagedThreadId >= 0, "Thread id greater than 0");
                highThreads.Add(highThread);
                Console.WriteLine($"Allocated thread has low thread id {highThread.ManagedThreadId} {highThreads.Count} threads parked");

                // Validate that there are no thread id overlaps
                foreach (var thread in threads)
                {
                    Assert(thread.ManagedThreadId != highThread.ManagedThreadId, "ManagedThreadId duplicate");
                }

                // If this happens more than 512 times, something is very clearly broken
                if (highThreads.Count > 512)
                {
                    Assert(false, "Unable to create thread with ThreadId > 1024");
                    break;
                }

                // Retry with another parked thread id
                continue;
            }

            highThread.Start();
            highThread.Join();
            // Ran high thread id tests to completion
            break;
        }
    }

    int Run()
    {
        NegTests();
        ContentionVariants();
        ThreadIdPosTests();
        return m_failed;
    }

    static int Main()
    {
        MonEnterTests tests = new MonEnterTests();
        int failed = tests.Run();
        if (0 != failed)
        {
            Console.WriteLine("Failed!");
        }
        else
        {
            Console.WriteLine("Success!");
        }
        return 100 + failed;
    }
}

