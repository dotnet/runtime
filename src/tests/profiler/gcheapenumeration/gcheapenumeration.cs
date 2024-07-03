// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Profiler.Tests
{
    class GCHeapEnumerationTests
    {
        static readonly string ShouldSetObjectAllocatedEventMaskEnvVar = "Set_Object_Allocated_Event_Mask";
        static readonly string ShouldSetMonitorGCEventMaskEnvVar = "Set_Monitor_GC_Event_Mask";
        static readonly Guid GCHeapEnumerationProfilerGuid = new Guid("8753F0E1-6D6D-4329-B8E1-334918869C15");

        [DllImport("Profiler")]
        private static extern void EnumerateGCHeapObjects();

        public static int EnumerateGCHeapObjectsSingleThreadNoPriorSuspension()
        {
            var customGCHeapObject = new CustomGCHeapObject();
            EnumerateGCHeapObjects();
            return 100;
        }

        public static int EnumerateGCHeapObjectsSingleThreadWithPriorSuspension()
        {
            var customObjectAllocatedToSuspendRuntime = new CustomObjectAllocatedToSuspendRuntime();
            return 100;
        }

        public static int EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension()
        {
            ManualResetEvent startEvent = new ManualResetEvent(false);
            Thread gcThread = new Thread(() =>
            {
                startEvent.WaitOne();
                GC.Collect();
            });
            gcThread.Name = "GC.Collect";
            gcThread.Start();

            Thread enumerateThread = new Thread(() =>
            {
                startEvent.WaitOne();
                Thread.Sleep(5000);
                var customGCHeapObject = new CustomGCHeapObject();
                EnumerateGCHeapObjects();
            });
            enumerateThread.Name = "EnumerateGCHeapObjects";
            enumerateThread.Start();

            startEvent.Set();
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                switch (args[1])
                {
                    case nameof(EnumerateGCHeapObjectsSingleThreadNoPriorSuspension):
                        return EnumerateGCHeapObjectsSingleThreadNoPriorSuspension();

                    case nameof(EnumerateGCHeapObjectsSingleThreadWithPriorSuspension):
                        return EnumerateGCHeapObjectsSingleThreadWithPriorSuspension();

                    case nameof(EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension):
                        return EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension();
                }
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsSingleThreadNoPriorSuspension), "FALSE", "FALSE"))
            {
                return 101;
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsSingleThreadWithPriorSuspension), "TRUE", "FALSE"))
            {
                return 102;
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension), "FALSE", "TRUE"))
            {
                return 103;
            }

            return 100;
        }

        private static bool RunProfilerTest(string testName, string shouldSetObjectAllocatedEventMask, string shouldSetMonitorGCEventMask)
        {
            try
            {
                return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                              testName: "GCHeapEnumeration",
                                              profilerClsid: GCHeapEnumerationProfilerGuid,
                                              profileeArguments: testName,
                                              envVars: new Dictionary<string, string>
                                              {
                                                {ShouldSetObjectAllocatedEventMaskEnvVar, shouldSetObjectAllocatedEventMask},
                                                {ShouldSetMonitorGCEventMaskEnvVar, shouldSetMonitorGCEventMask},
                                              }
                                              ) == 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        class CustomGCHeapObject {}
        class CustomObjectAllocatedToSuspendRuntime {}
    }
}
