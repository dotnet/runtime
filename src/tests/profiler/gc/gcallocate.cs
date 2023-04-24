// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Profiler.Tests
{
    class GCAllocateTests
    {
        static readonly Guid GcAllocateEventsProfilerGuid = new Guid("55b9554d-6115-45a2-be1e-c80f7fa35369");

        public static int RunTest(String[] args) 
        {
            int[] large = new int[100000];
            int[] pinned = GC.AllocateArray<int>(32, true);

            // don't let the jit to optimize these allocations
            GC.KeepAlive(large);
            GC.KeepAlive(pinned);

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
                                          testName: "GCCallbacksAllocate",
                                          profilerClsid: GcAllocateEventsProfilerGuid);
        }
    }
}
