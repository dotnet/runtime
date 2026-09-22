// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Profiler.Tests
{
    class GCNotificationProfilerTeardown
    {
        private static readonly Guid GCProfilerGuid = new Guid("BCD8186F-1EEC-47E9-AFA7-396F879382C3");
        private static readonly Guid MultipleProfilerGuid = new Guid("BFA8EF13-E144-49B9-B95C-FC1C150C7651");
        private const byte FailProfilerInitialization = 1;

        [DllImport("Profiler")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsNotificationProfilerWaitingForAllocationCallback();

        public static int RunTest()
        {
            Task attachTask = Task.Run(() =>
            {
                ProfilerControlHelpers.AttachProfilerToSelfExpectFailure(
                    MultipleProfilerGuid,
                    ProfilerTestRunner.GetProfilerPath(),
                    new byte[] { FailProfilerInitialization });
            });

            if (!SpinWait.SpinUntil(IsNotificationProfilerWaitingForAllocationCallback, TimeSpan.FromSeconds(30)))
            {
                throw new Exception("Timed out waiting for notification profiler initialization.");
            }

            object[] objects = new object[100_000];
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = new object();
            }

            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            attachTask.GetAwaiter().GetResult();
            GC.KeepAlive(objects);
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest();
            }

            return ProfilerTestRunner.Run(
                profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                testName: nameof(GCNotificationProfilerTeardown),
                profilerClsid: GCProfilerGuid);
        }
    }
}
