// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Profiler.Tests
{
    class TestClassForWeakHandle
    {
        public static int id;
        public string Tag { get; set; }

        public TestClassForWeakHandle(string tag)
        {
            Tag = $"{id}-{tag}";
        }
    }
    class TestClassForStrongHandle
    {
        public static int id;
        public string Tag = string.Empty;

        public TestClassForStrongHandle(string tag)
        {
            Tag = $"{id}-{tag}";
        }
    }
    class TestClassForPinnedHandle
    {
        public static int id;
        public string Tag = string.Empty;

        public TestClassForPinnedHandle(string tag)
        {
            Tag = $"{id}-{tag}";
        }
    }

    class HandlesTests
    {
        class Objects
        {
            public TestClassForWeakHandle _weak;
            public TestClassForStrongHandle _strong;
            public TestClassForPinnedHandle _pinned;

            // These objects will be allocated AFTER test objects.
            // It is expected that after a compacting GC, they should be copied over
            // previously allocated test objects that are no more referenced by handles.
            public string _afterWeak;
            public string _aferStrong;
            public string _afterPinned;
        }

        static readonly Guid HandlesProfilerGuid = new Guid("A0F96622-522D-4654-AA56-BF421E79B210");


        // The goal of this test is to validate the ICorProfilerInfo13 handle management methods:
        //   CreateHandle (weak, strong, pinned)
        //   DestroyHandle
        //   GetObjectIDFromHandle
        //
        // SCENARIO:
        //   1. Specific managed types instances are created but no reference are kept.
        //   2. The corresponding native HandlesProfiler creates a handle for each.
        //   3. A gen2 GC is triggered
        //   --> HandlesProfiler ensures:
        //       - weak wrapped objects are no more alive
        //       - strong and pinned wrapped objects are still alive
        //   4. A gen2 is triggered.
        //   --> HandlesProfiler destroys strong and pinned handles + wrap the corresponding
        //       instances with a weak reference
        //   5. A gen2 is triggered.
        //   --> HandlesProfiler ensures that no more instances are alive.
        //
        public static void DoWork(object parameter)
        {
            Objects objects = parameter as Objects;

            AllocateInstances(objects);
            Console.WriteLine($"weak = {objects._weak.Tag}");
            objects._weak = null;
            Console.WriteLine($"strong = {objects._strong.Tag}");
            objects._strong = null;
            Console.WriteLine($"weak = {objects._pinned.Tag}");
            objects._pinned = null;

            Console.WriteLine("Collection #1");
            GC.Collect(2);

            Console.WriteLine("Collection #2");
            GC.Collect(2);

            Console.WriteLine("Collection #3");
            GC.Collect(2);
        }

        private static void AllocateInstances(Objects objects)
        {
            Console.WriteLine("Allocating Weak");
            objects._weak = new TestClassForWeakHandle("W");

            Console.WriteLine("Allocating Strong");
            objects._strong = new TestClassForStrongHandle("S");

            Console.WriteLine("Allocating Pinned");
            objects._pinned = new TestClassForPinnedHandle("P");

            objects._afterWeak = "after weak-" + Environment.ProcessId;
            objects._aferStrong = "after strong-" + Environment.ProcessId;
            objects._afterPinned = "after pinned-" + Environment.ProcessId;
        }

        public static int RunTest(String[] args) 
        {
            if(args.Length > 1)
            {
                Console.WriteLine("usage: Handles runtest");
                return 1;
            }

            Console.WriteLine("Handles started. Control-C to exit");

            Thread myThread = new Thread(new ParameterizedThreadStart(DoWork));
            myThread.Name = "TestWorker";

            // Ensure types are loaded by CLR before creating any instance
            TestClassForWeakHandle.id = 1;
            TestClassForStrongHandle.id = 2;
            TestClassForPinnedHandle.id = 3;
            Objects objects = new Objects();

            myThread.Start(objects);
            Console.WriteLine("Worker thread started");

            myThread.Join();
            Console.WriteLine("Test Passed");

            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "Handles",
                                          profilerClsid: HandlesProfilerGuid);
        }
    }
}
