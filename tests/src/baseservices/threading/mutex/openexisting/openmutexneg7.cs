// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class OpenMutexNeg
{
    public ManualResetEvent mre;
    public Mutex mut;

    public OpenMutexNeg()
    {
        mre = new ManualResetEvent(false);
    }

    public static int Main()
    {
        OpenMutexNeg omn = new OpenMutexNeg();
        return omn.Run();
    }

    private int Run()
    {
        int iRet = -1;
        string sName = Common.GetUniqueName();
        //  open a Mutex that has been abandoned
        mut = new Mutex(false, sName);
        Thread th = new Thread(new ParameterizedThreadStart(AbandonMutex));
        th.Start(mut);
        mre.WaitOne();
        try
        {
            Mutex mut1 = Mutex.OpenExisting(sName);
            mut1.WaitOne();
        }
        catch (AbandonedMutexException)
        {
            //Expected	
            iRet = 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: " + 
                e.ToString());
        }

        GC.KeepAlive(mut);

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void AbandonMutex(Object o)
    {
        ((Mutex)o).WaitOne();
        mre.Set();
        Thread.Sleep(1000);
    }
}