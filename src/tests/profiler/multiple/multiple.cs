// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Profiler.Tests
{
    class MultiplyLoaded
    {
        static readonly Guid MultipleProfilerGuid = new Guid("BFA8EF13-E144-49B9-B95C-FC1C150C7651");
        static readonly string ProfilerPath = ProfilerTestRunner.GetProfilerPath();

        [DllImport("Profiler")]
        private static extern void PassCallbackToProfiler(ProfilerCallback callback);

        public static int RunTest(String[] args)
        {
            ManualResetEvent _profilerDone = new ManualResetEvent(false);
            PassCallbackToProfiler(() => _profilerDone.Set());

            ProfilerControlHelpers.AttachProfilerToSelf(MultipleProfilerGuid, ProfilerPath);

            try
            {
                Console.WriteLine("Throwing exception");
                throw new Exception("Test exception!");
            }
            catch
            {
                // intentionally swallow the exception
                Console.WriteLine("Exception caught");
            }

            Console.WriteLine("Waiting for profilers to all detach");
            if (!_profilerDone.WaitOne(TimeSpan.FromMinutes(10)))
            {
                throw new Exception("Test timed out waiting for the profilers to set the callback, test will fail.");
            }

            return 100;
        }

        public static int Main(string[] args)
        {
            // failing on MacOs 12 https://github.com/dotnet/runtime/issues/64765
            if (OperatingSystem.IsMacOS())
            {
                return 100;
            }
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "MultiplyLoaded",
                                          profilerClsid: MultipleProfilerGuid,
                                          loadAsNotification: true,
                                          notificationCopies: 2);
        }
    }
}
