// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitAnyEx
{
    private Mutex myMutex;
    private ManualResetEvent myMRE;

    public WaitAnyEx()
    {
        myMutex = new Mutex(false, Common.GetUniqueName());
        myMRE = new ManualResetEvent(false);
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
            "not signaling the other element");
        Thread t = new Thread(new ThreadStart(this.AbandonTheMutex));
        t.Start();
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Waiting...");
            int i = WaitHandle.WaitAny(
                new WaitHandle[]{myMutex,
                new ManualResetEvent(false)}, 30000);
            Console.WriteLine("WaitAny did not throw an " +
                "exception, i = " + i);
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

    private void AbandonTheMutex()
    {
        Console.WriteLine("Acquire the Mutex");
        myMutex.WaitOne();
        Console.WriteLine("Holding the Mutex");
        // Not calling ReleaseMutex() so the Mutex becomes abandoned
        myMRE.Set();
        Thread.Sleep(1000);
    }
}
