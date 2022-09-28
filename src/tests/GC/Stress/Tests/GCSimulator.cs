// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// disable warning about unused weakref
#pragma warning disable 414

internal interface PinnedObject
{
    void CleanUp();
    bool IsPinned();
}

namespace GCSimulator
{
    public enum LifeTimeENUM
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
        private class Node
        {
            public Node LeftChild;
            public Node RightChild;
            public int id;
            public T Data;
        }



        private Node _root;
        private int _count;

        public BinaryTreeObjectContainer()
        {
            _root = null;
            _count = 0;
        }

        public void Init(int numberOfObjects)
        {
            if (numberOfObjects <= 0)
            {
                return;
            }

            _root = new Node();
            _root.id = 0;
            // the total number of objects in a binary search tree = (2^n+1) - 1
            // where n is the depth of the tree
            int depth = (int)Math.Log(numberOfObjects, 2);

            _count = numberOfObjects;

            _root.LeftChild = CreateTree(depth, 1);
            _root.RightChild = CreateTree(depth, 2);
        }

        public void AddObjectAt(T o, int index)
        {
            Node node = Find(index, _root);

            if (node != null)
            {
                node.Data = o;
            }
        }


        public T GetObject(int index)
        {
            Node node = Find(index, _root);

            if (node == null)
            {
                return default(T);
            }

            return node.Data;
        }

