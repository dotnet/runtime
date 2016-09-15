// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Numerics.Performance.Tests
{
    public static class HashCodeTest
    {
        [Benchmark(InnerIterationCount = 100000000)]
        public static void HashTest()
        {
            Random rand = new Random(84329);
            Vector4 vector4 = new Vector4(
                Convert.ToSingle(rand.NextDouble()),
                Convert.ToSingle(rand.NextDouble()),
                Convert.ToSingle(rand.NextDouble()),
                Convert.ToSingle(rand.NextDouble()));

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        vector4.GetHashCode();
                    }
                }
            }
        }
    }
}
