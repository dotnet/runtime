// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Runtime;
using System.Reflection;
using System.Threading;
//Repro for bug Bug 656705: Heap corruption when using LOH compaction


namespace LOHCompactScenarioRepro
{
    public class Program
    {
        static int ListSize = 500;
        static List<byte[]> shortLivedList = new List<byte[]>(ListSize);
        static List<byte[]> LongLivedList = new List<byte[]>(ListSize);
        static volatile bool testDone = false;

        //There are several threads that allocate, and the main thread calls the compacting API
        public static int Main(string[] args)
        {
            int minutesTorRun = 10;

            if (args.Length > 0)
                minutesTorRun = Int32.Parse(args[0]);
            Console.WriteLine("Running {0} minutes", minutesTorRun);

            testDone = false;

            Thread AllocatingThread = new Thread(Allocate);
            AllocatingThread.Start();
            int numThreads = 100;
            Thread[] threadArr = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threadArr[i] = new Thread(AllocateTempObjects);
                threadArr[i].Start();
            }
            System.Diagnostics.Stopwatch stw = System.Diagnostics.Stopwatch.StartNew();

            int iter = 0;
            while (true)
            {
               GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();

                for (int i = 0; i < 100; i++)
                {
                    System.Threading.Thread.Sleep(80);
                    GC.Collect();
                }
                iter++;
                if (stw.ElapsedMilliseconds > minutesTorRun * 60 * 1000)
                {
                    Console.WriteLine("Time exceeded {0} min", minutesTorRun);
                    Console.WriteLine("Ran {0} iterations", iter);
                    break;
                }
            }


            testDone = true;
            AllocatingThread.Join();
            for (int i = 0; i < numThreads; i++)
            {
                threadArr[i].Join();
            }

            if (iter < 3)
            {
                Console.WriteLine("Test needs to run at least a few iterations in order to be useful.");
                return 5;
            }
            return 100;
        }

      

        public static void AllocateTempObjects(object threadInfoObj)
        {
            int listSize2 = 1000;
            List<byte[]> tempList = new List<byte[]>();
            while (!testDone)
            {
                byte[] temp = new byte[20];
                for (int i = 0; i < listSize2; i++)
                {
                    if (i % 200 == 0)
                    {
                        tempList.Add(new byte[85000]);
                    }
                    else
                    {
                        tempList.Add(new byte[50]);
                    }
                   
                }
                tempList.Clear();
            }

        }

        public static void Allocate(object threadInfoObj)
        {
            int ListSize = 300;
            System.Random rnd = new Random(1122);

            int listSize2 = 1000;
            List<byte[]> newList = new List<byte[]>(500 + 1000);


            while (!testDone)
            {
                for (int i = 0; i < ListSize; i++)
                {
                    newList.Add(new byte[85000]);
                    newList.Add(new byte[200]);
                    Thread.Sleep(10);
                }
                for (int i = 0; i < listSize2; i++)
                {
                    newList.Add(new byte[50]);
                }
                newList.Clear();
            }
        }

    }
}
