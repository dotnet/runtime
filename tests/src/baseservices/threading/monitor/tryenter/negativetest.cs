// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;


public class OutOfRange
{
    Object lockMe;
    Thread lockHolderThread;
    AutoResetEvent inLock;

    public static void ThreadAbort(Thread thread)
    {
        MethodInfo abort = null;
        foreach(MethodInfo m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
        }
        if (abort == null)
        {
            throw new Exception("Failed to get Thread.Abort method");
        }
        abort.Invoke(thread, new object[0]);
    }

    public OutOfRange(){
        lockMe = new Object();
        inLock = new AutoResetEvent(false);
    }

    public static int Main(string[] args)
    {
        OutOfRange oor = new OutOfRange();
        int ret =  oor.RunNegative(1);
        ret +=  oor.RunNegative(2);
        Console.WriteLine(ret/2 == 100 ? "Test Passed":"Test Failed");
        return ret/2;
    }


    public int RunNegative(int test)
    {
        Console.WriteLine("Running Negative Test "+test);
        int rValue = 0;
        TimeSpan ts;

        switch(test){
            case 1:
                Console.WriteLine("Int32.MaxValue + 1");
                ts = new TimeSpan(21474836480000);
                try{
                    Monitor.TryEnter(lockMe,ts);
                }
                catch(ArgumentOutOfRangeException)
                {
                    rValue = 100;
                }
                break;

            case 2:
                Console.WriteLine("TimeSpan(-2)");
                ts = new TimeSpan(-20000);
                Console.WriteLine(ts.TotalMilliseconds);
                NewThreadHoldLock();
                try{
                    Monitor.TryEnter(lockMe,ts);
                }
                catch(ArgumentOutOfRangeException)
                {
                    rValue = 100;
                }
                AbortLockHolderThread();
                break;
            
        }
        return rValue;
    }

    private void AbortLockHolderThread()
    {
        ThreadAbort(lockHolderThread);
    }

    private void NewThreadHoldLock()
    {
        lockHolderThread = new Thread(new ParameterizedThreadStart(HoldLock));
        lockHolderThread.Start(lockMe);
        inLock.WaitOne();
        
    }
    private void HoldLock(object foo)
    {
        lock(foo)
        {
	    inLock.Set();
            Thread.Sleep(-1);
        }        
    }    
}

