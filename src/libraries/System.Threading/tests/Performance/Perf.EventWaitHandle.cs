// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
