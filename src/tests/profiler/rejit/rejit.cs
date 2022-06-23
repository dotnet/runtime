// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Profiler.Tests
{
    class RejitWithInlining //: ProfilerTest
    {
        static readonly Guid ReJitProfilerGuid = new Guid("66F7A9DF-8858-4A32-9CFF-3AD0787E0186");

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static int MaxInlining()
        {
            // Jit everything normally
            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            TriggerReJIT();

            // TriggerInliningChain triggers a ReJIT, now this time we should call
            // in to the ReJITted code
            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            TriggerRevert();

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            return 100;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerReJIT()
        {
            Console.WriteLine("ReJIT should be triggered after this method...");
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerRevert()
        {
            Console.WriteLine("Revert should be triggered after this method...");
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerInliningChain()
        {
            Console.WriteLine("TriggerInlining");
            // Test Inlining through another method
            InlineeChain1();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerDirectInlining()
        {
            Console.WriteLine("TriggerDirectInlining");
            InlineeTarget();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void CallMethodWithoutInlining()
        {
            Console.WriteLine("CallMethodWithoutInlining");
            Action callMethod = InlineeTarget;
            callMethod();
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static void InlineeChain1()
        {
            Console.WriteLine("Inline.InlineeChain1");
            InlineeTarget();
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void InlineeTarget()
        {
            Console.WriteLine("Inline.InlineeTarget");
        }

        public static int RunTest(string[] args)
        {
            Console.WriteLine("maxinlining");
            return MaxInlining();
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ReJITWithInlining",
                                          profilerClsid: ReJitProfilerGuid,
                                          profileeOptions: ProfileeOptions.OptimizationSensitive);
        }
    }

    public class SeparateClassNeverLoaded
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerInliningChain()
        {
            Console.WriteLine("TriggerInlining");
            // Test Inlining through another method
            InlineeChain1();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerDirectInlining()
        {
            Console.WriteLine("TriggerDirectInlining");
            RejitWithInlining.InlineeTarget();
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static void InlineeChain1()
        {
            Console.WriteLine("Inline.InlineeChain1");
            RejitWithInlining.InlineeTarget();
        }
    }
}
