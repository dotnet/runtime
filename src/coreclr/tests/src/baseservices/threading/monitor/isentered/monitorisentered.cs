// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿#define DEBUG //make sure the Contract calls actually do something

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;


public static class MonitorIsHeldTest
{
    static int s_result = 100;

    static int Main()
    {
        try
        {
            RunNullTest();
            RunTest(new object(), "object without SyncBlk");
            RunTest(CreateObjectWithSyncBlk(), "object with SyncBlk");

            Console.WriteLine(s_result == 100 ? "Success!" : "FAILED!");
            return s_result;
        }
        catch (Exception e)
        {
            Console.WriteLine("Unhandled exception!");
            Console.WriteLine(e);
            return 999;
        }
    }

    static object CreateObjectWithSyncBlk()
    {
        // force an object to have a SyncBlk, to exercise the IsEntered code that
        // deals with these
        object o = new object();
        o.GetHashCode();
        return o;
    }

    static void RunNullTest()
    {
        try
        {
            Monitor.IsEntered(null);
        }
        catch (ArgumentNullException)
        {
            // expected
            return;
        }

        // no exception
        Console.WriteLine("Monitor.IsEntered(null) did not throw ArgumentNullException");
        s_result = 101;
    }

    static void RunTest(object obj, string caseName)
    {
        // lock not held by anyone
        if (Monitor.IsEntered(obj))
        {
            Console.WriteLine("{0}: lock should be initially unheld, but IsEntered == true", caseName);
            s_result = 102;
        }

        // lock held by this thread
        lock (obj)
        {
            if (!Monitor.IsEntered(obj))
            {
                Console.WriteLine("{0}: lock should be held by this thread, but IsEntered == false", caseName);
                s_result = 103;
            }

            // do it the way the user would
            Contract.Assert(Monitor.IsEntered(obj));
        }

        // now it's released
        if (Monitor.IsEntered(obj))
        {
            Console.WriteLine("{0}: lock should have been released, but IsEntered == true", caseName);
            s_result = 104;
        }

        // do it the way the user would
        Contract.Assert(!Monitor.IsEntered(obj));

        // Make another thread hold the lock
        ManualResetEventSlim lockHeldEvent = new ManualResetEventSlim();
        ManualResetEventSlim releaseLockEvent = new ManualResetEventSlim();

        Task otherThread = Task.Factory.StartNew(() =>
            {
                lock (obj)
                {
                    Contract.Assert(Monitor.IsEntered(obj));
                    lockHeldEvent.Set();
                    releaseLockEvent.Wait();
                }
            });

        lockHeldEvent.Wait();

        // lock is held by other thread, so we should get "false" 
        if (Monitor.IsEntered(obj))
        {
            Console.WriteLine("{0}: lock should be held by other thread, but IsEntered == true", caseName);
            s_result = 105;
        }

        releaseLockEvent.Set();
        otherThread.Wait();

        // lock is held by neither thread, so we should still get "false" 
        if (Monitor.IsEntered(obj))
        {
            Console.WriteLine("{0}: lock should not be held by any thread, but IsEntered == true", caseName);
            s_result = 106;
        }

    }
}
