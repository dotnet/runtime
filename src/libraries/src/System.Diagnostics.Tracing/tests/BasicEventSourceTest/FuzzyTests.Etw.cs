// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

namespace BasicEventSourceTests
{
    public partial class FuzzyTests
    {
        static partial void Test_Write_Fuzzy_TestEtw(List<SubTest> tests, EventSource logger)
        {
            if (TestUtilities.IsProcessElevated)
            {
                using (var listener = new EtwListener())
                {
                    EventTestHarness.RunTests(tests, listener, logger);
                }
            }
        }
    }
}
