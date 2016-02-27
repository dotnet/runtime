// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class C
{
    private ReaderWriterLockSlim rwls;
    private ManualResetEvent mre;
    private ManualResetEvent mre1;
    private int threadCount = 100;
    private int oneThreadAccess;

    static int Main()
    {
        return (new C()).RunTest();
    }

    public int RunTest()
    {
        int retCode = -10;
        rwls = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        threadCount = 0;
        oneThreadAccess = 0;

        retCode = DoTesting();

        return retCode;
    }

    private int DoTesting()
    {
        mre = new ManualResetEvent(false);
        mre1 = new ManualResetEvent(false);
        Thread[] threadArray = new Thread[5];
        threadArray[0] = new Thread(this.LockTaker);
        threadArray[0].Start();
        mre1.WaitOne();
        for (int i = 1; i < threadArray.Length; i++)
        {
            threadArray[i] = new Thread(this.LockCompetitor);
            threadArray[i].Start();
        }

        for (int i = 0; i < threadArray.Length; i++)
        {
            threadArray[i].Join();
        }

        mre = new ManualResetEvent(false);
        mre1 = new ManualResetEvent(false);
        threadArray[0] = new Thread(this.LockTakerWrite);
        threadArray[0].Start();
        mre1.WaitOne();
        for (int i = 1; i < threadArray.Length; i++)
        {
            threadArray[i] = new Thread(this.LockCompetitorWrite);
            threadArray[i].Start();
        }

        for (int i = 0; i < threadArray.Length; i++)
        {
            threadArray[i].Join();
        }
        if (threadCount == 8)
            return 100;
        else
            return threadCount;
    }

    void LockTaker()
    {
        rwls.EnterUpgradeableReadLock();
        Interlocked.Increment(ref oneThreadAccess);
        mre1.Set();
        mre.WaitOne();
        Thread.Sleep(1000);
        if (oneThreadAccess != 1)
        {
            Console.WriteLine("Expected original UpgradeLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 50;
        }
        Interlocked.Decrement(ref oneThreadAccess);
        if (oneThreadAccess != 0)
        {
            Console.WriteLine("Expected original UpgradeLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 60;
        }
        rwls.ExitUpgradeableReadLock();
    }

    void LockCompetitor()
    {
        Interlocked.Increment(ref threadCount);
        if (threadCount == 4)
            mre.Set();
        rwls.EnterUpgradeableReadLock();
        Interlocked.Increment(ref oneThreadAccess);
        Thread.Sleep(2000);
        if (oneThreadAccess != 1)
        {
            Console.WriteLine("Expected original UpgradeLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 70;
        }
        Interlocked.Decrement(ref oneThreadAccess);
        Thread.Sleep(3000);
        if (oneThreadAccess != 0)
        {
            Console.WriteLine("Expected original UpgradeLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 80;
        }
        rwls.ExitUpgradeableReadLock();
    }

    void LockTakerWrite()
    {
        rwls.EnterWriteLock();
        Interlocked.Increment(ref oneThreadAccess);
        mre1.Set();
        mre.WaitOne();
        Thread.Sleep(1000);
        if (oneThreadAccess != 1)
        {
            Console.WriteLine("Expected original WriteLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 50;
        }
        Interlocked.Decrement(ref oneThreadAccess);
        if (oneThreadAccess != 0)
        {
            Console.WriteLine("Expected original WriteLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 60;
        }
        rwls.ExitWriteLock();
    }

    void LockCompetitorWrite()
    {
        Interlocked.Increment(ref threadCount);
        if (threadCount == 8)
            mre.Set();
        rwls.EnterWriteLock();
        Interlocked.Increment(ref oneThreadAccess);
        Thread.Sleep(2000);
        if (oneThreadAccess != 1)
        {
            Console.WriteLine("Expected original WriteLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 70;
        }
        Interlocked.Decrement(ref oneThreadAccess);
        Thread.Sleep(3000);
        if (oneThreadAccess != 0)
        {
            Console.WriteLine("Expected original WriteLock to have the lock, but a second thread appears to have modified oneThreadAccess");
            threadCount = 80;
        }
        rwls.ExitWriteLock();
    }
}