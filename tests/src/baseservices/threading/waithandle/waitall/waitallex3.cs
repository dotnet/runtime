// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

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
        Console.WriteLine("Abandoning more than one mutex mix " +
            "with other WaitHandles, signaling other elements");
        CreateArray(64);
        Thread t = new Thread(new ThreadStart(this.AbandonAllMutexes));
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

    private void AbandonAllMutexes()
    {
        Mutex m = new Mutex();
        AutoResetEvent are = new AutoResetEvent(false);

        foreach(WaitHandle w in wh)
        {
            if(w.GetType() == m.GetType())
                w.WaitOne();
        }
        myMRE.Set();
        Thread.Sleep(1000);
        foreach(WaitHandle w in wh)
        {
            if(w.GetType() == myMRE.GetType())
                ((ManualResetEvent)w).Set();
            if(w.GetType() == are.GetType())
                ((AutoResetEvent)w).Set();
        }
    }

    private void CreateArray(int numElements)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            switch(i%4)
            {
                case 0:
                    wh[i] = new AutoResetEvent(false);
                    break;
                case 1:
                    wh[i] = new ManualResetEvent(false);
                    break;
                case 2:
                    wh[i] = new Mutex(false, Common.GetUniqueName());
                    break;
                case 3:
                    wh[i] = new Semaphore(5,5);
                    break;
            }
        }
    }
}
