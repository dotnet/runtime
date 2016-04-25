// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Tests
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
