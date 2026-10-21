// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
            AddLayoutRoots();
            EnumerateGCHeapObjectsWithoutProfilerRequestedRuntimeSuspension();
            return 100;
        }

        public static int EnumerateGCHeapObjectsSingleThreadWithinProfilerRequestedRuntimeSuspension()
        {
            AddLayoutRoots();
            EnumerateGCHeapObjectsWithinProfilerRequestedRuntimeSuspension();
            return 100;
        }

        // Test invoking ProfToEEInterfaceImpl::EnumerateGCHeapObjects during non-profiler requested runtime suspension, e.g. during GC
        // ProfToEEInterfaceImpl::EnumerateGCHeapObjects should be invoked by the GarbageCollectionStarted callback
        public static int EnumerateGCHeapObjectsMultiThreadWithCompetingRuntimeSuspension()
        {
            AddLayoutRoots();
            GC.Collect();
            return 100;
        }

        private static void AddLayoutRoots()
        {
            object first = new();
            object second = new();
            _rootObjects.Add(new CustomGCHeapObject());
            _rootObjects.Add(new SparseObject
            {
                First = first,
                Number = 7,
                Second = second,
            });
            _rootObjects.Add(new DerivedObject { Base = first, Derived = second });
            _rootObjects.Add(new GenericObject<object> { Value = first });
            _rootObjects.Add(new MixedValue { First = first, Number = 42, Second = null });
            _rootObjects.Add(new MixedValue[]
            {
                new() { First = first, Number = 1, Second = null },
                new() { First = null, Number = 2, Second = second },
            });
            _rootObjects.Add(new object?[] { first, null, second });
            _rootObjects.Add(new object?[][]
            {
                new object?[] { first, null },
                new object?[] { second },
            });
            _rootObjects.Add(new object?[2, 3]
            {
                { first, null, second },
                { null, first, null },
            });
            _rootObjects.Add(new object?[22_000]);
            _rootObjects.Add(GC.AllocateArray<object?>(17, pinned: true));
            _rootObjects.Add((object)new MixedValue
            {
                First = first,
                Number = 3,
                Second = second,
            });

            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("P15Collectible"),
                AssemblyBuilderAccess.RunAndCollect);
            TypeBuilder builder = assembly
                .DefineDynamicModule("P15Collectible")
                .DefineType("CollectibleReferenceObject");
            builder.DefineField("Reference", typeof(object), FieldAttributes.Public);
            Type type = builder.CreateType()!;
            object instance = Activator.CreateInstance(type)!;
            type.GetField("Reference")!.SetValue(instance, first);
            _rootObjects.Add(type);
            _rootObjects.Add(instance);
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

        class SparseObject
        {
            public object? First;
            public int Number;
            public object? Second;
        }

        class BaseObject
        {
            public object? Base;
        }

        class DerivedObject : BaseObject
        {
            public object? Derived;
        }

        class GenericObject<T>
        {
            public T? Value;
        }

        struct MixedValue
        {
            public object? First;
            public int Number;
            public object? Second;
        }
    }
}
