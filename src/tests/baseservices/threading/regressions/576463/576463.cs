// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;
public class Test
{
    bool _aRun = false;
    public void A()
    {
        _aRun = true;
        Console.WriteLine("A");
    }

    bool _bRun = false;
    public void B()
    {
        _bRun = true;
        Console.WriteLine("B");
    }

    volatile bool _cRun = false;
    public void C()
    {
        _cRun = true;
        Console.WriteLine("C");
        if (s_takeLock)
        {
            Console.WriteLine("C: Entering   -- Monitor on _objLock");

            Monitor.Enter(_objLock);

            Console.WriteLine("C: Entered    -- Monitor on _objLock");

            Console.WriteLine("C: Exiting   -- Monitor on _objLock");
            Monitor.Exit(_objLock);
            Console.WriteLine("C: Exited   -- Monitor on _objLock");
        }
    }

    bool _dRun = false;
    public void D()
    {
        _dRun = true;
        Console.WriteLine("D");
    }

    public bool Pass()
    {
        bool testPassed = true;
        if (_aRun == false)
        {
            Console.WriteLine("Delegate A did not run");
            testPassed = false;
        }

        if (_bRun == false)
        {
            Console.WriteLine("Delegate B did not run");
            testPassed = false;
        }

        if (_cRun == false)
        {
            Console.WriteLine("Delegate C did not run");
            testPassed = false;
        }

        if (_dRun == false)
        {
            Console.WriteLine("Delegate D did not run");
            testPassed = false;
        }

        return testPassed;
    }

    public void Send()
    {
        if (_cb != null)
            _cb();
    }

    volatile object _objLock = new object();

    delegate void GenericCallback();
    event GenericCallback _cb;

    public static void ReadArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "/contention":
                    s_contention = true;
                    goto case "/lock";
                case "/lock":
                    s_takeLock = true;
                    break;
                default:
                    break;
            }
        }
    }

    static bool s_takeLock = false;
    static bool s_contention = false;

    [Fact]
    public static int TestEntryPoint() => Run(new string[0]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(string[] args)
    {
        ReadArgs(args);

        Test t = new Test();

        // subscribe methods to the event
        t._cb += t.A;
        t._cb += t.B;
        // C will take a Monitor on t._objLock, if we are holding it when
        // C tries to take the lock it should spin, and in the failure case
        // D will not get called.
        t._cb += t.C;
        t._cb += t.D;

        if (s_contention)
        {
            Console.WriteLine("Main: Entering   -- Monitor on _objLock");
            Monitor.Enter(t._objLock);
            Console.WriteLine("Main: Entered   -- Monitor on _objLock");
        }

        // Start a new thread with t.Send() as the ThreadStart delegate
        Thread newThread = new Thread(new ThreadStart(t.Send));
        newThread.Start();

        if (s_contention)
        {
            while (!t._cRun)
            {
                Thread.Sleep(100);
            }

            // We know C has started running, wait a little bit to let
            // it get into the Monitor.Enter code. This has a possible race.
            Thread.Sleep(5000);

            // Once we're here we know that C has started running and
            // is presumably in the Monitor.Enter code, now we release 
            // the lock to let C have it
            Console.WriteLine("Main: Exiting   -- Monitor on _objLock");
            Monitor.Exit(t._objLock);
            Console.WriteLine("Main: Exited   -- Monitor on _objLock");
        }

        // Wait for the eventing on the other thread to finish
        newThread.Join();

        // Check whether or not all callbacks were called
        if (t.Pass())
        {
            Console.WriteLine("Test passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test failed!");
            Console.WriteLine("If some delegegates did not run, this failure is most likely due to the loopcount register not being properly tracked during assembly for monitor.");
            return 50;
        }
    }
}
