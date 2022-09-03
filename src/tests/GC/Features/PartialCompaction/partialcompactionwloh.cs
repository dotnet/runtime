// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime;

//Test for Partial Compaction
//Goals: create fragmentation in Gen2
//Allocation should not be too intense
//For testing the concurrent phase of partial compaction: update references between objects
//This test also had LOH objects and creates fragmentation in LOH
//What the test does:
// 1.Allocating phase:
//-Allocate n regions
//-When objects get in Gen2 release the objects used to create spaces
//-Create refs from objects in these regions to ephemeral objects
// 2.Steady state
//- randomly change references between objects
//- release some of the existing objects (to create fragmentation in the existing regions)
// Repeat from 1.
namespace PartialCompactionTest
{
    public class PartialCompactionTest
    {

        //Define the size buckets:
        public struct SizeBucket
        {
            public int minsize;
            public int maxsize;
            //public float percentage;  //percentage of objects that fall into this bucket
            public SizeBucket(int min, int max)
            {
                //
                minsize = min;
                maxsize = max;
            }

        }

        //Buckets are defined as following:
        //Bucket1: 17 bytes to 200 bytes
        //Bucket2: 200bytes to 1000 bytes
        //Bucket3: 1000 bytes to 10000 bytes
        //Bucket4: 10000 bytes to 80000 bytes
        //The rest is over 85000 bytes (Large Object Heap)
        private const int SIZEBUCKET_COUNT = 4;
        private const int BUCKET1_MIN = 50;
        private const int BUCKET2_MIN = 200;
        private const int BUCKET3_MIN = 1000;
        private const int BUCKET4_MIN = 10000;
        private const int BUCKETS_MAX = 80000;
        //////

        public const int DEFAULT_ITERATIONS = 100;
        public static int countIters = DEFAULT_ITERATIONS;
        public static long timeout = 600; //seconds
        public static SizeBucket[] sizeBuckets = new SizeBucket[SIZEBUCKET_COUNT];
        public static int randomSeed;

        public static int pointerSize = 4;  //bytes
        [ThreadStatic]
        public static Random Rand;

        //new
        public static bool timeBased = true;
        public static int maxHeapMB = 100;  //max heap in MB
        public static long maxAllocation; //bytes
        public static int regionSizeMB = 4; //MB
        public static double EstimatedHeapSize = 0; //bytes
        public static double EstimatedObjectCount = 0; //estimate how many objects we have
        public static List<Object> Visited = new List<Object>(2500);  //for estimating the objects count
        public static double AvgObjectSize = 0; //bytes
        public static List<Object> staticArr = new List<Object>(2500);
        public static List<GCHandle> gcHandleArr = new List<GCHandle>(2500);
        public static List<GCHandle> weakList = new List<GCHandle>(2500);
        public static List<Region> regionList = new List<Region>(2500);
        public static Object[] ephemeralList = new Object[2500];


        public static int Main(string[] args)
        {
            if (!ParseArgs(args))
                return 101;

            maxAllocation = maxHeapMB * 1024 * 1024;
            Rand = new Random(randomSeed);
            List<Object> Arr = new List<Object>(2500);

                pointerSize = IntPtr.Size;

            RunTest(Arr);
            GC.KeepAlive(Arr);
            return 100;

        }

        public static Object CreateObject(int size, bool pinned)
        {
            if (!pinned)
            {
                int sz = size / pointerSize;
                Object[] o = new Object[sz];
                for (int i = 0; i < sz; i++)
                {
                    o[i] = null;
                }
                return o;
            }
            else
            {
                byte[] b = new byte[size];
                for (int i = 0; i < size; i++)
                {
                    b[i] = 5;
                }
                return b;
            }
        }




        public static void InitialAllocation(List<Object> Arr)
        {
            for (int i = 0; i < 5; i++)
            {
                Object[] o = new Object[3];
                staticArr.Add(o);
                weakList.Add(GCHandle.Alloc(o, GCHandleType.Weak));

            }
            AllocatingPhase(Arr, 50);
        }

