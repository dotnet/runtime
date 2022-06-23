// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

class BasicThreading
{
    public const int Pass = 100;
    public const int Fail = -1;

    internal static int Run()
    {
        SimpleReadWriteThreadStaticTest.Run(42, "SimpleReadWriteThreadStatic");

        ThreadStaticsTestWithTasks.Run();

        if (ThreadTest.Run() != Pass)
            return Fail;

        if (TimerTest.Run() != Pass)
            return Fail;
        
        if (FinalizeTest.Run() != Pass)
            return Fail;

        return Pass;
    }
}

class FinalizeTest
{
    public static bool visited = false;
    public class Dummy
    {
        ~Dummy()
        {
            FinalizeTest.visited = true;
        }
    }

    public static int Run()
    {
        int iterationCount = 0;
        while (!visited && iterationCount++ < 1000000)
        {
            GC.KeepAlive(new Dummy());
            GC.Collect();
        }

        if (visited)
        {
            Console.WriteLine("FinalizeTest passed");
            return BasicThreading.Pass;
        }
        else
        {
            Console.WriteLine("FinalizeTest failed");
            return BasicThreading.Fail;
        }
    }
}

class SimpleReadWriteThreadStaticTest
{
    public static void Run(int intValue, string stringValue)
    {
        NonGenericReadWriteThreadStaticsTest(intValue, "NonGeneric" + stringValue);
        GenericReadWriteThreadStaticsTest(intValue + 1, "Generic" + stringValue);
    }

    class NonGenericType
    {
        [ThreadStatic]
        public static int IntValue;

        [ThreadStatic]
        public static string StringValue;
    }

    class GenericType<T, V>
    {
        [ThreadStatic]
        public static T ValueT;

        [ThreadStatic]
        public static V ValueV;
    }

    static void NonGenericReadWriteThreadStaticsTest(int intValue, string stringValue)
    {
        NonGenericType.IntValue = intValue;
        NonGenericType.StringValue = stringValue;

        if (NonGenericType.IntValue != intValue)
        {
            throw new Exception("SimpleReadWriteThreadStaticsTest: wrong integer value: " + NonGenericType.IntValue.ToString());
        }

        if (NonGenericType.StringValue != stringValue)
        {
            throw new Exception("SimpleReadWriteThreadStaticsTest: wrong string value: " + NonGenericType.StringValue);
        }
    }

    static void GenericReadWriteThreadStaticsTest(int intValue, string stringValue)
    {
        GenericType<int, string>.ValueT = intValue;
        GenericType<int, string>.ValueV = stringValue;

        if (GenericType<int, string>.ValueT != intValue)
        {
            throw new Exception("GenericReadWriteThreadStaticsTest1a: wrong integer value: " + GenericType<int, string>.ValueT.ToString());
        }

        if (GenericType<int, string>.ValueV != stringValue)
        {
            throw new Exception("GenericReadWriteThreadStaticsTest1b: wrong string value: " + GenericType<int, string>.ValueV);
        }

        intValue++;
        GenericType<int, int>.ValueT = intValue;
        GenericType<int, int>.ValueV = intValue + 1;

        if (GenericType<int, int>.ValueT != intValue)
        {
            throw new Exception("GenericReadWriteThreadStaticsTest2a: wrong integer value: " + GenericType<int, string>.ValueT.ToString());
        }

        if (GenericType<int, int>.ValueV != (intValue + 1))
        {
            throw new Exception("GenericReadWriteThreadStaticsTest2b: wrong integer value: " + GenericType<int, string>.ValueV.ToString());
        }

        GenericType<string, string>.ValueT = stringValue + "a";
        GenericType<string, string>.ValueV = stringValue + "b";

        if (GenericType<string, string>.ValueT != (stringValue + "a"))
        {
            throw new Exception("GenericReadWriteThreadStaticsTest3a: wrong string value: " + GenericType<string, string>.ValueT);
        }

        if (GenericType<string, string>.ValueV != (stringValue + "b"))
        {
            throw new Exception("GenericReadWriteThreadStaticsTest3b: wrong string value: " + GenericType<string, string>.ValueV);
        }
    }
}

