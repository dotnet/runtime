// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class Program
{
    static long x;

    static void DoWork()
    { 
        for (int i=0; i<5000; i++)
        { 
            Interlocked.Add(ref x, 1);
            Interlocked.Add(ref x, -1);
        }
    }
    
    [Fact]
    public static int TestEntryPoint()
    { 
        Thread[] threads;
        bool     retVal;

        threads = new Thread[99];
        retVal  = true;

        for (int j=0; j<10; j++)
        { 
            x = 0;
            for (int i = 0; i < 99; i += 1)
            { 
                threads[i] = new Thread(DoWork);
                threads[i].Start();
            }
            for (int i = 0; i < 99; i += 1)
            { 
                threads[i].Join();
            }
            long y = Interlocked.Add(ref x, 0);

            if (0 != y)
            {
                TestLibrary.Logging.WriteLine("Wrong value: " + y + " (0 expected)");
                retVal = false;
            }
        }

        if (retVal && 0 == x)
        {
            TestLibrary.Logging.WriteLine("PASS");
            return 100;
        }
        else
        {
            TestLibrary.Logging.WriteLine("FAIL x=" + x);
            return 0;
        }
    }
}
