// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Profiler.Tests
{
    class TestClassForWeakHandle
    {
        public static int id;
    }
    class TestClassForStrongHandle
    {
        public static int id;
    }
    class TestClassForPinnedHandle
    {
        public static int id;
    }

    class HandlesTests
    {
        static readonly Guid HandlesProfilerGuid = new Guid("A0F96622-522D-4654-AA56-BF421E79B210");

        // The goal of this test is to validate the ICorProfilerInfo13 handle management methods:
        //   CreateHandle (weak, strong, pinned)
        //   DestroyHandle
        //   GetObjectIDFromHandle
        //
        // SCENARIO:
        //   1. Specific managed types instances are created but no reference are kept.
        //   2. The corresponding native HandlesProfiler creates a handle for each.
        //   3. A gen0 GC is triggered 
        //   --> HandlesProfiler ensures:
        //       - weak wrapped objects are no more alive
        //       - strong and pinned wrapped objects are still alive
        //   4. A gen1 is triggered.
        //   5. HandlesProfiler destroys strong and pinned handles.
        //   6. A gen2 is triggered.
        //   7. HandlesProfiler ensures that no more instances are alive.
        //
        public static void DoWork() 
        {
            // Ensure types are loaded by CLR before creating any instance
            TestClassForWeakHandle.id = 1;
            TestClassForStrongHandle.id = 2;
            TestClassForPinnedHandle.id = 3;

            AllocateInstances();
            GC.Collect(0);
            GC.Collect(1);
            GC.Collect(2);
        }

        private static void AllocateInstances()
        {
            var w = new TestClassForWeakHandle();
            var s = new TestClassForStrongHandle();
            var p = new TestClassForPinnedHandle();
        }

        public static int RunTest(String[] args) 
        {
            if(args.Length > 1)
            {
                Console.WriteLine("usage: Handles runtest");
                return 1;
            }

            Console.WriteLine("Handles started. Control-C to exit");

            Thread myThread = new Thread(new ThreadStart(DoWork));
            myThread.Name = "TestWorker";
            myThread.Start();
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
