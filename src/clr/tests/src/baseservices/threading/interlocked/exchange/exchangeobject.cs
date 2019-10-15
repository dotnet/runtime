// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace InteropTests
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    class ExchangeObject
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            int rValue = 0;
            Thread[] threads = new Thread[100];
            ThreadSafe tsi = new ThreadSafe();
            for (int i = 0; i < threads.Length - 1; i++)
            {
                if (i % 2 == 0)
                    threads[i] = new Thread(new ThreadStart(tsi.ThreadWorkerA));
                else
                    threads[i] = new Thread(new ThreadStart(tsi.ThreadWorkerB));
                threads[i].Start();
            }
            threads[threads.Length - 1] = new Thread(new ThreadStart(tsi.ThreadChecker));
            threads[threads.Length - 1].Start();
            tsi.Signal();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            if (tsi.Pass)
                rValue = 100;
            Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
            return rValue;
        }
    }

    public class ThreadSafe
    {
        ManualResetEvent signal;
        private Object totalValue = null;
        private int numberOfIterations;
        private Object newValueA = new Object();
        private Object newValueB = new Object();
        private bool success;
        public ThreadSafe(): this(10000) { }
        public ThreadSafe(int loops)
        {
            totalValue = newValueA;
            success = true;
            signal = new ManualResetEvent(false);
            numberOfIterations = loops;
        }

        public void Signal()
        {
            signal.Set();
        }

        public void ThreadWorkerA()
        {
            signal.WaitOne();
            for (int i = 0; i < numberOfIterations; i++)
                Interlocked.Exchange(ref totalValue, newValueB);

        }
        public void ThreadWorkerB()
        {
            signal.WaitOne();
            for (int i = 0; i < numberOfIterations; i++)
                Interlocked.Exchange(ref totalValue, newValueB);

        }
        public void ThreadChecker()
        {
            signal.WaitOne();
            Object tmpVal;
            for (int i = 0; i < numberOfIterations; i++)
            {
                tmpVal = totalValue;
                if (tmpVal != newValueB && tmpVal != newValueA)
                {
                    Console.WriteLine(tmpVal + "," + newValueB + "," + newValueA);
                    success = false;
                }
                Thread.Sleep(0);
            }

        }

        public bool Pass
        {
            get
            {
                return (success);
            }
        }
    }
}
