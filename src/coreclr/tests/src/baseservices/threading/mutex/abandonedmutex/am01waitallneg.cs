// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitAllEx
{
    private Mutex myMutex01 = new Mutex(false);
    private Mutex myMutex02 = new Mutex(false);
    private Mutex myMutex03 = new Mutex(false);
    private ManualResetEvent myMRE = new ManualResetEvent(false);
    private int iRet = -1;

    public static int Main()
    {
        WaitAllEx wae = new WaitAllEx();
        wae.Run();
        Console.WriteLine(wae.iRet == 100 ? "Test Passed!" : "Test Failed");
        return wae.iRet;
    }

    private void Run()
    {
        Thread t = new Thread(new ThreadStart(this.AbandonTheMutex));
        t.Start();
        myMRE.WaitOne();

        try
        {
            Console.WriteLine("Waiting...");
            bool bRet = WaitHandle.WaitAll(
                new WaitHandle[]{myMutex01, myMutex02, myMutex03}, 5000);
            Console.WriteLine("WaitAll did not throw an " +
                "exception, bRet = " + bRet);
        }
        catch(AbandonedMutexException am)
        {
            Console.WriteLine("AbandonedMutexException thrown! Checking values...");
            if(-1 == am.MutexIndex && null == am.Mutex)
                iRet = 100;
        }
    }

    private void AbandonTheMutex()
    {
        myMutex03.WaitOne();
        myMRE.Set();
        Thread.Sleep(1000);
    }
}
