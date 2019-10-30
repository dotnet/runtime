// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Globalization;

namespace CompareExchangeDouble
{
    class CompareExchangeDouble
    {
        static int Main(string[] args)
        {
            // Check number of args
            if(args.Length != 2)
            {
                Console.WriteLine("USAGE:  CompareExchangeDouble " +
                    "/loops:<int> /addVal:<double>");
                return -1;
            }

            // Get the args
            int loops=100;
            double valueToAdd = 1E+100;
        
            for(int i=0;i<args.Length;i++)
            {
                if(args[i].ToLower().StartsWith("/loops:"))
                {
                    loops = Convert.ToInt32(args[i].Substring(7));
                    continue;
                }

                if(args[i].ToLower().StartsWith("/addval:"))
                {
					CultureInfo myCultureInfo = new CultureInfo("en-US");
					valueToAdd = Double.Parse(args[i].Substring(8), myCultureInfo);
                    continue;
                }
            }

            int rValue = 0;
            Thread[] threads = new Thread[100];
            ThreadSafe tsi = new ThreadSafe(loops,valueToAdd);
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(new ThreadStart(tsi.ThreadWorker));
                threads[i].Start();
            }
            
            tsi.Signal();

            for(int i=0;i<threads.Length;i++)
                threads[i].Join();
            double expected = 0.0D;
            for(int i=0;i<threads.Length*loops;i++)
                expected = (double)(expected + valueToAdd);
            if(tsi.Total == expected)
                rValue = 100;
            Console.WriteLine("Expected: "+expected);
            Console.WriteLine("Actual  : "+tsi.Total);
            Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
            return rValue;
        }
    }

    public class ThreadSafe
    {
        ManualResetEvent signal;
        private double totalValue = 0D;        
        private int numberOfIterations;
        private double valueToAdd;
        public ThreadSafe(): this(100,1E+100) { }
        public ThreadSafe(int loops, double addend)
        {
            signal = new ManualResetEvent(false);
            numberOfIterations = loops;
            valueToAdd = addend;
        }

        public void Signal()
        {
            signal.Set();
        }

        public void ThreadWorker()
        {
            signal.WaitOne();
            for(int i=0;i<numberOfIterations;i++)
                AddToTotal(valueToAdd);
        }

        public double Expected
        {
            get
            {
                return (numberOfIterations * valueToAdd);
            }
        }

        public double Total
        {
            get { return totalValue; }
        }

        private double AddToTotal(double addend)
        {
            double initialValue, computedValue;
            do
            {
                initialValue = totalValue;
                computedValue = (double)(initialValue + addend);
            } 
            while (initialValue != Interlocked.CompareExchange(
                ref totalValue, computedValue, initialValue));
            return computedValue;
        }
    }    
}
