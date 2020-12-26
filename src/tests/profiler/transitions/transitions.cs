// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Profiler.Tests
{
    class GCTests
    {
        static readonly Guid TransitionsGuid = new Guid("027AD7BB-578E-4921-B29F-B540363D83EC");

        [DllImport("Profiler")]
        public static extern void DoPInvoke(int i);

        public static int RunTest(String[] args) 
        {
            DoPInvoke(13);

            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "Transitions",
                                          profilerClsid: TransitionsGuid);
        }
    }
}
