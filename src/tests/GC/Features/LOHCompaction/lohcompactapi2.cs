// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Reflection;
using System.Threading;


namespace LOHCompactAPI
{
    class Program
    {
        static int ListSize = 500;
        static List<byte[]> shortLivedList = new List<byte[]>(ListSize);
        static List<byte[]> LongLivedList = new List<byte[]>(ListSize);
        static volatile bool testDone = false;

        //There are several threads that allocate, and the main thread calls the compacting API
        //Verify that the compaction mode changes to default after a blocking GC happened, and does not change if a blocking GC did not happen.
        public static int Main(string[] args)
        {
            int retVal = 100;
            int iterations = 10;

            if (args.Length > 0)
                iterations = Int32.Parse(args[0]);
            Console.WriteLine("Running {0} iterations", iterations);

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

            for (int i = 0; i < iterations; i++)
            {
                if (!Test1())
                {
                    retVal = 1;
                    break;
                }
                Thread.Sleep(3);
            }
            Console.WriteLine("Test1 passed");

            for (int i = 0; i < iterations; i++)
            {
                if (!Test2())
                {
                    retVal = 1;
                    break;
                }
                Thread.Sleep(100);
            }
            Console.WriteLine("Test2 passed");


            testDone = true;
            AllocatingThread.Join();
            for (int i = 0; i < numThreads; i++)
            {
                threadArr[i].Join();
            }
            return retVal;
        }

        public static bool Test1()
        {


            Console.WriteLine("Setting GCLargeObjectHeapCompactionMode.CompactOnce");
            int GCCount = 0;
            int initialGCCount = GetBlockingGen2Count();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GCCount = GetBlockingGen2Count();
            if (initialGCCount != GCCount)
            {
                Console.WriteLine("A GC happened while setting CompactOnce. Old count {0}, new Count {1}", initialGCCount, GCCount);
                //skip this run
                return true;
            }


            Thread.Sleep(100);
            int currentGCCount = GetBlockingGen2Count();
            GCLargeObjectHeapCompactionMode mode = GCSettings.LargeObjectHeapCompactionMode;
            GCCount = GetBlockingGen2Count();
            if (currentGCCount != GCCount)  //a GC happened in between these calls
            {
                Console.WriteLine("A GC happened while getting Compaction Mode. Old count {0}, new Count {1}", currentGCCount, GCCount);
                //skip this run
                return true;
            }

            Console.WriteLine("initial GC count: {0}; currentGCCount: {1}", initialGCCount, currentGCCount);
            Console.WriteLine(mode);
            if (currentGCCount == initialGCCount)
            {
                if (mode != GCLargeObjectHeapCompactionMode.CompactOnce)
                {
                    Console.WriteLine("GCLargeObjectHeapCompactionMode should be CompactOnce; instead it is " + mode);
                    return false;
                }
            }
            else
            {
                if (mode != GCLargeObjectHeapCompactionMode.Default)
                {
                    Console.WriteLine("GCLargeObjectHeapCompactionMode should be Default; instead it is " + mode);
                    return false;
                }
            }
            return true;

        }

        public static bool Test2()
        {


            Console.WriteLine("Setting GCLargeObjectHeapCompactionMode.CompactOnce");
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GCLargeObjectHeapCompactionMode mode = GCSettings.LargeObjectHeapCompactionMode;
            Console.WriteLine(mode);
            if (mode != GCLargeObjectHeapCompactionMode.Default)
            {
                Console.WriteLine("GCLargeObjectHeapCompactionMode should be CompactOnce; instead it is " + mode);
                return false;
            }

            return true;
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
                    tempList.Add(new byte[50]);
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




        //Only count the blocking gen2 GC's. Concurrent GC's should be subtracted from the total GC count.
        public static int GetBlockingGen2Count()
        {

            //Get the number of concurrent collections (can use this method only through reflection):
            MethodInfo collectionCountmethod = null;
            Type GCType = Type.GetType("System.GC");
            foreach(MethodInfo m in GCType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (m.Name.Equals("_CollectionCount") && m.GetParameters().Length == 2) collectionCountmethod = m;
            }
            if (collectionCountmethod == null)
            {
                Console.WriteLine("collectionCount method is null");
                return 0;
            }
            if (collectionCountmethod == null)
            {
                Console.WriteLine("collectionCount method is null");
                return 0;
            }
            object[] parameters = new object[2];
            parameters[0] = 2;
            parameters[1] = 1; // special gc count
            int backgroundCollections = (int)collectionCountmethod.Invoke(null, parameters);
            int TotalCollections = GC.CollectionCount(2);
            Console.WriteLine("Total collections {0}, background {1}", TotalCollections, backgroundCollections);
            return (TotalCollections - backgroundCollections);
        }
    }
}
