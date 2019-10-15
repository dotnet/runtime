// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class OpenMutexPos
{
    Mutex mut;

    public static int Main()
    {
        OpenMutexPos omp = new OpenMutexPos();
        return omp.Run();
    }

    private int Run()
    {
        int iRet = -1;
        string sName = Common.GetUniqueName();
        // Open an abandoned mutex
        using (mut = new Mutex(false, sName))
        {
            Thread t = new Thread(new ThreadStart(AbandonMutex));
            t.Start();
            t.Join();
            try
            {
                Mutex mut1 = Mutex.OpenExisting(sName);
                iRet = 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception thrown: " +
                    ex.ToString());
            }
        }

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void AbandonMutex()
    {
        mut.WaitOne();
    }
}