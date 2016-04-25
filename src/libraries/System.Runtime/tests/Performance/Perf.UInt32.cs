// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_UInt32
    {
        [Benchmark]
        public void ToString_()
        {
            uint testint = new uint();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        testint.ToString(); testint.ToString(); testint.ToString();
                        testint.ToString(); testint.ToString(); testint.ToString();
                        testint.ToString(); testint.ToString(); testint.ToString();
                    }
        }
    }
}
