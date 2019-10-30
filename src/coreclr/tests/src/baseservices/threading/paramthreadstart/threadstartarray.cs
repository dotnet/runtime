// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class ThreadStartArray
{
    Mutex[] m;
    AutoResetEvent are = new AutoResetEvent(false);

    public static int Main()
    {
        // Abandon this mutex
        Mutex[] mArr = new Mutex[64];
        for(int i=0;i<mArr.Length;i++)
            mArr[i] = new Mutex(false);
        
        ThreadStartArray tsa = new ThreadStartArray();
        return tsa.Run(mArr);
    }

    private int Run(Mutex[] mPass)
    {
        bool bRet = false;
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(mPass);
        are.WaitOne();
        
        // Check to make sure the array is abandoned
        try
        {
            WaitHandle.WaitAny(m, 10000);
        }
        catch(AbandonedMutexException)
        {
            bRet = true;
        }
        catch(Exception ex)
        {
            Console.WriteLine("Unexpected exception thrown: " + ex.ToString());
        }

        Console.WriteLine(bRet ? "Test Passed" : "Test Failed");
        return (bRet ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        for(int i = 0;i<((Mutex[])o).Length;i++)
            ((Mutex[])o)[i].WaitOne();

        m = (Mutex[])o;
        are.Set();
        Thread.Sleep(1000);
    }
}