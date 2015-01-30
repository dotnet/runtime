using System.Diagnostics;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// disable warning about unused weakref
#pragma warning disable 414

interface PinnedObject
{
    void CleanUp();
    bool IsPinned();

}

namespace GCSimulator
{    public enum LifeTimeENUM
    {
        Short,
        Medium,
        Long
    }

    public interface LifeTime
    {
        LifeTimeENUM LifeTime
        {
            get;
            set;
        }

    }

    public interface LifeTimeStrategy
    {
        int NextObject(LifeTimeENUM lifeTime);
        bool ShouldDie(LifeTime o, int index);
    }

    /// <summary>
    /// This interfact abstract the object contaienr , allowing us to specify differnt datastructures
    /// implementation.
    /// The only restriction on the ObjectContainer is that the objects contained in it must implement
    /// LifeTime interface.
    /// Right now we have a simple array container as a stock implementation for that. for more information 
    /// see code:#ArrayContainer
    /// </summary>
    /// <param name="o"></param>
    /// <param name="index"></param>

    public interface ObjectContainer<T> where T : LifeTime
    {
        void Init(int numberOfObjects);
        void AddObjectAt(T o, int index);
        T GetObject(int index);
        T SetObjectAt(T o, int index);
        int Count
        {
            get;
        }
    }


    public sealed class BinaryTreeObjectContainer<T> : ObjectContainer<T> where T : LifeTime
    {

        class Node
        {
            public Node LeftChild;
            public Node RightChild;
            public int id;
            public T Data;

        }



        private Node root;
        private int count;

        public BinaryTreeObjectContainer()
        {
            root = null;
            count = 0;
        }

        public void Init(int numberOfObjects)
        {

            if (numberOfObjects <= 0)
            {
                return;
            }

            root = new Node();
            root.id = 0;
            // the total number of objects in a binary search tree = (2^n+1) - 1
            // where n is the depth of the tree 
            int depth = (int)Math.Log(numberOfObjects, 2);

            count = numberOfObjects;

            root.LeftChild = CreateTree(depth, 1);
            root.RightChild = CreateTree(depth, 2);



        }

        public void AddObjectAt(T o, int index)
        {
            Node node = Find(index, root);

            if (node != null)
            {
                node.Data = o;
            }

        }


        public T GetObject(int index)
        {


            Node node = Find(index, root);

            if (node == null)
            {
                return default(T);
            }

            return node.Data;

        }

        public T SetObjectAt(T o, int index)
        {

            Node node = Find(index, root);

            if (node == null)
            {
                return default(T);
            }

            T old = node.Data;
            node.Data = o;
            return old;

        }

        public int Count
        {
            get
            {
                return count;
            }
        }



        private Node CreateTree(int depth, int id)
        {
            if (depth <= 0 || id >= Count)
            {
                return null;
            }


            Node node = new Node();
            node.id = id;

            node.LeftChild = CreateTree(depth - 1, id * 2 + 1);
            node.RightChild = CreateTree(depth - 1, id * 2 + 2);

            return node;
        }

        private Node Find(int id, Node node)
        {

            // we want to implement find and try to avoid creating temp objects..
            // Our Tree is fixed size,  we don;t allow modifying the actual
            // tree by adding or deleting nodes ( that would be more
            // interesting, but would give us inconsistent perf numbers.           

            // Traverse the tree ( slow, but avoids allocation ), we can write
            // another tree that is a BST, or use SortedList<T,T> which uses
            // BST as the implementation

            if (node == null)
                return null;
            if (id == node.id)
                return node;

            Node retNode = null;
            // find in the left child
            retNode = Find(id, node.LeftChild);

            // if not found, try the right child.
            if (retNode == null)
                retNode = Find(id, node.RightChild);

            return retNode;

        }

    }



    //#ArrayContainer Simple Array Stock Implemntation for ObjectContainer
    public sealed class ArrayObjectContainer<T> : ObjectContainer<T> where T : LifeTime
    {
        private T[] objContainer = null;
        public void Init(int numberOfObjects)
        {
            objContainer = new T[numberOfObjects];

        }

        public void AddObjectAt(T o, int index)
        {
            objContainer[index] = o;
        }

        public T GetObject(int index)
        {
            return objContainer[index];
        }

        public T SetObjectAt(T o, int index)
        {
            T old = objContainer[index];
            objContainer[index] = o;
            return old;
        }

        public int Count
        {
            get
            {
                return objContainer.Length;
            }
        }
    }



    public delegate void ObjectDiedEventHandler(LifeTime o, int index);

    public sealed class ObjectLifeTimeManager
    {
        private LifeTimeStrategy strategy;

        private ObjectContainer<LifeTime> objectContainer = null;
        // 

        public void SetObjectContainer(ObjectContainer<LifeTime> objectContainer)
        {
            this.objectContainer = objectContainer;
        }

        public event ObjectDiedEventHandler objectDied;

