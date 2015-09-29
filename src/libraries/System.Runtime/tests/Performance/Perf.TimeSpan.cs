// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_TimeSpan
    {
        [Benchmark]
        public void ctor_int_int_int()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        new TimeSpan(7, 8, 10); new TimeSpan(7, 8, 10); new TimeSpan(7, 8, 10);
                        new TimeSpan(7, 8, 10); new TimeSpan(7, 8, 10); new TimeSpan(7, 8, 10);
                        new TimeSpan(7, 8, 10); new TimeSpan(7, 8, 10); new TimeSpan(7, 8, 10);
                    }
        }

        [Benchmark]
        public void FromSeconds()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        TimeSpan.FromSeconds(50); TimeSpan.FromSeconds(50); TimeSpan.FromSeconds(50);
                        TimeSpan.FromSeconds(50); TimeSpan.FromSeconds(50); TimeSpan.FromSeconds(50);
                        TimeSpan.FromSeconds(50); TimeSpan.FromSeconds(50); TimeSpan.FromSeconds(50);
                    }
        }
    }
}
