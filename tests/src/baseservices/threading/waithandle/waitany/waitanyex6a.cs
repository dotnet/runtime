// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

// This is a non-9x test.

class WaitAnyEx
{
    WaitHandle[] wh;

    public static int Main(string[] args)
    {
        // Check number of args
        if(args.Length != 2)
        {
            Console.WriteLine("USAGE:  WaitAnyEx6a /size:<int> /pos:<int>");
            return -1;
        }

        // Get the args
        int iPos=-1, iSize = -1;;
        
        for(int i=0;i<args.Length;i++)
        {
            if(args[i].ToLower().StartsWith("/size:"))
            {
                iSize = Convert.ToInt32(args[i].Substring(6));
                continue;
            }

            if(args[i].ToLower().StartsWith("/pos:"))
            {
                iPos = Convert.ToInt32(args[i].Substring(5));
            }
        }

        WaitAnyEx wae = new WaitAnyEx();
        return wae.Run(iSize, iPos);
    }

    private int Run(int iArraySize, int iPosToAbandon)
    {
        int iRet = -1;
        Console.WriteLine("Abandon a particular mutex - " + iArraySize + ", " + 
            iPosToAbandon);
        CreateMutexArray(iArraySize);
        Thread t = new Thread(new 
            ParameterizedThreadStart(this.AbandonMutexPos));
        t.Start(iPosToAbandon);
        t.Join();
        int i = -1;
        try
        {
            Console.WriteLine("Waiting...");
            i = WaitHandle.WaitAny(wh);
            if(0 == iPosToAbandon)
                Console.WriteLine("WaitAny didn't return an " +
                    "AbandonedMutexException");
            else
                // Expected to pass
                iRet = 100;
        }
        catch(AbandonedMutexException)
        {
            // Expected if pos is first
            if(iPosToAbandon == 0)
                iRet = 100;
            else
                Console.WriteLine("WaitAny threw AbandonedMutexException " +
                    "and shouldn't have!");
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + 
                e.ToString());
        }
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void AbandonMutexPos(Object o)
    {
        wh[Convert.ToInt32(o)].WaitOne();
    }

    private void CreateMutexArray(int numElements)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            wh[i] = new Mutex();
        }
    }
}