class ThreadStaticsTestWithTasks
{
    static object lockObject = new object();
    const int TotalTaskCount = 32;

    public static void Run()
    {
        Task[] tasks = new Task[TotalTaskCount];
        for (int i = 0; i < tasks.Length; ++i)
        {
            tasks[i] = Task.Factory.StartNew((param) =>
            {
                int index = (int)param;
                int intTestValue = index * 10;
                string stringTestValue = "ThreadStaticsTestWithTasks" + index;

                // Try to run the on every other task
                if ((index % 2) == 0)
                {
                    lock (lockObject)
                    {
                        SimpleReadWriteThreadStaticTest.Run(intTestValue, stringTestValue);
                    }
                }
                else
                {
                    SimpleReadWriteThreadStaticTest.Run(intTestValue, stringTestValue);
                }
            }, i);
        }
        for (int i = 0; i < tasks.Length; ++i)
        {
            tasks[i].Wait();
        }
    }
}

class ThreadTest
{
    private static readonly List<Thread> s_startedThreads = new List<Thread>();

    private static int s_passed;
    private static int s_failed;

    private static void Expect(bool condition, string message)
    {
        if (condition)
        {
            Interlocked.Increment(ref s_passed);
        }
        else
        {
            Interlocked.Increment(ref s_failed);
            Console.WriteLine("ERROR: " + message);
        }
    }

    private static void ExpectException<T>(Action action, string message)
    {
        Exception ex = null;
        try
        {
            action();
        }
        catch (Exception e)
        {
            ex = e;
        }

        if (!(ex is T))
        {
            message += string.Format(" (caught {0})", (ex == null) ? "no exception" : ex.GetType().Name);
        }
        Expect(ex is T, message);
    }

    private static void ExpectPassed(string testName, int expectedPassed)
    {
        // Wait for all started threads to finish execution
        foreach (Thread t in s_startedThreads)
        {
            t.Join();
        }

        s_startedThreads.Clear();

        Expect(s_passed == expectedPassed, string.Format("{0}: Expected s_passed == {1}, got {2}", testName, expectedPassed, s_passed));
        s_passed = 0;
    }

    private static void TestStartMethod()
    {
        // Case 1: new Thread(ThreadStart).Start()
        var t1 = new Thread(() => Expect(true, "Expected t1 to start"));
        t1.Start();
        s_startedThreads.Add(t1);

        // Case 2: new Thread(ThreadStart).Start(parameter)
        var t2 = new Thread(() => Expect(false, "This thread must not be started"));
        // InvalidOperationException: The thread was created with a ThreadStart delegate that does not accept a parameter.
        ExpectException<InvalidOperationException>(() => t2.Start(null), "Expected InvalidOperationException for t2.Start()");

        // Case 3: new Thread(ParameterizedThreadStart).Start()
        var t3 = new Thread(obj => Expect(obj == null, "Expected obj == null"));
        t3.Start();
        s_startedThreads.Add(t3);

        // Case 4: new Thread(ParameterizedThreadStart).Start(parameter)
        var t4 = new Thread(obj => Expect((int)obj == 42, "Expected (int)obj == 42"));
        t4.Start(42);
        s_startedThreads.Add(t4);

        // Start an unstarted resurrected thread.
        // CoreCLR: ThreadStateException, NativeAOT: no exception.
        Thread unstartedResurrected = Resurrector.CreateUnstartedResurrected();
        unstartedResurrected.Start();
        s_startedThreads.Add(unstartedResurrected);

        // Threads cannot started more than once
        t1.Join();
        ExpectException<ThreadStateException>(() => t1.Start(), "Expected ThreadStateException for t1.Start()");

        ExpectException<ThreadStateException>(() => Thread.CurrentThread.Start(),
            "Expected ThreadStateException for CurrentThread.Start()");

        Thread stoppedResurrected = Resurrector.CreateStoppedResurrected();
        ExpectException<ThreadStateException>(() => stoppedResurrected.Start(),
            "Expected ThreadStateException for stoppedResurrected.Start()");

        ExpectPassed(nameof(TestStartMethod), 7);
    }

