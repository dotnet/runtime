// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitOneEx
{
    private Mutex myMutex;
    private ManualResetEvent myMRE;
    private int iRet = -1;

    public WaitOneEx()
    {
        myMutex = new Mutex(false);
        myMRE = new ManualResetEvent(false);
    }

    public static int Main()
    {
        WaitOneEx wao = new WaitOneEx();
        wao.Run();
        Console.WriteLine(100 == wao.iRet ? "Test Passed" : "Test Failed");
        return wao.iRet;
    }

    private void Run()
    {
        Console.WriteLine("Test abandoned mutex is thrown using WaitOne");
        Thread t = new Thread(new ThreadStart(this.AbandonTheMutex));
        t.Start();
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Wait on an abandoned mutex");
            bool bRet = myMutex.WaitOne(10000);
            Console.WriteLine("WaitOne did not throw an exception!");
        }
        catch(AbandonedMutexException am)
        {
            Console.WriteLine("AbandonedMutexException thrown! Checking values...");
            if(-1 == am.MutexIndex && null == am.Mutex)
                iRet = 100;
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + e.ToString());
        }
    }

    private void AbandonTheMutex()
    {
        Console.WriteLine("Acquire the Mutex");
        myMutex.WaitOne();
        myMRE.Set();
        Thread.Sleep(1000);
    }
}
