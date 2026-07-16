// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Profiler.Tests
{
    // Regression test for https://github.com/dotnet/runtime/issues/130242.
    //
    // Runs a forward P/Invoke (into the profiler's native DoPInvoke) which calls back into a
    // managed [UnmanagedCallersOnly] target. The profiler enables both ELT and code-transition
    // monitoring and asserts that the forward P/Invoke is reported only via transition callbacks
    // (never ELT), while the reverse P/Invoke target still receives ELT.
    unsafe class EltTransitions
    {
        static readonly string PInvokeExpectedNameEnvVar = "PInvoke_Transition_Expected_Name";
        static readonly string ReversePInvokeExpectedNameEnvVar = "ReversePInvoke_Transition_Expected_Name";
        static readonly Guid EltTransitionsGuid = new Guid("C7A0B5D1-9E3F-4A21-8B77-1C2D3E4F5061");

        [DllImport("Profiler")]
        public static extern void DoPInvoke(delegate* unmanaged<int, int> callback, int i);

        [UnmanagedCallersOnly]
        private static int DoReversePInvoke(int i)
        {
            return i;
        }

        public static int BlittablePInvokeToUnmanagedCallersOnly()
        {
            DoPInvoke(&DoReversePInvoke, 13);

            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 1 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                switch (args[1])
                {
                    case nameof(BlittablePInvokeToUnmanagedCallersOnly):
                        return BlittablePInvokeToUnmanagedCallersOnly();
                }
            }

            if (!RunProfilerTest(nameof(BlittablePInvokeToUnmanagedCallersOnly), nameof(DoPInvoke), nameof(DoReversePInvoke)))
            {
                return 101;
            }

            return 100;
        }

        private static bool RunProfilerTest(string testName, string pInvokeExpectedName, string reversePInvokeExpectedName)
        {
            try
            {
                return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "EltTransitions",
                                          profilerClsid: EltTransitionsGuid,
                                          profileeArguments: testName,
                                          envVars: new Dictionary<string, string>
                                          {
                                              { PInvokeExpectedNameEnvVar, pInvokeExpectedName },
                                              { ReversePInvokeExpectedNameEnvVar, reversePInvokeExpectedName },
                                          }) == 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }
    }
}
