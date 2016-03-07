// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

/*
 * Test case verifies that when we take a thread abort exception
 * while blocked waiting to re-acquire a monitor after doing a Monitor.Wait
 * that we do not allow the thread to run again until the first thread
 * has released it's lock.
 *
 */
class Test
{
    object someLock = new object();
    volatile bool thread1Locked = false, thread2exception = false;
    int success = 100;
    
    static int Main()
    {
        return(new Test().RunTest());
    }
    
    int RunTest()
    {
        Console.WriteLine ("Main thread starting");
        Thread secondThread = new Thread (new ThreadStart(ThreadJob));
        secondThread.Start();
        Console.WriteLine ("Main thread sleeping");
        Thread.Sleep(500);
        lock (someLock)
        {
            thread1Locked = true;
            Console.WriteLine ("Main thread acquired lock - pulsing monitor");
            Monitor.Pulse(someLock);
            Console.WriteLine ("Monitor pulsed; interrupting second thread");
            ThreadEx.Abort(secondThread);
            Thread.Sleep(1000);
            Console.WriteLine ("Main thread still owns lock...");
            Thread.Sleep(2000);
            Console.WriteLine ("Main thread still owns lock...");
            thread1Locked = false;
            if(thread2exception)
            {
                Console.WriteLine("Thread2 took exception too early");
                success = 95;
            }
        }
        secondThread.Join();
        if(success == 100)
        {
            Console.WriteLine("Test passed");
        }
        else
        {
            Console.WriteLine("Test failed");
        }
        return(success);
    }
    
    void ThreadJob()
    {
        Console.WriteLine ("Second thread starting");
        
        lock (someLock)
        {
            Console.WriteLine ("Second thread acquired lock - about to wait");
            try
            {
                Monitor.Wait(someLock);
            }                
            catch (Exception e)
            {
                thread2exception = true;
                if(thread1Locked)
                {
                    success = 98;
                }
                Console.WriteLine ("Second thread caught an exception: {0}", e);
                if(Monitor.TryEnter(someLock))
                {
                    Console.WriteLine("Thread2 holds the lock");
                    Monitor.Exit(someLock);
                }                
                else
                {
                    Console.WriteLine("Couldn't recurse on lock");
                    success = 97;
                }      
                
                if(!(e.GetType().ToString().Equals("System.Threading.ThreadAbortException")))
                {
                    Console.WriteLine("Wrong exception: {0}",e);
                    success = 92;
                }                
            }
        }
    }
}

