// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LifeTimeFX;

interface PinnedObject
{
    void CleanUp();
    bool IsPinned();

}

namespace GCSimulator
{
       
    class RandomLifeTimeStrategy : LifeTimeStrategy
    {
        private int counter = 0;
        private int mediumLifeTime = 30;
        private int shortLifeTime = 3;
        private int mediumDataCount = 1000000;
        private int shortDataCount = 5000;

        private Random rand = new Random(123456);

        public RandomLifeTimeStrategy(int mediumlt, int shortlt, int mdc, int sdc)
        {
            mediumLifeTime = mediumlt;
            shortLifeTime = shortlt;
            mediumDataCount = mdc;
            shortDataCount = sdc;

        }
        public int MediumLifeTime
        {
            set
            {
                mediumLifeTime = value;
            }
        }
        public int ShortLifeTime
        {
            set
            {
                shortLifeTime = value;
            }
        }

        public int NextObject(LifeTimeENUM lifeTime)
        {
            switch (lifeTime)
            {
                case LifeTimeENUM.Short:
                    return rand.Next() % shortDataCount;

                case LifeTimeENUM.Medium:
                    return (rand.Next() % mediumDataCount) + shortDataCount;


                case LifeTimeENUM.Long:
                    return 0;
            }
            return 0;
        }
        public bool ShouldDie(LifeTime o, int index)
        {
            counter++;
            LifeTimeENUM lifeTime = o.LifeTime;
            switch (lifeTime)
            {
                case LifeTimeENUM.Short:
                    if (counter % shortLifeTime == 0)
                        return true;
                    break;
                case LifeTimeENUM.Medium:
                    if (counter % mediumLifeTime == 0)
                        return true;
                    break;
                case LifeTimeENUM.Long:
                    return false;

            }
            return false;
        }
    }

    /// <summary>
    /// we might want to implement a different strategy that decide the life time of the object based on the time 
    /// elapsed since the last object access.
    /// 
    /// </summary>
    class TimeBasedLifeTimeStrategy : LifeTimeStrategy
    {
        private int lastMediumTickCount = Environment.TickCount;
        private int lastShortTickCount = Environment.TickCount;
        private int lastMediumIndex = 0;
        private int lastShortIndex = 0;

        public int NextObject(LifeTimeENUM lifeTime)
        {
            switch (lifeTime)
            {
                case LifeTimeENUM.Short:
                    return lastShortIndex;
                case LifeTimeENUM.Medium:
                    return lastMediumIndex;
                case LifeTimeENUM.Long:
                    return 0;
            }
            return 0;
        }

        public bool ShouldDie(LifeTime o, int index)
        {

            LifeTimeENUM lifeTime = o.LifeTime;
            // short objects will live for 20 seconds, long objects will live for more.
            switch (lifeTime)
            {
                case LifeTimeENUM.Short:
                    if (Environment.TickCount - lastShortTickCount > 1) // this is in accureat enumber, since
                    // we will be finsh iterating throuh the short life time object in less than 1 ms , so we need
                    // to switch either to QueryPeroformanceCounter, or to block the loop for some time through 
                    // Thread.Sleep, the other solution is to increase the number of objects a lot.
                    {
                        lastShortTickCount = Environment.TickCount;
                        lastShortIndex = index;
                        return true;
                    }

                    break;
                case LifeTimeENUM.Medium:
                    if (Environment.TickCount - lastMediumTickCount > 20)
                    {
                        lastMediumTickCount = Environment.TickCount;
                        lastMediumIndex = index;
                        return true;
                    }

                    break;
                case LifeTimeENUM.Long:
                    break;
            }
            return false;
        }
    }

    class ObjectWrapper : LifeTime, PinnedObject
    {

        private bool pinned;
        private bool weakReferenced;
        private GCHandle gcHandle;
        private LifeTimeENUM lifeTime;
        private WeakReference weakRef;

        private byte[] data;
        private int dataSize;
        public int DataSize
        {
            set
            {
                dataSize = value;
                data = new byte[dataSize];
                if (pinned)
                {
                    gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                }
                
                if (weakReferenced)
                {
                    weakRef = new WeakReference(data);
                }
            }

        }

        public LifeTimeENUM LifeTime
        {
            get
            {
                return lifeTime;
            }
            set
            {
                this.lifeTime = value;
            }
        }

        public bool IsPinned()
        {
            return pinned;
        }
        
        public bool IsWeak()
        {
            return weakReferenced;
        }
        
       
        public void CleanUp()
        {
            if (pinned)
            {
                gcHandle.Free();
            }
        }

        public ObjectWrapper(bool runFinalizer, bool pinned, bool weakReferenced)
        {
            this.pinned = pinned;
            this.weakReferenced = weakReferenced;
            if (!runFinalizer)
            {
                GC.SuppressFinalize(this);
            }
        }


        ~ObjectWrapper()
        {
            // DO SOMETHING BAD IN FINALIZER
            data = new byte[dataSize];
        }
    }

