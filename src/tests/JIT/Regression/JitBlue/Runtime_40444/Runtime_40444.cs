// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;

class Runtime_40444
{
    
    public static int t2_result = 0;
    public static int t2_finished;
    
    static void Thread2()
    {
        t2_result++;
        t2_finished = 1;
    }
    
//    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestVolatileRead(ref int address)
    {
        int ret = address;
        Thread.MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
        return ret;
    }

    static bool Test()
    {
        bool result = false;
        t2_finished = 0;

        // Run Thread2() in a new thread
        new Thread(new ThreadStart(Thread2)).Start();
        
        // Wait for Thread2 to signal that it has a result by setting
        // t2_finished to 1.
        for (int i=0; i<10000000; i++)
        {
            if (TestVolatileRead(ref t2_finished)==1)
            {
                Console.WriteLine("{1}: result = {0}", t2_result, i);
                result = true;
                break;
            }
        }
        if (result == false)
        {
            Console.WriteLine("FAILED");
        }
        return result;
    }

    static int Main()
    {
        for (int i=0; i<100; i++)
        {
            if (!Test())
            {
                return -1;
            }
        }
        Console.WriteLine("Passed");
        return t2_result;
    }
}
