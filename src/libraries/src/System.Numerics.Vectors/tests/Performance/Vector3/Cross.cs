// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Numerics.Tests
{
    public static partial class Perf_Vector3
    {
        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void CrossBenchmark()
        {
            var expectedResult = Vector3.Zero;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 result;

                using (iteration.StartMeasurement())
                {
                    result = CrossTest();
                }

                VectorTests.AssertEqual(expectedResult, result);
            }
        }

        public static Vector3 CrossTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector3.Cross(result, VectorTests.Vector3ValueInverted);
            }

            return result;
        }
    }
}
