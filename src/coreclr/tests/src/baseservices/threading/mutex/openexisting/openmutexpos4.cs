// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

/// <summary>
/// Verifies if we CreateMutex adn then OpenMutex we don't have ownership of the mutex.
/// </summary>
public class Test
{
    const string mutexName = "MySharedMutex";
    static ManualResetEvent manualEvent = new ManualResetEvent(false);
    static ManualResetEvent exitEvent = new ManualResetEvent(false);
    static ManualResetEvent reuseBeforeReleaseEvent = new ManualResetEvent(false);
    int success = 100;


    public void CreateMutexThread()
    {
        Console.WriteLine("Inside thread which creates a mutex");

        Mutex mutex = new Mutex(true, mutexName);


        Console.WriteLine("Mutex created");
        
        manualEvent.Set();
        reuseBeforeReleaseEvent.WaitOne();
        mutex.ReleaseMutex();
        
        exitEvent.WaitOne();
    }

    public void ReuseMutexThread()
    {
        Console.WriteLine("Waiting to reuse mutex");
        manualEvent.WaitOne();
        bool exists;

        Mutex mutex = new Mutex(true, mutexName, out exists);
        
        reuseBeforeReleaseEvent.Set();
        if (exists)
        {
            Console.WriteLine("Error, created new mutex!");
            success = 97;
        }
        else
        {
            mutex.WaitOne();
        }

        
        try
        {
            Console.WriteLine("Mutex reused {0}", exists);
            mutex.ReleaseMutex();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception: {0}", e);
            success = 98;
        }

        exitEvent.Set();
    }

    int RunTest()
    {
        Thread t1 = new Thread(new ThreadStart(CreateMutexThread));
        Thread t2 = new Thread(new ThreadStart(ReuseMutexThread));
        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        if (success == 100) Console.WriteLine("Test passed"); else Console.WriteLine("Test failed");
        return (success);
    }

    public static int Main()
    {
        return (new Test().RunTest());
    }
}

