// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Tracing.Tests.Common;

namespace Profiler.Tests
{
    class DynamicOptimization
    {
        static readonly Guid DynamicOptimizationProfilerGuid = new Guid("C26D02FE-9E4C-484E-8984-F86724AA98B5");

        public static int RunTest(String[] args)
        {
            // This test validates that:
            // - Switching COR_PRF_DISABLE_OPTIMIZATIONS can be done dynamically (by calling SwitchJitOptimization) that makes the profiler update the event mask
            // - Modules loaded while COR_PRF_DISABLE_OPTIMIZATIONS is 0 are Jitted with optimizations even if it set to 1 later
            // - Modules loaded while COR_PRF_DISABLE_OPTIMIZATIONS is 1 are Jitted without optimization even if it is set to 0 later

            // to do so, we load the same assembly 3 time in different alc before / after enabling / disalbling COR_PRF_DISABLE_OPTIMIZATIONS.
            // then we explicitly JIT a method in each of the loaded modules and use event traces to check that the method is loaded with an expected Optimization Tier

            // Then it make similar tests for inlining (we get inline counts from the profiler callbacks this time)

            // Load assembly in different states of COR_PRF_DISABLE_OPTIMIZATIONS
            string currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testAssemblyFullPath = Path.Combine(currentAssemblyDirectory, "..", "DynamicOptimizationTestLib", "DynamicOptimizationTestLib.dll");
            var beforeDisableOptimizations = new AssemblyLoadContext("before disable", true).LoadFromAssemblyPath(testAssemblyFullPath);
            SwitchJitOptimization(true);
            var afterDisableOptimizations = new AssemblyLoadContext("after disable", true).LoadFromAssemblyPath(testAssemblyFullPath);
            SwitchJitOptimization(false);
            var afterReenableOptimizations = new AssemblyLoadContext("after reenable", true).LoadFromAssemblyPath(testAssemblyFullPath);

            // JIT and check each case
            Console.WriteLine("Trigger JIT and check that module loaded before disabling optimizations is not jitted with MinOpts");
            var r = CompileTestMethodAndCheckOptimizationTier(beforeDisableOptimizations, false);
            if (r != 100)
            {
                return r;
            }
            Console.WriteLine("Trigger JIT and check that module loaded after disabling optimizations is jitted with MinOpts");
            r = CompileTestMethodAndCheckOptimizationTier(afterDisableOptimizations, true);
            if (r != 100)
            {
                return r;
            }
            Console.WriteLine("Trigger JIT and check that module loaded after re-enabling optimizations is not jitted with MinOpts");
            r = CompileTestMethodAndCheckOptimizationTier(afterReenableOptimizations, false);
            if (r != 100)
            {
                return r;
            }

            // now we do a similar test for inlining
            Console.WriteLine("Testing disabling inlining");

            var beforeDisableInlining = new AssemblyLoadContext("before disable inlining", true).LoadFromAssemblyPath(testAssemblyFullPath);
            SwitchInlining(true);
            var afterDisableInlining = new AssemblyLoadContext("after disable inlining", true).LoadFromAssemblyPath(testAssemblyFullPath);
            SwitchInlining(false);
            var afterReenableInlining = new AssemblyLoadContext("after reenable inlining", true).LoadFromAssemblyPath(testAssemblyFullPath);

            var initialInlineCount = GetInlineCount();
            Console.WriteLine($"Before starting inlining tests, inline count is: {initialInlineCount}");
            // first case: inlining count should have increased
            RuntimeHelpers.PrepareMethod(GetMainMethod(beforeDisableInlining).MethodHandle);
            var actual = GetInlineCount();
            Console.WriteLine($"After jitting first case, inline count is: {actual}");
            var expected = initialInlineCount + 1;
            if (expected != actual)
            {
                throw new Exception($"Expected {expected}, got {actual}");
            }
            // first case: inlining count should not have increased
            RuntimeHelpers.PrepareMethod(GetMainMethod(afterDisableInlining).MethodHandle);
            actual = GetInlineCount();
            Console.WriteLine($"After jitting second case, inline count is: {actual}");
            if (expected != actual)
            {
                throw new Exception($"Expected {expected}, got {GetInlineCount()}");
            }
            // third case: should have inlined
            RuntimeHelpers.PrepareMethod(GetMainMethod(afterReenableInlining).MethodHandle);
            actual = GetInlineCount();
            Console.WriteLine($"After jitting third case, inline count is: {actual}");
            expected++;
            if (expected != actual)
            {
                throw new Exception($"Expected {expected}, got {GetInlineCount()}");
            }

            Console.WriteLine("PROFILER TEST PASSES");
            return 100;
        }

        static EventPipeProvider _jitEventProvider = new EventPipeProvider("Microsoft-Windows-DotNETRuntime", eventLevel: EventLevel.Verbose, keywords: (long)ClrTraceEventParser.Keywords.Jit);

        static MethodInfo GetMainMethod(Assembly assembly) => assembly.GetType("Profiler.Tests.DynamicOptimizationTestLib").GetMethod("Main");
        static int CompileTestMethodAndCheckOptimizationTier(Assembly assembly, bool optimizationsDisabled)
        {
            var method = GetMainMethod(assembly);

            return
            IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>(),
                () => RuntimeHelpers.PrepareMethod(method.MethodHandle),
                new List<EventPipeProvider> { _jitEventProvider },
                1024,
                optimizationsDisabled ? ValidateUnoptimized : ValidateOptimized);
        }

        private static Func<int> ValidateUnoptimized(EventPipeEventSource source)
        {
            OptimizationTier lastTier = OptimizationTier.Unknown;

            source.Clr.MethodLoadVerbose += e =>
            {
                if (e.MethodName == "Main")
                {
                    lastTier = e.OptimizationTier;
                    Console.WriteLine($"MethodLoadVerbose: {e}");
                }
            };
            return () =>
            {
                if (lastTier != OptimizationTier.MinOptJitted && lastTier != OptimizationTier.Unknown)
                {
                    Console.WriteLine($"Expected MinOptJitted, got {lastTier}");
                    return -1;
                }
                return 100;
            };
        }
        private static Func<int> ValidateOptimized(EventPipeEventSource source)
        {
            OptimizationTier lastTier = OptimizationTier.Unknown;

            source.Clr.MethodLoadVerbose += e =>
            {
                if (e.MethodName == "Main")
                {
                    lastTier = e.OptimizationTier;
                    Console.WriteLine($"MethodLoadVerbose: {e}");
                }
            };

            return () =>
            {
                if (lastTier == OptimizationTier.MinOptJitted)
                {
                    Console.WriteLine($"Expected not MinOptJitted, got {lastTier}");
                    return -1;
                }
                return 100;
            };
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "DynamicOptimization",
                                          profilerClsid: DynamicOptimizationProfilerGuid);
        }

        // this makes the profiler enable / disable COR_PRF_DISABLE_OPTIMIZATIONS dynamically
        [DllImport("Profiler")]
        public static extern int SwitchJitOptimization(bool disable);

        // this makes the profiler enable / disable COR_PRF_DISABLE_INLINING dynamically
        [DllImport("Profiler")]
        public static extern int SwitchInlining(bool disable);

        // this retrieves the number of inlining operations that occured
        [DllImport("Profiler")]
        public static extern int GetInlineCount();
    }
}
