// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Numerics.Tests
{
    public static partial class Perf_Vector3
    {
        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void NormalizeBenchmark()
        {
            var expectedResult = new Vector3(-0.577350318f, 0.577350318f, -0.577350318f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = NormalizeTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 NormalizeTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector3.Normalize(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void NormalizeJitOptimizeCanaryBenchmark()
        {
            var expectedResult = new Vector3(-16777216.0f, 16777216.0f, -16777216.0f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = NormalizeJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 NormalizeJitOptimizeCanaryTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += Vector3.Normalize(result);
            }

            return result;
        }
    }
}
