// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitAllEx
{
    private Mutex myMutex;
    private ManualResetEvent myMRE;

    private WaitAllEx()
    {
        myMutex = new Mutex(false, Common.GetUniqueName());
        myMRE = new ManualResetEvent(false);
    }

    public static int Main()
    {
        WaitAllEx wae = new WaitAllEx();
        return wae.Run();
    }

    private int Run()
    {
        int iRet = -1;
        Console.WriteLine("Testing Mutex and non-Mutex, " +
            "and signaling the other element");
        Thread t = new Thread(new ThreadStart(this.AbandonAndSet));
        t.Start();
        myMRE.WaitOne();
        bool bRet = false;
        try
        {
            Console.WriteLine("Waiting...");
            bRet = WaitHandle.WaitAll(
                new WaitHandle[]{myMutex, myMRE}, 5000);
            Console.WriteLine("WaitAll did not throw an " +
                "exception, bRet = " + bRet);
        }
        catch(AbandonedMutexException)
        {
            // Expected
            iRet = 100;
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + 
                e.ToString());
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
        Thread.Sleep(1000);
    }
}
