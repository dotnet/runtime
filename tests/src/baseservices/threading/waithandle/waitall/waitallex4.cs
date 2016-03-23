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
        Console.WriteLine("Abandoning only one Mutex " +
            "in array with other WaitHandles");
        CreateArray(64);
        Thread t = new Thread(new ThreadStart(this.AbandonOne));
        t.Start();
        myMRE.WaitOne();
        bool bRet = false;
        try
        {
            Console.WriteLine("Waiting...");
            bRet = WaitHandle.WaitAll(wh, 5000);
            // Expected timeout and no exception thrown
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

    private void AbandonOne()
    {
        Mutex m = new Mutex();
        foreach(WaitHandle w in wh)
        {
            if(w.GetType() == m.GetType())
            {
                w.WaitOne();
                break;
            }
        }
        myMRE.Set();
        Thread.Sleep(1000);
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
