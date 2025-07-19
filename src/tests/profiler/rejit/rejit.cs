// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Profiler.Tests
{
    class RejitWithInlining //: ProfilerTest
    {
        static readonly Guid ReJitProfilerGuid = new Guid("66F7A9DF-8858-4A32-9CFF-3AD0787E0186");

        static System.Text.StringBuilder OutputBuilder = new ();

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWriteLine(string s)
        {
            OutputBuilder.AppendLine(s);
            Console.WriteLine(s);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static int MaxInlining()
        {
            // Jit everything normally
            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            TriggerReJIT();

            OutputBuilder.Clear();

            // TriggerInliningChain triggers a ReJIT, now this time we should call
            // in to the ReJITted code
            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            if (!OutputBuilder.ToString().Contains("Hello from profiler rejit method"))
                return 1234;

            TriggerRevert();

            OutputBuilder.Clear();

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            if (OutputBuilder.ToString().Contains("Hello from profiler rejit method"))
                return 1235;

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
            TestWriteLine("TriggerInlining");
            // Test Inlining through another method
            InlineeChain1();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerDirectInlining()
        {
            TestWriteLine("TriggerDirectInlining");
            InlineeTarget();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void CallMethodWithoutInlining()
        {
            TestWriteLine("CallMethodWithoutInlining");
            Action callMethod = InlineeTarget;
            callMethod();
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static void InlineeChain1()
        {
            TestWriteLine("Inline.InlineeChain1");
            InlineeTarget();
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void InlineeTarget()
        {
            TestWriteLine("Inline.InlineeTarget");
        }

        public static int RunTest(string[] args)
        {
            TestWriteLine("maxinlining");
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
            RejitWithInlining.TestWriteLine("TriggerInlining");
            // Test Inlining through another method
            InlineeChain1();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerDirectInlining()
        {
            RejitWithInlining.TestWriteLine("TriggerDirectInlining");
            RejitWithInlining.InlineeTarget();
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static void InlineeChain1()
        {
            RejitWithInlining.TestWriteLine("Inline.InlineeChain1");
            RejitWithInlining.InlineeTarget();
        }
    }
}
