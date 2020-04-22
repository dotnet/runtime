// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.IO;
using System.Runtime.Loader;

namespace Profiler.Tests
{
    class Program
    {
        static readonly Guid GetRuntimeInformationProfilerGuid = new Guid("4CF56B6D-F8FB-4056-AF4A-F6413DD738B1");

        static int Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest();
            }

            Console.WriteLine("Launching...");
            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "UnitTestGetRuntimeInformation",
                                          additionalEnvVars: new Dictionary<string, string> { ["FX_PRODUCT_VERSION"] = "5.1.306"},
                                          profilerClsid: GetRuntimeInformationProfilerGuid);
        }

        static int RunTest()
        {
            Console.WriteLine($"Version={Environment.Version}");

            return 100;
        }
    }
}
