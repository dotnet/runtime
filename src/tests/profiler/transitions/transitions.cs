// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Profiler.Tests
{
    unsafe class Transitions
    {
        static readonly string PInvokeExpectedNameEnvVar = "PInvoke_Transition_Expected_Name";
        static readonly string ReversePInvokeExpectedNameEnvVar = "ReversePInvoke_Transition_Expected_Name";
        static readonly Guid TransitionsGuid = new Guid("027AD7BB-578E-4921-B29F-B540363D83EC");

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int InteropDelegate(int i);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int InteropDelegateNonBlittable(bool b);

        private static int DoDelegateReversePInvoke(int i)
        {
            return i;
        }

        private static int DoDelegateReversePInvokeNonBlittable(bool b)
        {
            return b ? 1 : 0;
        }

        public static int BlittablePInvokeToBlittableInteropDelegate()
        {
            InteropDelegate del = DoDelegateReversePInvoke;
            
            DoPInvoke((delegate* unmanaged<int,int>)Marshal.GetFunctionPointerForDelegate(del), 13);
            GC.KeepAlive(del);

            return 100;
        }

        public static int NonBlittablePInvokeToNonBlittableInteropDelegate()
        {
            InteropDelegateNonBlittable del = DoDelegateReversePInvokeNonBlittable;
            
            DoPInvokeNonBlitable((delegate* unmanaged<int,int>)Marshal.GetFunctionPointerForDelegate(del), true);
            GC.KeepAlive(del);

            return 100;
        }

        [UnmanagedCallersOnly]
        private static int DoReversePInvoke(int i)
        {
            return i;
        }

        [DllImport("Profiler")]
        public static extern void DoPInvoke(delegate* unmanaged<int,int> callback, int i);

        [DllImport("Profiler", EntryPoint = nameof(DoPInvoke))]
        public static extern void DoPInvokeNonBlitable(delegate* unmanaged<int,int> callback, bool i);

        public static int BlittablePInvokeToUnmanagedCallersOnly()
        {
            DoPInvoke(&DoReversePInvoke, 13);

            return 100;
        }

        public static int NonBlittablePInvokeToUnmanagedCallersOnly()
        {
            DoPInvokeNonBlitable(&DoReversePInvoke, true);

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
                    case nameof(BlittablePInvokeToBlittableInteropDelegate):
                        return BlittablePInvokeToBlittableInteropDelegate();
                    case nameof(NonBlittablePInvokeToUnmanagedCallersOnly):
                        return NonBlittablePInvokeToUnmanagedCallersOnly();
                    case nameof(NonBlittablePInvokeToNonBlittableInteropDelegate):
                        return NonBlittablePInvokeToNonBlittableInteropDelegate();
                }
            }

            if (!RunProfilerTest(nameof(BlittablePInvokeToUnmanagedCallersOnly), nameof(DoPInvoke), nameof(DoReversePInvoke)))
            {
                return 101;
            }

            if (!RunProfilerTest(nameof(BlittablePInvokeToBlittableInteropDelegate), nameof(DoPInvoke), "Invoke"))
            {
                return 102;
            }

            if (!RunProfilerTest(nameof(NonBlittablePInvokeToUnmanagedCallersOnly), nameof(DoPInvokeNonBlitable), nameof(DoReversePInvoke)))
            {
                return 101;
            }

            if (!RunProfilerTest(nameof(NonBlittablePInvokeToNonBlittableInteropDelegate), nameof(DoPInvokeNonBlitable), "Invoke"))
            {
                return 102;
            }

            return 100;
        }

        private static bool RunProfilerTest(string testName, string pInvokeExpectedName, string reversePInvokeExpectedName)
        {
            try
            {
                return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "Transitions",
                                          profilerClsid: TransitionsGuid,
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
