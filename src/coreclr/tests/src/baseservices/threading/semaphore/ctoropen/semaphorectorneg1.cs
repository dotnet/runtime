// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class CtorTest
{
    public static int Main(string[] args)
    {
        // Check args
        if (args.Length != 2)
        {
            Console.WriteLine("USAGE: SemaphoreCtorNeg1 /iCount:<int> /mCount:<int>");
            return -1;
        }

        // Get the args
        int iCount = -1, mCount = -1;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower().StartsWith("/icount:"))
            {
                iCount = Convert.ToInt32(args[i].Substring(8));
                continue;
            }

            if (args[i].ToLower().StartsWith("/mcount:"))
            {
                mCount = Convert.ToInt32(args[i].Substring(8));
                continue;
            }
        }
        CtorTest ct = new CtorTest();
        return ct.Run(iCount, mCount);
    }
           
    private int Run(int initCount, int maxCount)
    {
        int iRet = -1;
        Semaphore sem = null;
        try
        {
            using (sem = new Semaphore(initCount, maxCount))
            {
                Console.WriteLine("Semaphore was created!");
            }
        }
        catch(ArgumentException)
        {
            //  Expected
            iRet = 100;
        }
        catch(Exception e)
        {
            //  other exceptions are not valid
            Console.WriteLine("Unexpected exception thrown:  " + 
                e.ToString());
        }
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
}