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

        [DllImport("Profiler")]
        private static extern void EnumerateGCHeapObjects();

        [DllImport("Profiler")]
        private static extern void SuspendRuntime();

        [DllImport("Profiler")]
        private static extern void ResumeRuntime();

        [DllImport("Profiler")]
        private static extern void EnumerateHeapObjectsInBackgroundThread();

        public static int EnumerateGCHeapObjectsSingleThreadNoPriorSuspension()
        {
            var _ = new CustomGCHeapObject();
            EnumerateGCHeapObjects();
            return 100;
        }

        public static int EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension()
        {
            var _ = new CustomGCHeapObject();
            SuspendRuntime();
            EnumerateGCHeapObjects();
            ResumeRuntime();
            return 100;
        }

        public static int EnumerateGCHeapObjectsInBackgroundThreadWithRuntimeSuspension()
        {
            EnumerateHeapObjectsInBackgroundThread();
            GC.Collect();
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
                Thread.Sleep(1000);
                var _ = new CustomGCHeapObject();
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

                    case nameof(EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension):
                        return EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension();

                    case nameof(EnumerateGCHeapObjectsInBackgroundThreadWithRuntimeSuspension):
                        return EnumerateGCHeapObjectsInBackgroundThreadWithRuntimeSuspension();

                    case nameof(EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension):
                        return EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension();
                }
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsSingleThreadNoPriorSuspension), "FALSE"))
            {
                return 101;
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension), "FALSE"))
            {
                return 102;
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsInBackgroundThreadWithRuntimeSuspension), "TRUE"))
            {
                return 103;
            }

            if (!RunProfilerTest(nameof(EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension), "FALSE"))
            {
                return 104;
            }

            return 100;
        }

        private static bool RunProfilerTest(string testName, string shouldSetMonitorGCEventMask)
        {
            try
            {
                return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                              testName: "GCHeapEnumeration",
                                              profilerClsid: GCHeapEnumerationProfilerGuid,
                                              profileeArguments: testName,
                                              envVars: new Dictionary<string, string>
                                              {
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
    }
}
