// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;

class Account
{
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

    private int balance = 0;
    public void Deposit()
    {
        lock (this)
        {
            try { Monitor.Wait(this); }
            catch(SynchronizationLockException) { }
            finally { balance += 100; }
        }
    }
    public static int Main()
    {
        int ret = 0;
        Account a = new Account();
        Thread t = new Thread(new ThreadStart(a.Deposit));
        t.Start();
        Thread.Sleep(100);
        lock (a)
        {
            Console.WriteLine(a.balance); // Output: 0
            Monitor.Pulse(a);
            ThreadAbort(t);
            Thread.Sleep(100);
            if(a.balance == 0)
                ret = 100;
            Console.WriteLine(a.balance); // Output: 100.00 (bug)
        }
        Console.WriteLine(100 == ret ? "Test Passed" : "Test Failed");
        return ret;
    }
}      