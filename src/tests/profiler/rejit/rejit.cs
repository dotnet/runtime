// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Profiler.Tests
{
    class RejitWithInlining //: ProfilerTest
    {
        static readonly Guid ReJitProfilerGuid = new Guid("66F7A9DF-8858-4A32-9CFF-3AD0787E0186");

        static System.Text.StringBuilder OutputBuilder = new ();
        static bool SuppressOutput;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWriteLine(string s)
        {
            if (SuppressOutput) return;
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

            string matchString = "Hello from profiler rejit method 'InlineeTarget'!";
            int numRejittedTargets = OutputBuilder.ToString().Split(matchString).Length;
            if (numRejittedTargets != 4)
            {
                Console.WriteLine("ReJIT did not update all instances of InlineeTarget!");
                return 1234;
            }

            TriggerRevert();

            OutputBuilder.Clear();

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            if (OutputBuilder.ToString().Contains(matchString))
            {
                Console.WriteLine("ReJIT revert of InlineeTarget was not complete!");
                return 1235;
            }

            // Currently there will still be some 'Hello from profiler rejit method' messages left
            //  in the output in certain configurations. This is because the test profiler does not revert all
            //  the methods that it modified - reverts are not symmetric with rejits.
            // See https://github.com/dotnet/runtime/issues/117823

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
        private static void TriggerReJITFinalTier()
        {
            Console.WriteLine("ReJIT (final tier) should be triggered after this method...");
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerRevertFinalTier()
        {
            Console.WriteLine("Revert (final tier) should be triggered after this method...");
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerVirtualReJIT()
        {
            Console.WriteLine("Virtual ReJIT should be triggered after this method...");
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerVirtualRevert()
        {
            Console.WriteLine("Virtual Revert should be triggered after this method...");
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static int TieredRejit()
        {
            string matchString = "Hello from profiler rejit method 'InlineeTarget'!";

            // Phase 1: Non-final tier (Tier0) - methods are freshly compiled at quick JIT
            Console.WriteLine("=== Phase 1: ReJIT at non-final tier (Tier0) ===");

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            TriggerReJIT();

            OutputBuilder.Clear();

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            if (!OutputBuilder.ToString().Contains(matchString))
            {
                Console.WriteLine("Phase 1 FAILED: ReJIT at Tier0 did not replace function body!");
                return 1;
            }
            Console.WriteLine("Phase 1 PASSED: ReJIT at Tier0 correctly replaced function body.");

            TriggerRevert();

            OutputBuilder.Clear();

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            if (OutputBuilder.ToString().Contains(matchString))
            {
                Console.WriteLine("Phase 1 revert FAILED: original function body not restored!");
                return 2;
            }
            Console.WriteLine("Phase 1 revert PASSED.");

            // Promote methods to Tier1 via hot loop, then wait for background compilation
            Console.WriteLine("=== Promoting methods to Tier1 via hot loop ===");
            SuppressOutput = true;
            for (int i = 0; i < 200; i++)
            {
                TriggerDirectInlining();
                CallMethodWithoutInlining();
                TriggerInliningChain();
            }
            SuppressOutput = false;
            Thread.Sleep(500);

            // Phase 2: Final tier (Tier1) - methods have been promoted
            Console.WriteLine("=== Phase 2: ReJIT at final tier (Tier1) ===");

            TriggerReJITFinalTier();

            OutputBuilder.Clear();

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            if (!OutputBuilder.ToString().Contains(matchString))
            {
                Console.WriteLine("Phase 2 FAILED: ReJIT at Tier1 did not replace function body!");
                return 3;
            }
            Console.WriteLine("Phase 2 PASSED: ReJIT at Tier1 correctly replaced function body.");

            TriggerRevertFinalTier();

            OutputBuilder.Clear();

            TriggerDirectInlining();
            CallMethodWithoutInlining();
            TriggerInliningChain();

            if (OutputBuilder.ToString().Contains(matchString))
            {
                Console.WriteLine("Phase 2 revert FAILED: original function body not restored!");
                return 4;
            }
            Console.WriteLine("Phase 2 revert PASSED.");

            return 100;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static int VirtualRejit()
        {
            string matchString = "Hello from profiler rejit method 'VirtualTarget'!";
            string originalString = "Original VirtualTarget";

            Console.WriteLine("=== Virtual ReJIT at non-final tier (Tier0) ===");

            VirtualBase obj = new VirtualDerived();

            // Call to JIT VirtualDerived.VirtualTarget at Tier0 (non-final tier).
            // For backpatchable (virtual) methods, this redirects the precode target
            // to Tier0 native code without modifying the vtable slot.
            string result = CallVirtualTarget(obj);
            Console.WriteLine($"Before rejit: {result}");

            // Trigger ReJIT on the virtual method.
            // The profiler calls RequestReJIT which activates a new IL version.
            // PublishNativeCodeVersion calls ResetCodeEntryPoint to redirect calls
            // back through the prestub so the new IL can be compiled.
            TriggerVirtualReJIT();

            // Call the virtual method again — the rejitted version should execute.
            result = CallVirtualTarget(obj);
            Console.WriteLine($"After rejit: {result}");

            if (!result.Contains(matchString))
            {
                Console.WriteLine("FAIL: Virtual method ReJIT did not take effect at Tier0!");
                Console.WriteLine($"Expected result to contain: {matchString}");
                Console.WriteLine($"Actual result: {result}");
                return 1;
            }
            Console.WriteLine("Virtual ReJIT PASSED.");

            // Trigger revert to restore the original method body.
            TriggerVirtualRevert();

            // Call the virtual method again — the original version should execute.
            result = CallVirtualTarget(obj);
            Console.WriteLine($"After revert: {result}");

            if (!result.Contains(originalString))
            {
                Console.WriteLine("FAIL: Virtual method revert did not take effect!");
                Console.WriteLine($"Expected result to contain: {originalString}");
                Console.WriteLine($"Actual result: {result}");
                return 2;
            }
            Console.WriteLine("Virtual Revert PASSED.");

            return 100;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static string CallVirtualTarget(VirtualBase obj)
        {
            return obj.VirtualTarget();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void TriggerInliningChain()
        {
            TestWriteLine("TriggerInliningChain");
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
            Action<string> callMethod = InlineeTarget;
            callMethod("CallMethodWithoutInlining");
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static void InlineeChain1()
        {
            TestWriteLine(" Inline.InlineeChain1");
            InlineeTarget();
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void InlineeTarget([CallerMemberName] string callerMemberName = null)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("  Inline.InlineeTarget");
            sb.Append(' ');
            sb.Append('(');
            sb.Append(callerMemberName);
            sb.Append(')');
            TestWriteLine(sb.ToString());
        }

        public static int RunTest(string[] args)
        {
            TestWriteLine("maxinlining");
            return MaxInlining();
        }

        public static int RunTieredTest(string[] args)
        {
            Console.WriteLine("Running tiered rejit test");
            return TieredRejit();
        }

        public static int RunVirtualTest(string[] args)
        {
            Console.WriteLine("Running virtual rejit test");
            return VirtualRejit();
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length > 1 && args[1].Equals("Tiered", StringComparison.OrdinalIgnoreCase))
                    return RunTieredTest(args);
                if (args.Length > 1 && args[1].Equals("Virtual", StringComparison.OrdinalIgnoreCase))
                    return RunVirtualTest(args);
                return RunTest(args);
            }

            // Run the original inlining test (TC disabled)
            int result = ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ReJITWithInlining",
                                          profilerClsid: ReJitProfilerGuid,
                                          profileeOptions: ProfileeOptions.OptimizationSensitive);
            if (result != 100)
                return result;

            // Run the tiered rejit test (TC enabled)
            result = ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ReJITWithTiering",
                                          profilerClsid: ReJitProfilerGuid,
                                          profileeArguments: "Tiered",
                                          envVars: new Dictionary<string, string>
                                          {
                                              { "DOTNET_TieredCompilation", "1" },
                                              { "DOTNET_REJIT_TIERED_MODE", "1" }
                                          });
            if (result != 100)
                return result;

            // Run the virtual rejit test (TC enabled, tests backpatchable virtual method ReJIT at Tier0)
            result = ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ReJITVirtual",
                                          profilerClsid: ReJitProfilerGuid,
                                          profileeArguments: "Virtual",
                                          envVars: new Dictionary<string, string>
                                          {
                                              { "DOTNET_TieredCompilation", "1" },
                                              { "DOTNET_REJIT_VIRTUAL_MODE", "1" }
                                          });

            return result;
        }
    }

    abstract class VirtualBase
    {
        public abstract string VirtualTarget();
    }

    class VirtualDerived : VirtualBase
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public override string VirtualTarget()
        {
            return "Original VirtualTarget";
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
