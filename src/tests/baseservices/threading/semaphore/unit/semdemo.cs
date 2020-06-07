// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

 
using System;
using System.Threading;

class Test
{
    private Semaphore sem = null;
    private const int numIterations = 1;
    private const int numThreads = 10;
    private int m_success;

    static int Main()
    {
	Test t = new Test();
	t.RunTest();
	return t.CheckRet();
	
    }

    private void RunTest()
    {
       	Semaphore s = new Semaphore(3,3);
	sem = s;
	
	Thread[] threads =new Thread[numThreads];

        // Create the threads that will use the protected resource.
        for(int i = 0; i < numThreads; i++)
        {
            threads[i] = new Thread(new ThreadStart(this.MyThreadProc));
            threads[i].Name = String.Format("Thread{0}", i + 1);
            threads[i].Start();
        }

        // The main thread exits, but the application continues to
        // run until all foreground threads have exited.
	
	for(int i =0; i< numThreads; i++) {
	    threads[i].Join();
	} 
    }

    private void MyThreadProc()
    {
        for(int i = 0; i < numIterations; i++)
        {
            UseResource();
        }
    }

    private int CheckRet()
    {
	Console.WriteLine(m_success == numThreads ? "Test Passed":"Test Failed");
	return (m_success == numThreads ? 100:-1);
    }

    private void Success()
    {
        Interlocked.Increment(ref m_success);
    }

    // This method represents a resource that must be synchronized
    // so that only one thread at a time can enter.
    private void UseResource()
    {
        // Wait until it is safe to enter.
        sem.WaitOne();

        Console.WriteLine("{0} has entered the protected area", 
            Thread.CurrentThread.Name);

        // Place code to access non-reentrant resources here.

        // Simulate some work.
        Thread.Sleep(500);
	Success();
        Console.WriteLine("{0} is leaving the protected area\r\n", 
            Thread.CurrentThread.Name);
         
        // Release the Mutex.
        sem.Release();
	
    }
}
