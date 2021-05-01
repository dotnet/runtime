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

namespace ReverseStartupTests
{
    public delegate void ProfilerCallback();

    class ReverseStartup
    {
        static readonly Guid ReverseStartupProfilerGuid = new Guid("9C1A6E14-2DEC-45CE-9061-F31964D8884D");

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }

            ProfilerTestRunner.ProcessLaunched += AttachProfiler;
            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ReverseStartup",
                                          profilerClsid: Guid.Empty,
                                          profileeOptions: ProfileeOptions.NoStartupAttach);
        }

        public static void AttachProfiler(Process childProcess)
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

            Console.WriteLine($"Setting profiler {profilerPath} as startup profiler via diagnostics IPC.");
            ProfilerControlHelpers.SetStartupProfilerViaIPC(ReleaseOnShutdownGuid, profilerPath, childProcess.Id);
            
            return 100;
        }
    }
}
