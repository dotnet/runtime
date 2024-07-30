// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace BasicEventSourceTests
{
    public partial class FuzzyTests
    {
        static partial void Test_Write_Fuzzy_TestEtw(List<SubTest> tests, EventSource logger)
        {
            if (PlatformDetection.IsPrivilegedProcess)
            {
                using (var listener = new EtwListener())
                {
                    EventTestHarness.RunTests(tests, listener, logger);
                }
            }
        }
    }
}