        public void Init(int numberObjects)
        {
            objectContainer.Init(numberObjects);
            //objContainer = new object[numberObjects];
        }

        public LifeTimeStrategy LifeTimeStrategy
        {
            set
            {
                strategy = value;
            }
        }

        public void AddObject(LifeTime o, int index)
        {
            objectContainer.AddObjectAt(o, index);
            //objContainer[index] = o;
        }

        public void Run()
        {
            LifeTime objLifeTime;

            for (int i = 0; i < objectContainer.Count; ++i)
            {
                objLifeTime = objectContainer.GetObject(i);
                //object o = objContainer[i];
                //objLifeTime = o as LifeTime;

                if (strategy.ShouldDie(objLifeTime, i))
                {
                    int index = strategy.NextObject(objLifeTime.LifeTime);
                    LifeTime oldObject = objectContainer.SetObjectAt(null, index);
                    //objContainer[index] = null;
                    // fire the event 
                    objectDied(oldObject, index);
                }

            }
        }
    }

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
    /// elabsed since the last object acceess.
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
                    data = null;
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
            if (gcHandle.IsAllocated)
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
            // DO SOMETHING STUPID IN FINALIZER
            data = new byte[dataSize];
            CleanUp();
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
        private static float percentPinned = 0.1F;
        private static float percentWeak = 0.0F;

        private static int numThreads = 1;

        private static bool runFinalizer = false;
        private static string strategy = "Random";

        private static string objectGraph = "List";

        private static List<Thread> threadList = new List<Thread>();

        private static Stopwatch stopWatch = new Stopwatch();
        private static Object objLock = new Object();
        private static uint currentIterations = 0;

        //keep track of the collection count for generations 0, 1, 2
        private static int[] currentCollections = new int[3];
        private static int outputFrequency = 0; //after how many iterations the data is printed 
        private static System.TimeSpan totalTime;