    class ClientSimulator
    {
        [ThreadStatic]
        private static ObjectLifeTimeManager lifeTimeManager;

        private static int meanAllocSize = 17;

        private static int mediumLifeTime = 30;
        private static int shortLifeTime = 3;

        private static int mediumDataSize = meanAllocSize;
        private static int shortDataSize = meanAllocSize;

        private static int mediumDataCount = 1000000;
        private static int shortDataCount = 5000;

        private static int countIters = 500;
        private static float percentPinned = 0.02F;
        private static float percentWeak = 0.0F;

        private static int numThreads = 1;

        private static bool runFinalizer = false;
        private static string strategy = "Random";

        private static bool noTimer = false;
        
        private static string objectGraph = "List";


        private static List<Thread> threadList = new List<Thread>();
        public static int Main(string[] args)
        {

            bool shouldContinue = ParseArgs(args);
            if (!shouldContinue)
            {
                return 1;
            }
                       
            int timer = 0;
            // Run the test.

            for (int i = 0; i < numThreads; ++i)
            {
                Thread thread = new Thread(RunTest);                               
                threadList.Add(thread);                                
                thread.Start();
                
            }

            foreach (Thread t in threadList)
            {
                t.Join();
            }
            
            return 100;
        }
        

        public static void RunTest(object threadInfoObj)
        {
            
            // Allocate the objects.
            lifeTimeManager = new ObjectLifeTimeManager();                                    
            LifeTimeStrategy ltStrategy;           

            int threadMediumLifeTime = mediumLifeTime;
            int threadShortLifeTime = shortLifeTime;
            int threadMediumDataSize = mediumDataSize;
            int threadShortDataSize = shortDataSize;
            int threadMediumDataCount = mediumDataCount;
            int threadShortDataCount = shortDataCount;
            float threadPercentPinned = percentPinned;
            float threadPercentWeak = percentWeak;
            bool threadRunFinalizer = runFinalizer;
            string threadStrategy = strategy;
            string threadObjectGraph = objectGraph;                       
            
            if (threadObjectGraph.ToLower() == "tree")
            {
                lifeTimeManager.SetObjectContainer(new BinaryTreeObjectContainer<LifeTime>());   
            }
            else
            {
                lifeTimeManager.SetObjectContainer(new ArrayObjectContainer<LifeTime>());
            }
            
            lifeTimeManager.Init(threadShortDataCount + threadMediumDataCount);    
            

            if (threadStrategy.ToLower()=="random")
            {
                ltStrategy = new RandomLifeTimeStrategy(threadMediumLifeTime, threadShortLifeTime, threadMediumDataCount, threadShortDataCount);
            }
            else
            {
                // may be we need to specify the elapsed time.
                ltStrategy = new TimeBasedLifeTimeStrategy();
            }

            lifeTimeManager.LifeTimeStrategy = ltStrategy;
            lifeTimeManager.objectDied += new ObjectDiedEventHandler(objectDied);

            for (int i=0; i < threadShortDataCount + threadMediumDataCount; ++i)
            {
                bool pinned = false;
                if (threadPercentPinned!=0)
                {
                    pinned = (i % ((int)(1/threadPercentPinned))==0);
                }

                bool weak = false;
                if (threadPercentWeak!=0)
                {
                    weak = (i % ((int)(1/threadPercentWeak))==0);
                }

                ObjectWrapper oWrapper = new ObjectWrapper(threadRunFinalizer, pinned, weak);                
                if (i < threadShortDataCount)
                {
                    oWrapper.DataSize = threadShortDataSize;
                    oWrapper.LifeTime = LifeTimeENUM.Short;
                }
                else
                {
                    oWrapper.DataSize = threadMediumDataSize;
                    oWrapper.LifeTime = LifeTimeENUM.Medium;                
                }                

                lifeTimeManager.AddObject(oWrapper, i);
            }

            for (int i = 0; i < countIters; ++i)
            {
            
                // Run the test.
                lifeTimeManager.Run();
            }

        }

        private static void objectDied(LifeTime lifeTime, int index)
        {
            // put a new fresh object instead;

            LifeTimeENUM lifeTimeEnum;
            lifeTimeEnum = lifeTime.LifeTime;

            ObjectWrapper oWrapper = lifeTime as ObjectWrapper;
            bool weakReferenced = oWrapper.IsWeak();
            bool pinned = oWrapper.IsPinned();
            if (pinned)
            { 
                oWrapper.CleanUp(); 
            }
                        
            oWrapper = new ObjectWrapper(runFinalizer, pinned, weakReferenced);
            oWrapper.LifeTime = lifeTimeEnum;
            oWrapper.DataSize = lifeTime.LifeTime == LifeTimeENUM.Short ? shortDataSize : mediumDataSize;
            lifeTimeManager.AddObject(oWrapper, index);
        }

