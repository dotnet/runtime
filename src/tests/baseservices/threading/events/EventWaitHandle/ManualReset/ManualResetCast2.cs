// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ManualResetCtor
{
    EventWaitHandle ewh;
    int iRet = -1;

    public static int Main()
    {
        ManualResetCtor mrc = new ManualResetCtor();
        return mrc.Run();
    }

    private int Run()
    {
        // Testing the initialState = false for a ManualResetEvent
        ewh = (EventWaitHandle)new ManualResetEvent(false);

        Thread t = new Thread(new ThreadStart(ThreadWorker));
        t.Start();
        t.Join();

        ewh.Set();
        // when doing another wait, it should return immediately
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
        // This should NOT return immediately
        bool b = ewh.WaitOne(5000);//, false);
        if(b)
            Console.WriteLine("WaitOne returned successful");
        else
            iRet = 50;
    }
}
