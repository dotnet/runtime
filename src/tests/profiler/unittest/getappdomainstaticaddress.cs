// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        static readonly Guid GetAppDomainStaticAddressProfilerGuid = new Guid("604D76F0-2AF2-48E0-B196-80C972F6AFB7");

        static int Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest();
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "UnitTestGetAppDomainStaticAddress",
                                          profilerClsid: GetAppDomainStaticAddressProfilerGuid);
        }

        static int RunTest()
        {
            LoadCollectibleAssembly();

            Thread.Sleep(TimeSpan.FromSeconds(3));

            return 100;
        }

        private static void LoadCollectibleAssembly()
        {
            var collectibleContext = new AssemblyLoadContext("Collectible", true);

            var asmDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var dynamicLibrary = collectibleContext.LoadFromAssemblyPath(Path.Combine(asmDir, "unloadlibrary.dll"));
            var testType = dynamicLibrary.GetType("UnloadLibrary.TestClass");

            object instance = Activator.CreateInstance(testType);

            Console.WriteLine(instance.GetHashCode());
            GC.Collect();
            collectibleContext.Unload();
        }
    }
}
