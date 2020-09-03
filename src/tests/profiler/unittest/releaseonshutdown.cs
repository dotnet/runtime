// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Profiler.Tests
{
    class ReleaseOnShutdown
    {
        static readonly Guid ReleaseOnShutdownGuid = new Guid("B8C47A29-9C1D-4EEA-ABA0-8E8B3E3B792E");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WasteTime()
        {
            // Give time for the profiler to detach
            Console.WriteLine("Sleeping...");
            for (int i = 0; i < 100; ++i)
            {
                Thread.Sleep(100);
            }
        }

        public static int RunTest(string[] args)
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

            WasteTime();

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
