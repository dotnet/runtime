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
using System.Runtime.InteropServices;
using System.Threading;

namespace SlowPathELTTests
{
    class SlowPathELTEnter
    {
        static readonly Guid EventPipeWritingProfilerGuid = new Guid("0B36296B-EC47-44DA-8320-DC5E3071DD06");

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return SlowPathELTHelpers.RunTest();
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ELTSlowPathEnter",
                                          profilerClsid: EventPipeWritingProfilerGuid);
        }
    }
}
