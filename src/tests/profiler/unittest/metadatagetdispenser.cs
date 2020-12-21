// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Profiler.Tests
{
    class MetadataGetDispenser //: ProfilerTest
    {
        static readonly Guid MetaDataGetDispenserProfilerGuid = new Guid("7198FF3E-50E8-4AD1-9B89-CB15A1D6E740");

        public static int RunTest(string[] args)
        {
            // The profiler will do all the work, this is just a dummy app to let it run.
            // We're trying to verify that MetaDataGetDispenser is still available to profilers
            // since some of them rely on it.
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "UnitTestMetadataGetDispenser",
                                          profilerClsid: MetaDataGetDispenserProfilerGuid);
        }
    }
}
