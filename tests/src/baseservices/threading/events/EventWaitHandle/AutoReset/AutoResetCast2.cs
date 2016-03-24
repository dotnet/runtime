// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class AutoResetCtor
{
    EventWaitHandle ewh;
    int iRet = -1;

    public static int Main()
    {
        AutoResetCtor arc = new AutoResetCtor();
        return arc.Run();
    }

    private int Run()
    {
        // Testing the initialState = false for an AutoResetEvent
        ewh = (EventWaitHandle)new AutoResetEvent(false);

        Thread t = new Thread(new ThreadStart(ThreadWorker));
        t.Start();
        t.Join();

        ewh.Set();
        // when doing another wait, it should return immediately
        Console.WriteLine("Main: Waiting...");
        bool b = ewh.WaitOne(5000);//, false);
        if(b)
            iRet += 50;
        else
            Console.WriteLine("WaitOne() timed out");

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
    
    private void ThreadWorker()
    {
        Console.WriteLine("TW: Waiting...");
        // This should NOT return immediately
        bool b = ewh.WaitOne(5000);//, false);
        if(b)
            Console.WriteLine("WaitOne returned successful");
        else
            iRet = 50;
    }
}