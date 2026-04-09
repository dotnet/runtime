// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Profiler.Tests
{
    class EnumThreadsTests
    {
        static readonly Guid EnumThreadsProfilerGuid = new Guid("0742962D-2ED3-44B0-BA84-06B1EF0A0A0B");

        public static int EnumerateThreadsWithNonProfilerRequestedRuntimeSuspension()
        {
            GC.Collect();
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                switch (args[1])
                {
                    case nameof(EnumerateThreadsWithNonProfilerRequestedRuntimeSuspension):
                        return EnumerateThreadsWithNonProfilerRequestedRuntimeSuspension();
                    default:
                        return 102;
                }
            }

            if (!RunProfilerTest(nameof(EnumerateThreadsWithNonProfilerRequestedRuntimeSuspension)))
            {
                return 101;
            }

            return 100;
        }

        private static bool RunProfilerTest(string testName)
        {
            try
            {
                return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                              testName: "EnumThreads",
                                              profilerClsid: EnumThreadsProfilerGuid,
                                              profileeArguments: testName
                                              ) == 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }
    }
}
