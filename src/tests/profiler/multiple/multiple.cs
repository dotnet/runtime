// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Profiler.Tests
{
    class MultiplyLoaded
    {
        static readonly Guid MultipleProfilerGuid = new Guid("BFA8EF13-E144-49B9-B95C-FC1C150C7651");
        static readonly string ProfilerPath = ProfilerTestRunner.GetProfilerPath();

        public static int RunTest(String[] args) 
        {
            for (int i = 0; i < 16; ++i)
            {
                ProfilerControlHelpers.AttachProfilerToSelf(MultipleProfilerGuid, ProfilerPath);
            }

            try
            {
                throw new Exception("Test exception!");
            }
            catch
            {
                // intentionally swallow the exception
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
                                          testName: "MultiplyLoaded",
                                          profilerClsid: MultipleProfilerGuid,
                                          loadAsNotification: true,
                                          notificationCopies: 16);
        }
    }
}
