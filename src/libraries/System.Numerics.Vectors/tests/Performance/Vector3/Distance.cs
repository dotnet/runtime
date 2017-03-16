// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Numerics.Tests
{
    public static partial class Perf_Vector3
    {
        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceBenchmark()
        {
            const float expectedResult = 3.46410155f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DistanceTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DistanceTest()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector3.Distance(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceJitOptimizeCanaryBenchmark()
        {
            const float expectedResult = 67108864.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DistanceJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DistanceJitOptimizeCanaryTest()
        {
            var result = 0.0f;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += Vector3.Distance(value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }
    }
}
