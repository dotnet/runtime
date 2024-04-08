// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

namespace Profiler.Tests
{
    class ALCTest
    {
        static readonly Guid AssemblyProfilerGuid = new Guid("19A49007-9E58-4E31-B655-83EC3B924E7B");

        public static int RunTest(String[] args)
        {
            string currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testAssemblyFullPath = Path.Combine(currentAssemblyDirectory, "..", "TestFile", "TestFile.dll");
            
            int exitCode = TestLibrary.Utilities.ExecuteAndUnload(testAssemblyFullPath, args);
            return exitCode;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ALCTest",
                                          profilerClsid: AssemblyProfilerGuid);
        }
    }
}