        /// <summary>
        /// Parse the arguments, no error checking is done yet.
        /// TODO: Add more error checking.
        ///
        ///  Populate variables with defaults, then overwrite them with config settings.  Finally overwrite them with command line parameters
        /// </summary>
        public static bool ParseArgs(string[] args)
        {

            for (int i = 0; i < args.Length; ++i)
            {
                string currentArg = args[i];
                string currentArgValue;
                if (currentArg.StartsWith("-") || currentArg.StartsWith("/"))
                {
                    currentArg = currentArg.Substring(1);
                }
                else
                {
                    continue;
                }

                if (currentArg.StartsWith("?"))
                {
                    Usage();
                    return false;
                }
                if (currentArg.StartsWith("iter") || currentArg.Equals("i")) // number of iterations
                {
                    currentArgValue = args[++i];
                    countIters = Int32.Parse(currentArgValue);
                }
                if (currentArg.StartsWith("datasize") || currentArg.Equals("dz"))
                {
                    currentArgValue = args[++i];
                    mediumDataSize = Int32.Parse(currentArgValue);
                }

                if (currentArg.StartsWith("sdatasize") || currentArg.Equals("sdz"))
                {
                    currentArgValue = args[++i];
                    shortDataSize = Int32.Parse(currentArgValue);
                }

                if (currentArg.StartsWith("datacount") || currentArg.Equals("dc"))
                {
                    currentArgValue = args[++i];
                    mediumDataCount = Int32.Parse(currentArgValue);
                }

                if (currentArg.StartsWith("sdatacount") || currentArg.Equals("sdc"))
                {
                    currentArgValue = args[++i];
                    shortDataCount = Int32.Parse(currentArgValue);
                }


                if (currentArg.StartsWith("lifetime") || currentArg.Equals("lt"))
                {
                    currentArgValue = args[++i];
                    shortLifeTime = Int32.Parse(currentArgValue);
                    mediumLifeTime = shortLifeTime * 10;
                }

                if (currentArg.StartsWith("threads") || currentArg.Equals("t"))
                {
                    currentArgValue = args[++i];
                    numThreads = Int32.Parse(currentArgValue);
                }
                if (currentArg.StartsWith("fin") || currentArg.Equals("f"))
                {
                    runFinalizer = true;
                }

                if (currentArg.StartsWith("datapinned") || currentArg.StartsWith("dp")) // percentage data pinned
                {
                    currentArgValue = args[++i];
                    percentPinned = float.Parse(currentArgValue);
                }

                if (currentArg.StartsWith("strategy")) //strategy that if the object died or not
                {
                    currentArgValue = args[++i];
                    strategy = currentArgValue;
                }
                
                if (currentArg.StartsWith("notimer"))
                {
                    noTimer = true;
                }
                
                if (currentArg.StartsWith("dataweak") || currentArg.StartsWith("dw") )
                {
                    currentArgValue = args[++i];
                    percentWeak = float.Parse(currentArgValue);
                }
                
                if (currentArg.StartsWith("objectgraph") || currentArg.StartsWith("og") )
                {
                    currentArgValue = args[++i];
                    objectGraph = currentArgValue;
                }
            }

            return true;
        }

        public static void Usage()
        {
            Console.WriteLine("GCSimulator [-?] [-i <num Iterations>] [-dz <data size in bytes>] [-lt <life time>] [-t <num threads>] [-f] [-dp <percent data pinned>]");
            Console.WriteLine("Options");
            Console.WriteLine("-? Display the usage and exit");
            Console.WriteLine("-i [-iter] <num iterations> : specify number of iterations for the test, default is " + countIters);
            Console.WriteLine("-dz [-datasize] <data size> : specify the data size in bytes, default is " + mediumDataSize);
            Console.WriteLine("-sdz [sdatasize] <data size> : specify the short lived  data size in bytes, default is " + shortDataSize);
            Console.WriteLine("-dc [datacount] <data count> : specify the medium lived  data count , default is " + mediumDataCount);
            Console.WriteLine("-sdc [sdatacount] <data count> : specify the short lived  data count, default is " + shortDataCount);
            Console.WriteLine("-lt [-lifetime] <number> : specify the life time of the objects, default is " + shortLifeTime);
            Console.WriteLine("-t [-threads] <number of threads> : specifiy number of threads , default is " + numThreads);
            Console.WriteLine("-f [-fin]  : specify whether to do allocation in finalizer or not, default is no");
            Console.WriteLine("-dp [-datapinned] <percent of data pinned> : specify the percentage of data that we want to pin, default is " + percentPinned);
            Console.WriteLine("-dw [-dataweak] <percent of data weak referenced> : specify the percentage of data that we want to weak reference, default is " + percentWeak);
            Console.WriteLine("-strategy < indicate the strategy for deciding when the objects should die, right now we support only Random and Time strategy, default is Random");
            Console.WriteLine("-og [-objectgraph] <List|Tree> : specify whether to use a List- or Tree-based object graph, default is " + objectGraph);                
            Console.WriteLine("-notimer < indicate that we do not want to run the performance timer and output any results , default is no");                
        }

    }
}
