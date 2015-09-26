// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.Xunit.Performance;

namespace System.Threading.Tests
{
    public class Perf_EventWaitHandle
    {
        [Benchmark]
        public void Set_Reset()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset))
                using (iteration.StartMeasurement())
                {
                    are.Set(); are.Reset(); are.Set(); are.Reset();
                    are.Set(); are.Reset(); are.Set(); are.Reset();
                    are.Set(); are.Reset(); are.Set(); are.Reset();
                    are.Set(); are.Reset(); are.Set(); are.Reset();
                }
            }
        }
    }
}