    private static void TestJoinMethod()
    {
        var t = new Thread(() => { });
        ExpectException<InvalidOperationException>(() => t.Start(null), "Expected InvalidOperationException for t.Start()");
        ExpectException<ThreadStateException>(() => t.Join(), "Expected ThreadStateException for t.Join()");

        Thread stoppedResurrected = Resurrector.CreateStoppedResurrected();
        Expect(stoppedResurrected.Join(1), "Expected stoppedResurrected.Join(1) to return true");

        Expect(!Thread.CurrentThread.Join(1), "Expected CurrentThread.Join(1) to return false");

        ExpectPassed(nameof(TestJoinMethod), 4);
    }

    private static void TestCurrentThreadProperty()
    {
        Thread t = null;
        t = new Thread(() => Expect(Thread.CurrentThread == t, "Expected CurrentThread == t on thread t"));
        t.Start();
        s_startedThreads.Add(t);

        Expect(Thread.CurrentThread != t, "Expected CurrentThread != t on main thread");

        ExpectPassed(nameof(TestCurrentThreadProperty), 2);
    }

    private static void TestNameProperty()
    {
        var t = new Thread(() => { });

        t.Name = null;
        // It is OK to set the null Name multiple times
        t.Name = null;
        Expect(t.Name == null, "Expected t.Name == null");

        const string ThreadName = "My thread";
        t.Name = ThreadName;
        Expect(t.Name == ThreadName, string.Format("Expected t.Name == \"{0}\"", ThreadName));

        t.Name = null;
        Expect(t.Name == null, "Expected t.Name == null");

        ExpectPassed(nameof(TestNameProperty), 3);
    }

    private static void TestConcurrentIsBackgroundProperty()
    {
        int spawnedCount = 10000;
        Task[] spawned = new Task[spawnedCount];

        for (int i = 0; i < spawnedCount; i++)
        {
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            var t = new Thread(() => {
                Thread.CurrentThread.IsBackground = !Thread.CurrentThread.IsBackground;
                mres.Wait();
            });
            s_startedThreads.Add(t);
            spawned[i] = Task.Factory.StartNew(() => { t.Start(); });
            Task.Factory.StartNew(() => {
                Expect(true, "Always true");
                for (int i = 0; i < 10000; i++)
                {
                    t.IsBackground = i % 2 == 0;
                }
                mres.Set();
            });
        }
        Task.WaitAll(spawned);
        ExpectPassed(nameof(TestConcurrentIsBackgroundProperty), spawnedCount);
    }

    private static void TestIsBackgroundProperty()
    {
        // Thread created using Thread.Start
        var t_event = new AutoResetEvent(false);
        var t = new Thread(() => t_event.WaitOne());

        t.Start();
        s_startedThreads.Add(t);

        Expect(!t.IsBackground, "Expected t.IsBackground == false");
        t_event.Set();
        t.Join();
        ExpectException<ThreadStateException>(() => Console.WriteLine(t.IsBackground),
            "Expected ThreadStateException for t.IsBackground");

        // Thread pool thread
        Task.Factory.StartNew(() => Expect(Thread.CurrentThread.IsBackground, "Expected IsBackground == true")).Wait();

        // Resurrected threads
        Thread unstartedResurrected = Resurrector.CreateUnstartedResurrected();
        Expect(unstartedResurrected.IsBackground == false, "Expected unstartedResurrected.IsBackground == false");

        Thread stoppedResurrected = Resurrector.CreateStoppedResurrected();
        ExpectException<ThreadStateException>(() => Console.WriteLine(stoppedResurrected.IsBackground),
            "Expected ThreadStateException for stoppedResurrected.IsBackground");

        // Main thread
        Expect(!Thread.CurrentThread.IsBackground, "Expected CurrentThread.IsBackground == false");

        ExpectPassed(nameof(TestIsBackgroundProperty), 6);
    }

    private static void TestIsThreadPoolThreadProperty()
    {
#if false   // The IsThreadPoolThread property is not in the contract version we compile against at present
        var t = new Thread(() => { });

        Expect(!t.IsThreadPoolThread, "Expected t.IsThreadPoolThread == false");
        Task.Factory.StartNew(() => Expect(Thread.CurrentThread.IsThreadPoolThread, "Expected IsThreadPoolThread == true")).Wait();
        Expect(!Thread.CurrentThread.IsThreadPoolThread, "Expected CurrentThread.IsThreadPoolThread == false");

        ExpectPassed(nameof(TestIsThreadPoolThreadProperty), 3);
#endif
    }

