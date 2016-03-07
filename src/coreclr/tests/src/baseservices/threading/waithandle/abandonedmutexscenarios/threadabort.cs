// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;

class WaitOneEx
{
    private Mutex myMutex;
    private AutoResetEvent myARE;
    int successes;
    int failures;
    int expected;

    public WaitOneEx()
    {
        myMutex = new Mutex(false, "myMutex");
                myARE = new AutoResetEvent(true);
        successes = 0;
        failures  = 0;
        expected  = 3;
    }

    public static int Main()
    {
        WaitOneEx wao = new WaitOneEx();
        wao.TestOne();
        wao.TestTwo();
        wao.TestThree();
        return wao.CheckSuccess();
    }

    // This test ensures that an AbandonedMutexException is thrown when
    // the thread is aborted and the mutex abandoned.
    private void TestOne()
    {
        Console.WriteLine("MT: Test One");
        // First thread abandon's the mutex by doing a thread.abort
        Thread t = new Thread(new ThreadStart(this.AbandonTheMutex));
        t.Start();
        // Second thread catches the exception 
        bool bRet = false;

        myARE.Set();
        Thread.Sleep(500);
        ThreadAbort(t);
        try
        {
            Console.WriteLine("MT: Wait on an abandoned mutex");
            bRet = myMutex.WaitOne();
        }
        catch(AbandonedMutexException)
        {
            // Expected
            Console.WriteLine("MT PASS: AbandonedMutexException thrown!");
            Success();
            // Release the Mutex
            myMutex.ReleaseMutex();
            return;
        }
        catch(Exception e)
        {
            Failure("MT FAIL: Unexpected exception thrown: " +
                e.ToString());
            myMutex.ReleaseMutex();
            return;
        }
        Failure("MT FAIL: Test did not throw AbandonedMutexException");
        // Release the Mutex
        myMutex.ReleaseMutex();
    }

    // This test ensures that releasing a Mutex in the catch works and
    // does not throw an AbandonedMutexException
    private void TestTwo()
    {
        Console.WriteLine("Test Two");
        // First thread abandon's the mutex by doing a thread.abort
        Thread t = new Thread(new ThreadStart(this.ReleaseInCatch));
        t.Start();
        // Second thread catches the exception 
        bool bRet = false;

        myARE.Set();
        Thread.Sleep(500);
        ThreadAbort(t);
        try
        {
            Console.WriteLine("Wait on an abandoned mutex");
            bRet = myMutex.WaitOne();
        }
        catch(AbandonedMutexException)
        {
            // Not Expected
            Failure("MT FAIL: Test threw an AbandonedMutexException!");
            // Release the Mutex
            myMutex.ReleaseMutex();
            return;
        }
        catch(Exception e)
        {
            Failure("MT FAIL: Unexpected exception thrown: " +
                e.ToString());
            myMutex.ReleaseMutex();
            return;
        }
        Console.WriteLine("MT PASS: No exception thrown!");
        Success();
        // Release the Mutex
        myMutex.ReleaseMutex();
    }

    // This test ensures that releasing a Mutex in the finally works and
    // does not throw an AbandonedMutexException
    private void TestThree()
    {
        Console.WriteLine("Test Three");
        // First thread abandon's the mutex by doing a thread.abort
        Thread t = new Thread(new ThreadStart(this.ReleaseInFinally));
        t.Start();
        // Second thread catches the exception 
        bool bRet = false;

        myARE.Set();
        Thread.Sleep(500);
        ThreadAbort(t);
        try
        {
            Console.WriteLine("Wait on an abandoned mutex");
            bRet = myMutex.WaitOne();
        }
        catch(AbandonedMutexException)
        {
            // Not Expected
            Failure("MT FAIL: Test threw an AbandonedMutexException!");
            // Release the Mutex
            myMutex.ReleaseMutex();
            return;
        }
        catch(Exception e)
        {
            Failure("MT FAIL: Unexpected exception thrown: " +
                e.ToString());
            myMutex.ReleaseMutex();
            return;
        }
        Console.WriteLine("MT PASS: No exception thrown!");
        Success();
        // Release the Mutex
        myMutex.ReleaseMutex();
    }

    private void AbandonTheMutex()
    {
        Console.WriteLine("ATM: Acquire the Mutex");
        myMutex.WaitOne();
        Console.WriteLine("ATM: Waiting for signal");
        // wait for signal to abort
        myARE.WaitOne();
        try
        {
            Thread.Sleep(10000);
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("ATM: ThreadAbortException thrown");
            // swallow exception to abandon mutex
            myARE.Reset();
        }
    }

    private void ReleaseInCatch()
    {
        Console.WriteLine("RIC: Acquire the Mutex");
        myMutex.WaitOne();
        Console.WriteLine("RIC: Waiting for signal");
        myARE.WaitOne();
        try
        {
            Thread.Sleep(10000);
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("RIC: ThreadAbortException thrown, releasing mutex");
            myMutex.ReleaseMutex();
        }
    }

    private void ReleaseInFinally()
    {
        Console.WriteLine("RIF: Acquire the Mutex");
        myMutex.WaitOne();
        Console.WriteLine("RIF: Waiting for signal");
        myARE.WaitOne();
        try
        {
            Thread.Sleep(10000);
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("RIF: ThreadAbortException thrown");
        }
        finally
        {
            myMutex.ReleaseMutex();
        }
    }

    private int CheckSuccess()
    {
        if(successes == expected && failures == 0)
        {
            Console.WriteLine("**** All tests passed! ****");
            return 100;
        }
        Console.WriteLine("There were one or more failures");
        return -1;
    }

    private void Failure(string message)
    {
        Console.WriteLine(message);
        failures++;
    }

    private void Success()
    {    
        successes++;
    }    

    private static void ThreadAbort(Thread thread)
    {
        MethodInfo abort = null;
        foreach(MethodInfo m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
        }
        if (abort == null) {
            throw new Exception("Failed to get Thread.Abort method");
        }
        abort.Invoke(thread, new object[0]);
     }
}
