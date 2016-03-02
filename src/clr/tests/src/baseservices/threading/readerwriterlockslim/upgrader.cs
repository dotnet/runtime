// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class Upgrader
{
    private static ReaderWriterLockSlim rwls = new ReaderWriterLockSlim();
    private const int readerCount = 10;
    private static CountdownEvent ev = new CountdownEvent(readerCount);

    /// <summary>
    /// This is the thread body that will try to enter read/write/upgrade 
    /// lock.
    /// </summary>
    /// <param name="threadIndex"></param>
    /// <param name="lockAction">0: try enter read lock; 1: try enter write lock; other value: try enter upgrade lock</param>
    public static void ThreadMethod(object parameters)
    {
        object[] parameterArray = (object[]) parameters;
        int threadIndex = (int)parameterArray[0];
        int lockAction = (int)parameterArray[1];

        if (lockAction == 0)
            Upgrader.rwls.EnterReadLock();
        else if (lockAction == 1)
            Upgrader.rwls.EnterWriteLock();
        else
            Upgrader.rwls.EnterUpgradeableReadLock();

        Upgrader.ev.Signal();
        Console.WriteLine("    @Thread {0} Signal ", threadIndex);
        Thread.CurrentThread.Join();
    }

    public static int RunTest()
    {
        int retCode = 0;

        try
        {
            Upgrader.rwls.EnterUpgradeableReadLock();
            Console.WriteLine("    Main thread EnterUpgradeableReadLock !");

            for (int i = 0; i < readerCount; i++)
            {
                Thread t = new Thread(new ParameterizedThreadStart(Upgrader.ThreadMethod));
                object[] parameters = new object[] { i, 0 };
                // The thread MUST be set to background thread otherwise it will be dead forever
                t.IsBackground = true;
                t.Start(parameters);
                Console.WriteLine("    @Thread {0} start ", i);
            }

            ev.Wait();      
            Console.WriteLine("    ev.wait ");
            if (rwls.TryEnterWriteLock(5 * 1000))
            {
                Console.WriteLine(" Wrong: @Main thread enter Write Lock! ");
                retCode = 90;
            }
            else
            {
                Console.WriteLine("    @Main thread cannot enter Write Lock! ");
                retCode = 100;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Wrong: Unexpected exception happened!");
            Console.WriteLine(e.ToString());
            retCode = 80;
        }
        return retCode;
    }

    static int Main()
    {
        int retCode = Upgrader.RunTest();
        if (retCode == 100)
            Console.WriteLine("    Test Passed! ");
        else
            Console.WriteLine("    Test Failed! ");
        return retCode;
    }
}