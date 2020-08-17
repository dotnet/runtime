
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Runtime_40444
{
    public static int t2_result;

    public static int t2_finished;

    public static int s_divisor;
    
    static void Thread2()
    {
        t2_result++;
        t2_finished = 1;
    }
    
    static int TestVolatileRead(ref int address)
    {
        int ret = address;
        Thread.MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
        return ret;
    }

    static bool Test(ref bool result)
    {
        int loc_finished;        
        t2_finished = 0;

        // Run Thread2() in a new thread
        new Thread(new ThreadStart(Thread2)).Start();
        
        // 
        //Wait for Thread2 to signal that it has a result by setting
        // t2_finished to 1.
        //
        // We wait by performing this loop 1000 million times
        //
        // It is important that we have no calls in the loop
        // and that the JIT inlines the method TestVolatileRead
        // 
        //
        int i = 0; 
        int divisor = s_divisor;
        for (; ; )
        {
            if (TestVolatileRead(ref t2_finished)==1)
            {
                // The value was chnaged by Thread2
                // We print out how many iterations we looped for and 
                // return true
                Console.WriteLine("{0}: t2_result = {1}", i, t2_result);
                result = true;

                // The other thread has run and we just saw the value of t2_finsihed change
                // so we return true and pass the test
                //             
                return true;
            }

            // Integer division is somewhat expensive and will add additional time for the loop to execute
            // Chain two divides so the processir can't hide the latency
            //
            if (((i / divisor) / divisor) == 1)
            {
               divisor++;
            }

            i++;
            if (i == 1000000000)  // 1000 million
            {
                loc_finished = t2_finished;
                break;
            }
        }

        // If loc_finished is zero then the other thread never run and we need to try again
        //
        if (loc_finished == 0)
        {
            // We will return false, saying that we couldn't tell if the test failed
            //
            return false;
        }
        else
        {
            // If we count up to 1000 million we complete the loop and fail the test
            // Before the fix this would always happen because we woudl hoist the read
            // of t2_finished out of the loop
            //
            Console.WriteLine("{0}: FAILED, t2_result = {1}, t2_finished is {2}", i, t2_result, t2_finished);
            result = false;

            // The other thread has run and we never saw the value of t2_finsihed change
            // so we return true and fail the test
            //             
            return true;
        }
    }

    static int Main()
    {
        bool passes_test = false;
        bool test_result = false;
        
        for (int i=0; i<100; i++)
        {
            t2_result = 0;
            s_divisor = 1000000;

            // Test returns true when it is able to determine pass or fail
            // and it sets passes_test to true when it passes
            //

            test_result = Test(ref passes_test);
            if (test_result)
            {
                break;
            }
        }

        if (passes_test)
        {
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            if (test_result)
            {
                Console.WriteLine("FAILED");
                return -1;
            }
            else
            {
                Console.WriteLine("Unable to determine");
                return 101;
            }
        }
    }
}
