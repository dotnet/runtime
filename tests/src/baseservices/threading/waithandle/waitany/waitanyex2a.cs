// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

// This is a non-9x test

class WaitAnyEx
{
    private Mutex myMutex;
    private ManualResetEvent myMRE;

    public WaitAnyEx()
    {
        myMRE = new ManualResetEvent(false);
        myMutex = new Mutex();
    }

    public static int Main()
    {
        WaitAnyEx wae = new WaitAnyEx();
        return wae.Run();
    }

    private int Run()
    {
        int iRet = -1;
        Console.WriteLine("Testing Mutex and non-Mutex, " +
            "and signaling the other element");
        Thread t = new Thread(new ThreadStart(this.AbandonAndSet));
        t.Start();
        t.Join();
        try
        {
            Console.WriteLine("Waiting...");
            int i = WaitHandle.WaitAny(
                new WaitHandle[]{myMutex, myMRE}, 30000);
            Console.WriteLine("WaitAny did not throw AbandonedMutexException. Result: {0}", i);
        }
        catch(AbandonedMutexException)
        {
            iRet = 100;
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + 
                e.ToString());
        }
        finally
        {
            myMRE.Reset();
        }
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void AbandonAndSet()
    {
        Console.WriteLine("Acquire the Mutex");
        myMutex.WaitOne();
        Console.WriteLine("Holding the Mutex");
        // Signaling the Event
        myMRE.Set();
        // Not calling ReleaseMutex() so the Mutex becomes abandoned
    }
}