        public static int Main(string[] args)
        {
            stopWatch.Start();

            for (int i = 0; i < 3; i++)
            {
                currentCollections[i] = 0;
            }

            if (!ParseArgs(args))
                return 101;

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


        public static void RunTest()
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


            if (threadStrategy.ToLower() == "random")
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

            for (int i = 0; i < threadShortDataCount + threadMediumDataCount; ++i)
            {
                bool pinned = false;
                if (threadPercentPinned != 0)
                {
                    pinned = (i % ((int)(1 / threadPercentPinned)) == 0);
                }

                bool weak = false;
                if (threadPercentWeak != 0)
                {
                    weak = (i % ((int)(1 / threadPercentWeak)) == 0);
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

            lock (objLock)
            {
                Console.WriteLine("Thread {0} Running With Configuration: ", System.Threading.Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("==============================");
                Console.WriteLine("[Thread] Medium Lifetime " + threadMediumLifeTime);
                Console.WriteLine("[Thread] Short Lifetime " + threadShortLifeTime);
                Console.WriteLine("[Thread] Medium Data Size " + threadMediumDataSize);
                Console.WriteLine("[Thread] Short Data Size " + threadShortDataSize);
                Console.WriteLine("[Thread] Medium Data Count " + threadMediumDataCount);
                Console.WriteLine("[Thread] Short Data Count " + threadShortDataCount);
                Console.WriteLine("[Thread] % Pinned " + threadPercentPinned);
                Console.WriteLine("[Thread] % Weak " + threadPercentWeak);
                Console.WriteLine("[Thread] RunFinalizers " + threadRunFinalizer);
                Console.WriteLine("[Thread] Strategy " + threadStrategy);
                Console.WriteLine("[Thread] Object Graph " + threadObjectGraph);
                Console.WriteLine("==============================");
            }


            for (int i = 0; i < countIters; ++i)
            {

                // Run the test.
                lifeTimeManager.Run();

                if (outputFrequency > 0)
                {
                    lock (objLock)
                    {
                        currentIterations++;
                        if (currentIterations % outputFrequency == 0)
                        {
                            Console.WriteLine("Iterations = {0}", currentIterations);
                            Console.WriteLine("AllocatedMemory = {0} bytes", GC.GetTotalMemory(false));

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

                    }
                }
            }

        }

        private static void objectDied(LifeTime lifeTime, int index)
        {
            // put a new fresh object instead;

            ObjectWrapper oWrapper = lifeTime as ObjectWrapper;
            oWrapper.CleanUp();

            oWrapper = new ObjectWrapper(runFinalizer, oWrapper.IsPinned(), oWrapper.IsWeak());
            oWrapper.LifeTime = lifeTime.LifeTime;
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
            countIters = 500;

            try
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
                        Console.WriteLine("Error! Unexpected argument {0}", currentArg);
                        return false;
                    }

                    if (currentArg.StartsWith("?"))
                    {
                        Usage();
                        System.Environment.FailFast("displayed help");
                    }
                    else if (currentArg.StartsWith("iter") || currentArg.Equals("i")) // number of iterations
                    {
                        currentArgValue = args[++i];
                        countIters = Int32.Parse(currentArgValue);
                    }
                    else if (currentArg.StartsWith("datasize") || currentArg.Equals("dz"))
                    {
                        currentArgValue = args[++i];
                        mediumDataSize = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("sdatasize") || currentArg.Equals("sdz"))
                    {
                        currentArgValue = args[++i];
                        shortDataSize = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("datacount") || currentArg.Equals("dc"))
                    {
                        currentArgValue = args[++i];
                        mediumDataCount = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("sdatacount") || currentArg.Equals("sdc"))
                    {
                        currentArgValue = args[++i];
                        shortDataCount = Int32.Parse(currentArgValue);
                    }


                    else if (currentArg.StartsWith("lifetime") || currentArg.Equals("lt"))
                    {
                        currentArgValue = args[++i];
                        shortLifeTime = Int32.Parse(currentArgValue);
                        mediumLifeTime = shortLifeTime * 10;
                    }

                    else if (currentArg.StartsWith("threads") || currentArg.Equals("t"))
                    {
                        currentArgValue = args[++i];
                        numThreads = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("fin") || currentArg.Equals("f"))
                    {
                        runFinalizer = true;
                    }

                    else if (currentArg.StartsWith("datapinned") || currentArg.StartsWith("dp")) // percentage data pinned
                    {
                        currentArgValue = args[++i];
                        percentPinned = float.Parse(currentArgValue);
                        if (percentPinned < 0 || percentPinned > 1)
                        {
                            Console.WriteLine("Error! datapinned should be a number from 0 to 1");
                            return false;
                        }
                    }

                    else if (currentArg.StartsWith("strategy")) //strategy that if the object died or not
                    {
                        currentArgValue = args[++i];
                        if ((currentArgValue.ToLower() == "random") || (currentArgValue.ToLower() == "time"))
                            strategy = currentArgValue;
                        else
                        {
                            Console.WriteLine("Error! Unexpected argument for strategy: {0}", currentArgValue);
                            return false;
                        }
                    }

                    else if (currentArg.StartsWith("dataweak") || currentArg.StartsWith("dw"))
                    {
                        currentArgValue = args[++i];
                        percentWeak = float.Parse(currentArgValue);
                        if (percentWeak < 0 || percentWeak > 1)
                        {
                            Console.WriteLine("Error! dataweak should be a number from 0 to 1");
                            return false;
                        }
                    }

                    else if (currentArg.StartsWith("objectgraph") || currentArg.StartsWith("og"))
                    {
                        currentArgValue = args[++i];
                        if ((currentArgValue.ToLower() == "tree") || (currentArgValue.ToLower() == "list"))
                            objectGraph = currentArgValue;
                        else
                        {
                            Console.WriteLine("Error! Unexpected argument for objectgraph: {0}", currentArgValue);
                            return false;
                        }
                    }
                    else if (currentArg.Equals("out")) //output frequency
                    {
                        currentArgValue = args[++i];
                        outputFrequency = int.Parse(currentArgValue);
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
            return true;
        }


        public static void Usage()
        {
            Console.WriteLine("GCSimulator [-?] [options]");
            Console.WriteLine("\nOptions");
            Console.WriteLine("\nGlobal:");
            Console.WriteLine("-? Display the usage and exit");
            Console.WriteLine("-i [-iter] <num iterations> : specify number of iterations for the test, default is " + countIters);
            Console.WriteLine("\nThreads:");
            Console.WriteLine("-t [-threads] <number of threads> : specifiy number of threads, default is " + numThreads);
            Console.WriteLine("\nData:");
            Console.WriteLine("-dz [-datasize] <data size> : specify the data size in bytes, default is " + mediumDataSize);
            Console.WriteLine("-sdz [sdatasize] <data size> : specify the short lived  data size in bytes, default is " + shortDataSize);
            Console.WriteLine("-dc [datacount] <data count> : specify the medium lived  data count, default is " + mediumDataCount);
            Console.WriteLine("-sdc [sdatacount] <data count> : specify the short lived  data count, default is " + shortDataCount);
            Console.WriteLine("-lt [-lifetime] <number> : specify the life time of the objects, default is " + shortLifeTime);
            Console.WriteLine("-f [-fin]  : specify whether to do allocation in finalizer or not, default is no");
            Console.WriteLine("-dp [-datapinned] <percent of data pinned> : specify the percentage of data that we want to pin (number from 0 to 1), default is " + percentPinned);
            Console.WriteLine("-dw [-dataweak] <percent of data weak referenced> : specify the percentage of data that we want to weak reference, (number from 0 to 1) default is " + percentWeak);
            Console.WriteLine("-strategy < indicate the strategy for deciding when the objects should die, right now we support only Random and Time strategy, default is Random");
            Console.WriteLine("-og [-objectgraph] <List|Tree> : specify whether to use a List- or Tree-based object graph, default is " + objectGraph);
            Console.WriteLine("-out <iterations> : after how many iterations to output data");
        }

    }
}
