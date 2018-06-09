// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitAnyEx
{
    WaitHandle[] wh;
    private ManualResetEvent myMRE;

    public static int Main()
    {
        WaitAnyEx wae = new WaitAnyEx();
        return wae.Run();
    }

    private int Run()
    {
        int iRet = -1;
        Console.WriteLine("Abandon all mutexes in array");
        CreateMutexArray(64);
        myMRE = new ManualResetEvent(false);
        Thread t = new Thread(new ThreadStart(this.AbandonAllMutexes));
        t.Start();
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Waiting...");
            int i = WaitHandle.WaitAny(wh, 30000);
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

    private void AbandonAllMutexes()
    {
        foreach(WaitHandle w in wh)
        {
            if(w is Mutex)
                w.WaitOne();
        }
        myMRE.Set();
        Thread.Sleep(1000);
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