    private static void TestManagedThreadIdProperty()
    {
        int t_id = 0;
        var t = new Thread(() => {
            Expect(Thread.CurrentThread.ManagedThreadId == t_id, "Expected CurrentTread.ManagedThreadId == t_id on thread t");
            Expect(Environment.CurrentManagedThreadId == t_id, "Expected Environment.CurrentManagedThreadId == t_id on thread t");
        });

        t_id = t.ManagedThreadId;
        Expect(t_id != 0, "Expected t_id != 0");
        Expect(Thread.CurrentThread.ManagedThreadId != t_id, "Expected CurrentTread.ManagedThreadId != t_id on main thread");
        Expect(Environment.CurrentManagedThreadId != t_id, "Expected Environment.CurrentManagedThreadId != t_id on main thread");

        t.Start();
        s_startedThreads.Add(t);

        // Resurrected threads
        Thread unstartedResurrected = Resurrector.CreateUnstartedResurrected();
        Expect(unstartedResurrected.ManagedThreadId != 0, "Expected unstartedResurrected.ManagedThreadId != 0");

        Thread stoppedResurrected = Resurrector.CreateStoppedResurrected();
        Expect(stoppedResurrected.ManagedThreadId != 0, "Expected stoppedResurrected.ManagedThreadId != 0");

        ExpectPassed(nameof(TestManagedThreadIdProperty), 7);
    }

    private static void TestThreadStateProperty()
    {
        var t_event = new AutoResetEvent(false);
        var t = new Thread(() => t_event.WaitOne());

        Expect(t.ThreadState == ThreadState.Unstarted, "Expected t.ThreadState == ThreadState.Unstarted");
        t.Start();
        s_startedThreads.Add(t);

        Expect(t.ThreadState == ThreadState.Running || t.ThreadState == ThreadState.WaitSleepJoin,
            "Expected t.ThreadState is either ThreadState.Running or ThreadState.WaitSleepJoin");
        t_event.Set();
        t.Join();
        Expect(t.ThreadState == ThreadState.Stopped, "Expected t.ThreadState == ThreadState.Stopped");

        // Resurrected threads
        Thread unstartedResurrected = Resurrector.CreateUnstartedResurrected();
        Expect(unstartedResurrected.ThreadState == ThreadState.Unstarted,
            "Expected unstartedResurrected.ThreadState == ThreadState.Unstarted");

        Thread stoppedResurrected = Resurrector.CreateStoppedResurrected();
        Expect(stoppedResurrected.ThreadState == ThreadState.Stopped,
            "Expected stoppedResurrected.ThreadState == ThreadState.Stopped");

        ExpectPassed(nameof(TestThreadStateProperty), 5);
    }

    private static unsafe void DoStackAlloc(int size)
    {
        byte* buffer = stackalloc byte[size];
        Volatile.Write(ref buffer[0], 0);
    }

    private static void TestMaxStackSize()
    {
#if false   // The constructors with maxStackSize are not in the contract version we compile against at present
        // Allocate a 3 MiB buffer on the 4 MiB stack
        var t = new Thread(() => DoStackAlloc(3 << 20), 4 << 20);
        t.Start();
        s_startedThreads.Add(t);
#endif

        ExpectPassed(nameof(TestMaxStackSize), 0);
    }