        public T SetObjectAt(T o, int index)
        {
            Node node = Find(index, _root);

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
                return _count;
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



    //#ArrayContainer Simple Array Stock Implementation for ObjectContainer
    public sealed class ArrayObjectContainer<T> : ObjectContainer<T> where T : LifeTime
    {
        private T[] _objContainer = null;
        public void Init(int numberOfObjects)
        {
            _objContainer = new T[numberOfObjects];
        }

        public void AddObjectAt(T o, int index)
        {
            _objContainer[index] = o;
        }

        public T GetObject(int index)
        {
            return _objContainer[index];
        }

        public T SetObjectAt(T o, int index)
        {
            T old = _objContainer[index];
            _objContainer[index] = o;
            return old;
        }

        public int Count
        {
            get
            {
                return _objContainer.Length;
            }
        }
    }



    public delegate void ObjectDiedEventHandler(LifeTime o, int index);

    public sealed class ObjectLifeTimeManager
    {
        private LifeTimeStrategy _strategy;

        private ObjectContainer<LifeTime> _objectContainer = null;
        //

        public void SetObjectContainer(ObjectContainer<LifeTime> objectContainer)
        {
            _objectContainer = objectContainer;
        }

        public event ObjectDiedEventHandler objectDied;

        public void Init(int numberObjects)
        {
            _objectContainer.Init(numberObjects);
            //objContainer = new object[numberObjects];
        }

        public LifeTimeStrategy LifeTimeStrategy
        {
            set
            {
                _strategy = value;
            }
        }

        public void AddObject(LifeTime o, int index)
        {
            _objectContainer.AddObjectAt(o, index);
            //objContainer[index] = o;
        }

        public void Run()
        {
            LifeTime objLifeTime;

            for (int i = 0; i < _objectContainer.Count; ++i)
            {
                objLifeTime = _objectContainer.GetObject(i);
                //object o = objContainer[i];
                //objLifeTime = o as LifeTime;

                if (_strategy.ShouldDie(objLifeTime, i))
                {
                    int index = _strategy.NextObject(objLifeTime.LifeTime);
                    LifeTime oldObject = _objectContainer.SetObjectAt(null, index);
                    //objContainer[index] = null;
                    // fire the event
                    objectDied(oldObject, index);
                }
            }
        }
    }

    internal class RandomLifeTimeStrategy : LifeTimeStrategy
    {
        private int _counter = 0;
        private int _mediumLifeTime = 30;
        private int _shortLifeTime = 3;
        private int _mediumDataCount = 1000000;
        private int _shortDataCount = 5000;

        private Random _rand = new Random(123456);

        public RandomLifeTimeStrategy(int mediumlt, int shortlt, int mdc, int sdc)
        {
            _mediumLifeTime = mediumlt;
            _shortLifeTime = shortlt;
            _mediumDataCount = mdc;
            _shortDataCount = sdc;
        }
        public int MediumLifeTime
        {
            set
            {
                _mediumLifeTime = value;
            }
        }
        public int ShortLifeTime
        {
            set
            {
                _shortLifeTime = value;
            }
        }

        public int NextObject(LifeTimeENUM lifeTime)
        {
            switch (lifeTime)
            {
                case LifeTimeENUM.Short:
                    return _rand.Next() % _shortDataCount;

                case LifeTimeENUM.Medium:
                    return (_rand.Next() % _mediumDataCount) + _shortDataCount;


                case LifeTimeENUM.Long:
                    return 0;
            }
            return 0;
        }
        public bool ShouldDie(LifeTime o, int index)
        {
            _counter++;
            LifeTimeENUM lifeTime = o.LifeTime;
            switch (lifeTime)
            {
                case LifeTimeENUM.Short:
                    if (_counter % _shortLifeTime == 0)
                        return true;
                    break;
                case LifeTimeENUM.Medium:
                    if (_counter % _mediumLifeTime == 0)
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
    internal class TimeBasedLifeTimeStrategy : LifeTimeStrategy
    {
        private int _lastMediumTickCount = Environment.TickCount;
        private int _lastShortTickCount = Environment.TickCount;
        private int _lastMediumIndex = 0;
        private int _lastShortIndex = 0;

        public int NextObject(LifeTimeENUM lifeTime)
        {
            switch (lifeTime)
            {
                case LifeTimeENUM.Short:
                    return _lastShortIndex;
                case LifeTimeENUM.Medium:
                    return _lastMediumIndex;
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
                    if (Environment.TickCount - _lastShortTickCount > 1) // this is in accureat enumber, since
                    // we will be finsh iterating throuh the short life time object in less than 1 ms , so we need
                    // to switch either to QueryPeroformanceCounter, or to block the loop for some time through
                    // Thread.Sleep, the other solution is to increase the number of objects a lot.
                    {
                        _lastShortTickCount = Environment.TickCount;
                        _lastShortIndex = index;
                        return true;
                    }

                    break;
                case LifeTimeENUM.Medium:
                    if (Environment.TickCount - _lastMediumTickCount > 20)
                    {
                        _lastMediumTickCount = Environment.TickCount;
                        _lastMediumIndex = index;
                        return true;
                    }

                    break;
                case LifeTimeENUM.Long:
                    break;
            }
            return false;
        }
    }

    internal class ObjectWrapper : LifeTime, PinnedObject
    {
        private bool _pinned;
        private bool _weakReferenced;
        private GCHandle _gcHandle;
        private LifeTimeENUM _lifeTime;
        private WeakReference _weakRef;

        private byte[] _data;
        private int _dataSize;
        public int DataSize
        {
            set
            {
                _dataSize = value;
                _data = new byte[_dataSize];
                if (_pinned)
                {
                    _gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                }

                if (_weakReferenced)
                {
                    _weakRef = new WeakReference(_data);
                    _data = null;
                }
            }
        }

        public LifeTimeENUM LifeTime
        {
            get
            {
                return _lifeTime;
            }
            set
            {
                _lifeTime = value;
            }
        }

        public bool IsPinned()
        {
            return _pinned;
        }

        public bool IsWeak()
        {
            return _weakReferenced;
        }


        public void CleanUp()
        {
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }
        }

        public ObjectWrapper(bool runFinalizer, bool pinned, bool weakReferenced)
        {
            _pinned = pinned;
            _weakReferenced = weakReferenced;
            if (!runFinalizer)
            {
                GC.SuppressFinalize(this);
            }
        }


        ~ObjectWrapper()
        {
            // DO SOMETHING UNCONVENTIONAL IN FINALIZER
            _data = new byte[_dataSize];
            CleanUp();
        }
    }

    internal class ClientSimulator
    {
        [ThreadStatic]
        private static ObjectLifeTimeManager s_lifeTimeManager;

        private static int s_meanAllocSize = 17;

        private static int s_mediumLifeTime = 30;
        private static int s_shortLifeTime = 3;

        private static int s_mediumDataSize = s_meanAllocSize;
        private static int s_shortDataSize = s_meanAllocSize;

        private static int s_mediumDataCount = 1000000;
        private static int s_shortDataCount = 5000;

        private static int s_countIters = 500;
        private static float s_percentPinned = 0.1F;
        private static float s_percentWeak = 0.0F;

        private static int s_numThreads = 1;

        private static bool s_runFinalizer = false;
        private static string s_strategy = "Random";

        private static string s_objectGraph = "List";

        private static List<Thread> s_threadList = new List<Thread>();

        private static Stopwatch s_stopWatch = new Stopwatch();
        private static Object s_objLock = new Object();
        private static uint s_currentIterations = 0;

        //keep track of the collection count for generations 0, 1, 2
        private static int[] s_currentCollections = new int[3];
        private static int s_outputFrequency = 0; //after how many iterations the data is printed
        private static System.TimeSpan s_totalTime;

        public static int Main(string[] args)
        {
            s_stopWatch.Start();

            for (int i = 0; i < 3; i++)
            {
                s_currentCollections[i] = 0;
            }

            if (!ParseArgs(args))
                return 101;

            // Run the test.
            for (int i = 0; i < s_numThreads; ++i)
            {
                Thread thread = new Thread(RunTest);
                s_threadList.Add(thread);
                thread.Start();
            }

            foreach (Thread t in s_threadList)
            {
                t.Join();
            }

            return 100;
        }


        public static void RunTest()
        {
            // Allocate the objects.
            s_lifeTimeManager = new ObjectLifeTimeManager();
            LifeTimeStrategy ltStrategy;

            int threadMediumLifeTime = s_mediumLifeTime;
            int threadShortLifeTime = s_shortLifeTime;
            int threadMediumDataSize = s_mediumDataSize;
            int threadShortDataSize = s_shortDataSize;
            int threadMediumDataCount = s_mediumDataCount;
            int threadShortDataCount = s_shortDataCount;
            float threadPercentPinned = s_percentPinned;
            float threadPercentWeak = s_percentWeak;
            bool threadRunFinalizer = s_runFinalizer;
            string threadStrategy = s_strategy;
            string threadObjectGraph = s_objectGraph;

            if (threadObjectGraph.ToLower() == "tree")
            {
                s_lifeTimeManager.SetObjectContainer(new BinaryTreeObjectContainer<LifeTime>());
            }
            else
            {
                s_lifeTimeManager.SetObjectContainer(new ArrayObjectContainer<LifeTime>());
            }

            s_lifeTimeManager.Init(threadShortDataCount + threadMediumDataCount);


            if (threadStrategy.ToLower() == "random")
            {
                ltStrategy = new RandomLifeTimeStrategy(threadMediumLifeTime, threadShortLifeTime, threadMediumDataCount, threadShortDataCount);
            }
            else
            {
                // may be we need to specify the elapsed time.
                ltStrategy = new TimeBasedLifeTimeStrategy();
            }

            s_lifeTimeManager.LifeTimeStrategy = ltStrategy;
            s_lifeTimeManager.objectDied += new ObjectDiedEventHandler(objectDied);

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

                s_lifeTimeManager.AddObject(oWrapper, i);
            }

            lock (s_objLock)
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


            for (int i = 0; i < s_countIters; ++i)
            {
                // Run the test.
                s_lifeTimeManager.Run();

                if (s_outputFrequency > 0)
                {
                    lock (s_objLock)
                    {
                        s_currentIterations++;
                        if (s_currentIterations % s_outputFrequency == 0)
                        {
                            Console.WriteLine("Iterations = {0}", s_currentIterations);
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
                                newCollections[j] = collectionCount[j] - s_currentCollections[j];
                            }

                            //update the running count of collections
                            for (int j = 0; j < 3; j++)
                            {
                                s_currentCollections[j] = collectionCount[j];
                            }

                            Console.WriteLine("Gen 0 Collections = {0}", newCollections[0]);
                            Console.WriteLine("Gen 1 Collections = {0}", newCollections[1]);
                            Console.WriteLine("Gen 2 Collections = {0}", newCollections[2]);

                            s_stopWatch.Stop();
                            Console.Write("Elapsed time: ");
                            System.TimeSpan tSpan = s_stopWatch.Elapsed;
                            if (tSpan.Days > 0)
                                Console.Write("{0} days, ", tSpan.Days);
                            if (tSpan.Hours > 0)
                                Console.Write("{0} hours, ", tSpan.Hours);
                            if (tSpan.Minutes > 0)
                                Console.Write("{0} minutes, ", tSpan.Minutes);
                            Console.Write("{0} seconds, ", tSpan.Seconds);
                            Console.Write("{0} milliseconds", tSpan.Milliseconds);

                            s_totalTime += tSpan;
                            s_stopWatch.Reset();
                            s_stopWatch.Start();

                            Console.Write("  (Total time: ");
                            if (s_totalTime.Days > 0)
                                Console.Write("{0} days, ", s_totalTime.Days);
                            if (s_totalTime.Hours > 0)
                                Console.Write("{0} hours, ", s_totalTime.Hours);
                            if (s_totalTime.Minutes > 0)
                                Console.Write("{0} minutes, ", s_totalTime.Minutes);
                            Console.Write("{0} seconds, ", s_totalTime.Seconds);
                            Console.WriteLine("{0} milliseconds)", s_totalTime.Milliseconds);
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

            oWrapper = new ObjectWrapper(s_runFinalizer, oWrapper.IsPinned(), oWrapper.IsWeak());
            oWrapper.LifeTime = lifeTime.LifeTime;
            oWrapper.DataSize = lifeTime.LifeTime == LifeTimeENUM.Short ? s_shortDataSize : s_mediumDataSize;

            s_lifeTimeManager.AddObject(oWrapper, index);
        }

        /// <summary>
        /// Parse the arguments, no error checking is done yet.
        /// TODO: Add more error checking.
        ///
        ///  Populate variables with defaults, then overwrite them with config settings.  Finally overwrite them with command line parameters
        /// </summary>
        public static bool ParseArgs(string[] args)
        {
            s_countIters = 500;

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
                        s_countIters = Int32.Parse(currentArgValue);
                    }
                    else if (currentArg.StartsWith("datasize") || currentArg.Equals("dz"))
                    {
                        currentArgValue = args[++i];
                        s_mediumDataSize = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("sdatasize") || currentArg.Equals("sdz"))
                    {
                        currentArgValue = args[++i];
                        s_shortDataSize = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("datacount") || currentArg.Equals("dc"))
                    {
                        currentArgValue = args[++i];
                        s_mediumDataCount = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("sdatacount") || currentArg.Equals("sdc"))
                    {
                        currentArgValue = args[++i];
                        s_shortDataCount = Int32.Parse(currentArgValue);
                    }


                    else if (currentArg.StartsWith("lifetime") || currentArg.Equals("lt"))
                    {
                        currentArgValue = args[++i];
                        s_shortLifeTime = Int32.Parse(currentArgValue);
                        s_mediumLifeTime = s_shortLifeTime * 10;
                    }

                    else if (currentArg.StartsWith("threads") || currentArg.Equals("t"))
                    {
                        currentArgValue = args[++i];
                        s_numThreads = Int32.Parse(currentArgValue);
                    }

                    else if (currentArg.StartsWith("fin") || currentArg.Equals("f"))
                    {
                        s_runFinalizer = true;
                    }

                    else if (currentArg.StartsWith("datapinned") || currentArg.StartsWith("dp")) // percentage data pinned
                    {
                        currentArgValue = args[++i];
                        s_percentPinned = float.Parse(currentArgValue, System.Globalization.CultureInfo.InvariantCulture);
                        if (s_percentPinned < 0 || s_percentPinned > 1)
                        {
                            Console.WriteLine("Error! datapinned should be a number from 0 to 1");
                            return false;
                        }
                    }

                    else if (currentArg.StartsWith("strategy")) //strategy that if the object died or not
                    {
                        currentArgValue = args[++i];
                        if ((currentArgValue.ToLower() == "random") || (currentArgValue.ToLower() == "time"))
                            s_strategy = currentArgValue;
                        else
                        {
                            Console.WriteLine("Error! Unexpected argument for strategy: {0}", currentArgValue);
                            return false;
                        }
                    }

                    else if (currentArg.StartsWith("dataweak") || currentArg.StartsWith("dw"))
                    {
                        currentArgValue = args[++i];
                        s_percentWeak = float.Parse(currentArgValue, System.Globalization.CultureInfo.InvariantCulture);
                        if (s_percentWeak < 0 || s_percentWeak > 1)
                        {
                            Console.WriteLine("Error! dataweak should be a number from 0 to 1");
                            return false;
                        }
                    }

                    else if (currentArg.StartsWith("objectgraph") || currentArg.StartsWith("og"))
                    {
                        currentArgValue = args[++i];
                        if ((currentArgValue.ToLower() == "tree") || (currentArgValue.ToLower() == "list"))
                            s_objectGraph = currentArgValue;
                        else
                        {
                            Console.WriteLine("Error! Unexpected argument for objectgraph: {0}", currentArgValue);
                            return false;
                        }
                    }
                    else if (currentArg.Equals("out")) //output frequency
                    {
                        currentArgValue = args[++i];
                        s_outputFrequency = int.Parse(currentArgValue);
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
            Console.WriteLine("-i [-iter] <num iterations> : specify number of iterations for the test, default is " + s_countIters);
            Console.WriteLine("\nThreads:");
            Console.WriteLine("-t [-threads] <number of threads> : specifiy number of threads, default is " + s_numThreads);
            Console.WriteLine("\nData:");
            Console.WriteLine("-dz [-datasize] <data size> : specify the data size in bytes, default is " + s_mediumDataSize);
            Console.WriteLine("-sdz [sdatasize] <data size> : specify the short lived  data size in bytes, default is " + s_shortDataSize);
            Console.WriteLine("-dc [datacount] <data count> : specify the medium lived  data count, default is " + s_mediumDataCount);
            Console.WriteLine("-sdc [sdatacount] <data count> : specify the short lived  data count, default is " + s_shortDataCount);
            Console.WriteLine("-lt [-lifetime] <number> : specify the life time of the objects, default is " + s_shortLifeTime);
            Console.WriteLine("-f [-fin]  : specify whether to do allocation in finalizer or not, default is no");
            Console.WriteLine("-dp [-datapinned] <percent of data pinned> : specify the percentage of data that we want to pin (number from 0 to 1), default is " + s_percentPinned);
            Console.WriteLine("-dw [-dataweak] <percent of data weak referenced> : specify the percentage of data that we want to weak reference, (number from 0 to 1) default is " + s_percentWeak);
            Console.WriteLine("-strategy < indicate the strategy for deciding when the objects should die, right now we support only Random and Time strategy, default is Random");
            Console.WriteLine("-og [-objectgraph] <List|Tree> : specify whether to use a List- or Tree-based object graph, default is " + s_objectGraph);
            Console.WriteLine("-out <iterations> : after how many iterations to output data");
        }
    }
}