        public static void AllocatingPhase(List<Object> Arr, int maxRegions)
        {
            int regionSize = regionSizeMB * 1024 * 1024;
            //decide how many regions to allocate
            long size = maxAllocation - (long)EstimatedHeapSize;
            long regionsToAllocate = size / (long)(regionSize);

            if (regionsToAllocate <= 0)
            {
                System.Threading.Thread.Sleep(1000);
                return;
            }
            if (regionsToAllocate > maxRegions)
                regionsToAllocate = maxRegions;
            Console.WriteLine("Allocating {0} regions", regionsToAllocate);
            for (long i = 0; i < regionsToAllocate; i++)
            {
                bool LOH = false;
                int randNumber = Rand.Next(0, 20);
                if (randNumber == 5)
                    LOH = true;

                int pinnedPercentage = 0;
                if (i % 5 == 0)
                    pinnedPercentage = Rand.Next(0, 10);

                if (!LOH)
                {
                    int spaceBucket = Rand.Next(0, sizeBuckets.Length);
                    int objectBucket = Rand.Next(0, sizeBuckets.Length);

                    AllocateRegion(regionSize, pinnedPercentage, sizeBuckets[spaceBucket].minsize, sizeBuckets[spaceBucket].maxsize, sizeBuckets[objectBucket].minsize, sizeBuckets[objectBucket].maxsize, Arr);
                }
                else
                {
                    Console.WriteLine("Allocating in LOH");
                    int minsize = 85000;
                    int maxsize = 200000;
                    AllocateRegion(regionSize, pinnedPercentage, minsize, maxsize, minsize, maxsize, Arr);
                }
                 if(i%3==0 && i>0)
                     DeleteSpaces();
                 if (i % 3 == 0 && i > 3)
                     CleanupRegions();
            }
            DeleteSpaces();
        }


        //want to create fragmentation in Gen2; when objects in the "spaces" list get in gen2, clear the list.
        public static void DeleteSpaces()
        {
            if (regionList.Count == 0)
                return;
            for (int i = regionList.Count - 1; i >= 0; i--)
            {
                Region r = regionList[i];
                if (r.Spaces.Count <= 0)
                    continue;
                if (GC.GetGeneration(r.Spaces[r.Spaces.Count - 1]) == 2)
                {
                    r.ReferenceEphemeralObjects();
                    r.Spaces.Clear();
                    r.Objects.Clear();
                }
            }
        }

        public static void CleanupRegions()
        {
            if (regionList.Count == 0)
                return;
            for (int i = regionList.Count - 1; i >= 0; i--)
            {
                Region r = regionList[i];
                if (r.Ephemeral != null && r.Ephemeral.Count > 0)
                {
                    if (GC.GetGeneration(r.Ephemeral[0]) >= 1)
                    {
                        regionList.RemoveAt(i);
                    }
                }
            }
        }

        public static void SteadyState(List<Object> Arr)
        {
            Console.WriteLine("Heap size=" + GC.GetTotalMemory(false));
            Console.WriteLine("Estimated Heap size=" + EstimatedHeapSize);
            for (int iter2 = 0; iter2 < 100; iter2++)
            {
                UpdateReferences();
                int randnumber2 = Rand.Next(0, 3);
                if (iter2 % 50 == 0 && randnumber2==2)
                {
                    Console.WriteLine("Setting LOH compaction mode & collect");
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                }
            }
            //randomly remove some objects

            RemoveObjects(Arr);
            for (int iter3 = 0; iter3 < 100; iter3++)
            {
                UpdateReferences();
            }



        }

        public static void UpdateReferences()
        {
            for (int i = 0; i < weakList.Count; i++)
            {
                if (weakList[i].Target == null)
                    continue;
                Object[] OAr = weakList[i].Target as Object[];
                if (OAr == null)
                    continue;


                for (int j = 0; j < OAr.Length; j++)
                {
                    if (OAr[j] != null)
                    {
                        int pos = Rand.Next(0, weakList.Count);
                        if (weakList[pos].IsAllocated)
                        {
                            OAr[j] = weakList[pos].Target;
                        }
                    }
                }
            }
        }

