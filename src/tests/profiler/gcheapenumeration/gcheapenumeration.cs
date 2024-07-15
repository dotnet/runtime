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
        static readonly string ShouldSetMonitorGCEventMaskEnvVar = "Set_Monitor_GC_Event_Mask";
        static readonly Guid GCHeapEnumerationProfilerGuid = new Guid("8753F0E1-6D6D-4329-B8E1-334918869C15");
        static List<object> _rootObjects = new List<object>();

        [DllImport("Profiler")]
        private static extern void EnumerateGCHeapObjectsWithoutProfilerRequestedRuntimeSuspension();

        [DllImport("Profiler")]
        private static extern void EnumerateGCHeapObjectsWithinProfilerRequestedRuntimeSuspension();

        public static int EnumerateGCHeapObjectsSingleThreadNoPriorSuspension()
        {
            _rootObjects.Add(new CustomGCHeapObject());
            EnumerateGCHeapObjectsWithoutProfilerRequestedRuntimeSuspension();
            return 100;
        }

        public static int EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension()
        {
            _rootObjects.Add(new CustomGCHeapObject());
            EnumerateGCHeapObjectsWithinProfilerRequestedRuntimeSuspension();
            return 100;
        }

        // Test invoking ProfToEEInterfaceImpl::EnumerateGCHeapObjects during non-profiler requested runtime suspension, e.g. during GC
        // ProfToEEInterfaceImpl::EnumerateGCHeapObjects should be invoked by the GarbageCollectionStarted callback
        public static int EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension()
        {
            _rootObjects.Add(new CustomGCHeapObject());
            GC.Collect();
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

                    case nameof(EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension):
                        return EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension();

                    case nameof(EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension):
                        return EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension();
                }
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsSingleThreadNoPriorSuspension), false))
            {
                return 101;
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension), false))
            {
                return 102;
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension), true))
            {
                return 103;
            }

            return 100;
        }

        private static bool RunProfilerTest(string testName, bool shouldSetMonitorGCEventMask)
        {
            try
            {
                return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                              testName: "GCHeapEnumeration",
                                              profilerClsid: GCHeapEnumerationProfilerGuid,
                                              profileeArguments: testName,
                                              envVars: new Dictionary<string, string>
                                              {
                                                {ShouldSetMonitorGCEventMaskEnvVar, shouldSetMonitorGCEventMask ? "TRUE" : "FALSE"},
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
    }
}
