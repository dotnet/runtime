// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ThreadStartNeg
{
    ManualResetEvent mre = new ManualResetEvent(false);

    public static int Main()
    {
        ThreadStartNeg tsn = new ThreadStartNeg();
        return tsn.Run();
    }

    private int Run()
    {
        int iRet = -1;
       
        try
        {
            Thread t = new Thread(new ThreadStart(ThreadWorker));
            t.Start(0);
            Console.WriteLine("Thread failed to throw an exception!");
        }
        catch(InvalidOperationException)
        {
            // Expected
            iRet = 100;
        }
        catch(Exception ex)
        {
            Console.WriteLine("Unexpected exception thrown: " + ex.ToString());
        }

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void ThreadWorker()
    {
        Console.WriteLine("In ThreadWorker");
    }
}