        public static void RemoveObjects(List<Object> Arr)
        {
            Console.WriteLine("Removing Objects");
            //Console.WriteLine("before: Arr.Count " + Arr.Count);
            for(int i= Arr.Count-1; i>=0; i--)
            {
                if (i % 4 == 0)
                {
                    if(GC.GetGeneration(Arr[i])==2)
                    Arr.RemoveAt(i);
                }
            }
            //Console.WriteLine("after: Arr.Count" + Arr.Count);
            //Console.WriteLine("before: staticArr.Count " + staticArr.Count);
            for (int j = staticArr.Count - 1; j >= 0; j--)
            {
                if (j % 4 == 0)
                {
                    if (GC.GetGeneration(staticArr[j]) == 2)
                    staticArr.RemoveAt(j);
                }
            }
            //Console.WriteLine("after: staticArr.Count " + staticArr.Count);
           // Console.WriteLine("before: gcHandleArr.Count " + gcHandleArr.Count);
            for (int k = gcHandleArr.Count - 1; k >= 0; k--)
            {
                if (k % 2 == 0)
                {
                    if (GC.GetGeneration(gcHandleArr[k].Target) == 2)
                    gcHandleArr[k].Free();
                    gcHandleArr.RemoveAt(k);
                }
            }
            //Console.WriteLine("after: gcHandleArr.Count " + gcHandleArr.Count);
            //remove weak handles for dead objects
            CleanupWeakReferenceArr();
            int objectCount = CountTotalObjects(Arr);
            Visited.Clear();
            //if pinned objects are more than 3% remove all of them
            if ((float)gcHandleArr.Count / (float)objectCount > 0.03f)
            {
                Console.WriteLine("removing all pinned objects");
                RemoveAllPinnedObjects();
            }
            //Console.WriteLine("total count " + objectCount);
            EstimatedHeapSize = objectCount * AvgObjectSize;
            //Console.WriteLine("After removing objects: Estimated Heap size= " + EstimatedHeapSize);
        }

        public static void RemoveAllPinnedObjects()
        {
            for (int k = 0; k < gcHandleArr.Count; k++)
            {
                gcHandleArr[k].Free();
            }
            gcHandleArr.Clear();
        }
        //estimate the total number of objects in the reference graph
        public static int CountTotalObjects(List<Object> Arr)
        {
            Visited.Clear();
            Console.WriteLine("Counting Objects..");
            //use the "visited" table
            int runningCount = 0;
           // runningCount += CountReferences(Arr[0]);
            for (int i = 0; i < Arr.Count; i++)
           {
               runningCount+= CountReferences(Arr[i]);
            }

            for (int i = 0; i < staticArr.Count; i++)
            {
                runningCount += CountReferences(staticArr[i]);
            }
            runningCount += gcHandleArr.Count;

            Console.WriteLine("Pinned GCHandles " + gcHandleArr.Count);

            return runningCount;
        }

        //counts the references of this objects
        public static int CountReferences( Object o)
        {
            if (Visited.Contains(o))
            {
                return 0;
            }
            else
                Visited.Add(o);
            int count = 1;

            Object[] oArr = o as Object[];
            if (oArr == null)
                return count;
            for (int i = 0; i < oArr.Length; i++)
            {
                if (oArr[i] != null)
                {
                    count += CountReferences(oArr[i]);
                }
            }

            return count;
        }
        public static void CleanupWeakReferenceArr()
        {
            for (int k = weakList.Count - 1; k >= 0; k--)
            {
                if (!weakList[k].IsAllocated)
                {
                    weakList.RemoveAt(k);
                }
                else if (weakList[k].Target == null)
                {
                    weakList[k].Free();
                    weakList.RemoveAt(k);
                }
            }
        }
        public static int AllocateRegion(int regionSize, float pinnedPercentage, int minSpace, int maxSpace, int minObject, int maxObject, List<Object> Arr)
        {
            int sizeCounter = 0;
            double pinnedCount = 0;
            double objectCount = 0;
            Object o;

            Region r = new Region();
            regionList.Add(r);
            while (sizeCounter < regionSize)
            {
                byte[] Temp = new byte[Rand.Next(50, 200)];
                int objSize = Rand.Next(minObject, maxObject); //Console.WriteLine("Objsize " + objSize);
                if ((pinnedCount * 100.0 / objectCount) < pinnedPercentage)
                {
                    AddPinnedObject(objSize);
                    pinnedCount++;
                }
                else
                {
                    o = AddObject(objSize, Arr);
                    r.Objects.Add(o);
                }

                int spaceSize = Rand.Next(minSpace, maxSpace);
                r.Spaces.Add(new byte[spaceSize]);
                sizeCounter += objSize;
                sizeCounter += spaceSize;
                objectCount++;
                EstimatedObjectCount ++;
                EstimatedHeapSize += objSize;

            }
            //Console.WriteLine("Pinned objects in region: " + pinnedCount);
           // Console.WriteLine("Allocated {0} objects per this region", objectCount);
           // Console.WriteLine("Allocated {0} bytes per this region including spaces", sizeCounter);
            UpdateAvg();
            return sizeCounter;
        }

