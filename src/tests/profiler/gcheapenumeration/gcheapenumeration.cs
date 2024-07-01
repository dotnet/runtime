// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Profiler.Tests
{
    class GCHeapEnumerationTests
    {
        static readonly Guid GCHeapEnumerationProfilerGuid = new Guid("8753F0E1-6D6D-4329-B8E1-334918869C15");

        [DllImport("Profiler")]
        private static extern void EnumerateGCHeapObjects();

        public static int RunTest(String[] args)
        {
            var customGCHeapObject = new CustomGCHeapObject();
            EnumerateGCHeapObjects();

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
                                          testName: "GCHeapEnumeration",
                                          profilerClsid: GCHeapEnumerationProfilerGuid);
        }

        class CustomGCHeapObject
        {
        }
    }
}
