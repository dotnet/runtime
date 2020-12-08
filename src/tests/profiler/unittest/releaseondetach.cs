// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Profiler.Tests
{
    public delegate void ProfilerCallback();

    class ReleaseOnShutdown
    {
        static readonly Guid ReleaseOnShutdownGuid = new Guid("B8C47A29-9C1D-4EEA-ABA0-8E8B3E3B792E");

        static volatile bool _profilerDone = false;

        [DllImport("Profiler")]
        private static extern void PassBoolToProfiler(IntPtr boolPtr);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WasteTime()
        {
            // Give time for the profiler to detach
            Console.WriteLine("Waiting for profiler to detach...");
            bool profilerSetFlag = false;
            for (int i = 0; i < 100_000; ++i)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
                if (_profilerDone)
                {
                    profilerSetFlag = true;
                    break;
                }
            }

            if (!profilerSetFlag)
            {
                Console.WriteLine("Warning: test will fail because the profiler never had its destructor called.");
            }
        }

        public unsafe static int RunTest(string[] args)
        {
            string profilerName;
            if (TestLibrary.Utilities.IsWindows)
            {
                profilerName = "Profiler.dll";
            }
            else if (TestLibrary.Utilities.IsLinux)
            {
                profilerName = "libProfiler.so";
            }
            else
            {
                profilerName = "libProfiler.dylib";
            }

            string rootPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string profilerPath = Path.Combine(rootPath, profilerName);

            Console.WriteLine($"Attaching profiler {profilerPath} to self.");
            ProfilerControlHelpers.AttachProfilerToSelf(ReleaseOnShutdownGuid, profilerPath);

            // This warning is that the pointer to the volatile bool won't be treated as volatile,
            // but that's ok. The loop aboive in WastTime is what needs to read it as volatile.
            // The native part just sets it.
            #pragma warning disable CS0420
            fixed (bool *boolPtr = &_profilerDone)
            {
                PassBoolToProfiler(new IntPtr(boolPtr));

                WasteTime();
            }

            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "UnitTestReleaseOnShutdown",
                                          profilerClsid: ReleaseOnShutdownGuid,
                                          profileeOptions: ProfileeOptions.NoStartupAttach);
        }
    }
}
