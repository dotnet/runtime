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
        Console.WriteLine("Abandoning more than one mutex " +
            "mix with other WaitHandles");
        CreateArray(64);
        myMRE = new ManualResetEvent(false);
        Thread t = new Thread(new ThreadStart(this.AbandonAllMutexes));
        t.Start();
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Waiting...");
            int i = WaitHandle.WaitAny(wh);
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

    private void CreateArray(int numElements)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            switch(i%3)
            {
                case 0:
                    wh[i] = new ManualResetEvent(false);
                    break;
                case 1:
                    wh[i] = new Mutex();
                    break;
                case 2:
                    wh[i] = new Semaphore(0,5);
                    break;
            }
        }
    }
}
