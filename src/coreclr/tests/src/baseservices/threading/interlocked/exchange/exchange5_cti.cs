// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Threading;

/// <summary>
/// System.Threading.Interlocked.Exchange(System.Object@,System.Object)
/// </summary>


// This test makes sure that Exchange(object, object)
// plays nicely with other threads accessing shared state directly.
// The test spawns a bunch of threads, then each thread tries to 
// grab the mutex (set location=1), decrement a global resource count, 
// then release the mutex (set location=0).  While location=0, the 
// thread will be able to set it to 1 and enter the mutex to consume 
// the resource, but if it is 1, the thread will be denied entry.
// At the end, the test checks that:
// the sum of all entries + denials = total potential resources
// total potential resources = resources unconsumed + entries
public class InterlockedExchange5
{
    private const int c_THREADARRAT_SIZE = 10;      // how many threads to spawn
    private static int resource = 10;               // resources to be consumed
    private static Object location = 0;             // mutex being managed thru Exchange
    private static int entry = 0;                   // threads granted entry to the mutex
    private static int deny = 0;                    // threads denied entry to the mutex

    public static int Main(string[] args)
    {
        InterlockedExchange5 exchange5 = new InterlockedExchange5();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Threading.Interlocked.Exchange(System.Object@,System.Object)...");

        if (exchange5.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify multiple threads share the same resource by using Interlocked.Exchange method...");

        try
        {
            // create an array of many threads
            Thread[] threads = new Thread[c_THREADARRAT_SIZE];
            Random rand = new Random();

            // for each thread
            for (int i = 0; i < threads.Length; i++)
            {
                // each thread uses ConsumeResource
                threads[i] = new Thread(new ThreadStart(ConsumeResource));
                // and has a name
                threads[i].Name = String.Format("Thread{0}", i + 1);
                // put this (the spawning) thread to sleep for a random period of time
                Thread.Sleep(rand.Next(1, 100));
                // then start the spawned thread working
                threads[i].Start();
            }

            // Wait for all threads to complete
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            // entries + denials should equal original value of resource (10)
            if (entry + deny == 10)
            {
                // if any resources remain unconsumed, then those plus number
                // of successful entries should equal the original value of 
                // resource (10)
                if (resource > 0 && resource + entry != 10)
                {
                    TestLibrary.TestFramework.LogError("001", "The number of resource is consumed is wrong!");
                    retVal = false;
                }
            }
            else
            {
                TestLibrary.TestFramework.LogError("002", "The total number is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    private static void ConsumeResource()
    {
        // This is effectively a hand-coded mutex.
        // The thread exchanges the value 1 with the value 
        // already at location (initially 0).  So first time, it should 
        // set location=1 and return 0.  Then the thread 'consumes' a 
        // resource and records an 'entry', after which it sets location 
        // back to 0. Any thread hitting the Exchange while loc=1 will see
        // this and not enter the mutex - it will just record a 'denial'.
        // After all is said and done, denials+entries should equal total
        // resources consumed.
        // note that since location is actually an object, Exchange is going
        // to return an object which must be converted to Int32
        // note that here we use the object overload of Interlocked.Exchange
        if (Convert.ToInt32(Interlocked.Exchange(ref location, 1)) == 0) // corrected, was !=0
        {
            // this thread has the mutex
            if (resource > 0)
            {
                // consume a resource
                resource--;
                TestLibrary.TestFramework.LogInformation(String.Format("The resource is reduced, the remainder is {0}", resource));
            }
            else
            {
                // no more resources to consume - this really should never happen
                TestLibrary.TestFramework.LogInformation("The resource is empty!");
            }

            // release the mutex
            // note that here we use the object overload 
            // of Exchange
            Interlocked.Exchange(ref location, 0);
            // increment the entry count - 
            Interlocked.Increment(ref entry);   // corrected, was entry++;
                                                // which is not thread safe
        }
        else
        {
            // the thread could not enter the mutex because another
            // thread has set the location to 1
            TestLibrary.TestFramework.LogInformation("This is not available!");
            // increment the denial count, no resource is consumed
            Interlocked.Increment(ref deny);    // corrected, was deny++;
                                                // which is not thread safe
        }
    }
}
