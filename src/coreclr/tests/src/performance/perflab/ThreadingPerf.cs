// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Threading;

namespace PerfLabTests
{
    public class JITIntrinsics
    {
        private static int s_i;
        private static string s_s;

        [Benchmark(InnerIterationCount = 100000)]
        public static void CompareExchangeIntNoMatch()
        {
            s_i = 0;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Interlocked.CompareExchange(ref s_i, 5, -1);
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void CompareExchangeIntMatch()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                s_i = 1;
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Interlocked.CompareExchange(ref s_i, 5, 1);
            }
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void CompareExchangeObjNoMatch()
        {
            s_s = "Hello";
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Interlocked.CompareExchange(ref s_s, "World", "What?");
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void CompareExchangeObjMatch()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                s_s = "What?";
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Interlocked.CompareExchange(ref s_s, "World", "What?");
            }
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void InterlockedIncrement()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Interlocked.Increment(ref s_i);
        }

        [Benchmark(InnerIterationCount = 100000)]
        public static void InterlockedDecrement()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Interlocked.Decrement(ref s_i);
        }
    }
}