    static int s_startedThreadCount = 0;
    private static void TestStartShutdown()
    {
        Thread[] threads = new Thread[2048];

        // Creating a large number of threads
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() => { Interlocked.Increment(ref s_startedThreadCount); });
            threads[i].Start();
        }

        // Wait for all threads to shutdown;
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Join();
        }

        Expect(s_startedThreadCount == threads.Length,
            String.Format("Not all threads completed. Expected: {0}, Actual: {1}", threads.Length, s_startedThreadCount));
        ExpectPassed(nameof(TestStartShutdown), 1);
    }

    public static int Run()
    {
        TestStartMethod();
        TestJoinMethod();

        TestCurrentThreadProperty();
        TestNameProperty();
        TestIsBackgroundProperty();
        TestIsThreadPoolThreadProperty();
        TestManagedThreadIdProperty();
        TestThreadStateProperty();

        TestMaxStackSize();
        TestStartShutdown();
        
        TestConcurrentIsBackgroundProperty();

        return (s_failed == 0) ? BasicThreading.Pass : BasicThreading.Fail;
    }

    /// <summary>
    /// Creates resurrected Thread objects.
    /// </summary>
    class Resurrector
    {
        static Thread s_unstartedResurrected;
        static Thread s_stoppedResurrected;

        bool _unstarted;
        Thread _thread = new Thread(() => { });

        Resurrector(bool unstarted)
        {
            _unstarted = unstarted;
            if (!unstarted)
            {
                _thread.Start();
                _thread.Join();
            }
        }

        ~Resurrector()
        {
            if (_unstarted && (s_unstartedResurrected == null))
            {
                s_unstartedResurrected = _thread;
            }
            else if(!_unstarted && (s_stoppedResurrected == null))
            {
                s_stoppedResurrected = _thread;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CreateInstance(bool unstarted)
        {
            GC.KeepAlive(new Resurrector(unstarted));
        }

        static Thread CreateResurrectedThread(ref Thread trap, bool unstarted)
        {
            trap = null;

            while (trap == null)
            {
                // Call twice to override the address of the first allocation on the stack (for conservative GC)
                CreateInstance(unstarted);
                CreateInstance(unstarted);

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // We would like to get a Thread object with its internal SafeHandle member disposed.
            // The current implementation of SafeHandle postpones disposing until the next garbage
            // collection. For this reason we do a couple more collections.
            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return trap;
        }

        public static Thread CreateUnstartedResurrected()
        {
            return CreateResurrectedThread(ref s_unstartedResurrected, unstarted: true);
        }

        public static Thread CreateStoppedResurrected()
        {
            return CreateResurrectedThread(ref s_stoppedResurrected, unstarted: false);
        }
    }
}

class TimerTest
{
    private static AutoResetEvent s_event;
    private static Timer s_timer;
    private static volatile int s_periodicTimerCount;

    public static int Run()
    {
        s_event = new AutoResetEvent(false);
        s_timer = new Timer(TimerCallback, null, 200, Timeout.Infinite);

        bool timerFired = s_event.WaitOne(TimeSpan.FromSeconds(5));
        if (!timerFired)
        {
            Console.WriteLine("The timer test failed: timer has not fired.");
            return BasicThreading.Fail;
        }

        // Change the timer to a very long value
        s_event.Reset();
        s_timer.Change(3000000, Timeout.Infinite);
        timerFired = s_event.WaitOne(500);
        if (timerFired)
        {
            Console.WriteLine("The timer test failed: timer fired earlier than expected.");
            return BasicThreading.Fail;
        }

        // Try change existing timer to a small value and make sure it fires
        s_event.Reset();
        s_timer.Change(200, Timeout.Infinite);
        timerFired = s_event.WaitOne(TimeSpan.FromSeconds(5));
        if (!timerFired)
        {
            Console.WriteLine("The timer test failed: failed to change the existing timer.");
            return BasicThreading.Fail;
        }

        // Test a periodic timer
        s_periodicTimerCount = 0;
        s_event.Reset();
        s_timer = new Timer(PeriodicTimerCallback, null, 200, 20);
        while (s_periodicTimerCount < 3)
        {
            timerFired = s_event.WaitOne(TimeSpan.FromSeconds(5));
            if (!timerFired)
            {
                Console.WriteLine("The timer test failed: the periodic timer has not fired.");
                return BasicThreading.Fail;
            }
        }

        // Stop the periodic timer
        s_timer.Change(Timeout.Infinite, Timeout.Infinite);

        return BasicThreading.Pass;
    }

    private static void TimerCallback(object state)
    {
        s_event.Set();
    }

    private static void PeriodicTimerCallback(object state)
    {
        Interlocked.Increment(ref s_periodicTimerCount);
        s_event.Set();
    }
}
