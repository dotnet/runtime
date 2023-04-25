// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Profiler.Tests
{
    class NonGCHeapTests
    {
        static readonly Guid GcAllocateEventsProfilerGuid = new Guid("EF0D191C-3FC7-4311-88AF-E474CBEB2859");

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AllocateNonGcHeapObjects()
        {
            // When this method is invoked, JIT is expected to trigger allocations for these
            // string literals and they're expected to end up in a nongc segment (also known as frozen)
            Consume("string1");
            Consume("string2");
            Consume("string3");
            Consume("string4");
            Consume("string5");
            Consume("string6");

            int gen = GC.GetGeneration("string7");
            if (gen != int.MaxValue)
                throw new Exception("object is expected to be in a non-gc heap for this test to work");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Consume(object o) {}

        public static int RunTest(String[] args) 
        {
            AllocateNonGcHeapObjects();
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
                                          testName: "NonGCHeapAllocate",
                                          profilerClsid: GcAllocateEventsProfilerGuid);
        }
    }
}