        public static void UpdateAvg()
        {
            AvgObjectSize = (double)EstimatedHeapSize / (double)EstimatedObjectCount;
            //Console.WriteLine("Avg object size " + AvgObjectSize);
        }
        public static void AddPinnedObject(int objSize)
        {
            gcHandleArr.Add(GCHandle.Alloc(CreateObject(objSize, true), GCHandleType.Pinned));
        }

        public static void AddRef(Object from, Object to)
        {
            Object[] arrFrom = from as Object[];
            for (int i = 0; i < arrFrom.Length; i++)
            {
                if (arrFrom[i] == null)
                {
                    arrFrom[i] = to;
                    break;
                }
            }

        }

        //add ref from this object to existing objects
        public static void AddRefFrom(Object from)
        {
            int pos = Rand.Next(0, weakList.Count);
            bool found = false;
            while (!found)
            {
                pos = Rand.Next(0, weakList.Count);
                if (!weakList[pos].IsAllocated)
                    continue;
                if (weakList[pos].Target != null)
                {
                    AddRef(from, weakList[pos].Target);
                    found = true;
                }
            }
        }

        //add ref from this object to existing objects
        public static void AddRefTo(Object to)
        {
            int pos = Rand.Next(0, weakList.Count);
            bool found = false;
            while (!found)
            {
                pos = Rand.Next(0, weakList.Count);
                if (!weakList[pos].IsAllocated)
                    continue;
                if (weakList[pos].Target != null)
                {
                    AddRef(weakList[pos].Target, to);
                    found = true;
                }
            }
        }

        //add as reference to existing objects
        public static Object AddObject(int size, List<Object> Arr)
        {
            bool found = false;
            Object[] o = new Object[size / pointerSize];
            int r = Rand.Next(0, 10);
            if (r == 0)
            {
                staticArr.Add(o);
                //add ref from this object to existing objects
                AddRefFrom(o);
            }
            else if (r == 1)
            {
                Arr.Add(o);
                AddRefFrom(o);
            }
            else
            {
                //add as reference to existing objects
                AddRefTo(o);

            }

            //find an empty place in array
            found = false;
            for (int i = 0; i < weakList.Count; i++)
            {
                if (!weakList[i].IsAllocated)
                {
                    weakList[i] = GCHandle.Alloc(o, GCHandleType.Weak);
                    found = true;
                }
            }
            if(!found)
                weakList.Add(GCHandle.Alloc(o, GCHandleType.Weak));
            return o;
        }

        public static void AddEphemeralObject(int size)
        {
            for(int i=0; i<ephemeralList.Length; i++)
            {
                if(ephemeralList[i]==null || (GC.GetGeneration(ephemeralList[i])>=1))
                {
                    ephemeralList[i] = new byte[size];
                    break;
                }

            }
        }

        public static void RunTest(List<Object> Arr)
        {
            System.Diagnostics.Stopwatch threadStopwatch = new System.Diagnostics.Stopwatch();
            threadStopwatch.Start();



            //Steady state: objects die and others are created

            int iter = 0;
            while (true)
            {
                Console.WriteLine("Iteration# " + iter);
                int randnumber = Rand.Next(0, 30);
                if (randnumber == 1)
                {
                    Console.WriteLine("Setting LOH compaction mode");
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                }
                int randnumber2 = Rand.Next(0, 30);
                if (randnumber2 == 1)
                {
                    Console.WriteLine("GC.Collect");
                    GC.Collect();
                }

                Console.WriteLine("Allocating phase. Start at {0}", DateTime.Now);
                if(iter==0)
                    InitialAllocation(Arr);
                else
                    AllocatingPhase(Arr, 20);

                if (randnumber == 2)
                {
                    Console.WriteLine("Setting LOH compaction mode");
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                }
                if (randnumber2 == 2)
                {
                    Console.WriteLine("GC.Collect");
                    GC.Collect();
                }
               Console.WriteLine("starting steady state. Time is {0}", DateTime.Now);
               SteadyState(Arr);
               Console.WriteLine("End steady state. Time is {0}", DateTime.Now);
               if (randnumber == 3)
               {
                   Console.WriteLine("Setting LOH compaction mode");
                   GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
               }
               if (randnumber2 == 3)
               {
                   Console.WriteLine("GC.Collect");
                   GC.Collect();
               }
               iter++;

               if (timeBased)
               {
                   if (threadStopwatch.ElapsedMilliseconds / 1000 > timeout)
                       break;
               }
               else //not timebased
               {
                   if(iter>=countIters)
                       break;
               }
            }

        }



