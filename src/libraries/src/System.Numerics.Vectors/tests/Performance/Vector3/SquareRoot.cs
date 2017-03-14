// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Numerics.Tests
{
    public static partial class Perf_Vector3
    {
        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SquareRootBenchmark()
        {
            var expectedResult = new Vector3(float.NaN, 1.0f, float.NaN);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SquareRootTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 SquareRootTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector3.SquareRoot(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SquareRootJitOptimizeCanaryBenchmark()
        {
            var expectedResult = new Vector3(float.NaN, 2.81474977e+14f, float.NaN);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SquareRootJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 SquareRootJitOptimizeCanaryTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += Vector3.SquareRoot(result);
            }

            return result;
        }
    }
}
