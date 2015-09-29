// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_Enum
    {
        private enum testEnum
        {
            Red = 1,
            Blue = 2
        }

        [Benchmark]
        public void Parse()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        Enum.Parse(typeof(testEnum), "Red"); Enum.Parse(typeof(testEnum), "Red"); Enum.Parse(typeof(testEnum), "Red");
                        Enum.Parse(typeof(testEnum), "Red"); Enum.Parse(typeof(testEnum), "Red"); Enum.Parse(typeof(testEnum), "Red");
                        Enum.Parse(typeof(testEnum), "Red"); Enum.Parse(typeof(testEnum), "Red"); Enum.Parse(typeof(testEnum), "Red");
                    }
        }
    }
}
