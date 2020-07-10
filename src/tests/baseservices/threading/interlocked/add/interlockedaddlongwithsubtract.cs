// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
//Regression for DevDiv Bugs 48020
namespace ExchangeAdd
{
    class InterlockedAddInt
    {
        static int Main(string[] args)
        {
            // Check number of args
            if(args.Length != 2)
            {
                Console.WriteLine("USAGE:  InterlockedAddLongWithSubtract " +
                    "/loops:<int> /addVal:<int>");
                return -1;
            }

            // Get the args
            int loops=100;
            long valueToAdd = 0;
        
            for(int i=0;i<args.Length;i++)
            {
                if(args[i].ToLower().StartsWith("/loops:"))
                {
                    loops = Convert.ToInt32(args[i].Substring(7));
                    continue;
                }

                if(args[i].ToLower().StartsWith("/addval:"))
                {
                    valueToAdd = Convert.ToInt32(args[i].Substring(8));
                    continue;
                }
            }

            int rValue = 0;
            Thread[] threads = new Thread[100];
            ThreadSafe tsi = new ThreadSafe(loops, valueToAdd);
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(new ThreadStart(tsi.ThreadWorker));
                threads[i].Start();
            }

            tsi.Signal();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            if (tsi.Total == tsi.Expected * threads.Length)
                rValue = 100;
            Console.WriteLine("Expected: " + (tsi.Expected * threads.Length));
            Console.WriteLine("Actual:   " + tsi.Total);
            Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
            return rValue;
        }
    }

    public class ThreadSafe
    {
        ManualResetEvent signal;
        private long totalValue = 0;
        private int numberOfIterations;
        private long valueToAdd;
        private long valueToSubtract;
        public ThreadSafe(): this(100,100) { }
        public ThreadSafe(int loops, long iAdd)
        {
            signal = new ManualResetEvent(false);
            numberOfIterations = loops;
            valueToAdd = iAdd;
            valueToSubtract = 0-iAdd;
        }

        public void Signal()
        {
            signal.Set();
        }

        public void ThreadWorker()
        {
            signal.WaitOne();
            for (int i = 0; i < numberOfIterations; i++)
            {
                Interlocked.Add(ref totalValue, valueToAdd);
                Interlocked.Add(ref totalValue, valueToSubtract);
            }

        }
        public long Expected
        {
            get
            {
                return (0);
            }
        }
        public long Total
        {
            get { return totalValue; }
        }
    }
}
