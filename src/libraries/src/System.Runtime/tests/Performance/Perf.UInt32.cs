// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
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
