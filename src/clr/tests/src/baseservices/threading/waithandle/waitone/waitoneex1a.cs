// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitOneEx
{
    private Mutex myMutex;
    private ManualResetEvent myMRE;

    public WaitOneEx()
    {
        myMutex = new Mutex();
        myMRE = new ManualResetEvent(false);
    }

    public static int Main()
    {
        WaitOneEx wao = new WaitOneEx();
        return wao.Run();
    }

    private int Run()
    {
        int iRet = -1;
        Console.WriteLine("Test abandoned mutex is thrown using WaitOne");
        Thread t = new Thread(new ThreadStart(this.AbandonTheMutex));
        t.Start();
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Wait on an abandoned mutex");
            bool bRet = myMutex.WaitOne();
            Console.WriteLine("WaitOne did not throw an exception!");
        }
        catch(AbandonedMutexException)
        {
            // Expected
            iRet = 100;
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + e.ToString());
        }
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void AbandonTheMutex()
    {
        Console.WriteLine("Acquire the Mutex");
        myMutex.WaitOne();
        myMRE.Set();
        Thread.Sleep(1000);
    }
}
