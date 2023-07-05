// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

//Test for Partial Compaction
//Goals: create fragmentation in Gen2
//Allocation should not be too intense
//For testing the concurrent phase of partial compaction: update references between objects
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

        public static int threadCount = 1;
        public static bool timeBased = true;
        public static int maxDepth = 10;
        public static int maxHeapMB = 100;  //max heap in MB
        public static long maxAllocation; //bytes
        public static int regionSizeMB = 4; //MB
        public static double EstimatedHeapSize = 0; //bytes
        public static double EstimatedObjectCount = 0; //estimate how many objects we have
        public static double AvgObjectSize = 0; //bytes

        [ThreadStatic]
        public static List<ObjectWrapper> staticArr = new List<ObjectWrapper>(2500);
        [ThreadStatic]
        public static List<Region> regionList = new List<Region>(2500);
        public static int staticIndex = 0;
        public static ObjectWrapper staticObject;



        public static int Main(string[] args)
        {
            if (!ParseArgs(args))
                return 101;

            maxAllocation = maxHeapMB * 1024 * 1024;
            Rand = new Random(randomSeed);
            List<ObjectWrapper> Arr = new List<ObjectWrapper>(2500);

                pointerSize = IntPtr.Size;

            RunTest(Arr);
            GC.KeepAlive(Arr);
            return 100;

        }


        public static void AllocatingPhase(List<ObjectWrapper> Arr, int maxRegions)
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
                int spaceBucket = Rand.Next(0, sizeBuckets.Length);
                int objectBucket = Rand.Next(0, sizeBuckets.Length);
                int pinnedPercentage = 0;
                if (i % 5 == 0)
                    pinnedPercentage = Rand.Next(0, 10);
                Region r = new Region(pinnedPercentage, sizeBuckets[spaceBucket].minsize, sizeBuckets[spaceBucket].maxsize, sizeBuckets[objectBucket].minsize, sizeBuckets[objectBucket].maxsize);
                r.Initialize(Arr);
                regionList.Add(r);
                if (i % 3 == 0 && i > 0)
                    DeleteSpaces();
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
                if (regionList[i].ClearSpaces())
                {
                    regionList.RemoveAt(i);
                }
            }
        }


        public static void SteadyState(List<ObjectWrapper> Arr)
        {
            Console.WriteLine("Heap size=" + GC.GetTotalMemory(false));
            Console.WriteLine("Estimated Heap size=" + EstimatedHeapSize);
            EstimatedObjectCount = CountTotalObjects(Arr);
            ClearVisitedFlag(Arr);
            Console.WriteLine("Before shuffle:EstimatedObjectCount " + EstimatedObjectCount);
            Console.WriteLine("Heap size=" + GC.GetTotalMemory(false));
            int iterCount = (int)(EstimatedObjectCount / 30);
            Console.WriteLine("Shuffle {0} times", iterCount);
            for (int iter2 = 0; iter2 < iterCount; iter2++)
            {
         //       Console.WriteLine("Shuffle iter " + iter2);
                ShuffleReferences(Arr);
            }
            EstimatedObjectCount = CountTotalObjects(Arr);
            ClearVisitedFlag(Arr);
            Console.WriteLine("After shuffle:EstimatedObjectCount " + EstimatedObjectCount);
            Console.WriteLine("Heap size=" + GC.GetTotalMemory(false));
            EstimatedHeapSize = EstimatedObjectCount * AvgObjectSize;
            Console.WriteLine("Estimated Heap size=" + EstimatedHeapSize);
            //randomly remove some objects

            if (EstimatedObjectCount >100)
                RemoveObjects(Arr);


        }

        public static void ShuffleReferences(List<ObjectWrapper> Arr)
        {
            ClearVisitedFlag(Arr);
            //Console.WriteLine("Shuffle");
            EstimatedObjectCount = CountTotalObjects(Arr);
        //    Console.WriteLine("EstimatedObjectCount " + EstimatedObjectCount);
            int randNumber = Rand.Next(0, (int)EstimatedObjectCount); //Console.WriteLine(randNumber);
            int randNumber2 = Rand.Next(0, (int)EstimatedObjectCount); //Console.WriteLine(randNumber2);
            PickObject(Arr, randNumber);

            ObjectWrapper Object1 = staticObject;
            for (int i = 0; i < Object1.m_data.Length; i++)
            {
                Object1.m_data[i] = (byte)(Object1.m_data[i] + randNumber);
            }
            PickObject(Arr, randNumber2);
            for (int i = 0; i < staticObject.m_data.Length; i++)
            {
                staticObject.m_data[i] = (byte)(staticObject.m_data[i] + randNumber2);
            }

            if (Object1 != null)
            {
                Object1.ref1 = staticObject;
            //    Console.Write("Set ref from {0}", Object1.m_dataSize);
           //     if (staticObject != null)
            //        Console.WriteLine("to " + staticObject.m_dataSize);
            }

        }

        public static void PickObject(List<ObjectWrapper> Arr, int index)
        {
            ClearVisitedFlag(Arr);
            staticObject=null;
            staticIndex = 0;
            for (int i = 0; i < Arr.Count; i++)
            {
                //Console.WriteLine("in Arr, pos=" + i);
                if (staticObject != null)
                    return;
               // Console.WriteLine("now pick object in Arr, pos=" + i);
                 PickObject(Arr[i], index);
            }

            for (int i = 0; i < staticArr.Count; i++)
            {
               // Console.WriteLine("in staticArr, pos=" + i);
                if (staticObject != null)
                    return;
               // Console.WriteLine("now pick object in static Arr, pos=" + i);
                 PickObject(staticArr[i], index);
            }
        }

        public static void PickObject(ObjectWrapper o, int index)
        {
            if (o.visited)
            {
                return;
            }
            o.visited = true;
       //    Console.WriteLine("Try Pick object" + staticIndex);

            if(staticObject != null)
                return;
            if (staticIndex == index)
            {
             //   Console.WriteLine("Object {0}; found for index {1}", o.m_dataSize, staticIndex);
                staticObject = o;
                return;
            }

            staticIndex++;


            if (o.ref1 != null && staticObject == null)
                PickObject(o.ref1, index);
            if (o.ref2 != null && staticObject == null)
                PickObject(o.ref2, index);
            if (o.ref3 != null && staticObject == null)
                PickObject(o.ref3, index);
            if (o.arrayRefs != null && staticObject == null)
            {
                for (int i = 0; i < o.arrayRefs.Length; i++)
                {
                    if (o.arrayRefs[i] != null && staticObject == null)
                        PickObject(o.arrayRefs[i], index);
                }
            }
        }
        public static void RemoveObjects(List<ObjectWrapper> Arr)
        {
            /*
            int CountPinned = CountPinnedObjects(Arr);
            Console.WriteLine("pinned objects, before removing= " + CountPinned);
            int Count = CountTotalObjects(Arr);
            Console.WriteLine("total objects, before removing= " + Count);
            Console.WriteLine("percentage pinned " + (float)CountPinned * 100.0f / (float)Count);
            */
            Console.WriteLine("Removing Objects");
            //Console.WriteLine("before: Arr.Count " + Arr.Count);
            for (int i = Arr.Count - 1; i >= 0; i--)
            {
                if (i % 4 == 0)
                {
                    if (GC.GetGeneration(Arr[i]) == 2)
                    {
                        Arr.RemoveAt(i);
                    }
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

            //Console.WriteLine("before: gcHandleArr.Count " + gcHandleArr.Count);
            //remove weak handles for dead objects

            EstimatedObjectCount = CountTotalObjects(Arr);
            EstimatedHeapSize = EstimatedObjectCount * AvgObjectSize;
            //Console.WriteLine("After removing objects: Estimated Heap size= " + EstimatedHeapSize);
        }



        //estimate the total number of objects in the reference graph
        public static int CountTotalObjects(List<ObjectWrapper> Arr)
        {
            ClearVisitedFlag(Arr);
           // Console.WriteLine("Counting Objects..");
            //use the "visited" table
            int runningCount = 0;

            for (int i = 0; i < Arr.Count; i++)
            {
                runningCount += CountReferences(Arr[i]);
            }

            for (int i = 0; i < staticArr.Count; i++)
            {
                runningCount += CountReferences(staticArr[i]);
            }
           // Console.WriteLine("Counted {0} objects", runningCount);
            return runningCount;
        }

        public static int CountPinnedObjects(List<ObjectWrapper> Arr)
        {
            ClearVisitedFlag(Arr);
            Console.WriteLine("Counting Objects..");
            //use the "visited" table
            int runningCount = 0;

            for (int i = 0; i < Arr.Count; i++)
            {
                runningCount += CountPinnedReferences(Arr[i]);
            }

            for (int i = 0; i < staticArr.Count; i++)
            {
                runningCount += CountPinnedReferences(staticArr[i]);
            }
            Console.WriteLine("Counted {0} objects", runningCount);
            return runningCount;
        }

        public static void ClearVisitedFlag(List<ObjectWrapper> Arr)
        {
           // Console.WriteLine("Clearing flag..");

            for (int i = 0; i < Arr.Count; i++)
            {
                ClearVisitedFlag(Arr[i]);
            }

            for (int i = 0; i < staticArr.Count; i++)
            {
                ClearVisitedFlag(staticArr[i]);
            }
        }

        //counts the references of this objects
        public static int CountReferences(ObjectWrapper o)
        {
            if (o.visited)
            {
                return 0;
            }
            else
                o.visited = true;
            int count = 1;

            if (o.ref1 != null)
                count+= CountReferences(o.ref1);
            if (o.ref2 != null)
                 count+= CountReferences(o.ref2);
            if (o.ref3 != null)
                count+= CountReferences(o.ref3);
            if (o.arrayRefs != null)
            {
                for (int i = 0; i < o.arrayRefs.Length; i++)
                {
                    if (o.arrayRefs[i] != null)
                    {
                        count += CountReferences(o.arrayRefs[i]);
                    }
                }
            }
            return count;
        }

        public static void ClearVisitedFlag(ObjectWrapper o)
        {
            if (!o.visited)
            {
                return;
            }
            else
                o.visited = false;

            if (o.ref1 != null)
                ClearVisitedFlag(o.ref1);
            if (o.ref2 != null)
                ClearVisitedFlag(o.ref2);
            if (o.ref3 != null)
                ClearVisitedFlag(o.ref3);
            if (o.arrayRefs != null)
            {
                for (int i = 0; i < o.arrayRefs.Length; i++)
                {
                    if (o.arrayRefs[i] != null)
                    {
                        ClearVisitedFlag(o.arrayRefs[i]);
                    }
                }
            }

        }
        //counts the pinned references of this objects
        public static int CountPinnedReferences(ObjectWrapper o)
        {
            if (o.visited)
            {
                return 0;
            }
            else
                o.visited=true;
            int count = 0;
            if (o.m_pinned)
                count = 1;

            if (o.ref1 != null)
                count += CountPinnedReferences(o.ref1);
            if (o.ref2 != null)
                count += CountPinnedReferences(o.ref2);
            if (o.ref3 != null)
                count += CountPinnedReferences(o.ref3);
            if (o.arrayRefs != null)
            {
                for (int i = 0; i < o.arrayRefs.Length; i++)
                {
                    if (o.arrayRefs[i] != null)
                    {
                        count += CountPinnedReferences(o.arrayRefs[i]);
                    }
                }
            }
            return count;
        }



        public static void UpdateAvg()
        {
            AvgObjectSize = (double)EstimatedHeapSize / (double)EstimatedObjectCount;
            //Console.WriteLine("Avg object size " + AvgObjectSize);

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



        public static void RunTest(List<ObjectWrapper> Arr)
        {
            System.Diagnostics.Stopwatch threadStopwatch = new System.Diagnostics.Stopwatch();
            threadStopwatch.Start();

            int iter = 0;
            while (true)
            {
                Console.WriteLine("Allocating phase. Start at {0}", DateTime.Now);
                AllocatingPhase(Arr, 20);

                Console.WriteLine("starting steady state. Time is {0}", DateTime.Now);
                SteadyState(Arr);
                Console.WriteLine("End steady state. Time is {0}", DateTime.Now);
                iter++;

                if (timeBased)
                {
                    if (threadStopwatch.ElapsedMilliseconds / 1000 > timeout)
                        break;
                }
                else //not timebased
                {
                    if (iter >= countIters)
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
                    else if (String.Compare(currentArg.ToLower(), "depth") == 0) // number of iterations
                    {
                        currentArgValue = args[++i];
                        maxDepth = Int32.Parse(currentArgValue);
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
            if (countIters < 1)
            {
                Console.WriteLine("Incorrect values for arguments");
                return false;
            }
            InitializeSizeBuckets();

            Console.WriteLine("Repro with: ");
            Console.WriteLine("==============================");
            if (timeBased)
                Console.WriteLine("-timeout " + timeout);
            else
                Console.WriteLine("-iter " + countIters);
            Console.WriteLine("-maxHeapMB " + maxHeapMB);
            Console.WriteLine("-regionSizeMB " + regionSizeMB);
            Console.WriteLine("-randomseed " + randomSeed);
            Console.WriteLine("-depth " + maxDepth);
            Console.WriteLine("==============================");
            return true;
        }


        public static void Usage()
        {
            Console.WriteLine("ECO1 [options]");
            Console.WriteLine("\nOptions");
            Console.WriteLine("-? Display the usage and exit");
            Console.WriteLine("-iter <num iterations> : specify number of iterations for the test, default is " + countIters);
            Console.WriteLine("If using time based instead of iterations:");
            Console.WriteLine("-timeout <seconds> : when to stop the test, default is " + timeout);
            Console.WriteLine("-maxHeapMB <MB> : max heap size in MB to allocate, default is " + maxHeapMB);
            Console.WriteLine("-regionSizeMB <MB> : regionSize, default is " + regionSizeMB);
            Console.WriteLine("-depth <depth of reference tree>, default is " + maxDepth);

            Console.WriteLine("-randomseed <seed> : random seed(for repro)");
        }

        public class Region
        {
            public  List<Object> Spaces = new List<Object>(2500);

            public int depth;
            public int size = 0; //bytes
            public float pinnedPercentage;
            public int pinnedCount = 0;
            public int objectCount = 0;
            public int minObjectSize;
            public int maxObjectsize;
            public int minSpaceSize;
            public int maxSpaceSize;

            //create an empty region
            public Region(float pinnedPercentage, int minSpace, int maxSpace, int minObject, int maxObject)
            {
                this.pinnedPercentage = pinnedPercentage;
                minSpaceSize = minSpace;
                maxSpaceSize = maxSpace;
                minObjectSize = minObject;
                maxObjectsize = maxObject;

            }

            //add objects to region
            public void Initialize(List<ObjectWrapper> Arr)
            {
                while (size < regionSizeMB*1024*1024)
                {
                    //create an object with the characteristics(size, pinned) of this region. The object is added either o the static array or to Arr
                    bool useStatic = Rand.Next(0, 2) == 0 ? true : false;

                    if (useStatic)
                        staticArr.Add(ObjectWrapper.AddObject(this, 0, null));
                    else
                        Arr.Add(ObjectWrapper.AddObject(this, 0, null));

                }

                UpdateAvg();
            }

            public bool ClearSpaces()
            {
                if (Spaces.Count <= 0)
                {
                    Console.WriteLine("Spaces.Count <= 0");
                    return false;
                }
                if (GC.GetGeneration(Spaces[Spaces.Count - 1]) == 2)
                {
                    Spaces.Clear();
                    return true;
                }
                return false;
            }

        }

        public class ObjectWrapper
        {
            public bool visited = false;
            public ObjectWrapper parent = null;
            public GCHandle m_pinnedHandle;
            public bool m_pinned = false;
            public ObjectWrapper ref1;
            public ObjectWrapper ref2;
            public ObjectWrapper ref3;
            public byte[] m_data;
            public ObjectWrapper[] arrayRefs = null;
            public int m_dataSize;
            public int depth = 0;

            public static ObjectWrapper AddObject(Region r, int depth, ObjectWrapper Parent)
            {

                if (r.size >= regionSizeMB * 1024 * 1024 || depth>maxDepth)
                    return null;
                byte[] Temp = new byte[Rand.Next(50, 200)];
                int size = Rand.Next(r.minObjectSize, r.maxObjectsize);

                bool pinned = false;
                if ((r.pinnedCount * 100.0 / (double)r.objectCount) < r.pinnedPercentage)
                {
                    pinned = true;
                    r.pinnedCount++;
                }
                int randNumber = Rand.Next(0, 20);
                bool arrayrefs = false;
                if (randNumber == 1)
                    arrayrefs = true;
                int references = Rand.Next(0, 3);
                int arrayrefCount = 0;
                if (arrayrefs)
                {
                    arrayrefCount = Rand.Next(10, 100);
                    references = Rand.Next(3, arrayrefCount);
                }


                ObjectWrapper ow = new ObjectWrapper(size, pinned, references, arrayrefs, arrayrefCount, depth);
                if (randNumber == 7)
                    ow.parent = Parent;

                if (!arrayrefs) //object has up to 3 references to other objects
                {
                    if (references > 0)
                    {
                        ow.ref1 = AddObject(r, ow.depth+1, ow);
                    }
                    if (references > 1)
                    {
                        ow.ref2 = AddObject(r, ow.depth + 1, ow);
                    }
                    if (references > 2)
                    {
                        ow.ref3 = AddObject(r, ow.depth + 1, ow);
                    }
                }
                else  //object has an array of references
                {
                    for (int i = 0; i < arrayrefCount; i++)
                    {
                        ow.arrayRefs[i] = AddObject(r, depth+1, ow);
                    }
                }
                r.size += size;


                int spaceSize = Rand.Next(r.minSpaceSize, r.maxSpaceSize);
                r.Spaces.Add(new byte[spaceSize]);

                r.size += spaceSize;
                r.objectCount++;
                EstimatedObjectCount++;
                EstimatedHeapSize += size;
                return ow;
            }
            public ObjectWrapper(int datasize, bool pinned, int references, bool arrayrefs, int arrayRefCount, int depth)
            {
                this.depth = depth;
                //we want m_data to have an approximate size of dataSize
                m_dataSize = datasize;
                m_pinned = pinned;

                m_data = new byte[datasize];
                for (int i = 0; i < datasize; i++)
                {
                    m_data[i] = (byte)(references - i);
                }
                if(pinned)
                    m_pinnedHandle = GCHandle.Alloc(m_data, GCHandleType.Pinned);

                if (arrayrefs)
                {
                    arrayRefs = new ObjectWrapper[arrayRefCount];
                }


            }
            public void CleanUp()
            {
                if (m_pinned)
                {
                    if (m_pinnedHandle.IsAllocated)
                    {
                        m_pinnedHandle.Free();
                    }
                }
                GC.SuppressFinalize(this);
            }
            ~ObjectWrapper()
            {
                CleanUp();
            }

        }

    }
}
