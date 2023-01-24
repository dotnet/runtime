// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Purpose of program: exercise the GC, with various object sizes and lifetimes.
// Allocate objects that have an expiration time specified. When the object's lifetime expires, it is made garbage and then other new objects are created.
//
// Each object has a lifetime attached to it (in ObjectWrapper). New objects are added to a collection. When the lifetime is expired, the object is removed from the collection and subject to GC.
// There are several threads which access  the collection in random positions and if there is no object in that position they will create a new one.
//One thread is responsible to updating the objects'age and removing expired objects.
//The lifetime and the objects'size can be set by command line arguments.
//The objects'size is organized in buckets (size ranges), the user can specify the percentage for each bucket.
//Collection type can be array or binary tree.




namespace StressAllocator
{
    public class StressAllocator
    {
        //Define the size buckets:
        public struct SizeBucket
        {
            public int minsize;
            public int maxsize;
            public float percentage;  //percentage of objects that fall into this bucket
            public SizeBucket(int min, int max, float percentObj)
            {
                minsize = min;
                maxsize = max;
                percentage = percentObj;
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
        //// DEFAULT PARAMETERS
        //These parameters may be overridden by command line parameters
        public const int DEFAULT_MINLIFE = 3; //milliseconds
        public const int DEFAULT_MAXLIFE = 30; //milliseconds
        public const int DEFAULT_OBJCOUNT = 2000; //object count

        public const int DEFAULT_ITERATIONS = 400;
        public const float DEFAULT_PINNED = 1.0F; //percent pinned
        public const float DEFAULT_BUCKET1 = 30.0F; //percentage objects with size in this bucket
        public const float DEFAULT_BUCKET2 = 30.0F;
        public const float DEFAULT_BUCKET3 = 20.0F;
        public const float DEFAULT_BUCKET4 = 10.0F;
        //remaining will be allocated on Large Object Heap
        public const int DEFAULT_THREADS = 4;  //number of allocating threads
        public const int THREAD_IDLE_TIME = 0; //milliseconds
        public const int MAX_REFS = 4; //max number of references to another object
        ///////////// end default parameters


        public static long timeout = 0;
        public static SizeBucket[] sizeBuckets = new SizeBucket[SIZEBUCKET_COUNT];
        //Default settings:
        //minimum and maximum object lifetime (milliseconds)
        public static int minLife = DEFAULT_MINLIFE;
        public static int maxLife = DEFAULT_MAXLIFE;

        //how many objects will be initially allocated
        public static int objCount = DEFAULT_OBJCOUNT;


        public static int countIters = DEFAULT_ITERATIONS;
        public static float percentPinned = DEFAULT_PINNED;
        public static bool usePOH = false;  // if true, use POH allocations instead of pinned handles.
        public static bool LOHpin = false;  //if true, apply the percentPinned to just LOH, not overall.
        public static float percentBucket1 = DEFAULT_BUCKET1;
        public static float percentBucket2 = DEFAULT_BUCKET2;
        public static float percentBucket3 = DEFAULT_BUCKET3;
        public static float percentBucket4 = DEFAULT_BUCKET4;


        public static int maxRef = MAX_REFS;
        public static int numThreads = DEFAULT_THREADS;
        public static int threadIdleTime = THREAD_IDLE_TIME;//milliseconds
        public static int randomSeed;

        public static ObjArray objectCollection;

        public static List<Thread> threadList = new List<Thread>();

        public static System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        public static Object objLock = new Object();
        private static bool s_noLocks = false;  //for the option to not use locks when accessing objects.

        //keeping track of status:
        public static UInt64 current_TotalObjCount;
        public static UInt64 current_pinObjCount;
        public static UInt64[] current_bucketObjCount = new UInt64[SIZEBUCKET_COUNT];  //how many objects are in each bucket
        public static UInt64 current_LOHObjects = 0;
        public static bool testDone = false;
        //for status output:
        //keep track of the collection count for generations 0, 1, 2
        public static int[] currentCollections = new int[3];
        public static int outputFrequency = 0; //after how many iterations the data is printed
        public static System.TimeSpan totalTime;
        //weak ref array that keeps track of all objects
        public static WeakReferenceCollection WR_All = new WeakReferenceCollection();
        public static ObjectWrapper dummyObject;
        public static int pointerSize = 4;  //bytes
        [ThreadStatic]
        public static Random Rand;


        //This is because Random is not thread safe and it stops returning a random number after a while
        //public static int GetRandomNumber(int min, int max)
        //{
        //    lock(objLock)
        //    {
        //        return RandomObj.Next(min, max);
        //    }
        //}
        private static ObjectWrapper CreateObject()
        {
            bool isLOHObject = false;
            //pick a random value for the lifespan, from the min/max lifespan interval
            int lifeSpan = Rand.Next(minLife, maxLife);

            //decide the data size for this object

            int size = 0;
            for (int i = 0; i < SIZEBUCKET_COUNT; i++)
            {
                //First find what bucket to assign
                //find current percentage for bucket i; if smaller than target percentage, assign to this bucket
                if ((float)current_bucketObjCount[i] * 100.0F / (float)current_TotalObjCount < sizeBuckets[i].percentage)
                {
                    size = Rand.Next(sizeBuckets[i].minsize, sizeBuckets[i].maxsize);
                    //Console.WriteLine("bucket={0}, size {1}", i, size);
                    current_bucketObjCount[i]++;
                    break;
                }
            }
            if (size == 0)  //buckets are full; assign to LOH
            {
                isLOHObject = true;
                size = Rand.Next(85000, 130000);
                current_LOHObjects++;
                //Console.WriteLine("LOH " + size);
            }
            int references = Rand.Next(1, maxRef);

            //decide if to make this object pinned
            bool pin = false;

            if ((LOHpin && isLOHObject) || !LOHpin)
            {
                float pinPercentage;
                if (LOHpin)
                {
                    pinPercentage = (float)current_pinObjCount * 100.0F / (float)current_LOHObjects;
                }
                else
                {
                    pinPercentage = (float)current_pinObjCount * 100.0F / (float)current_TotalObjCount;
                }
                if (pinPercentage < percentPinned)
                {
                    pin = true;
                    current_pinObjCount++;
                }
            }



            ObjectWrapper myNewObject;
            myNewObject = new ObjectWrapper(lifeSpan, size, pin, references);
            current_TotalObjCount++;
            /*
            lock (objLock)
            {
                Console.WriteLine("Created object with: ");
                Console.WriteLine("datasize= " + size);
                Console.WriteLine("lifetime= " + lifeSpan);
                Console.WriteLine("pinned= " + pin);
            }
             */

            WR_All.Add(myNewObject);


            return myNewObject;
        }

        /*
        static void AddReference(ObjectWrapper parent, ObjectWrapper child)
        {
            //find an empty position to add a reference in the parent
            //else use a random position
            int position = GetRandomNumber(maxRef);
            for (int i = 0; i < maxRef; i++)
            {
                if (parent.objRef[i] == null)
                {
                    position = i;
                    break;
                }
            }

            parent.objRef[position] = child;

        }
         * */

        public static int Main(string[] args)
        {
            //            if (Environment.Is64BitProcess)
            pointerSize = 8;

            stopWatch.Start();

            for (int i = 0; i < 3; i++)
            {
                currentCollections[i] = 0;
            }

            if (!ParseArgs(args))
                return 101;
            dummyObject = new ObjectWrapper(0, 0, true, 0);

            objectCollection = new ObjArray();
            objectCollection.Init(objCount);
            //One thread is in charge of updating the object age
            Thread thrd = new Thread(UpdateObjectAge);
            thrd.Start();
            //another thread is removing expired objects
            Thread thrd2 = new Thread(RemoveExpiredObjects);
            thrd2.Start();
            //another thread is removing weak references to dead objects
            Thread thrd3 = new Thread(RemoveWeakReferences);
            thrd3.Start();

            // Run the test.
            for (int i = 0; i < numThreads; ++i)
            {
                Thread thread = new Thread(RunTest);


                threadList.Add(thread);
                thread.Start(i);
            }

            foreach (Thread t in threadList)
            {
                t.Join();
            }
            testDone = true;

            return 100;
        }


        public static void RunTest(object threadInfoObj)
        {
            System.Diagnostics.Stopwatch threadStopwatch = new System.Diagnostics.Stopwatch();
            threadStopwatch.Start();
            int threadIndex = (int)threadInfoObj;
            //initialize the thread static random with a different seed for each thread
            Rand = new Random(randomSeed + threadIndex);

            //Allocate the initial objects. Each thread creates objects for one portion of the array.
            int objPerThread = objCount / numThreads;
            int remainder = objCount % numThreads;
            if (threadIndex == numThreads - 1)
            {
                objPerThread += remainder;
            }

            int begin = threadIndex * (objCount / numThreads);
            Console.WriteLine("thread " + threadIndex + "; allocating " + objPerThread + " objects;");
            int beginIndex = threadIndex * (objCount / numThreads);
            for (int i = beginIndex; i < beginIndex + objPerThread; i++)
            {
                objectCollection.SetObjectAt(CreateObject(), i);
            }
            //lock (objLock)
            //{
            //    Console.WriteLine("thread {1}:Number of objects in collection: {0}; objCount={2}", objectCollection.Count, threadIndex, current_TotalObjCount);
            //}
            //Console.WriteLine("thread {1}:Number of objects in WR: {0}", WR_All.Count, threadIndex);
            Console.WriteLine("starting steady state");
            //Steady state: objects die and others are created

            for (int i = 0; i < countIters; ++i)
            {
                for (int j = 0; j < objCount; j++)
                {
                    // Randomly access a position in the collection
                    int pos = Rand.Next(0, objCount);
                    //Console.WriteLine("pos " + pos);
                    bool ret;
                    if (s_noLocks)
                    {
                        ret = objectCollection.AccessObjectAt(pos);
                    }
                    else
                    {
                        lock (objLock)
                        {
                            ret = objectCollection.AccessObjectAt(pos);
                        }
                    }
                    //Console.WriteLine("Thread " + threadIndex + " accessing object at " + pos + " expired= " + ret);



                }

                if ((Rand.Next(0, numThreads) != 0))
                {
                    Thread.Sleep(threadIdleTime);
                    //Console.WriteLine("Number of objects in collection: {0}", objectCollection.Count);
                    //Console.WriteLine("Number of objects in WR: {0}", WR_All.Count);
                }

                if (outputFrequency > 0 && i > 0)
                {
                    if ((i % outputFrequency == 0 || i == countIters - 1) && threadIndex == 0)
                    {
                        OutputGCStats(i);
                    }
                }
            }
            testDone = true;
        }

        public static void UpdateObjectAge(object threadInfoObj)
        {
            long previousTime = 0;
            while (!testDone)
            {
                long currentTime = stopWatch.ElapsedMilliseconds;
                //Console.WriteLine("time when starting loop:" + stopWatch.ElapsedMilliseconds);
                if (currentTime - previousTime >= 1)
                {
                    //Console.WriteLine("time to update" + (currentTime - previousTime));
                    objectCollection.UpdateObjectsAge(currentTime - previousTime);
                }
                else
                    System.Threading.Thread.Sleep(1);
                previousTime = currentTime;
            }
        }


        public static void RemoveExpiredObjects(object threadInfoObj)
        {
            long previousTime = 0;
            while (!testDone)
            {
                long currentTime = stopWatch.ElapsedMilliseconds;
                if (currentTime - previousTime >= 1)
                {
                    //Console.WriteLine("time when removeExpired loop:" + (currentTime - previousTime));
                    objectCollection.RemoveExpiredObjects();
                }
                else
                    System.Threading.Thread.Sleep(1);
                previousTime = currentTime;
            }
        }

        public static void RemoveWeakReferences(object threadInfoObj)
        {
            System.Threading.Thread.Sleep(100);
            while (!testDone)
            {
                System.Threading.Thread.Sleep(100);
                WR_All.RemoveDeadObjects();
            }
        }


        public static void OutputGCStats(int iterations)
        {
            Console.WriteLine("Iterations = {0}", iterations);
            Console.WriteLine("AllocatedMemory = {0} bytes", GC.GetTotalMemory(false));
            Console.WriteLine("Number of objects in collection: {0}", objectCollection.Count);

            //get the number of collections and the elapsed time for this group of iterations
            int[] collectionCount = new int[3];
            for (int j = 0; j < 3; j++)
            {
                collectionCount[j] = GC.CollectionCount(j);
            }

            int[] newCollections = new int[3];
            for (int j = 0; j < 3; j++)
            {
                newCollections[j] = collectionCount[j] - currentCollections[j];
            }

            //update the running count of collections
            for (int j = 0; j < 3; j++)
            {
                currentCollections[j] = collectionCount[j];
            }

            Console.WriteLine("Gen 0 Collections = {0}", newCollections[0]);
            Console.WriteLine("Gen 1 Collections = {0}", newCollections[1]);
            Console.WriteLine("Gen 2 Collections = {0}", newCollections[2]);

            stopWatch.Stop();
            Console.Write("Elapsed time: ");
            System.TimeSpan tSpan = stopWatch.Elapsed;
            if (tSpan.Days > 0)
                Console.Write("{0} days, ", tSpan.Days);
            if (tSpan.Hours > 0)
                Console.Write("{0} hours, ", tSpan.Hours);
            if (tSpan.Minutes > 0)
                Console.Write("{0} minutes, ", tSpan.Minutes);
            Console.Write("{0} seconds, ", tSpan.Seconds);
            Console.Write("{0} milliseconds", tSpan.Milliseconds);

            totalTime += tSpan;
            stopWatch.Reset();
            stopWatch.Start();

            Console.Write("  (Total time: ");
            if (totalTime.Days > 0)
                Console.Write("{0} days, ", totalTime.Days);
            if (totalTime.Hours > 0)
                Console.Write("{0} hours, ", totalTime.Hours);
            if (totalTime.Minutes > 0)
                Console.Write("{0} minutes, ", totalTime.Minutes);
            Console.Write("{0} seconds, ", totalTime.Seconds);
            Console.WriteLine("{0} milliseconds)", totalTime.Milliseconds);
            Console.WriteLine("----------------------------------");
        }

        public static void InitializeSizeBuckets()
        {
            sizeBuckets[0] = new SizeBucket(BUCKET1_MIN, BUCKET2_MIN, percentBucket1);
            sizeBuckets[1] = new SizeBucket(BUCKET2_MIN, BUCKET3_MIN, percentBucket2);
            sizeBuckets[2] = new SizeBucket(BUCKET3_MIN, BUCKET4_MIN, percentBucket3);
            sizeBuckets[3] = new SizeBucket(BUCKET4_MIN, BUCKETS_MAX, percentBucket4);
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
                    }
                    else if (String.Compare(currentArg.ToLower(), "minlife") == 0)
                    {
                        currentArgValue = args[++i];
                        minLife = Int32.Parse(currentArgValue);
                    }
                    else if (String.Compare(currentArg.ToLower(), "maxlife") == 0)
                    {
                        currentArgValue = args[++i];
                        maxLife = Int32.Parse(currentArgValue);
                    }
                    else if (String.Compare(currentArg.ToLower(), "objcount") == 0)
                    {
                        currentArgValue = args[++i];
                        objCount = Int32.Parse(currentArgValue);
                    }
                    //      else if (String.Compare(currentArg.ToLower(), "maxref") == 0)
                    //      {
                    //          currentArgValue = args[++i];
                    //          maxRef = Int32.Parse(currentArgValue);
                    //      }
                    else if (String.Compare(currentArg.ToLower(), "threads") == 0 || String.Compare(currentArg, "t") == 0)
                    {
                        currentArgValue = args[++i];
                        numThreads = Int32.Parse(currentArgValue);
                    }
                    else if (String.Compare(currentArg.ToLower(), "idletime") == 0 || String.Compare(currentArg, "t") == 0)
                    {
                        currentArgValue = args[++i];
                        threadIdleTime = Int32.Parse(currentArgValue);
                    }
                    else if (String.Compare(currentArg.ToLower(), "pinned") == 0)
                    {
                        currentArgValue = args[++i];
                        percentPinned = float.Parse(currentArgValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (String.Compare(currentArg.ToLower(), "usepoh") == 0)
                    {
                        currentArgValue = args[++i];
                        usePOH = bool.Parse(currentArgValue);
                    }
                    else if (String.Compare(currentArg.ToLower(), "lohpin") == 0)  //for LOH compacting testing, this is the option to apply the pinning percentage to LOH
                    {
                        LOHpin = true;
                    }
                    else if (String.Compare(currentArg.ToLower(), "bucket1") == 0)
                    {
                        currentArgValue = args[++i];
                        percentBucket1 = float.Parse(currentArgValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (String.Compare(currentArg.ToLower(), "bucket2") == 0)
                    {
                        currentArgValue = args[++i];
                        percentBucket2 = float.Parse(currentArgValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (String.Compare(currentArg.ToLower(), "bucket3") == 0)
                    {
                        currentArgValue = args[++i];
                        percentBucket3 = float.Parse(currentArgValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (String.Compare(currentArg.ToLower(), "bucket4") == 0)
                    {
                        currentArgValue = args[++i];
                        percentBucket4 = float.Parse(currentArgValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (String.Compare(currentArg.ToLower(), "nolocks") == 0)
                    {
                        s_noLocks = true;
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
                    else if (String.Compare(currentArg.ToLower(), "out") == 0) //output frequency
                    {
                        currentArgValue = args[++i];
                        outputFrequency = int.Parse(currentArgValue);
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
            if (countIters < 1 || numThreads < 1 || minLife < 1 || maxLife < 1 || objCount < 1 || outputFrequency < 0)
            {
                Console.WriteLine("Incorrect values for arguments");
                return false;
            }
            if (percentPinned < 0 || percentPinned > 100)
            {
                Console.WriteLine("Incorrect values for percent arguments");
                return false;
            }
            if (percentBucket1 + percentBucket2 + percentBucket3 + percentBucket4 > 100)
            {
                Console.WriteLine("Bad values for buckets percentage");
                return false;
            }
            InitializeSizeBuckets();

            Console.WriteLine("Repro with: ");
            Console.WriteLine("==============================");
            Console.WriteLine("-iter " + countIters);
            Console.WriteLine("-minlife " + minLife);
            Console.WriteLine("-maxlife " + maxLife);
            Console.WriteLine("-objcount " + objCount);
            Console.WriteLine("-t " + numThreads);
            Console.WriteLine("-pinned " + percentPinned);
            Console.WriteLine("-usepoh " + usePOH);
            Console.WriteLine("-bucket1 " + percentBucket1);
            Console.WriteLine("-bucket2 " + percentBucket2);
            Console.WriteLine("-bucket3 " + percentBucket3);
            Console.WriteLine("-bucket4 " + percentBucket4);
            //     Console.WriteLine("-collectiontype " + CollectionTypeToString(objectCollectionType));
            //      Console.WriteLine("-maxref " + maxRef);
            Console.WriteLine("-out " + outputFrequency);
            Console.WriteLine("-randomseed " + randomSeed);
            Console.WriteLine("==============================");
            return true;
        }


        public static void Usage()
        {
            Console.WriteLine("GCSimulator [options]");
            Console.WriteLine("\nOptions");
            Console.WriteLine("-? Display the usage and exit");
            Console.WriteLine("-i [-iter] <num iterations> : specify number of iterations for the test, default is " + countIters);
            Console.WriteLine("-t <number of threads> : specifiy number of threads, default is " + numThreads);
            Console.WriteLine("-minlife  <milliseconds> : minimum object lifetime, default is " + minLife);
            Console.WriteLine("-maxlife  <milliseconds> : maximum object lifetime, default is " + maxLife);
            Console.WriteLine("-objcount <count> : how many objects are initially allocated, default is " + objCount);
            Console.WriteLine("-pinned <percent of pinned objects> : specify the percentage of data that we want to pin (number from 0 to 100), default is " + percentPinned);
            Console.WriteLine("-usepoh <true/false> : specify whether to use poh for pinning, default is " + false);
            Console.WriteLine("-bucket1 <percentage> : specify the percentage of be in size bucket1(" + BUCKET1_MIN + "bytes to " + BUCKET2_MIN + "bytes), default is " + DEFAULT_BUCKET1);
            Console.WriteLine("-bucket2 <percentage> : specify the percentage of be in size bucket2(" + BUCKET2_MIN + "bytes to " + BUCKET3_MIN + "bytes), default is " + DEFAULT_BUCKET2);
            Console.WriteLine("-bucket3 <percentage> : specify the percentage of be in size bucket3(" + BUCKET3_MIN + "bytes to " + BUCKET4_MIN + "bytes), default is " + DEFAULT_BUCKET3);
            Console.WriteLine("-bucket4 <percentage> : specify the percentage of be in size bucket4(" + BUCKET4_MIN + "bytes to " + BUCKETS_MAX + "bytes), default is " + DEFAULT_BUCKET4);
            //     Console.WriteLine("-collectiontype  <List|Tree|Graph> : specify whether to use a list, tree or graph, default is " + CollectionTypeToString(objectCollectionType));
            //     Console.WriteLine("-maxref <number of references> : maximum number of references an object can have(only for tree and graph collection type), default is " + maxRef);
            Console.WriteLine("-out <iterations> : after how many iterations to output data");
            Console.WriteLine("-randomseed <seed> : random seed(for repro)");
        }







        public class ObjectWrapper
        {
            public GCHandle m_pinnedHandle;
            public bool m_pinned = false;
            public int m_lifeTime;  //milliseconds
            public long m_age = 0;
            public long m_creationTime;
            //           public Object[] objRef = new Object[maxRef]; //references to other objects
            //       public int refCount = 0;  //how many objects point at this object

            public Object[] m_data;
            protected byte[] m_pinnedData;
            public int m_dataSize;


            public ObjectWrapper(int lifetime, int datasize, bool pinned, int references)
            {
                m_creationTime = DateTime.Now.Ticks / 10000;
                //we want m_data to have an approximate size of dataSize
                m_dataSize = datasize;
                m_lifeTime = lifetime;
                m_pinned = pinned;
                //we want m_data to have an approximate size of m_dataSize

                if (!pinned) //Cannot pin m_data because we cannot pin reference type objects
                {
                    m_data = new Object[m_dataSize / pointerSize];
                    for (int i = 0; i < m_data.Length; i += 100)
                    {
                        m_data[i] = "abc";
                    }

                    for (int i = 0; i < references; i++)
                    {
                        //set up a reference from this new object to other objects
                        //We can set from the new object to:
                        //1. old objects (from the weak reference array)
                        //2. create new objects
                        //decide on one of those options:
                        int option = Rand.Next(0, 20);
                        int toIndex;
                        int fromIndex = Rand.Next(0, m_data.Length);
                        switch (option)
                        {
                            case 0:  //objects in the (strong reference) collection
                                toIndex = Rand.Next(0, objectCollection.Count);
                                ObjectWrapper ow = objectCollection.GetObjectAt(toIndex);
                                if (ow != dummyObject)
                                    m_data[fromIndex] = ow;
                                break;

                            case 1:  //objects in the weak reference collection
                                toIndex = Rand.Next(0, WR_All.Count);
                                m_data[fromIndex] = WR_All.GetObjectAt(toIndex);
                                break;
                            case 2: //new objects
                                m_data[fromIndex] = CreateObject();
                                break;
                        }
                    }

                    WR_All.Add(m_data);
                }
                else   //pinned
                {
                    m_pinnedData = System.GC.AllocateArray<byte>(m_dataSize, pinned: usePOH);
                    for (int i = 0; i < m_dataSize; i += 1000)
                    {
                        m_pinnedData[i] = 5;
                    }

                    if (!usePOH)
                        m_pinnedHandle = GCHandle.Alloc(m_pinnedData, GCHandleType.Pinned);

                    WR_All.Add(m_pinnedData);
                }
            }
            public void CleanUp()
            {
                if (m_pinned && !usePOH)
                {
                    if (m_pinnedHandle.IsAllocated)
                        m_pinnedHandle.Free();
                }
            }
            ~ObjectWrapper()
            {
                CleanUp();
            }
        }



        /*
                /////////////////////////// Collection definition
                public interface ObjCollection
                {
                    void Init(int numberOfObjects);
                    ObjectWrapper GetObjectAt(int index);
                    void SetObjectAt(ObjectWrapper o, int index);

                    bool AccessObjectAt(int index);

                    int Count
                    {
                        get;
                    }
                    int Size
                    {
                        get;
                    }
                    //One pass through the collection, updates objects'age and removes expired ones
                    void UpdateObjectsAge(long elapsedMsec);
                    void RemoveExpiredObjects();

                }

         * */
        public class ObjArray
        {
            private ObjectWrapper[] _array;
            private int _size;
            public void Init(int numberOfObjects)
            {
                _array = new ObjectWrapper[numberOfObjects];
                for (int i = 0; i < numberOfObjects; i++)
                {
                    _array[i] = dummyObject;
                }
                _size = numberOfObjects;
            }
            public void SetObjectAt(ObjectWrapper o, int index)
            {
                if (index >= _size)
                {
                    Console.WriteLine("AddObjectAt " + index + " index is out of bounds");
                }
                _array[index] = o;
            }

            public bool AccessObjectAt(int index)
            {
                if (index >= _size)
                    return false;
                if (/*m_Array[index] == null || */_array[index] == dummyObject)
                {
                    _array[index] = CreateObject();
                    return true;
                }

                //        if (!noLocks)
                //         {
                long timeNow = DateTime.Now.Ticks / 10000;

                if ((timeNow - _array[index].m_creationTime) > _array[index].m_lifeTime)
                {
                    //    Console.WriteLine("Object age" + (timeNow - m_Array[index].m_creationTime) + "; lifetime=" + m_Array[index].m_lifeTime);
                    //object is expired; put a new one in its place
                    _array[index] = CreateObject();
                    return true;
                }
                //      }

                return false;
            }
            public ObjectWrapper GetObjectAt(int index)
            {
                if (index >= _size)
                {
                    Console.WriteLine("GetObject " + index + " index is out of bounds");
                }
                return _array[index];
            }

            public int Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < _size; i++)
                    {
                        if (_array[i] != dummyObject)
                            count++;
                    }
                    return count;
                }
            }

            public int Size
            {
                get
                {
                    return _size;
                }
            }

            //One pass through the collection, updates objects'age
            public void UpdateObjectsAge(long elapsedMsec)
            {
                for (int i = 0; i < _size; i++)
                {
                    ObjectWrapper o = _array[i];
                    o.m_age += (int)elapsedMsec;
                }
            }


            //One pass through the collection, removes expired ones
            public void RemoveExpiredObjects()
            {
                for (int i = 0; i < _size; i++)
                {
                    if (_array[i] != dummyObject)
                    {
                        if (s_noLocks)
                        {
                            if (_array[i].m_age >= _array[i].m_lifeTime)
                            {
                                _array[i] = dummyObject;
                            }
                        }
                        else
                        {
                            lock (objLock)
                            {
                                if (_array[i].m_age >= _array[i].m_lifeTime)
                                {
                                    _array[i] = dummyObject;
                                }
                            }
                        }
                    }
                }
            }
        }

        public class WeakReferenceCollection
        {
            private object _WRLock;
            private List<WeakReference> _WR;
            public WeakReferenceCollection()
            {
                _WRLock = new Object();
                _WR = new List<WeakReference>();
            }
            public void Add(Object o)
            {
                lock (_WRLock)
                {
                    _WR.Add(new WeakReference(o));
                }
            }

            public Object GetObjectAt(int index)
            {
                lock (_WRLock)
                {
                    if (index >= _WR.Count)
                        index = _WR.Count - 1;
                    if (_WR[index] == null)
                    {
                        Console.WriteLine("WRAll null:" + index);
                        // commented out for coreclr
                        //Environment.Exit(0);
                    }
                    if (_WR[index].IsAlive)
                    {
                        if (_WR[index].Target != null)
                        {
                            Object[] target = _WR[index].Target as Object[];
                            if (target != null)
                            {
                                return target;
                            }
                        }
                    }
                    return null;
                }
            }

            public void CheckWRAll()
            {
                lock (_WRLock)
                {
                    for (int i = 0; i < _WR.Count; i++)
                    {
                        if (_WR[i] == null)
                        {
                            Console.WriteLine("null:" + i);
                            // commented out for coreclr
                            //Environment.Exit(0);
                        }
                    }
                }
            }
            public int Count
            {
                get
                {
                    lock (_WRLock)
                    {
                        return _WR.Count;
                    }
                }
            }

            public void RemoveDeadObjects()
            {
                lock (_WRLock)
                {
                    int endCollection = _WR.Count - 1;
                    for (int i = endCollection; i >= 0; i--)
                    {
                        if (!_WR[i].IsAlive)
                        {
                            _WR.RemoveAt(i);
                        }
                    }
                }
            }
            ////Try setting  a reference from an object in the list to the new object
            //public bool SetRefFromOldToNew(Object myNewObject)
            //{
            //    //Pick a random list position and determine if it is possible to set a ref from this list position to the new object
            //    lock (WRLock)
            //    {
            //        int pos = Rand.Next(0, WR.Count);
            //        if (WR[pos] == null)
            //            return false;
            //        if (!WR[pos].IsAlive)
            //            return false;
            //        ObjectWrapper ow = WR[pos].Target as ObjectWrapper;
            //        if (ow == null)
            //        {
            //            Object[] data = WR[pos].Target as Object[];
            //            if (data != null)
            //            {
            //                int index = Rand.Next(0, data.Length);
            //                data[index] = myNewObject;
            //                return true;
            //            }
            //        }
            //        else
            //        {
            //            if (ow.m_pinned == true)
            //                return false;

            //            if (ow.m_data != null)
            //            {
            //                int index = Rand.Next(0, ow.m_data.Length);
            //                ow.m_data[index] = myNewObject;
            //                Console.WriteLine("Set ref from {0}, {1}", pos, index);
            //                return true;
            //            }
            //        }

            //    }
            //    return false;

            //}
        }
    }
}