        public static void InitializeSizeBuckets()
        {
            sizeBuckets[0] = new SizeBucket(BUCKET1_MIN, BUCKET2_MIN);
            sizeBuckets[1] = new SizeBucket(BUCKET2_MIN, BUCKET3_MIN);
            sizeBuckets[2] = new SizeBucket(BUCKET3_MIN, BUCKET4_MIN);
            sizeBuckets[3] = new SizeBucket(BUCKET4_MIN, BUCKETS_MAX);
        }
        /// Parse the arguments and also initialize values that are not set by args
        public static bool ParseArgs(string[] args)
        {
            randomSeed = (int)DateTime.Now.Ticks;

            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string currentArg = args[i]; //Console.WriteLine(currentArg);
                    string currentArgValue;
                    if (currentArg.StartsWith("-") || currentArg.StartsWith("/"))
                    {
                        currentArg = currentArg.Substring(1);
                    }
                    else
                    {
                        Console.WriteLine("Error! Unexpected argument {0}", currentArg);
                        return false;
                    }

                    if (currentArg.StartsWith("?"))
                    {
                        Usage();
                        return false;
                    }
                    else if (String.Compare(currentArg.ToLower(), "iter") == 0) // number of iterations
                    {
                        currentArgValue = args[++i];
                        countIters = Int32.Parse(currentArgValue);
                        timeBased = false;
                    }
                    else if (String.Compare(currentArg.ToLower(), "maxheapmb") == 0)
                    {
                        currentArgValue = args[++i];
                        maxHeapMB = Int32.Parse(currentArgValue);
                    }
                    else if (String.Compare(currentArg.ToLower(), "regionsizemb") == 0)
                    {
                        currentArgValue = args[++i];
                        regionSizeMB = Int32.Parse(currentArgValue);
                    }
                    else if (String.Compare(currentArg.ToLower(), "timeout") == 0) //seconds; if 0 run forever
                    {
                        currentArgValue = args[++i];
                        timeout = Int64.Parse(currentArgValue);
                        if (timeout == -1)
                        {
                            timeout = Int64.MaxValue;
                        }
                    }
                    else if (String.Compare(currentArg.ToLower(), "randomseed") == 0) // number of iterations
                    {
                        currentArgValue = args[++i];
                        randomSeed = Int32.Parse(currentArgValue);
                    }
                    else
                    {
                        Console.WriteLine("Error! Unexpected argument {0}", currentArg);
                        return false;
                    }

                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Incorrect arguments");
                Console.WriteLine(e.ToString());
                return false;
            }

            //do some basic checking of the arguments
            if (countIters < 1 )
            {
                Console.WriteLine("Incorrect values for arguments");
                return false;
            }
            InitializeSizeBuckets();

            Console.WriteLine("Repro with: ");
            Console.WriteLine("==============================");
            if(timeBased)
                Console.WriteLine("-timeout " + timeout);
            else
                Console.WriteLine("-iter " + countIters);
            Console.WriteLine("-maxHeapMB " + maxHeapMB);
            Console.WriteLine("-regionSizeMB " + regionSizeMB);
            Console.WriteLine("-randomseed " + randomSeed);
            Console.WriteLine("==============================");
            return true;
        }


        public static void Usage()
        {
            Console.WriteLine("PartialCompactionTest [options]");
            Console.WriteLine("\nOptions");
            Console.WriteLine("-? Display the usage and exit");
            Console.WriteLine("-iter <num iterations> : specify number of iterations for the test, default is " + countIters);
            Console.WriteLine("If using time based instead of iterations:");
            Console.WriteLine("-timeout <seconds> : when to stop the test, default is " + timeout);
            Console.WriteLine("-maxHeapMB <MB> : max heap size in MB to allocate, default is " + maxHeapMB);
            Console.WriteLine("-regionSizeMB <MB> : regionSize, default is " + regionSizeMB);

            Console.WriteLine("-randomseed <seed> : random seed(for repro)");
        }

        public class Region
        {
            public List<Object> Spaces = new List<Object>(2500);
            public List<Object> Objects = new List<Object>(2500);
            public List<Object> Ephemeral = new List<Object>(2500);
            public void ReferenceEphemeralObjects()
            {
                //create refs from ephemeral objects to gen2 objects
                if (GC.GetGeneration(Objects[0]) == 2)
                {
                    int size = Rand.Next(30, 20000);
                    Object[] eph = new Object[size];
                    Ephemeral.Add(eph);
                    AddRef(eph, Objects[Rand.Next(0, Objects.Count)]);
                }
                Objects.Clear();
            }
        }

    }
}
