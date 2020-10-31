// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

class OpenSemaphoreNeg
{
    public static int Main()
    {
        OpenSemaphoreNeg osn = new OpenSemaphoreNeg();
        return osn.Run();
    }
   
    private int Run()
    {
        int iRet = -1;
        Semaphore sem;
        try
        {
            sem = Semaphore.OpenExisting(null);
        }
        catch (ArgumentNullException)
        {
            //Expected
            iRet = 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: " +
                e.ToString());
        }

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
}
