// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace EventPipeTests
{
    class ReverseStartup
    {
        static readonly Guid ReverseStartupProfilerGuid = new Guid("9C1A6E14-2DEC-45CE-9061-F31964D8884D");

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest();
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ReverseStartup",
                                          profilerClsid: ReverseStartupProfilerGuid,
                                          profileeOptions: ProfileeOptions.NoStartupAttach);
        }

        public static int RunTest()
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

            _profilerDone = new ManualResetEvent(false);
            Console.WriteLine($"Attaching profiler {profilerPath} to self.");
            ProfilerControlHelpers.AttachProfilerToSelf(ReleaseOnShutdownGuid, profilerPath);

            PassCallbackToProfiler(() => _profilerDone.Set());
            if (!_profilerDone.WaitOne(TimeSpan.FromMinutes(5)))
            {
                Console.WriteLine("Profiler did not set the callback, test will fail.");
            }

            return 100;
        }
    }
}
