// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

// This is a non-9x test

class WaitAllEx
{
    private WaitHandle[] wh;
    private ManualResetEvent myMRE;

    private WaitAllEx()
    {
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
        Console.WriteLine("Abandon before a wait");
        CreateMutexArray(64);

        Thread t = new Thread(new ThreadStart(this.AbandonMutexWait));
        t.Start();
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Waiting...");
            bool bRet = WaitHandle.WaitAll(wh, 10000);
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

    private void AbandonMutexWait()
    {
        wh[63].WaitOne();
        myMRE.Set();
    }

    private void CreateMutexArray(int numElements)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            wh[i] = new Mutex(false, Common.GetUniqueName());
        }
    }
}
