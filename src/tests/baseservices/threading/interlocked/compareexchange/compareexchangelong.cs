// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

namespace CompareExchangeLong
{
    class CompareExchangeLong
    {
        static int Main(string[] args)
        {
            // Check number of args
            if(args.Length != 2)
            {
                Console.WriteLine("USAGE:  CompareExchangeLong " +
                    "/loops:<int> /addVal:<long>");
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
                    valueToAdd = Convert.ToInt64(args[i].Substring(8));
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

            for(int i=0;i<threads.Length;i++)
                threads[i].Join();

            if(tsi.Total == tsi.Expected * threads.Length)
                rValue = 100;
            Console.WriteLine("Expected: "+ (tsi.Expected * threads.Length));
            Console.WriteLine("Actual  : "+ tsi.Total);
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
        public ThreadSafe(): this(100,Int64.MaxValue) { }
        public ThreadSafe(int loops, long value)
        {
            signal = new ManualResetEvent(false);
            numberOfIterations = loops;
            valueToAdd = value;
        }

        public void Signal()
        {
            signal.Set();
        }

        public void ThreadWorker()
        {
            signal.Set();
            for(int i=0;i<numberOfIterations;i++)
                AddToTotal(valueToAdd);
        }

        public long Expected
        {
            get
            {
                return (numberOfIterations * valueToAdd);
            }
        }

        public long Total
        {
            get { return totalValue; }
        }

        private long AddToTotal(long addend)
        {
            long initialValue, computedValue;
            signal.WaitOne();
            do
            {
                initialValue = totalValue;
                computedValue = initialValue + addend;
            } 
            while (initialValue != Interlocked.CompareExchange(ref totalValue, 
                computedValue, initialValue));
            return computedValue;
        }
    }    
}
