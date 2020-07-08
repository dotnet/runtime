// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

class OpenMutexNeg
{
    public static int Main()
    {
        OpenMutexNeg omn = new OpenMutexNeg();
        return omn.Run();
    }

    private int Run()
    {
        int iRet = -1;
        string sName = Common.GetUniqueName();
        //  open a Mutex with the same name as a Semaphore
        using (Semaphore sem = new Semaphore(10, 10, sName))
        {
            try
            {
                Mutex mut = Mutex.OpenExisting(sName);
            }
            catch(WaitHandleCannotBeOpenedException)
            {
                //Expected	
                iRet = 100;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception where WaitHandleCannotBeOpenedException was expected: " +
                    e.ToString());
            }
        }

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
}
